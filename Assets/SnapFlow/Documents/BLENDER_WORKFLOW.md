# SnapFlow Blender Workflow in Unity

Replicates the Blender origin setup workflow directly within Unity Editor.

---

## 🎯 Overview

This tool mirrors the standard Blender workflow for preparing 3D models:

**Blender:**
```
1. Set Origin → Origin to Geometry
2. Object → Apply → Apply Rotation
3. Object → Apply → Apply Scale
```

**Unity (SnapFlow):**
```
1. SnapFlow/Blender Workflow/Set Origin to Geometry
2. SnapFlow/Blender Workflow/Apply Rotation
3. SnapFlow/Blender Workflow/Apply Scale
```

Or use **"Apply All Transforms"** to do all three at once.

---

## 🔧 Menu Commands

Access via the **SnapFlow** menu in top menu bar:

```
SnapFlow/Blender Workflow/
├─ Set Origin to Geometry
├─ Apply Rotation
├─ Apply Scale
└─ Apply All Transforms ⭐ (with automatic prefab save prompt)
```

---

## ⚡ Automated Workflow: Transforms + Prefab Creation

**Apply All Transforms** now automatically handles prefab creation:

**NEW:** ⭐ **Automatic Parent-Child Processing**

When you select a parent object, the tool automatically:
- ✅ Detects all child objects with meshes
- ✅ Processes them in a single operation
- ✅ Shows you how many objects will be processed (parent + children)
- ✅ Applies transforms to all of them

Example workflow with hierarchy:

```
Assembly (parent - no mesh)
├─ Bolt_01 (mesh) ← will be processed
├─ Bolt_02 (mesh) ← will be processed
├─ Gear_Assembly (parent)
│  ├─ Gear_01 (mesh) ← will be processed
│  └─ Gear_02 (mesh) ← will be processed
└─ Washer (mesh) ← will be processed

Select: Assembly
Run: Apply All Transforms
Result: 5 objects processed (all children with meshes)
```

```
Select model(s)
  ↓
SnapFlow → Blender Workflow → Apply All Transforms
  ├─ Detects children automatically
  ├─ Sets origin to geometry
  ├─ Bakes rotation and scale
  ↓
  "Save as Prefab?" prompt appears
  ├─ Yes → File picker shows (choose custom save location)
  └─ No → Skip prefab creation
  ↓
✅ All transforms applied + Prefab saved (optional)
```

### Single Model Example

```
1. Import: engine_bolt.fbx (wrong transforms)
2. Select in Hierarchy
3. SnapFlow → Blender Workflow → Apply All Transforms
4. Confirm: "Apply All Transforms?"
   ✓ Sets origin to geometry
   ✓ Bakes rotation and scale
5. Confirm: "Save as Prefab?"
   - Yes → File picker appears
     • You choose: Assets/MyPrefabs/engine_bolt_Variant.prefab
   - No → Skip (done, transforms applied)
6. ✅ Result:
   - Model transforms applied
   - Prefab saved where YOU chose
   - Ready to use in SnapFlow managers
```

### Batch Processing Example

```
1. Import folder: 5 bolt models (all have wrong transforms)
2. Select all 5 in Hierarchy (Ctrl+A)
3. SnapFlow → Blender Workflow → Apply All Transforms
4. Confirm dialogs:
   - "Apply All Transforms?" → "Yes, Apply All"
   - "Save as Prefab?" → "Yes, Save as Prefab"
5. Folder picker appears: "Select Folder for Prefabs"
   - You choose: Assets/SnapFlow/Prefabs/
   (Or any custom location!)
6. ✅ Result:
   - All 5 transforms applied
   - 5 prefab variants created:
     • Bolt_01_Variant.prefab
     • Bolt_02_Variant.prefab
     • Bolt_03_Variant.prefab
     • Bolt_04_Variant.prefab
     • Bolt_05_Variant.prefab
   - All saved to folder YOU chose
```

### Skip Prefab Creation Example

```
1. Select model (wrong transforms)
2. SnapFlow → Blender Workflow → Apply All Transforms
3. Confirm transforms
4. "Save as Prefab?" → "No, Skip"
5. ✅ Result:
   - Transforms applied
   - NO prefab created
   - Model ready for other use
```

### Parent-Child Hierarchy Processing ⭐ NEW

**Perfect for complex assemblies with multiple parts!**

