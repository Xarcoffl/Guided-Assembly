using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


[CustomEditor(typeof(DisassemblyManager))]
    public class DisassemblyManagerEditor : Editor
    {
        private static readonly Color SnapFlowBlue = new Color(0.2f, 0.5f, 0.8f);
        private static readonly Color SnapFlowOrange = new Color(1f, 0.6f, 0.2f);
        
        private SerializedProperty DisassemblyProperty;
        private SerializedProperty stepCompleteSound;
        private SerializedProperty HighlightMaterial;
        private SerializedProperty progressBar;
        private SerializedProperty progressText;
        private SerializedProperty stepDescriptionText;
        private SerializedProperty onDisassemblyComplete;

        private ReorderableList reorderableSteps;
        private List<bool> foldoutStates = new();

        private bool showDebugLogs;
        private bool showGizmos;

        private void OnEnable()
        {
            DisassemblyProperty = serializedObject.FindProperty("steps");
            stepCompleteSound = serializedObject.FindProperty("stepCompleteSound");
            HighlightMaterial = serializedObject.FindProperty("HighlightMaterial");
            progressBar = serializedObject.FindProperty("progressBar");
            progressText = serializedObject.FindProperty("progressText");
            stepDescriptionText = serializedObject.FindProperty("stepDescriptionText");
            onDisassemblyComplete = serializedObject.FindProperty("onDisassemblyComplete");
            
            
            reorderableSteps = new ReorderableList(serializedObject, DisassemblyProperty, true, true, false, true);
            reorderableSteps.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Disassembly Steps", EditorStyles.boldLabel);
                
                Rect clearButtonRect = new Rect(rect.xMax - 200, rect.y, 90, EditorGUIUtility.singleLineHeight);
                Rect autoDescButtonRect = new Rect(rect.xMax - 100, rect.y, 90, EditorGUIUtility.singleLineHeight);
                
                
                if (GUI.Button(clearButtonRect, "Reset"))
                {
                    if (EditorUtility.DisplayDialog("Confirm Clear", "Are you sure you want to remove all steps?", "Confirm", "Cancel"))
                    {
                        DisassemblyProperty.ClearArray();
                        foldoutStates.Clear();
                        /*Log("Steps cleared.");*/
                    }
                }
                
                if (GUI.Button(autoDescButtonRect, "Auto-Desc"))
                {
                    for (int i = 0; i < DisassemblyProperty.arraySize; i++)
                    {
                        var step = DisassemblyProperty.GetArrayElementAtIndex(i);
                        var grabObj = step.FindPropertyRelative("objectToRemove").objectReferenceValue;
                        var snapZone = step.FindPropertyRelative("sourceSnapZone").objectReferenceValue;
                        var desc = step.FindPropertyRelative("StepDescription");

                        string grabName = grabObj ? grabObj.name : "None";
                        string snapName = snapZone ? snapZone.name : "None";
                        desc.stringValue = $"Remove {grabName} in {snapName}";

                        /*Log($"Auto-desc for Step {i + 1}: {desc.stringValue}");*/
                    }
                }
            };

            reorderableSteps.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SyncFoldoutList();
                var step = DisassemblyProperty.GetArrayElementAtIndex(index);
                var objectToRemove = step.FindPropertyRelative("objectToRemove");
                var snapZone = step.FindPropertyRelative("sourceSnapZone");

                string stepName = objectToRemove.objectReferenceValue ? $"Step {index + 1} : {objectToRemove.objectReferenceValue.name}" : $"Step {index + 1}";
                float indentOffset = 15f;
                Rect foldoutRect = new Rect(rect.x + indentOffset, rect.y, rect.width - indentOffset, EditorGUIUtility.singleLineHeight);
                foldoutStates[index] = EditorGUI.Foldout(foldoutRect, foldoutStates[index], stepName, true);
                if (foldoutStates[index])
                {
                    EditorGUI.indentLevel++;
                    float yOffset = rect.y + EditorGUIUtility.singleLineHeight + 4;
                    float fullWidth = rect.width;
                    float fullHeight = rect.height;
                    float lineHeight = EditorGUIUtility.singleLineHeight;
                    
                    var removeObj = objectToRemove.objectReferenceValue;
                    var snapObj =  snapZone.objectReferenceValue;
                    
                    if (!removeObj)
                    {
                        EditorGUI.HelpBox(new Rect(rect.x, yOffset, fullWidth, lineHeight + 5), "Missing: Object to Remove", MessageType.Warning);
                        yOffset += lineHeight + 10;
                    }

                    if (!snapObj)
                    {
                        EditorGUI.HelpBox(new Rect(rect.x, yOffset, fullWidth, lineHeight + 5), "Missing: Snap Zone", MessageType.Warning);
                        yOffset += lineHeight + 15;
                    }

                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, lineHeight), snapZone, new GUIContent("Snap Zone"));
                    yOffset += lineHeight + 10;
                    
                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, lineHeight), objectToRemove, new GUIContent("Object To Remove"));
                    yOffset += lineHeight + 10;
                    
                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, lineHeight*5),
                        step.FindPropertyRelative("StepDescription"), new GUIContent("Step Description"));
                    yOffset += lineHeight * 6;
                    var unsnapEvents = step.FindPropertyRelative("onUnSnapEvents");
                    float snapHeight = EditorGUI.GetPropertyHeight(unsnapEvents, true);
                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, snapHeight), unsnapEvents, new GUIContent("Un-Snap Events"), true);
                    yOffset += snapHeight + 10;

                    EditorGUI.indentLevel--;
                }
            };



            reorderableSteps.elementHeightCallback = (index) =>
            {
                SyncFoldoutList();
                if (!foldoutStates[index]) return EditorGUIUtility.singleLineHeight + 10;

                var step = DisassemblyProperty.GetArrayElementAtIndex(index);
                float height = EditorGUIUtility.singleLineHeight * 4 + 30;
                
                
                height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("onUnSnapEvents"), true) + 10;
          
                height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("StepDescription"), true) + 10;

                var grabGO = step.FindPropertyRelative("objectToRemove").objectReferenceValue;
                var UnsnapGO = step.FindPropertyRelative("sourceSnapZone").objectReferenceValue;

                if (!grabGO) height += EditorGUIUtility.singleLineHeight + 15;
                if (!UnsnapGO) height += EditorGUIUtility.singleLineHeight + 25;
                
                height += (EditorGUIUtility.singleLineHeight) * 3;
                
                return height;
            };




        }
        private void SyncFoldoutList()
        {
            while (foldoutStates.Count < DisassemblyProperty.arraySize) foldoutStates.Add(true);
            while (foldoutStates.Count > DisassemblyProperty.arraySize) foldoutStates.RemoveAt(foldoutStates.Count - 1);
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHeader();
            DrawGeneralSettings();
            DrawReorderableSteps();
            DrawFooter();
            serializedObject.ApplyModifiedProperties();
        }
        
        private new static void DrawHeader()
        {
            GUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            
            
                normal = { textColor = SnapFlowBlue}
            };
            
            GUIStyle taglinestyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            
            GUILayout.Label("SNAP FLOW", titleStyle);
            GUILayout.Label("Disassembly Editor", taglinestyle);
            GUILayout.Space(10);
        }
        
        private void DrawGeneralSettings()
        {
      
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sound Clips",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(stepCompleteSound, new GUIContent("Success Sound Clip"));
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Highlights and Description",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(HighlightMaterial, new GUIContent("Highlight Material"));
            EditorGUILayout.PropertyField(stepDescriptionText, new GUIContent("Step Description Text"));
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Progress UI",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(progressBar, new GUIContent("Progress Bar"));
            EditorGUILayout.PropertyField(progressText, new GUIContent("Progress Text"));
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Debug :",EditorStyles.whiteLabel);
            showDebugLogs = EditorGUILayout.Toggle("Enable Debug Logs", showDebugLogs);
            showGizmos = EditorGUILayout.Toggle("Visualize Gizmos", showGizmos);
            SnapFlowDEditorGizmos.ShowGizmos = showGizmos;
            GUILayout.Space(10);
        }
        
        private void DrawReorderableSteps()
        {
            if (DisassemblyProperty == null)
            {
                EditorGUILayout.HelpBox("Steps property not found.", MessageType.Warning);
                return;
            }

            reorderableSteps.DoLayoutList();
        }
        
        private void DrawFooter()
        {
            GUILayout.Space(10);
            GUI.backgroundColor = SnapFlowOrange;
            if (GUILayout.Button(new GUIContent("Add New Step"), GUILayout.Height(30)))
            {
                DisassemblyProperty.arraySize++;
                foldoutStates.Add(true);
                Log("New step added.");
            }
            GUI.backgroundColor = Color.white;
        }

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log("[SnapFlow Editor] " + message);
            }
        }

    }
