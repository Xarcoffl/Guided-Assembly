# SnapFlow Model Origin Constraint

## 📐 Origin Standard Requirement

All 3D models used in SnapFlow assembly/disassembly workflows must have:

- **Position**: (0, 0, 0) - No local offset
- **Rotation**: (0, 0, 0) - No local rotation
- **Scale**: (1, 1, 1) - Uniform, unscaled
- **Parent**: At origin or no parent with offset

This ensures:
✅ Consistent behavior across all models  
✅ Accurate physics and collision detection  
✅ Proper snap zone alignment  
✅ Predictable highlight/material application  
✅ Reliable auto-reset positioning  

---

## 🔍 Validation Tools

### Quick Validation Commands

Access validation tools via the **SnapFlow menu** in the top menu bar:

```
SnapFlow/Validation/
├─ Validate All Model Origins
├─ Auto-Fix All Model Origins
├─ Validate Selected Model
├─ Fix Selected Model
└─ Open Origin Validator Window
```

### Method 1: Validate All Models

```
SnapFlow → Validation → Validate All Model Origins
```

**What it does:**
- Scans all AssemblyStepManager steps
- Scans all DisassemblyManager steps
- Checks each model's transform properties
- Reports any issues found

**Output:**
- Dialog showing total items checked and issues found
- Console logs showing specific problem models

### Method 2: Auto-Fix All Models

```
SnapFlow → Validation → Auto-Fix All Model Origins
```

**What it does:**
- Automatically resets all problematic models to origin standard
- Records undo for safety
- Confirms count of fixed models

**Before:**
```
Position: (2.5, -1.3, 0.8)
Rotation: (45, 90, 22)
Scale: (0.9, 1.1, 1.0)
```

**After:**
```
Position: (0, 0, 0)
Rotation: (0, 0, 0)
Scale: (1, 1, 1)
```

### Method 3: Validate Single Model

```
1. Select model in hierarchy
2. SnapFlow → Validation → Validate Selected Model
```

Shows if the selected model meets origin requirements.

### Method 4: Fix Single Model

```
1. Select model in hierarchy
2. SnapFlow → Validation → Fix Selected Model
```

Resets the selected model to origin standard.

### Method 5: Interactive Validator Window

```
SnapFlow → Validation → Open Origin Validator Window
```

**Features:**
- Detailed list of all issues found
- Individual select/fix buttons per model
- Auto-refresh option
- Side-by-side validation and repair

---

## 🎯 Why This Matters

### 1. Physics Accuracy

```
WITH origin at (0,0,0):
- FixedJoint connects correctly
- Colliders align with physics engine
- Rigidbody center of mass is predictable

WITHOUT origin (example: centered at 2,2,2):
- Joint offset causes unexpected physics
- Colliders may not align
- Grab points feel off
```

### 2. Material/Highlight Consistency

```
Highlight system applies materials to renderers:
- Material swap works consistently
- All child renderers get highlighted
- Unhighlight restoration works properly

With offset origins:
- Highlight logic may miss parts
- Scale/rotation affects visual feedback
- Material caching becomes unreliable
```

### 3. Auto-Reset Functionality

```
Auto-reset caches initial transforms during Start():
- Stores position as (0, 0, 0)
- Stores rotation as (0, 0, 0)
- Restores to exact state when dropped

With offset origins:
- Reset positions are wrong
- Objects don't return to intended location
- User confusion and workflow breaks
```

### 4. Snap Zone Alignment

```
Snap zones use world position/rotation:
- Zone at origin aligns with model at origin
- Snapping is reliable and predictable

With offset origins:
- Zone and model don't align properly
- Snapping may fail or feel janky
- User needs to compensate manually
```

---

## ✅ Pre-Import Checklist

When preparing 3D models for SnapFlow:

- [ ] Model exists as separate mesh file (.fbx, .obj, etc.)
- [ ] Pivot/origin is at (0, 0, 0) in modeling software
- [ ] Model is not scaled or rotated in its file
- [ ] Parent objects are at origin (if grouped)
- [ ] No hidden offset transformations
- [ ] Local and world coordinates match

### Blender Preparation

```
1. Select object in object mode
2. Object → Set Origin → Origin to Geometry (or Cursor)
   (Position cursor at 0,0,0 first)
3. Object → Clear → Clear Scale
4. Object → Clear → Clear Rotation
5. Ctrl+A → Apply All Transforms
6. Export as .fbx with "Apply Scalings" = FBX All
```

### Maya Preparation

```
1. Select object
2. Modify → Freeze Transformations
3. Maya Attribute Editor: Set Translate X/Y/Z = 0
4. Maya Attribute Editor: Set Rotate X/Y/Z = 0
5. Maya Attribute Editor: Set Scale X/Y/Z = 1
6. File → Export Selection
```

### 3ds Max Preparation

```
1. Select object
2. Hierarchy → Affect Pivot Only (enable)
3. Hierarchy → Center to Object
4. Hierarchy → Affect Pivot Only (disable)
5. Hold Shift + Move/Rotate to reset transforms
6. Export as .fbx
```