```
Hierarchy:
Motor (parent - no mesh)
├─ Motor_Housing (mesh - wrong transforms)
├─ Motor_Shaft (mesh - wrong transforms)
└─ Motor_Bearings (parent)
   ├─ Bearing_01 (mesh - wrong transforms)
   └─ Bearing_02 (mesh - wrong transforms)

Workflow:
1. Select: Motor (the top parent)
2. SnapFlow → Blender Workflow → Apply All Transforms
3. Dialog shows:
   - Selected objects: 1
   - Objects to process (incl. children): 4
   (Motor_Housing, Motor_Shaft, Bearing_01, Bearing_02)
4. Confirm
5. ✅ Result:
   - All 4 child objects processed
   - Transforms applied to all
   - Can save all as prefabs in one go
```

**Key Feature: Automatic Child Detection**
- Select a parent = all children with meshes included
- No manual selection needed
- Saves time with complex assemblies
- Works with nested hierarchies (children of children)

### Multi-Selection Example

You can also select multiple parents:

```
Hierarchy:
Assembly_A
├─ Part_1 (mesh)
└─ Part_2 (mesh)

Assembly_B
├─ Part_3 (mesh)
└─ Part_4 (mesh)

Workflow:
1. Select: Assembly_A AND Assembly_B
2. SnapFlow → Blender Workflow → Apply All Transforms
3. Dialog shows:
   - Selected objects: 2
   - Objects to process (incl. children): 4
   (Part_1, Part_2, Part_3, Part_4)
4. Confirm
5. ✅ Result:
   - All 4 parts processed
   - All transforms applied
```

### Skip Prefab Creation Example

```
1. Select model (wrong transforms)
2. SnapFlow → Blender Workflow → Apply All Transforms
3. Confirm transforms
4. "Save as Prefab?" → "No, Skip"
5. ✅ Result:
   - Transforms applied
   - NO prefab created
   - Model ready for other use
```

### Multi-Selection Support ⭐

**All commands support batch processing of multiple objects!**

**Single Object:**
```
1. Select 1 model in Hierarchy
2. SnapFlow → Blender Workflow → Apply All Transforms
3. After applying:
   - File picker: Choose exact location
   - example: Assets/Prefabs/MyModel_Variant.prefab
4. ✅ Applied + Prefab saved
```

**Multiple Objects:**
```
1. Select multiple models in Hierarchy (Ctrl+Click or Shift+Click)
2. SnapFlow → Blender Workflow → Apply All Transforms
3. ✅ Applied to all 5 objects (in batch)
```

**Batch Dialog Example:**
```
Apply All Transforms

This will:
1. Set origin to geometry center
2. Bake rotation into mesh
3. Bake scale into mesh

Selected objects: 5
Valid objects: 5

Yes, Apply All | Cancel
```

After completion:
```
✅ Complete

All transforms applied to 5 object(s).

Position, rotation, and scale are now baked.

[OK]
```

---

## 📋 Step-by-Step Workflow

### Step 1: Set Origin to Geometry

**Blender equivalent:** Object → Set Origin → Origin to Geometry

**What it does:**
- Calculates mesh bounds
- Moves pivot point to geometric center
- Adjusts transform to compensate
- Updates all colliders

**When to use:**
- After importing a model with offset geometry
- When pivot is not at mesh center
- Before applying rotations/scales

**Example:**
```
Before:
  Pivot: (0, 0, 0)
  Mesh center: (2, 1, -0.5)

After:
  Pivot: (2, 1, -0.5) → moved to mesh center
  Transform compensates for the change
```

**How to use:**
```
1. Select model in Hierarchy
2. SnapFlow → Blender Workflow → Set Origin to Geometry
3. Confirm in dialog
4. ✅ Origin moved to geometry center
```

---

### Step 2: Apply Rotation

**Blender equivalent:** Object → Apply → Apply Rotation

**What it does:**
- Bakes current local rotation into mesh vertices
- Rotates all vertex positions by the rotation
- Rotates mesh normals for lighting
- Adjusts colliders to match
- Resets local rotation to (0, 0, 0)

**When to use:**
- After rotating model and wanting to "freeze" the rotation
- Before exporting if rotation should be permanent
- To ensure colliders align with actual mesh

