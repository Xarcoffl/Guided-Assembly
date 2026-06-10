# 🏗️ SnapFlow - VR Assembly/Disassembly Framework

A comprehensive Unity VR framework for building interactive assembly and disassembly training simulators using the Berkeley Haptics (BNG) framework.

---

## 📋 Table of Contents

- [Overview](#overview)
- [System Architecture](#system-architecture)
- [Core Components](#core-components)
- [Step Validation Logic](#step-validation-logic)
- [Material Caching System](#material-caching-system)
- [BNG Framework Integration](#bng-framework-integration)
- [Editor Setup](#editor-setup)
- [State Diagrams](#state-diagrams)
- [User Journey](#user-journey)
- [Features](#features)
- [Quick Start](#quick-start)

---

## 🎯 Overview

SnapFlow is a VR training framework designed for the Guided-Assembly project that enables users to:

- **Assemble** objects step-by-step by snapping components together
- **Disassemble** objects in reverse by detaching components
- Receive **real-time feedback** via audio, haptics, and visual highlights
- Learn through **validated interaction** with error correction

### Key Benefits

✅ **Step-based progression** - Clear, guided workflow  
✅ **Strict mode enforcement** - Prevents mistakes before they happen  
✅ **Auto-reset mechanism** - Automatically returns dropped objects  
✅ **Material safety** - No global contamination  
✅ **Event-driven** - Extensible callback system  
✅ **VR-optimized** - Haptic and audio feedback  

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     VR User Input                           │
│              (Grip Button in Hand Controller)               │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    BNG Framework                            │
│  (Grabbable Component + SnapZone Physics Integration)       │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│         AssemblyStepManager or DisassemblyManager           │
│            (Step-based Workflow Management)                 │
└─────────────────────────────────────────────────────────────┘
                              ↓
                    ┌─────────┴─────────┐
                    ↓                   ↓
        ┌──────────────────┐  ┌──────────────────┐
        │ Correct Object?  │  │ Wrong Object?    │
        └──────────────────┘  └──────────────────┘
                    ↓                   ↓
        ┌──────────────────┐  ┌──────────────────┐
        │ ✅ Advance Step  │  │ ❌ Error Feedback │
        │ Fire Events      │  │ Auto-Reset       │
        │ Update Progress  │  │ Play Error Sound │
        └──────────────────┘  └──────────────────┘
                    ↓
        ┌──────────────────────────┐
        │ All Steps Complete?      │
        │ YES → Trigger Completion │
        │ NO → Setup Next Step     │
        └──────────────────────────┘
```

---

## 🔧 Core Components

### 1. **AssemblyStepManager** (Forward Workflow)

Forward sequential progression: Step 0 → 1 → 2 → ... → Complete

#### Initialization
```csharp
Start()
  ├─ CacheInitialTransforms() - Store all object positions/rotations
  ├─ CacheInitialMaterials() - Store all renderer materials
  ├─ InitializeStepLookup() - Create Grabbable→Step mapping
  ├─ SetupStep(0) - Begin first step
  └─ UpdateProgressUI() - Display 1/N steps
```

#### Step Execution
```csharp
SetupStep(stepIndex)
  ├─ Disable all snap zones
  ├─ Enable only current snap zone
  ├─ Disable all object grab physics (GrabPhysics.None)
  ├─ Enable current object (GrabPhysics.FixedJoint)
  ├─ Highlight current object & target zone
  ├─ Register grab event listener → OnObjectGrabbed()
  └─ Register snap event listener → OnObjectSnapped()
```

#### Event Flow
```csharp
OnObjectGrabbed(stepIndex)
  ├─ Unhighlight grabbed object
  ├─ Highlight target snap zone
  └─ Fire custom grab events

OnObjectSnapped(snappedObject, stepIndex)
  ├─ Validate: snappedObject == step.objectToGrab?
  ├─ If WRONG:
  │  ├─ Log warning
  │  ├─ TriggerErrorFeedback(position)
  │  └─ ResetObject() to initial position
  ├─ If CORRECT:
  │  ├─ Set _currentStepSnapped = true
  │  ├─ Lock snap zone (CanRemoveItem = false)
  │  ├─ Unhighlight zone
  │  ├─ Fire onSnapEvents
  │  ├─ PlaySound(stepCompleteSound)
  │  ├─ TriggerHaptic(controller)
  │  ├─ _currentStep++
  │  ├─ UpdateProgressUI()
  │  └─ If more steps: SetupStep(_currentStep)
  │     Else: Fire onAssemblyComplete

OnObjectReleased(stepIndex) [Auto-Reset]
  ├─ Check if !_currentStepSnapped
  ├─ If true: ResetObject(step)
  └─ Restore position/rotation and clear velocities
```

---

### 2. **DisassemblyManager** (Reverse Workflow)

Reverse sequential progression: Step N → N-1 → ... → 0 → Complete

#### Initialization
```csharp
Start()
  ├─ CacheInitialMaterials() - Store zone renderers
  ├─ _currentStep = steps.Count - 1 (start at end)
  ├─ Initialize CompletedSteps HashSet (empty)
  ├─ SetupStep(_currentStep)
  └─ UpdateProgressUI() - Display N/N steps
```

#### Step Execution
```csharp
SetupStep(stepIndex)
  ├─ Return if invalid index
  ├─ Get current step from list
  ├─ Enable only current snap zone for removal
  ├─ HighlightSnapZone(zone, true)
  ├─ Register detach event listener → OnObjectUnsnapped()
  └─ Set step description

OnObjectUnsnapped(detachedObject, stepIndex)
  ├─ Validate: detachedObject == step.objectToRemove?
  ├─ If WRONG:
  │  ├─ Log warning
  │  └─ TriggerErrorFeedback(position)
  ├─ If already completed: Return (duplicate prevention)
  ├─ If CORRECT:
  │  ├─ Mark step completed: CompletedSteps.Add(stepIndex)
  │  ├─ Fire onUnSnapEvents
  │  ├─ PlaySound(stepCompleteSound)
  │  ├─ _currentStep--
  │  ├─ UpdateProgressUI() - Display countdown
  │  └─ If _currentStep >= 0: SetupStep(_currentStep)
  │     Else: Fire onDisassemblyComplete

HighlightSnapZone(zone, highlight)
  ├─ Get all MeshRenderers in zone children
  ├─ If highlight:
  │  └─ Apply highlightMaterial to each
  └─ If unhighlight:
     └─ Restore cached materials from dictionary
```

---

## 🎯 Step Validation Logic

### Assembly Validation Flow

```
User grabs object in VR
  ↓
OnObjectGrabbed event fires
  ├─ Highlight target snap zone
  └─ Fire grab event callbacks
  ↓
User positions object and releases at snap zone
  ↓
Snap zone detects object (OnSnapEvent)
  ↓
OnObjectSnapped validation:
  ├─ snappedObject == step.objectToGrab?
  │
  ├─ YES ✓
  │  ├─ Advance step counter
  │  ├─ Fire snap event callbacks
  │  ├─ Play success audio
  │  ├─ Trigger haptic feedback
  │  └─ Setup next step or complete
  │
  └─ NO ✗
     ├─ Trigger error audio
     ├─ Trigger haptic error pulse
     ├─ Reset object to initial position
     └─ User must try again
```

### Disassembly Validation Flow

```
User grabs attached object
  ↓
SnapZone detects removal (OnDetachEvent)
  ↓
OnObjectUnsnapped validation:
  ├─ detachedObject == step.objectToRemove?
  │
  ├─ YES ✓
  │  ├─ Mark step as completed
  │  ├─ Count down step counter
  │  ├─ Fire event callbacks
  │  ├─ Play success audio
  │  └─ Setup previous step or complete
  │
  └─ NO ✗
     ├─ Trigger error audio
     ├─ Trigger haptic error pulse
     └─ Object remains attached, user must try again
```

---

## 💾 Material Caching System

Both managers implement safe material handling to prevent global contamination:

### Caching Process

```csharp
CacheInitialMaterials()
  ├─ Loop through all steps
  ├─ For each object/zone:
  │  ├─ Get all MeshRenderers (including children)
  │  ├─ For each renderer:
  │  │  └─ Store renderer → material[] in Dictionary
  │  └─ Never modify sharedMaterial
  └─ Create dictionary entry per unique renderer
```

### Highlight Process

```csharp
Highlight (on grab or as target):
  ├─ Get affected renderers
  └─ Swap to highlightMaterial
     └─ Material property, not overwriting originals

Unhighlight (when step completes):
  ├─ Get affected renderers
  └─ Restore from cached materials
     ├─ Use Clone() to prevent reference sharing
     └─ Material restored to exact original state
```

### Data Structure

```csharp
Dictionary<MeshRenderer, Material[]> CachedMaterials
  ├─ Key: MeshRenderer component instance
  ├─ Value: Clone of original material array
  └─ Updated: During CacheInitialMaterials()
              Referenced: During highlight/unhighlight
              Prevents: Global sharedMaterial contamination
```

---

## 🎮 BNG Framework Integration

### Component Setup

Each grabbable object requires:
```csharp
Grabbable component
  ├─ GrabPhysics: FixedJoint (for current step) or None (for others)
  ├─ GrabButton: Grip (default)
  ├─ Grabtype: HoldDown
  ├─ GrabMechanic: Snap
  └─ OnGrab event (fires when grabbed)

SnapZone component
  ├─ Trigger collider (BoxCollider, isTrigger=true)
  ├─ OnSnapEvent: Fires when object enters
  ├─ OnDetachEvent: Fires when object leaves
  └─ CanRemoveItem: Locked during assembly steps

GrabbableUnityEvents component
  └─ onGrab: Relays grab event to SnapFlow managers
```

### Physics Modes

```
During Assembly:
├─ Current object: GrabPhysics.FixedJoint
│                  └─ Can be grabbed and snapped
├─ Other objects: GrabPhysics.None
│                 └─ Cannot be grabbed (strict mode)
└─ Snap zones: Physics constraints when snapped

During Disassembly:
├─ Objects: Remain snapped via FixedJoint
├─ User removes by grabbing + pulling away
└─ OnDetachEvent fires when joint breaks
```

### Event Chain

```
1. User presses grip button on controller
   ↓
2. Grabbable.OnGrab fires (BNG internal)
   ↓
3. GrabbableUnityEvents.onGrab fires
   ↓
4. SnapFlow.OnObjectGrabbed(stepIndex)
   └─ Highlight zone, fire events, etc.
   ↓
5. User releases at snap zone
   ↓
6. Physics FixedJoint connects object
   ↓
7. SnapZone.OnSnapEvent fires
   ↓
8. SnapFlow.OnObjectSnapped(object, stepIndex)
   └─ Validate, advance step, etc.
```

---

## 🔧 Editor Setup

### Hierarchy Context Menu

Right-click any object in hierarchy:

```
SnapFlow/
├─ Snap Flow Objects/
│  ├─ Grabbable/
│  │  ├─ Add Grab
│  │  └─ Remove Grab
│  └─ Snapzone/
│     ├─ Add Zone
│     └─ Remove Zone
└─ Assign Snap Flow → Opens setup panel
```

### Setup Panel

Select target object → Choose Assembly or Disassembly manager → Select step → Link

This automatically:
- Finds or creates required components
- Assigns Grabbable/SnapZone to manager step
- Configures physics and constraints

---

## 📊 State Diagrams

### Assembly Step Progression

```
┌─────────────────────────────────────────────────────────┐
│                    START                                │
│            Initialize All Caches                        │
│         Setup Step 0, Update Progress                   │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│               WAITING FOR GRAB                          │
│      Current object highlighted, ready                  │
│    User can grab object with hand controller            │
└─────────────────────────────────────────────────────────┘
                        ↓
        ┌───────────────┴───────────────┐
        ↓                               ↓
   ┌─────────────┐          ┌──────────────────┐
   │  GRAB EVENT │          │  WRONG OBJECT    │
   │   FIRED     │          │  (auto-reset)    │
   └─────────────┘          └──────────────────┘
        ↓                               ↓
   ┌────────────────┐         ┌────────────────┐
   │ Highlight Zone │         │ Error Feedback │
   │ Remove Highlight         │ Play Error Snd │
   │ Object highlight         │ Haptic Feedback│
   └────────────────┘         │ Reset Position │
        ↓                      └────────────────┘
   ┌────────────────┐
   │ AWAIT SNAP     │
   │ (at snap zone) │
   └────────────────┘
        ↓
   ┌────────────────┐
   │ SNAP EVENT     │
   │ (validated)    │
   └────────────────┘
        ↓
        ┌──────────────────────┐
        │ Complete Step        │
        ├──────────────────────┤
        │ Fire onSnapEvents    │
        │ Play completion snd  │
        │ Haptic feedback      │
        │ Update Progress UI   │
        └──────────────────────┘
        ↓
    ┌─────────────────────────┐
    │  More Steps?            │
    ├─────────────────────────┤
    │ YES → Setup Next Step   │ (repeat cycle)
    │ NO → Assembly Complete! │
    └─────────────────────────┘
```

### Disassembly Countdown

```
Start at final step (N-1) and count down to 0
  ↓
Similar cycle to assembly, but:
  ├─ Progress counts DOWN (N/N, N-1/N, etc.)
  ├─ User removes objects (detach)
  ├─ Steps tracked in CompletedSteps HashSet
  └─ When _currentStep < 0: Disassembly Complete
```

---

## 🚀 User Journey

### Complete Assembly Workflow

```
1. SCENE LOADS
   └─ AssemblyStepManager.Start()
      ├─ Cache positions, rotations, materials
      ├─ Initialize step lookup
      ├─ Setup Step 0: Highlight Object A
      ├─ Enable only Zone A
      └─ Progress UI: 1/5 Steps

2. USER GRABS OBJECT A
   └─ Hand controller grip trigger
      ├─ Grabbable.OnGrab fires
      ├─ GrabbableUnityEvents.onGrab relays
      ├─ OnObjectGrabbed(0) called
      ├─ Highlight Zone A (target)
      ├─ Fire grab events
      └─ Audio/haptic feedback

3. USER SNAPS OBJECT A TO ZONE A
   └─ User positions and releases
      ├─ Physics FixedJoint connects
      ├─ SnapZone.OnSnapEvent fires
      ├─ OnObjectSnapped(ObjectA, 0) validates
      ├─ ✓ CORRECT - Proceed:
      │  ├─ Fire snap events
      │  ├─ Play success sound
      │  ├─ Haptic success pulse
      │  ├─ Lock zone (CanRemoveItem=false)
      │  ├─ Increment _currentStep → 1
      │  └─ UpdateProgressUI() → 2/5

4. SETUP STEP 1
   └─ Setup next step
      ├─ Disable Zone A
      ├─ Enable Zone B
      ├─ Highlight Object B (new current)
      ├─ Update description text
      └─ Repeat from Step 2

5. REPEAT FOR STEPS 2, 3, 4

6. FINAL STEP COMPLETE
   └─ After Step 4 snaps:
      ├─ _currentStep = 5 (≥ steps.Count)
      ├─ Fire onAssemblyComplete event
      ├─ Play completion sound
      ├─ Show completion UI
      └─ Ready for disassembly or restart
```

### Error Handling Example

```
During Step 1, user grabs WRONG object (ObjectC):
  ↓
OnObjectGrabbed fires (but for wrong object)
  └─ User drags to Zone B (but Zone A is active)
  ↓
OnObjectSnapped(ObjectC, 1) validation:
  ├─ snappedObject (ObjectC) != step.objectToGrab (ObjectB)
  ├─ WRONG OBJECT DETECTED
  ├─ TriggerErrorFeedback(zone_position)
  │  ├─ Play error sound
  │  └─ Haptic error pulse (distinctive pattern)
  ├─ ResetObject(ObjectC)
  │  ├─ Restore position to initial
  │  ├─ Restore rotation to initial
  │  ├─ Clear velocity
  │  └─ Clear angular velocity
  ├─ Do NOT advance step
  ├─ Progress UI stays at 2/5
  └─ User tries again
```

---

## ✨ Features

### Core Features
| Feature | Implementation | Purpose |
|---------|---|---|
| **Strict Mode** | Only current object grabbable (GrabPhysics.None for others) | Prevent wrong object interactions |
| **Auto-Reset** | OnObjectReleased() listener with ResetObject() | Return dropped objects to start position |
| **Error Feedback** | Audio clip + haptic controller vibration | Immediate user notification of mistakes |
| **Material Caching** | Dictionary<Renderer, Material[]> with Clone() | Prevent global sharedMaterial contamination |
| **Event System** | UnityEvents + custom delegates | Extensible callbacks for custom behavior |
| **Progress UI** | Slider + TextMeshPro text | Real-time user feedback |
| **Highlight System** | Renderer material swapping | Visual guidance for current step |
| **Physics Integration** | FixedJoint + SnapZone components | Realistic object snapping |
| **Bidirectional** | AssemblyStepManager + DisassemblyManager | Support both workflows |
| **VR-Optimized** | Controller haptics + positional audio | Immersive feedback |

### Debug Features
```csharp
debuglog: bool          // Enable/disable debug console output
showgizmos: bool        // Render debug visualizations (obsolete)
AssemblyEvents: bool    // Log all event callbacks to console
```

---

## 🚀 Quick Start

### 1. Setup Scene
```
Create GameObject "Snap Flow Manager"
├─ Add AssemblyStepManager component
├─ Add progress UI objects
└─ Assign AudioClips for feedback
```

### 2. Create Assembleable Objects
```
For each object (cube, gear, bolt, etc.):
├─ Right-click in hierarchy
├─ SnapFlow/Snap Flow Objects/Grabbable/Add Grab
└─ Creates all required components
```

### 3. Create Snap Zones
```
For each target snap location:
├─ Right-click in hierarchy
├─ SnapFlow/Snap Flow Objects/Snapzone/Add Zone
└─ Creates collider and snap components
```

### 4. Assign Objects to Manager
```
For each object:
├─ Right-click in hierarchy
├─ SnapFlow/Assign Snap Flow
├─ Select Assembly
├─ Select step number
└─ Assign to manager
```

### 5. Configure Manager
```
In AssemblyStepManager inspector:
├─ Set highlight materials
├─ Assign audio clips
├─ Link progress UI
├─ Enable/disable debug
└─ Customize events if needed
```

### 6. Test in VR
```
Play in VR
├─ Grab objects with hand controller
├─ Snap to zones
├─ Verify feedback audio/haptics
└─ Iterate on step design
```

---

## 📚 File Structure

```
Assets/SnapFlow/
├── Assembly/
│   ├── Core/
│   │   ├── AssemblyStepManager.cs (Main manager)
│   │   ├── Assembly.unity (Example scene)
│   │   └── GameObject.prefab
│   └── Editor/
│       └── AssemblyStepManagerEditor.cs (Custom inspector)
├── Disassembly/
│   ├── Core/
│   │   ├── DisassemblyManager.cs (Main manager)
│   │   └── [Similar structure]
│   └── Editor/
│       └── DisassemblyManagerEditor.cs
├── Tests/
│   ├── AssemblyStepManagerTests.cs (18 tests)
│   ├── DisassemblyManagerTests.cs (19 tests)
│   ├── SnapFlow.Tests.asmdef
│   └── README.md
├── Editor/
│   └── SnapFlowSetupPanel.cs (Hierarchy context menu)
├── Props/
│   └── [Example assembleable objects]
└── README.md (This file)
```

---

## 🧪 Testing

Run unit tests via Window > General > Test Runner:

- **AssemblyStepManagerTests**: 18 tests covering validation, progress, events
- **DisassemblyManagerTests**: 19 tests covering countdown, detach, completion

Tests verify:
- ✅ Correct object snapping advances steps
- ✅ Wrong objects trigger error feedback
- ✅ Null references handled safely
- ✅ Step bounds checked properly
- ✅ Events fire correctly
- ✅ Material caching/restoration works

---

## 🐛 Troubleshooting

### Objects Won't Grab
- Check `GrabPhysics` mode (should be `FixedJoint` for current step)
- Verify `Grabbable` component is attached
- Ensure colliders are present and not marked as trigger

### Wrong Objects Snappable
- Verify only current step's object has `GrabPhysics.FixedJoint`
- Check `SetupStep()` is setting other objects to `GrabPhysics.None`
- Debug: Enable `debuglog` to see step setup in console

### No Sound/Haptics on Error
- Assign `errorSound` AudioClip in manager
- Verify `Primary`/`Secondary` Grabber components are assigned
- Check volume levels and haptic device connection

### Materials Look Wrong
- Verify `CacheInitialMaterials()` ran during Start()
- Check highlight materials are assigned
- Look for exceptions in console during highlight

### Steps Not Advancing
- Enable `debuglog` and check validation messages
- Verify snap zone is at correct step index
- Check `OnSnapEvent` is being received

---

## 🎓 Best Practices

### Design Steps
- 🎯 **Clear progression**: 3-10 steps per assembly
- 📝 **Descriptive names**: "Snap left motor" vs "Step 1"
- 🎨 **Visual feedback**: Use distinct highlight colors
- 🔊 **Audio cues**: Different sounds for success/error

### Component Organization
- 🏗️ **Hierarchy structure**: Group related objects
- 📦 **Prefabs**: Save assembleable objects as reusable components
- 🔗 **References**: Use manager to link steps, not manual references
- ✨ **Cleanliness**: Remove unused debug code before production

### VR Considerations
- 🎮 **Controller reach**: Snap zones within natural arm reach
- ⏱️ **Step timing**: Not too fast, allow user to observe changes
- 📢 **Feedback intensity**: Haptic pulses clear but not uncomfortable
- 🎬 **Visual clarity**: Highlights visible but not distracting

---

## 📝 License

Part of the Snapflow (Multi User License)

---

## 🤝 Support

For issues or questions:
1. Check unit tests for usage examples
2. Enable `debuglog` for console output
3. Review error messages in console
4. Check material/component assignments in inspector

---

**Last Updated**: 2026-06-10  
**Version**: 75.8.4 (Production Ready)  
**Framework**: Unity 2023+ with BNG (VRIF)