[InitializeOnLoad]
public static class SnapFlowDEditorGizmos
{
    public static bool ShowGizmos = true;

    static SnapFlowDEditorGizmos()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!ShowGizmos) return;

        foreach (var manager in GameObject.FindObjectsOfType<AssemblyStepManager>())
        {
            var steps = manager.steps;
            if (steps == null || steps.Count == 0) return;
            

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var fromObj = step.objectToGrab;
                var toSnap = step.targetSnapZone;

                if (fromObj == null || toSnap == null)
                    continue;

                Vector3 fromPos = fromObj.transform.position;
                Vector3 toPos = toSnap.transform.position;
                Vector3 midPoint = (fromPos + toPos) / 2f + Vector3.up * 0.2f;

                Handles.color = Color.cyan;
                Handles.DrawLine(fromPos, toPos, 2f);

                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.white },
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                };
                Handles.Label(midPoint, $"Step {i + 1}", labelStyle);

                Material highlightMat = manager.HighlightMaterial;
                if (highlightMat != null)
                {
                    var matColor = highlightMat.color;
                    matColor.a = 0.3f;
                    Handles.color = matColor;

                    if (fromObj.TryGetComponent<Renderer>(out var fromRend))
                    {
                        Handles.DrawWireCube(fromRend.bounds.center, fromRend.bounds.size);
                    }

                    if (toSnap.TryGetComponent<Collider>(out var toCol))
                    {
                        Handles.DrawWireCube(toCol.bounds.center, toCol.bounds.size);
                    }
                }
            }
            
            manager.gameObject.name = "Snap flow - Disassembly";
            
        }
    }
}