**Example:**
```
Before:
  Rotation: 45 degrees around Z
  Mesh vertices: stored with original orientation
  Local Rotation: (0, 0, 45)

After:
  All vertices rotated 45 degrees
  Mesh now oriented with rotated geometry
  Local Rotation: (0, 0, 0)
```

**How to use:**
```
1. Select model with rotation in Hierarchy
2. SnapFlow → Blender Workflow → Apply Rotation
3. Confirm in dialog
4. ✅ Rotation baked into mesh
```

---

### Step 3: Apply Scale

**Blender equivalent:** Object → Apply → Apply Scale

**What it does:**
- Bakes current local scale into mesh vertices
- Scales all vertex positions by scale factors
- Adjusts colliders to match new size
- Resets local scale to (1, 1, 1)

**When to use:**
- After scaling model and wanting to "freeze" the scale
- When scale should be permanent part of mesh
- To normalize model size

**Example:**
```
Before:
  Scale: (0.9, 1.1, 1.0)
  Mesh vertices: original size
  Local Scale: (0.9, 1.1, 1.0)

After:
  All vertices scaled by (0.9, 1.1, 1.0)
  Mesh now actual desired size
  Local Scale: (1, 1, 1)
```

**How to use:**
```
1. Select model with scale in Hierarchy
2. SnapFlow → Blender Workflow → Apply Scale
3. Confirm in dialog
4. ✅ Scale baked into mesh
```

---

### Step 4: Apply All Transforms (One-Click)

**Combines all three steps in one operation**

**Does:**
1. Set origin to geometry center
2. Apply rotation
3. Apply scale

**Result:** Model is fully prepared in one action

**How to use:**
```
1. Select model in Hierarchy
2. SnapFlow → Blender Workflow → Apply All Transforms
3. Confirm in dialog
4. ✅ All transforms applied - model is ready
```

---

### Step 5: Create Prefab Variant ⭐

**Blender equivalent:** File → Export as New File (saves a prepared copy)

**What it does:**
- Creates a prefab asset from the model
- Saves to: `Assets/SnapFlow/Prefabs/`
- Naming: `[ModelName]_Variant.prefab`
- Handles duplicate names automatically

**When to use:**
- After applying transforms to preserve the state
- To create reusable prefab instances
- To save multiple variants of the same model

**Example:**
```
Before:
  Scene GameObject: engine_bolt
  No prefab asset exists

After:
  Scene GameObject: engine_bolt (unchanged)
  Prefab saved: Assets/SnapFlow/Prefabs/engine_bolt_Variant.prefab
  Can now be instantiated many times
```

**How to use (Option 1 - Create Prefab Only):**
```
1. Select model in Hierarchy (after transforms applied)
2. SnapFlow → Blender Workflow → Create Prefab Variant
3. Confirm in dialog
4. ✅ Prefab created at Assets/SnapFlow/Prefabs/[Name]_Variant.prefab
```

**How to use (Option 2 - Apply Transforms + Create Prefab Together):**
```
1. Select model in Hierarchy (with wrong transforms)
2. SnapFlow → Blender Workflow → Apply All Transforms & Create Prefab
3. Confirm in dialog
4. ✅ Transforms applied AND prefab created automatically
```

---

## 🎯 Complete Workflow Example

### Scenario: Import engine bolt with wrong origin

**Initial state in Blender before export:**
```
❌ Position: (1.5, 0.8, -0.2)
❌ Rotation: (30, 15, 0)
❌ Scale: (0.95, 1.0, 1.05)
```

**After importing to Unity:**
```
Same problematic transforms
```

**In Unity - Apply Blender workflow with Prefab Creation:**

```
1. Select bolt in Hierarchy
2. SnapFlow → Blender Workflow → Apply All Transforms & Create Prefab
3. Dialog shows what will happen
4. Click "Yes, Apply & Create"
5. ✅ RESULT:
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   Mesh geometry: baked with all transforms
   Colliders: updated to match
   Prefab saved: Assets/SnapFlow/Prefabs/engine_bolt_Variant.prefab
```

**Now ready for SnapFlow:**
- Use prefab in AssemblyStepManager or scenes
- Snap zones will align perfectly
- Physics will work as expected
- Auto-reset will position correctly
- Prefab can be instantiated multiple times

**Or for batch processing 5 similar bolts:**