---

## 🛠️ Fixing Existing Models

### Scenario 1: Model Imported with Offset

**Problem:** Model at (2, 1, -0.5) with rotation (45, 0, 0)

**Solution:**
```
1. Select model in Hierarchy
2. SnapFlow → Validation → Fix Selected Model
```

### Scenario 2: Grouped Models with Parent Offset

**Problem:**
```
Parent (offset: 1, 1, 0)
└─ Model (offset: 0, 0, 0)
```

**Solution:**
```
1. Select parent
2. SnapFlow → Validation → Fix Selected Model
3. Repeat for each child
4. Move model content if needed using Cut/Paste
```

### Scenario 3: Model in Prefab

**Problem:** Model in prefab has wrong origin

**Solution:**
```
1. Edit prefab in place (double-click prefab)
2. Fix model transforms
3. Save prefab (Ctrl+S)
4. All instances updated automatically
```

---

## 📋 Validation Report Example

```
✅ Validation Complete!
Items Checked: 24
Issues Found: 3

[SnapFlow] Bolt_01: Position is not (0,0,0). Current: (0.5, 0.2, -0.1)
[SnapFlow] Gear_Assembly: Rotation is not (0,0,0). Current: (0, 45, 0)
[SnapFlow] Piston_Rod: Scale is not (1,1,1). Current: (0.9, 1.0, 1.0)

→ Click "Auto-Fix All Model Origins" to correct these
```

---

## 🔧 API Reference

### SnapFlowModelOriginValidator

```csharp
// Static methods - call directly
public static void ValidateAllModels()
    // Scans all managers and reports issues

public static void AutoFixAllModels()
    // Fixes all problematic models with confirmation

public static void ValidateSelectedModel()
    // Validates currently selected GameObject

public static void FixSelectedModel()
    // Fixes currently selected GameObject
```

### Usage Example

```csharp
// In editor script
SnapFlow.Editor.SnapFlowModelOriginValidator.ValidateAllModels();
```

---

## ⚠️ Important Notes

### Before Auto-Fix

✅ **Safe:**
- Creates undo entries for all changes
- Works on scene and prefab instances
- Can be undone with Ctrl+Z

⚠️ **Be Aware:**
- Changes positions/rotations/scales
- May affect animation keyframes if any
- Snap zones need re-verification after fixing

### If Model Position Matters

If a model needs to be offset from its snap zone:

❌ **Wrong:**
```
Model at (2, 2, 2)  ← Don't do this
SnapZone at (0, 0, 0)
```

✅ **Right:**
```
Model at (0, 0, 0)
SnapZone at (2, 2, 2)  ← Offset the snap zone instead
```

### Performance Impact

Validation is O(n) where n = total objects in all steps:
- ~100 models: <10ms
- ~1000 models: <100ms
- Generally negligible

---

## 🚀 Best Practices

### During Model Creation

- Always start with origin at (0, 0, 0)
- Don't scale or rotate at model level
- Keep transforms clean from the start
- Document any special handling needed

### During Scene Setup

1. Import model
2. Immediately validate: `SnapFlow → Validation → Validate Selected Model`
3. If issues found: `Fix Selected Model`
4. Assign to manager step
5. Test snapping before proceeding

### During QA/Testing

- Run full validation before release: `Validate All Model Origins`
- Check edge cases with complex models
- Verify auto-reset positions are correct
- Test snap alignment with validator active

---

## 📊 Troubleshooting

### "Validation keeps finding issues after I fixed them"

**Cause:** Model component was fixed but parent wasn't

**Solution:**
```
1. Select model
2. While holding Shift, select parent
3. Fix parent with SnapFlow validator
4. Try validation again
```

### "Auto-fix changed positions unexpectedly"

**Cause:** Model offset was intentional offset now reset

**Solution:**
```
1. Ctrl+Z to undo
2. Move snap zone to correct position instead
3. Validate model again (should be clean)
```

### "Models keep getting wrong origin on import"

**Cause:** Import settings or export settings in modeling software

**Solution:**
```
1. In modeling software: Confirm model origin at (0,0,0)
2. In Unity: Check FBX import settings
   - Model → Location: (0, 0, 0)
   - Model → Rotation: (0, 0, 0)
   - Don't apply scale/rotation in import
3. Run validator to confirm
```

---

## 📝 Validation Report Template

Create a validation report before release:

```
# SnapFlow Model Origin Validation Report

**Date:** 2026-06-10
**Project:** Guided-Assembly
**Models Checked:** 24
**Issues Found:** 0
**Status:** ✅ PASSED

## Details

- AssemblyStepManager: 12 models validated
- DisassemblyManager: 12 models validated
- All models meet origin standard
- Ready for VR deployment

## Issues (if any)

[List any issues found and their resolution]

## Sign-Off

Validated by: [Name]
Ready for deployment: ✅ Yes / ❌ No
```

---

**Last Updated:** 2026-06-10  
**Version:** 1.0  
**Tool Location:** Assets/SnapFlow/Editor/SnapFlowOriginValidator.cs
