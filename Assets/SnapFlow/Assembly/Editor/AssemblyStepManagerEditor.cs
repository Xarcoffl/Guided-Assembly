using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SnapFlow.Assembly.Editor
{
    [CustomEditor(typeof(AssemblyStepManager))]
    public class SnapFlowEditor : UnityEditor.Editor
    {
        private static readonly Color SnapFlowBlue = new Color(0.2f, 0.5f, 0.8f);
        private static readonly Color SnapFlowOrange = new Color(1f, 0.6f, 0.2f);

        private SerializedProperty _stepsProperty;
        private SerializedProperty _stepCompleteSound;
        private SerializedProperty _errorSound;
        private SerializedProperty _highlightMaterial;
        private SerializedProperty _progressBar;
        private SerializedProperty _progressText;
        private SerializedProperty _stepDescriptionText;
        private SerializedProperty _onAssemblyCompleted;
        private SerializedProperty _sopMode;
      
      
        

        private ReorderableList _reorderableSteps;
        private List<bool> _foldoutStates = new();

        private SerializedProperty _showDebugLogs ;
        private SerializedProperty _showGizmos ;
        private SerializedProperty _assemblyEvents;

        private void OnEnable()
        {
      
            _stepsProperty = serializedObject.FindProperty("steps");
            _stepCompleteSound = serializedObject.FindProperty("stepCompleteSound");
            _highlightMaterial = serializedObject.FindProperty("HighlightMaterial");
            _errorSound = serializedObject.FindProperty("errorSound");
            _progressBar = serializedObject.FindProperty("progressBar");
            _progressText = serializedObject.FindProperty("progressText");
            _stepDescriptionText = serializedObject.FindProperty("stepDescriptionText");
            _onAssemblyCompleted = serializedObject.FindProperty("onAssemblyComplete");
            _showGizmos = serializedObject.FindProperty("showgizmos");
            _showDebugLogs = serializedObject.FindProperty("debuglog");
            _assemblyEvents = serializedObject.FindProperty("AssemblyEvents");
            _sopMode = serializedObject.FindProperty("sopMode");

            _reorderableSteps = new ReorderableList(serializedObject, _stepsProperty, true, true, false, true);
            _reorderableSteps.drawHeaderCallback = rect =>
            {
            
                EditorGUI.LabelField(rect, "Assembly Steps", EditorStyles.boldLabel);
            

                Rect clearButtonRect = new Rect(rect.xMax - 200, rect.y, 90, EditorGUIUtility.singleLineHeight);
                Rect autoDescButtonRect = new Rect(rect.xMax - 100, rect.y, 90, EditorGUIUtility.singleLineHeight);

                if (GUI.Button(clearButtonRect, "Reset"))
                {
                    if (EditorUtility.DisplayDialog("Confirm Clearing All Steps", "Are you sure you want to remove all steps?", "Remove", "Cancel"))
                    {
                        _stepsProperty.ClearArray();
                        _foldoutStates.Clear();
                        Log("Steps cleared.");
                    }
                }

                if (GUI.Button(autoDescButtonRect, "Auto-Desc"))
                {
                    for (int i = 0; i < _stepsProperty.arraySize; i++)
                    {
                        var step = _stepsProperty.GetArrayElementAtIndex(i);
                        var grabObj = step.FindPropertyRelative("objectToGrab").objectReferenceValue;
                        var snapZone = step.FindPropertyRelative("targetSnapZone").objectReferenceValue;
                        var desc = step.FindPropertyRelative("StepDescription");

                        string grabName = grabObj ? grabObj.name : "None";
                        string snapName = snapZone ? snapZone.name : "None";
                        desc.stringValue = $"Place {grabName} in {snapName}";

                        Log($"Auto-desc for Step {i + 1}: {desc.stringValue}");
                    }
                }
            };

            _reorderableSteps.drawElementCallback = (rect, index, _, _) =>
            {
                SyncFoldoutList();
                var step = _stepsProperty.GetArrayElementAtIndex(index);
                var grabObject = step.FindPropertyRelative("objectToGrab");
                var snapZone = step.FindPropertyRelative("targetSnapZone");

                string stepName = grabObject.objectReferenceValue ? $"Step {index + 1} : {grabObject.objectReferenceValue.name}" : $"Step {index + 1}";

                float indentOffset = 15f;
                Rect foldoutRect = new Rect(rect.x + indentOffset, rect.y, rect.width - indentOffset, EditorGUIUtility.singleLineHeight);
                _foldoutStates[index] = EditorGUI.Foldout(foldoutRect, _foldoutStates[index], stepName, true);

                if (_foldoutStates[index])
                {
                    EditorGUI.indentLevel++;
                    float yOffset = rect.y + EditorGUIUtility.singleLineHeight + 4;
                    float fullWidth = rect.width;
                    float lineHeight = EditorGUIUtility.singleLineHeight;

                    var grabObj = grabObject.objectReferenceValue;
                    var snapObj = snapZone.objectReferenceValue;

                    if (!grabObj)
                    {
                        EditorGUI.HelpBox(new Rect(rect.x, yOffset, fullWidth, lineHeight + 5), "Missing: Object to Grab", MessageType.Warning);
                        yOffset += lineHeight + 10;
                    }

                    if (!snapObj)
                    {
                        EditorGUI.HelpBox(new Rect(rect.x, yOffset, fullWidth, lineHeight + 5), "Missing: Snap Zone", MessageType.Warning);
                        yOffset += lineHeight + 15;
                    }

                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, lineHeight), grabObject, new GUIContent("Object to Grab"));
                    yOffset += lineHeight + 10;

                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, lineHeight), snapZone, new GUIContent("Snap Zone"));
                    yOffset += lineHeight + 10;

                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, lineHeight*5),
                        step.FindPropertyRelative("StepDescription"), new GUIContent("Step Description"));
                    yOffset += lineHeight * 6;

                    var grabEvents = step.FindPropertyRelative("onGrabEvents");
                    float grabHeight = EditorGUI.GetPropertyHeight(grabEvents, true);
                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, grabHeight), grabEvents, new GUIContent("Grab Events"), true);
                    yOffset += grabHeight + 4;

                    var snapEvents = step.FindPropertyRelative("onSnapEvents");
                    float snapHeight = EditorGUI.GetPropertyHeight(snapEvents, true);
                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, snapHeight), snapEvents, new GUIContent("Snap Events"), true);
                   

                    EditorGUI.indentLevel--;
                }
            };
        
        
            _reorderableSteps.elementHeightCallback = index =>
            {
                SyncFoldoutList();
                if (!_foldoutStates[index]) return EditorGUIUtility.singleLineHeight + 10;

                var step = _stepsProperty.GetArrayElementAtIndex(index);
                float height = EditorGUIUtility.singleLineHeight * 4 + 30;

                height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("onGrabEvents"), true) + 10;
                height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("onSnapEvents"), true) + 10;
                height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("StepDescription"), true) + 10;

                var grabGo = step.FindPropertyRelative("objectToGrab").objectReferenceValue;
                var snapGo = step.FindPropertyRelative("targetSnapZone").objectReferenceValue;

                if (!grabGo) height += EditorGUIUtility.singleLineHeight + 15;
                if (!snapGo) height += EditorGUIUtility.singleLineHeight + 25;
            
                height += (EditorGUIUtility.singleLineHeight) * 3;

                return height;
            };
        }

        private void SyncFoldoutList()
        {
            while (_foldoutStates.Count < _stepsProperty.arraySize) _foldoutStates.Add(true);
            while (_foldoutStates.Count > _stepsProperty.arraySize) _foldoutStates.RemoveAt(_foldoutStates.Count - 1);
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
            GUILayout.Button("SNAP FLOW", titleStyle);
            GUILayout.Label("Assembly Editor", taglinestyle);
            GUILayout.Space(10);
        }

        private void DrawGeneralSettings()
        {
      
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_sopMode);
            EditorGUILayout.LabelField("Sound Clips",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_stepCompleteSound, new GUIContent("Success Soundclip"));
            EditorGUILayout.PropertyField(_errorSound, new GUIContent("Error Soundclip"));
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Highlights and Description",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_highlightMaterial, new GUIContent("Highlight Material"));
            EditorGUILayout.PropertyField(_stepDescriptionText, new GUIContent("Step Description Text"));
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Progress UI",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_progressBar, new GUIContent("Progress Bar"));
            EditorGUILayout.PropertyField(_progressText, new GUIContent("Progress Text"));
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Debug :",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_showDebugLogs, new GUIContent("Debug Logs"));
            EditorGUILayout.PropertyField(_showGizmos, new GUIContent("Show Gizmos"));
            SnapFlowEditorGizmos.ShowGizmos = _showGizmos.boolValue;
            EditorGUILayout.PropertyField(_assemblyEvents, new GUIContent("Assembly Events"));
            GUILayout.Space(10);
            if (_assemblyEvents.boolValue)
            {
                EditorGUILayout.LabelField("Assembly Event",EditorStyles.whiteLabel);
                EditorGUILayout.PropertyField(_onAssemblyCompleted, new GUIContent("On-Assembly Complete"));
            }
            GUILayout.Space(10);
        }

        private void DrawReorderableSteps()
        {
            if (_stepsProperty == null)
            {
                EditorGUILayout.HelpBox("Steps property not found.", MessageType.Warning);
                return;
            }

            _reorderableSteps.DoLayoutList();
        }

        private void DrawFooter()
        {
            GUILayout.Space(10);
            GUI.backgroundColor = SnapFlowOrange;
            if (GUILayout.Button(new GUIContent("Add New Step"), GUILayout.Height(30)))
            {
                foreach (var manager in Object.FindObjectsOfType<AssemblyStepManager>())
                {
                    manager.name = "Snap Flow - Assembly";
                    break;
                }
              
                _stepsProperty.arraySize++;
                _foldoutStates.Add(true);
                Log("New step added.");
            }
            GUI.backgroundColor = Color.white;
        }

        private void Log(string message)
        {
            if (_showDebugLogs.boolValue)
            {
                Debug.Log("[SnapFlow Editor] " + message);
            }
        }
    }

    [InitializeOnLoad]
    public static class SnapFlowEditorGizmos
    {
        public static bool ShowGizmos = true;

        [Obsolete("Obsolete")]
        static SnapFlowEditorGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [Obsolete("Obsolete")]
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!ShowGizmos) return;

            foreach (var manager in Object.FindObjectsOfType<AssemblyStepManager>())
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
                        var matColor = Color.cyan;
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
            
                
            
            }
        }
    }
}