```
1. Select all 5 bolts in Hierarchy (Ctrl+Click each)
2. SnapFlow → Blender Workflow → Apply All Transforms & Create Prefab
3. Dialog shows: "Selected objects: 5, Valid objects: 5"
4. Click "Yes, Apply & Create"
5. ✅ RESULT:
   All 5 models: transforms applied
   Prefabs created:
     • engine_bolt_01_Variant.prefab
     • engine_bolt_02_Variant.prefab
     • engine_bolt_03_Variant.prefab
     • engine_bolt_04_Variant.prefab
     • engine_bolt_05_Variant.prefab
```

---

## ⚙️ Technical Details

### Set Origin to Geometry

```csharp
1. Calculate mesh bounds
2. Store center offset
3. Modify all vertex positions: vertex -= center_offset
4. Adjust collider positions
5. Move transform to compensate: transform.position += center_offset
6. RecalculateBounds() and RecalculateNormals()
```

### Apply Rotation

```csharp
1. Create rotation matrix from local rotation
2. Transform all vertices: vertex = rotation_matrix * vertex
3. Transform all normals: normal = rotation_matrix * normal
4. Update collider positions/rotations
5. Reset local rotation to identity (0, 0, 0)
6. RecalculateBounds()
```

### Apply Scale

```csharp
1. For each vertex: vertex *= scale
2. Update collider sizes and positions to match scale
3. Reset local scale to (1, 1, 1)
4. RecalculateBounds()
```

---

## ⚠️ Important Notes

### Mesh Creation

The tool works with:
✅ Static meshes (imported from .fbx, .obj, etc.)
✅ Meshes with colliders
✅ Meshes with multiple materials
✅ Meshes with multiple child objects

The tool **does NOT work with:**
❌ Skinned/rigged meshes (animated characters)
❌ Objects without MeshFilter
❌ Procedural meshes (may not have readable mesh data)

### Undo/Redo

- Operations are recorded in undo history
- ✅ Can undo: `Ctrl+Z`
- ⚠️ Large mesh deformations might be slow to undo
- 💡 Save before major operations if working with huge meshes

### Colliders

The tool automatically updates:
- ✅ BoxCollider
- ✅ SphereCollider
- ✅ CapsuleCollider
- ⚠️ MeshCollider (manual check recommended)
- ❌ TerrainCollider (not applicable)

---

## 🚀 Best Practices

### Batch Processing Multiple Objects

**Efficient workflow for many models:**

```
1. Import folder of prepared models
2. Select ALL models in Hierarchy (Ctrl+A or select root)
3. SnapFlow → Blender Workflow → Apply All Transforms
4. ✅ All processed in one operation
```

**Advantages:**
- ✅ Single undo point for all changes
- ✅ Fast: process 10-50 models at once
- ✅ Batch confirmation shows exact count
- ✅ Console logs track each object

**Example Batch Run:**
```
[SnapFlow] Set origin to geometry for Bolt_01. Center offset: (0.1, -0.05, 0.2)
[SnapFlow] Set origin to geometry for Bolt_02. Center offset: (0.1, -0.05, 0.2)
[SnapFlow] Set origin to geometry for Bolt_03. Center offset: (0.1, -0.05, 0.2)
[SnapFlow] Applied rotation to Bolt_01
[SnapFlow] Applied rotation to Bolt_02
[SnapFlow] Applied rotation to Bolt_03
[SnapFlow] Applied scale 0.9, 1.0, 1.0 to Bolt_01
[SnapFlow] Applied scale 0.9, 1.0, 1.0 to Bolt_02
[SnapFlow] Applied scale 0.9, 1.0, 1.0 to Bolt_03

Result: All transforms applied to 3 object(s).
```

### Smart Selection for Batch Processing

**Select models with issues only:**

```
1. Run Origin Validator
2. See which models need fixing
3. Select those specific models
4. Apply only necessary transforms
```

**Select by pattern:**

```
1. Click first model
2. Hold Ctrl + Click each additional model
3. Or: Hold Shift for range selection
```

### Working with Parent-Child Hierarchies ⭐

**Automatic child detection saves time:**

**Good Hierarchy Setup:**
```
Assembly (parent)
├─ Part_01 (has mesh)
├─ Part_02 (has mesh)
└─ SubAssembly (parent - no mesh)
   ├─ Part_03 (has mesh)
   └─ Part_04 (has mesh)

Workflow: Select Assembly → Automatically processes all parts with meshes
```

