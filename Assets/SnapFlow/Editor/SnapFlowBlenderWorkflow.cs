using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SnapFlow.Editor
{
    /// <summary>
    /// Replicates Blender workflow for model origin setup:
    /// 1. Set Origin to Geometry (move pivot to mesh center)
    /// 2. Apply Rotation (bake rotation into mesh)
    /// 3. Apply Scale (bake scale into mesh)
    /// </summary>
    public class SnapFlowBlenderWorkflow
    {
        private const float TOLERANCE = 0.0001f;

        // ========== HELPER: Collect objects and children with meshes ==========
        private static List<GameObject> CollectObjectsWithChildren(GameObject[] selectedObjects)
        {
            var result = new List<GameObject>();
            var processedSet = new HashSet<GameObject>();

            foreach (var obj in selectedObjects)
            {
                if (obj == null || processedSet.Contains(obj))
                    continue;

                // Add the object itself if it has a mesh
                var meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    result.Add(obj);
                    processedSet.Add(obj);
                }

                // Add all children with meshes
                var children = obj.GetComponentsInChildren<MeshFilter>();
                foreach (var child in children)
                {
                    if (child.sharedMesh != null && !processedSet.Contains(child.gameObject))
                    {
                        result.Add(child.gameObject);
                        processedSet.Add(child.gameObject);
                    }
                }
            }

            return result;
        }

        [MenuItem("SnapFlow/Blender Workflow/Set Origin to Geometry")]
        public static void SetOriginToGeometry()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject.", "OK");
                return;
            }

            // Collect all objects including children with meshes
            var allObjectsToProcess = CollectObjectsWithChildren(Selection.gameObjects);
            int validCount = allObjectsToProcess.Count;

            if (validCount == 0)
            {
                EditorUtility.DisplayDialog("No Mesh Found",
                    "None of the selected objects (or their children) have a MeshFilter with a valid mesh.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Set Origin to Geometry",
                $"This will move the pivot point to the center of the mesh.\n\n" +
                $"Selected objects: {Selection.gameObjects.Length}\n" +
                $"Objects to process (incl. children): {validCount}\n\n" +
                $"Continue?",
                "Yes", "Cancel"))
            {
                int processedCount = 0;
                foreach (var obj in allObjectsToProcess)
                {
                    Undo.RecordObject(obj.transform, "Set Origin to Geometry");
                    SetOriginToGeometryInternal(obj);
                    processedCount++;
                }
                EditorUtility.DisplayDialog("✅ Complete",
                    $"Origin moved to geometry center for {processedCount} object(s) (including children).", "OK");
            }
        }

        [MenuItem("SnapFlow/Blender Workflow/Apply Rotation")]
        public static void ApplyRotation()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject.", "OK");
                return;
            }

            // Collect all objects including children with meshes
            var allObjectsToProcess = CollectObjectsWithChildren(Selection.gameObjects);

            int validCount = 0;
            foreach (var obj in allObjectsToProcess)
            {
                if (!IsZero(obj.transform.localRotation.eulerAngles, TOLERANCE))
                    validCount++;
            }

            if (validCount == 0)
            {
                EditorUtility.DisplayDialog("No Rotation",
                    "None of the selected objects (or their children) have a rotation to apply.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Apply Rotation",
                $"This will bake the current rotation into the mesh.\n\n" +
                $"Selected objects: {Selection.gameObjects.Length}\n" +
                $"Objects to process (incl. children): {allObjectsToProcess.Count}\n" +
                $"Objects with rotation: {validCount}\n\n" +
                $"This cannot be undone easily. Continue?",
                "Yes, Apply", "Cancel"))
            {
                int processedCount = 0;
                foreach (var obj in allObjectsToProcess)
                {
                    if (!IsZero(obj.transform.localRotation.eulerAngles, TOLERANCE))
                    {
                        var meshFilter = obj.GetComponent<MeshFilter>();
                        Undo.RecordObject(meshFilter, "Apply Rotation");
                        Undo.RecordObject(obj.transform, "Apply Rotation");
                        ApplyRotationInternal(obj);
                        processedCount++;
                    }
                }
                EditorUtility.DisplayDialog("✅ Complete",
                    $"Rotation baked into mesh for {processedCount} object(s) (including children).", "OK");
            }
        }

        [MenuItem("SnapFlow/Blender Workflow/Apply Scale")]
        public static void ApplyScale()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject.", "OK");
                return;
            }

            // Collect all objects including children with meshes
            var allObjectsToProcess = CollectObjectsWithChildren(Selection.gameObjects);

            int validCount = 0;
            foreach (var obj in allObjectsToProcess)
            {
                if (!IsOne(obj.transform.localScale, TOLERANCE))
                    validCount++;
            }

            if (validCount == 0)
            {
                EditorUtility.DisplayDialog("No Scale",
                    "None of the selected objects (or their children) have a scale to apply.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Apply Scale",
                $"This will bake the current scale into the mesh.\n\n" +
                $"Selected objects: {Selection.gameObjects.Length}\n" +
                $"Objects to process (incl. children): {allObjectsToProcess.Count}\n" +
                $"Objects with scale: {validCount}\n\n" +
                $"This cannot be undone easily. Continue?",
                "Yes, Apply", "Cancel"))
            {
                int processedCount = 0;
                foreach (var obj in allObjectsToProcess)
                {
                    if (!IsOne(obj.transform.localScale, TOLERANCE))
                    {
                        var meshFilter = obj.GetComponent<MeshFilter>();
                        Undo.RecordObject(meshFilter, "Apply Scale");
                        Undo.RecordObject(obj.transform, "Apply Scale");
                        ApplyScaleInternal(obj);
                        processedCount++;
                    }
                }
                EditorUtility.DisplayDialog("✅ Complete",
                    $"Scale baked into mesh for {processedCount} object(s) (including children).", "OK");
            }
        }

        [MenuItem("SnapFlow/Blender Workflow/Apply All Transforms")]
        public static void ApplyAllTransforms()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject.", "OK");
                return;
            }

            // Collect all objects including children with meshes
            var allObjectsToProcess = CollectObjectsWithChildren(Selection.gameObjects);
            int validCount = allObjectsToProcess.Count;

            if (validCount == 0)
            {
                EditorUtility.DisplayDialog("No Mesh Found",
                    "None of the selected objects (or their children) have a MeshFilter with a valid mesh.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Apply All Transforms",
                $"This will bake position, rotation, and scale into the mesh.\n" +
                $"Selected objects: {Selection.gameObjects.Length}\n" +
                $"Objects to process (incl. children): {validCount}\n\n" +
                $"This cannot be undone easily. Continue?",
                "Yes, Apply", "Cancel"))
            {
                int processedCount = 0;
                foreach (var obj in allObjectsToProcess)
                {
                    Undo.RecordObject(obj.transform, "Apply All Transforms");
                    var meshFilter = obj.GetComponent<MeshFilter>();
                    Undo.RecordObject(meshFilter, "Apply All Transforms");

                    SetOriginToGeometryInternal(obj);
                    ApplyRotationInternal(obj);
                    ApplyScaleInternal(obj);
                    
                    processedCount++;
                }

                EditorUtility.DisplayDialog("✅ Complete",
                    $"All transforms applied to {processedCount} object(s) (including children).\n" +
                    $"Position, rotation, and scale are now baked.",
                    "OK");

                // Automatically offer to save as prefab
                if (EditorUtility.DisplayDialog("Save as Prefab?",
                    $"Would you like to save {processedCount} object(s) as prefab(s)?\\n\\n" +
                    $"(You will be asked where to save each time)",
                    "Yes, Save as Prefab", "No, Skip"))
                {
                    SavePrefabsWithFilePicker(allObjectsToProcess.ToArray());
                }
            }
        }

        // ========== INTERNAL IMPLEMENTATIONS ==========

        private static void SetOriginToGeometryInternal(GameObject gameObject)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            var mesh = meshFilter.sharedMesh;
            var bounds = mesh.bounds;
            var centerOffset = bounds.center;

            // Get all child transforms
            var children = gameObject.GetComponentsInChildren<Transform>();
            var childPositionOffsets = new Dictionary<Transform, Vector3>();

            // Record child positions relative to parent before change
            foreach (var child in children)
            {
                if (child != gameObject.transform)
                {
                    childPositionOffsets[child] = child.localPosition;
                }
            }

            // Move vertices to center origin
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= centerOffset;
            }

            // Update mesh
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Move colliders to compensate
            var colliders = gameObject.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (collider is BoxCollider box)
                {
                    box.center -= centerOffset;
                }
                else if (collider is SphereCollider sphere)
                {
                    sphere.center -= centerOffset;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    capsule.center -= centerOffset;
                }
            }

            // Move transform to compensate
            gameObject.transform.localPosition += gameObject.transform.TransformDirection(centerOffset);

            // Restore child positions
            foreach (var child in children)
            {
                if (child != gameObject.transform && childPositionOffsets.ContainsKey(child))
                {
                    child.localPosition = childPositionOffsets[child];
                }
            }

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(gameObject);

            Debug.Log($"[SnapFlow] Set origin to geometry for {gameObject.name}. Center offset: {centerOffset}");
        }

        private static void ApplyRotationInternal(GameObject gameObject)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            var rotation = gameObject.transform.localRotation;

            if (IsZero(rotation.eulerAngles, TOLERANCE))
            {
                Debug.Log("[SnapFlow] No rotation to apply.");
                return;
            }

            // Get rotation matrix
            var rotationMatrix = Matrix4x4.Rotate(rotation);

            // Apply to vertices
            var vertices = meshFilter.sharedMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = rotationMatrix.MultiplyPoint3x4(vertices[i]);
            }

            // Apply to normals
            var normals = meshFilter.sharedMesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = rotationMatrix.MultiplyVector(normals[i]);
            }

            // Update mesh
            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.normals = normals;
            meshFilter.sharedMesh.RecalculateBounds();

            // Move colliders
            var colliders = gameObject.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (collider is BoxCollider box)
                {
                    box.center = rotation * box.center;
                }
                else if (collider is CapsuleCollider capsule)
                {
                    capsule.center = rotation * capsule.center;
                }
            }

            // Reset transform
            gameObject.transform.localRotation = Quaternion.identity;

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(gameObject);

            Debug.Log($"[SnapFlow] Applied rotation to {gameObject.name}");
        }

        private static void ApplyScaleInternal(GameObject gameObject)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            var scale = gameObject.transform.localScale;

            if (IsOne(scale, TOLERANCE))
            {
                Debug.Log("[SnapFlow] No scale to apply.");
                return;
            }

            // Apply to vertices
            var vertices = meshFilter.sharedMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], scale);
            }

            // Update mesh
            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.RecalculateBounds();
            meshFilter.sharedMesh.RecalculateNormals();

            // Move colliders
            var colliders = gameObject.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (collider is BoxCollider box)
                {
                    box.center = Vector3.Scale(box.center, scale);
                    box.size = Vector3.Scale(box.size, scale);
                }
                else if (collider is SphereCollider sphere)
                {
                    sphere.center = Vector3.Scale(sphere.center, scale);
                    sphere.radius *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                }
                else if (collider is CapsuleCollider capsule)
                {
                    capsule.center = Vector3.Scale(capsule.center, scale);
                    capsule.radius *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                    capsule.height *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                }
            }

            // Reset transform
            gameObject.transform.localScale = Vector3.one;

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(gameObject);

            Debug.Log($"[SnapFlow] Applied scale {scale} to {gameObject.name}");
        }

        // ========== HELPERS ==========

        private static bool IsZero(Vector3 value, float tolerance)
        {
            return Mathf.Abs(value.x) < tolerance &&
                   Mathf.Abs(value.y) < tolerance &&
                   Mathf.Abs(value.z) < tolerance;
        }

        private static bool IsOne(Vector3 value, float tolerance)
        {
            return Mathf.Abs(value.x - 1f) < tolerance &&
                   Mathf.Abs(value.y - 1f) < tolerance &&
                   Mathf.Abs(value.z - 1f) < tolerance;
        }

        // ========== AUTOMATED PREFAB WORKFLOW ==========
        // Prefab creation is now automatically triggered after ApplyAllTransforms

        // ========== INTERNAL PREFAB METHODS ==========

        private static void SavePrefabsWithFilePicker(GameObject[] selectedObjects)
        {
            int validCount = 0;
            foreach (var obj in selectedObjects)
            {
                var meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    validCount++;
            }

            if (validCount == 0)
                return;

            // For single object, ask where to save directly
            if (validCount == 1)
            {
                var obj = selectedObjects[0];
                var meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    return;

                string suggestedName = $"{obj.name}_Variant.prefab";
                string savePath = EditorUtility.SaveFilePanelInProject(
                    "Save Prefab",
                    suggestedName,
                    "prefab",
                    $"Choose where to save prefab for {obj.name}");

                if (!string.IsNullOrEmpty(savePath))
                {
                    try
                    {
                        PrefabUtility.SaveAsPrefabAsset(obj, savePath);
                        EditorUtility.DisplayDialog("✅ Saved",
                            $"Prefab saved to:\n{savePath}",
                            "OK");
                        Debug.Log($"[SnapFlow] Created prefab: {savePath}");
                    }
                    catch (System.Exception ex)
                    {
                        EditorUtility.DisplayDialog("❌ Error",
                            $"Failed to save prefab: {ex.Message}",
                            "OK");
                        Debug.LogError($"[SnapFlow] Failed to create prefab: {ex.Message}");
                    }
                }
            }
            else
            {
                // For multiple objects, ask for folder
                string folderPath = EditorUtility.SaveFolderPanel(
                    "Select Folder for Prefabs",
                    "Assets",
                    "");

                if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith(Application.dataPath))
                {
                    EditorUtility.DisplayDialog("Cancelled", "Prefab save cancelled.", "OK");
                    return;
                }

                // Convert absolute path to asset path
                string assetPath = "Assets" + folderPath.Substring(Application.dataPath.Length);

                int createdCount = 0;
                foreach (var obj in selectedObjects)
                {
                    var meshFilter = obj.GetComponent<MeshFilter>();
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        continue;

                    try
                    {
                        string prefabName = $"{obj.name}_Variant.prefab";
                        string prefabPath = $"{assetPath}/{prefabName}";

                        // Handle duplicates
                        int counter = 1;
                        string basePath = prefabPath;
                        while (System.IO.File.Exists(prefabPath))
                        {
                            prefabPath = basePath.Replace(".prefab", $"_{counter}.prefab");
                            counter++;
                        }

                        PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
                        Debug.Log($"[SnapFlow] Created prefab: {prefabPath}");
                        createdCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SnapFlow] Failed to create prefab for {obj.name}: {ex.Message}");
                    }
                }

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog("✅ Complete",
                    $"Created {createdCount} prefab(s) in:\n{assetPath}",
                    "OK");
            }
        }
    }
}
