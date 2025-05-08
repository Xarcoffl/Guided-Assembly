using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SnapFlow.Disassembly.Editor
{
    [CustomEditor(typeof(DisassemblyManager))]
    public class DisassemblyManagerEditor : UnityEditor.Editor
    {
        private static readonly Color SnapFlowBlue = new Color(0.2f, 0.5f, 0.8f);
        private static readonly Color SnapFlowOrange = new Color(1f, 0.6f, 0.2f);
        
        private SerializedProperty _disassemblyProperty;
        private SerializedProperty _stepCompleteSound;
        private SerializedProperty _highlightMaterial;
        private SerializedProperty _progressBar;
        private SerializedProperty _progressText;
        private SerializedProperty _stepDescriptionText;
        private SerializedProperty _onDisassemblyComplete;

        private ReorderableList _reorderableSteps;
        private readonly List<bool> _foldoutStates = new();

        private SerializedProperty _showDebugLogs;
        private SerializedProperty _showGizmos;
        private SerializedProperty _enableDisEvent;

        private void OnEnable()
        {
            _disassemblyProperty = serializedObject.FindProperty("steps");
            _stepCompleteSound = serializedObject.FindProperty("stepCompleteSound");
            _highlightMaterial = serializedObject.FindProperty("highlightMaterial");
            _progressBar = serializedObject.FindProperty("progressBar");
            _progressText = serializedObject.FindProperty("progressText");
            _stepDescriptionText = serializedObject.FindProperty("stepDescriptionText");
            _onDisassemblyComplete = serializedObject.FindProperty("onDisassemblyComplete");
            _showGizmos = serializedObject.FindProperty("Showgizmos");
            _showDebugLogs = serializedObject.FindProperty("showDebug");
            _enableDisEvent = serializedObject.FindProperty("EnableDEvent");
            
            
            _reorderableSteps = new ReorderableList(serializedObject, _disassemblyProperty, true, true, false, true);
            _reorderableSteps.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Disassembly Steps", EditorStyles.boldLabel);
                
                Rect clearButtonRect = new Rect(rect.xMax - 200, rect.y, 90, EditorGUIUtility.singleLineHeight);
                Rect autoDescButtonRect = new Rect(rect.xMax - 100, rect.y, 90, EditorGUIUtility.singleLineHeight);
                
                
                if (GUI.Button(clearButtonRect, "Reset"))
                {
                    if (EditorUtility.DisplayDialog("Confirm Clear", "Are you sure you want to remove all steps?", "Remove", "Cancel"))
                    {
                        _disassemblyProperty.ClearArray();
                        _foldoutStates.Clear();
                        /*Log("Steps cleared.");*/
                    }
                }
                
                if (GUI.Button(autoDescButtonRect, "Auto-Desc"))
                {
                    for (int i = 0; i < _disassemblyProperty.arraySize; i++)
                    {
                        var step = _disassemblyProperty.GetArrayElementAtIndex(i);
                        var grabObj = step.FindPropertyRelative("objectToRemove").objectReferenceValue;
                        var snapZone = step.FindPropertyRelative("sourceSnapZone").objectReferenceValue;
                        var desc = step.FindPropertyRelative("stepDescription");

                        string grabName = grabObj ? grabObj.name : "None";
                        string snapName = snapZone ? snapZone.name : "None";
                        desc.stringValue = $"Remove {grabName} in {snapName}";

                        /*Log($"Auto-desc for Step {i + 1}: {desc.stringValue}");*/
                    }
                }
            };

            _reorderableSteps.drawElementCallback = (rect, index, _, _) =>
            {
                SyncFoldoutList();
                var step = _disassemblyProperty.GetArrayElementAtIndex(index);
                var objectToRemove = step.FindPropertyRelative("objectToRemove");
                var snapZone = step.FindPropertyRelative("sourceSnapZone");

                string stepName = objectToRemove.objectReferenceValue ? $"Step {index + 1} : {objectToRemove.objectReferenceValue.name}" : $"Step {index + 1}";
                float indentOffset = 15f;
                Rect foldoutRect = new Rect(rect.x + indentOffset, rect.y, rect.width - indentOffset, EditorGUIUtility.singleLineHeight);
                _foldoutStates[index] = EditorGUI.Foldout(foldoutRect, _foldoutStates[index], stepName, true);
                if (_foldoutStates[index])
                {
                    EditorGUI.indentLevel++;
                    float yOffset = rect.y + EditorGUIUtility.singleLineHeight + 4;
                    float fullWidth = rect.width;
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
                        step.FindPropertyRelative("stepDescription"), new GUIContent("Step Description"));
                    yOffset += lineHeight * 6;
                    var unsnapEvents = step.FindPropertyRelative("onUnSnapEvents");
                    float snapHeight = EditorGUI.GetPropertyHeight(unsnapEvents, true);
                    EditorGUI.PropertyField(new Rect(rect.x, yOffset, fullWidth, snapHeight), unsnapEvents, new GUIContent("Un-Snap Events"), true);

                    EditorGUI.indentLevel--;
                }
            };



            _reorderableSteps.elementHeightCallback = (index) =>
            {
                SyncFoldoutList();
                if (!_foldoutStates[index]) return EditorGUIUtility.singleLineHeight + 10;

                var step = _disassemblyProperty.GetArrayElementAtIndex(index);
                float height = EditorGUIUtility.singleLineHeight * 4 + 30;
                
                
                height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("onUnSnapEvents"), true) + 10;
          
                height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("stepDescription"), true) + 10;

                var grabGo = step.FindPropertyRelative("objectToRemove").objectReferenceValue;
                var unsnapGo = step.FindPropertyRelative("sourceSnapZone").objectReferenceValue;

                if (!grabGo) height += EditorGUIUtility.singleLineHeight + 15;
                if (!unsnapGo) height += EditorGUIUtility.singleLineHeight + 25;
                
                height += (EditorGUIUtility.singleLineHeight) * 3;
                
                return height;
            };




        }
        private void SyncFoldoutList()
        {
            while (_foldoutStates.Count < _disassemblyProperty.arraySize) _foldoutStates.Add(true);
            while (_foldoutStates.Count > _disassemblyProperty.arraySize) _foldoutStates.RemoveAt(_foldoutStates.Count - 1);
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
                fontStyle = FontStyle.Bold, 
            };
            
            GUILayout.Label("SNAP FLOW", titleStyle);
            GUILayout.Label("Disassembly Editor", taglinestyle);
            GUILayout.Space(10);
        }
        
        private void DrawGeneralSettings()
        {
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sound Clips",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_stepCompleteSound, new GUIContent("Success Sound Clip"));
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Highlights and Description",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_highlightMaterial, new GUIContent("Highlight Material"));
            EditorGUILayout.PropertyField(_stepDescriptionText, new GUIContent("Step Description Text"));
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Progress UI",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_progressBar, new GUIContent("Progress Bar"));
            EditorGUILayout.PropertyField(_progressText, new GUIContent("Progress Step Count"));
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Debug :",EditorStyles.whiteLabel);
            EditorGUILayout.PropertyField(_showDebugLogs, new GUIContent("Debug Logs"));
            EditorGUILayout.PropertyField(_showGizmos, new GUIContent("Show Gizmos"));
            EditorGUILayout.PropertyField(_enableDisEvent, new GUIContent("Disassembly Events"));
            SnapFlowDEditorGizmos.ShowGizmos = _showGizmos.boolValue;
            GUILayout.Space(10);
            if (_enableDisEvent.boolValue)
            {
                
                EditorGUILayout.LabelField("Disassembly Event",EditorStyles.whiteLabel);
                EditorGUILayout.PropertyField(_onDisassemblyComplete, new GUIContent("On-Disassembly Complete"));
            }
            GUILayout.Space(40);
        }
        
        private void DrawReorderableSteps()
        {
            if (_disassemblyProperty == null)
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
                foreach (var manager in Object.FindObjectsOfType<DisassemblyManager>())
                {
                    manager.name = "Snap Flow - Disassembly";
                    break;
                }
                _disassemblyProperty.arraySize++;
                _foldoutStates.Add(true);
                Log("New step added.");
            }
            GUI.backgroundColor = Color.white;
        }

        private void Log(string message)
        {
            var dlogs = _showDebugLogs.boolValue;
            if (dlogs)
            {
                Debug.Log("[SnapFlow Editor] " + message);
            }
        }

    }
    [InitializeOnLoad]
    public static class SnapFlowDEditorGizmos
    {
        public static bool ShowGizmos = true;

        [Obsolete("Obsolete")]
        static SnapFlowDEditorGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [Obsolete("Obsolete")]
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!ShowGizmos) return;

            foreach (var manager in Object.FindObjectsOfType<DisassemblyManager>())
            {
                var steps = manager.steps;
                if (steps == null || steps.Count == 0) return;
            
            

                for (int i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    var fromObj = step.objectToRemove;
                    var toSnap = step.sourceSnapZone;

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

                    Material highlightMat = manager.highlightMaterial;
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
            
                
            
            }
        }
    }
}