**Benefits:**
- ✅ Select once, process entire assembly
- ✅ Nested hierarchies supported
- ✅ Only objects with meshes are processed
- ✅ Parent objects without meshes are ignored
- ✅ Saves prefabs for all processed objects

**Common Use Cases:**
1. **Imported assemblies** - Select top level → all parts processed
2. **Complex models** - Select hierarchy → nested children processed
3. **Multiple assemblies** - Select multiple parents → all their children processed
4. **Prefab creation** - Process and save entire assembly structure at once

**Pro Tip:** Use hierarchies to organize models logically, then let the tool handle the bulk processing automatically!
4. Then: Apply workflow
```
---

## 📦 Prefab Organization

### Flexible Save Locations ⭐

**NEW:** Prefabs can now be saved ANYWHERE you want!

When you run "Apply All Transforms":
1. Transforms are applied
2. You're asked: "Save as Prefab?"
3. **You choose the location** via file/folder picker
4. Prefab saved where YOU specified

**Benefits:**
- ✅ Save to `Assets/SnapFlow/Prefabs/` (recommended)
- ✅ Or save to `Assets/Models/Variants/` (custom location)
- ✅ Or save to `Assets/ProjectPrefabs/` (project-specific)
- ✅ Each prefab can go to different location if needed
- ✅ Full control over file organization

### Recommended Organization

```
Assets/
├── SnapFlow/
│   ├── Prefabs/          ← For models prepared with workflow
│   │   ├── Bolt_01_Variant.prefab
│   │   ├── Bolt_02_Variant.prefab
│   │   └── ... more prefabs
│   ├── Assembly/
│   ├── Disassembly/
│   └── Tests/
```

**Alternative Organizations:**

```
Assets/                        Assets/
├── Models/                  ├── Prefabs/
│   ├── Raw/                 │   ├── Assembly/
│   └── Variants/            │   ├── Disassembly/
│       ├── bolt_Variant.prefab
│       └── gear_Variant.prefab
```

### Prefab Naming Convention

**Pattern:** `[OriginalName]_Variant.prefab`

**Examples:**
```
Model Name: engine_bolt → engine_bolt_Variant.prefab
Model Name: gearAssembly → gearAssembly_Variant.prefab
Model Name: piston → piston_Variant.prefab
```

**Duplicate Handling (automatic):**
```
Save 1st: piston_Variant.prefab ✓
Save 2nd (same source): piston_Variant_1.prefab ✓ (auto-numbered)
Save 3rd: piston_Variant_2.prefab ✓ (prevents overwrites)
```

### Single vs Batch Save Behavior

**Single Model:**
```
File Picker: Select file path and name
Example: Assets/Prefabs/engine_Variant.prefab
Result: Saves to exact location YOU choose
```

**Multiple Models:**
```
Folder Picker: Select destination folder
Example: Assets/SnapFlow/Prefabs/
Result: All prefabs saved to folder with auto-generated names
  → Model1_Variant.prefab
  → Model2_Variant.prefab
  → Model2_Variant_1.prefab (if duplicate name)
```

### Using Prefabs in Scenes

After creating prefab variants:

```
1. Drag prefab from Project panel to scene
2. Position as needed
3. Add to SnapFlow manager
4. Snap zones will work perfectly
5. Physics will be correct
6. Auto-reset will work reliably
```

**Advantages of Prefabs:**
- ✅ Reusable across scenes
- ✅ Consistent state guaranteed
- ✅ Easy to duplicate with proper transforms
- ✅ Modifications update all instances
- ✅ Prefab overrides for variations
### Before Importing Model

In Blender:
```
1. Set origin to geometry (Object → Set Origin)
2. Apply all transforms (Object → Apply)
3. Export as .fbx with "Apply Scalings: FBX All"
```

**Result:** Model imports already prepared

### If Importing Unprepared Model

In Unity:
```
1. Select model
2. SnapFlow → Blender Workflow → Apply All Transforms
3. Verify in inspector (Position, Rotation, Scale all at standard)
4. Add to SnapFlow manager
```

### Workflow Comparison

| Step | Blender | Unity |
|------|---------|-------|
| 1. Import model | ✓ | ✓ |
| 2. Verify origin | Manual inspect | Validate tool |
| 3. Set origin | Object → Set Origin | Apply All Transforms |
| 4. Apply rotation | Object → Apply | (Included in Apply All) |
| 5. Apply scale | Object → Apply | (Included in Apply All) |
| 6. Export | File → Export | (In-engine, ready) |

---

## 🧪 Testing the Workflow

### Quick Test

```
1. Create new cube in scene (GameObject → 3D Object → Cube)
2. Move to (2, 2, 2)
3. Rotate to (45, 45, 45)
4. Scale to (0.8, 1.2, 0.9)
5. Select cube
6. SnapFlow → Blender Workflow → Apply All Transforms
7. Verify in inspector:
   ✅ Position: (0, 0, 0)
   ✅ Rotation: (0, 0, 0)
   ✅ Scale: (1, 1, 1)
```

### Complex Test

```
1. Import complex model with offset
2. Add colliders
3. Add child objects (handles, attachment points)
4. Apply All Transforms
5. Verify:
   ✅ Main object at origin
   ✅ Child positions preserved
   ✅ Colliders aligned
   ✅ Geometry transformed correctly
```

---

## 🐛 Troubleshooting

### "No Mesh Found" Error

**Cause:** Selected object has no MeshFilter or mesh

**Solution:**
```
1. Verify object has MeshFilter component
2. Ensure MeshFilter has mesh assigned
3. Try on a different model
```

### Transform didn't apply

**Cause:** Mesh is shared/read-only

**Solution:**
```
1. In FBX import settings: disable "Read/Write Enabled"
2. Re-enable it in Import Settings
3. Try again
```

### Colliders no longer align

**Cause:** Complex collider setup not updated

**Solution:**
```
1. Manual adjust collider after transform
2. Or: Use only BoxCollider/SphereCollider for auto-update
3. Use MeshCollider (automatically conforms)
```

### Children moved unexpectedly

**Cause:** Child local positions were adjusted

**Solution:**
```
1. Undo (Ctrl+Z)
2. Manually adjust child positions before applying
3. Or: Parent world position should be maintained
```

### Batch Processing: Some objects skipped

**Cause:** Some selected objects don't have MeshFilter or have no transform to apply

**Example:**
```
Selected objects: 5
Valid objects: 3

Objects skipped (no MeshFilter):
- EmptyParent
- SnapZone_01

Objects skipped (no transform to apply):
- Bolt_02 (already at origin)
```

**Solution:**
```
This is normal behavior! Only objects with:
- ✅ Valid MeshFilter
- ✅ Transform to apply (position/rotation/scale != standard)
...will be processed.

Others are safely ignored.
```

### Batch Operation Too Slow

**Cause:** Processing very large meshes or many objects

**Solution:**
```
1. Process in smaller batches (5-10 objects at a time)
2. Or: Process one at a time if mesh has millions of vertices
3. Check console for per-object timing
```

### Prefab Save Dialog Cancelled

**Cause:** User clicked "Cancel" in file/folder picker

**Solution:**
```
1. Run ApplyAllTransforms again
2. When asked "Save as Prefab?" click "Yes, Save as Prefab"
3. Complete the file picker dialog
```

### File Picker Not Showing

**Cause:** Dialog may be hidden or behind another window

**Solution:**
```
1. Check taskbar for Unity window
2. Alt+Tab to bring Unity to focus
3. If still not visible: Restart Unity
4. Check console for error messages
```

### Prefab Created in Wrong Location

**Cause:** Chose different folder in picker

**Solution:**
```
1. Move the prefab file manually in Project window
   OR
2. Delete the prefab and run ApplyAllTransforms again
3. Choose correct location in file picker
```

### Single Object: File Picker for Custom Save

**Expected behavior:**
```
Single model selected
  ↓
ApplyAllTransforms
  ↓
"Save as Prefab?" → "Yes"
  ↓
File picker shows (can choose ANY location!)
You type: Assets/MyCustomFolder/MyModel_Variant.prefab
  ↓
✅ Prefab saved to YOUR chosen location
```

### Multiple Objects: Folder Picker

**Expected behavior:**
```
Multiple models selected
  ↓
ApplyAllTransforms
  ↓
"Save as Prefab?" → "Yes"
  ↓
Folder picker shows (choose destination folder)
You select: Assets/Prefabs/
  ↓
✅ All prefabs saved to that folder
  (Names auto-generated: Model_01_Variant.prefab, etc.)
```

### Duplicate Prefab Names in Batch Mode

**Automatic handling:**
```
Batch save 2x "engine.fbx"
  → First: engine_Variant.prefab
  → Second: engine_Variant_1.prefab (auto-numbered)
  (This prevents overwriting existing files!)
```

**Avoid duplicates:**
```
Select different models → different names created automatically
```

---

## 📊 Comparison: Blender vs Unity Workflow

### Blender Workflow
```
Model in Blender
  ↓
Set Origin to Geometry
  ↓
Apply Rotation
  ↓
Apply Scale
  ↓
Export
  ↓
Import to Unity
  ↓
Ready to use ✅
```

### Unity Workflow (before this tool)
```
Model in Blender
  ↓
Export (maybe with wrong transforms)
  ↓
Import to Unity
  ↓
Manually reset transforms (tedious)
  ↓
Ready to use ✅
```

### Unity Workflow (with SnapFlow)
```
Model in Blender
  ↓
Export (any state)
  ↓
Import to Unity
  ↓
SnapFlow → Apply All Transforms
  ↓
Ready to use ✅
```

---

## 🔗 Integration with Origin Validator

Two tools work together:

**Origin Validator:**
- Checks if models meet standard
- Can auto-fix simple cases

**Blender Workflow:**
- Prepares models the "right way"
- Bakes transforms into geometry
- More thorough preparation

**Recommended use:**
1. Use Blender Workflow to properly prepare model
2. Use Origin Validator to verify
3. Add to SnapFlow manager

---

## 📝 API Reference

```csharp
namespace SnapFlow.Editor
{
    public class SnapFlowBlenderWorkflow
    {
        // Menu commands (call via menu or keyboard shortcuts)
        [MenuItem("SnapFlow/Blender Workflow/Set Origin to Geometry")]
        public static void SetOriginToGeometry()

        [MenuItem("SnapFlow/Blender Workflow/Apply Rotation")]
        public static void ApplyRotation()

        [MenuItem("SnapFlow/Blender Workflow/Apply Scale")]
        public static void ApplyScale()

        [MenuItem("SnapFlow/Blender Workflow/Apply All Transforms")]
        public static void ApplyAllTransforms()
    }
}
```

---

## 🚄 Batch Processing Capabilities

### Supported Scenarios

| Scenario | Single | Multiple | Result |
|----------|--------|----------|--------|
| **Set Origin** | ✅ | ✅ | All origins moved to geometry |
| **Apply Rotation** | ✅ | ✅ | All rotations baked |
| **Apply Scale** | ✅ | ✅ | All scales baked |
| **Apply All** | ✅ | ✅ | All transforms baked (3 in 1) |

### Batch Processing Flow

```
Selection: 5 models
  ↓
Validation:
  ├─ Bolt_01: ✅ Has MeshFilter
  ├─ Bolt_02: ✅ Has MeshFilter
  ├─ Bolt_03: ✅ Has MeshFilter
  ├─ Snap_Zone: ❌ No MeshFilter (skipped)
  └─ Empty_Parent: ❌ No MeshFilter (skipped)
  ↓
Confirmation Dialog:
  "Selected objects: 5, Valid objects: 3"
  ↓
User: "Yes, Apply All"
  ↓
Processing:
  ├─ Process Bolt_01 ✓
  ├─ Process Bolt_02 ✓
  ├─ Process Bolt_03 ✓
  └─ Skip invalid objects
  ↓
Result Dialog:
  "All transforms applied to 3 object(s)."
```

### Performance Notes

- ✅ **Fast**: 10-50 objects in ~1-2 seconds
- ✅ **Efficient**: Single undo point for entire batch
- ⚠️ **Large Meshes**: May take longer (millions of vertices)
- ✅ **Console Logs**: Each object logged for tracking

### Smart Filtering

The tool automatically:
- ✅ Counts objects with valid MeshFilter
- ✅ Filters objects with nothing to apply
- ✅ Shows exact count in confirmation
- ✅ Reports processed count in result
- ✅ Logs details to console

---

## 🎓 What You Learn

This tool teaches you:
- How mesh vertices work in Unity
- Matrix transformation basics
- Collider behavior and updates
- Undo/redo recording
- Transform hierarchy management
- Editor scripting patterns

---

**Last Updated:** 2026-06-10  
**Version:** 1.0  
**File:** Assets/SnapFlow/Editor/SnapFlowBlenderWorkflow.cs  
**Status:** Production Ready ✅
