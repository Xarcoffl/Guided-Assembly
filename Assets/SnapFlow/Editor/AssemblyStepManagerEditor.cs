using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using BNG;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

[CustomEditor(typeof(AssemblyStepManager))]
public class SnapFlowEditor : Editor
{
    private static readonly Color SnapFlowBlue = new Color(0.2f, 0.5f, 0.8f);
    private static readonly Color SnapFlowOrange = new Color(1f, 0.6f, 0.2f);

    private SerializedProperty stepsProperty;
    private SerializedProperty stepCompleteSound;
    private SerializedProperty errorSound;
    private SerializedProperty HighlightMaterial;
    private SerializedProperty progressBar;
    private SerializedProperty progressText;
    private SerializedProperty stepDescriptionText;

    private ReorderableList reorderableSteps;
    private List<bool> foldoutStates = new();

    private bool showDebugLogs = false;
    private bool showGizmos = false;

    private void OnEnable()
    {
      
        stepsProperty = serializedObject.FindProperty("steps");
        stepCompleteSound = serializedObject.FindProperty("stepCompleteSound");
        HighlightMaterial = serializedObject.FindProperty("HighlightMaterial");
        errorSound = serializedObject.FindProperty("errorSound");
        progressBar = serializedObject.FindProperty("progressBar");
        progressText = serializedObject.FindProperty("progressText");
        stepDescriptionText = serializedObject.FindProperty("stepDescriptionText");

        reorderableSteps = new ReorderableList(serializedObject, stepsProperty, true, true, false, true);
        reorderableSteps.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Assembly Steps");
            

            Rect clearButtonRect = new Rect(rect.xMax - 200, rect.y, 90, EditorGUIUtility.singleLineHeight);
            Rect autoDescButtonRect = new Rect(rect.xMax - 100, rect.y, 90, EditorGUIUtility.singleLineHeight);

            if (GUI.Button(clearButtonRect, "Reset"))
            {
                if (EditorUtility.DisplayDialog("Confirm Clear", "Are you sure you want to remove all steps?", "Yes", "Cancel"))
                {
                    stepsProperty.ClearArray();
                    foldoutStates.Clear();
                    Log("Steps cleared.");
                }
            }

            if (GUI.Button(autoDescButtonRect, "Auto-Desc"))
            {
                for (int i = 0; i < stepsProperty.arraySize; i++)
                {
                    var step = stepsProperty.GetArrayElementAtIndex(i);
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

        reorderableSteps.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SyncFoldoutList();
            var step = stepsProperty.GetArrayElementAtIndex(index);
            var grabObject = step.FindPropertyRelative("objectToGrab");
            var snapZone = step.FindPropertyRelative("targetSnapZone");

            string stepName = grabObject.objectReferenceValue ? $"Step {index + 1} : {grabObject.objectReferenceValue.name}" : $"Step {index + 1}";

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
                yOffset += snapHeight + 10;

                

                EditorGUI.indentLevel--;
            }
        };

        reorderableSteps.elementHeightCallback = index =>
        {
            SyncFoldoutList();
            if (!foldoutStates[index]) return EditorGUIUtility.singleLineHeight + 10;

            var step = stepsProperty.GetArrayElementAtIndex(index);
            float height = EditorGUIUtility.singleLineHeight * 4 + 30;

            height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("onGrabEvents"), true) + 10;
            height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("onSnapEvents"), true) + 10;
            height += EditorGUI.GetPropertyHeight(step.FindPropertyRelative("StepDescription"), true) + 10;

            var grabGO = step.FindPropertyRelative("objectToGrab").objectReferenceValue;
            var snapGO = step.FindPropertyRelative("targetSnapZone").objectReferenceValue;

            if (!grabGO) height += EditorGUIUtility.singleLineHeight + 15;
            if (!snapGO) height += EditorGUIUtility.singleLineHeight + 25;
            
            height += (EditorGUIUtility.singleLineHeight) * 3;

            return height;
        };
    }

    private void SyncFoldoutList()
    {
        while (foldoutStates.Count < stepsProperty.arraySize) foldoutStates.Add(true);
        while (foldoutStates.Count > stepsProperty.arraySize) foldoutStates.RemoveAt(foldoutStates.Count - 1);
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
        GUILayout.Label("SNAP FLOW", titleStyle);
        GUILayout.Space(10);
    }

    private void DrawGeneralSettings()
    {
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stepCompleteSound, new GUIContent("Step Complete Sound"));
        EditorGUILayout.PropertyField(errorSound, new GUIContent("Error Sound"));
        EditorGUILayout.PropertyField(HighlightMaterial, new GUIContent("Highlight Material"));
        EditorGUILayout.PropertyField(progressBar, new GUIContent("Progress Bar"));
        EditorGUILayout.PropertyField(progressText, new GUIContent("Progress Text"));
        EditorGUILayout.PropertyField(stepDescriptionText, new GUIContent("Step Description Text"));

        GUILayout.Space(5);
        showDebugLogs = EditorGUILayout.Toggle("Enable Debug Logs", showDebugLogs);
        showGizmos = EditorGUILayout.Toggle("Visualize Gizmos", showGizmos);
        SnapFlowEditorGizmos.ShowGizmos = showGizmos;

        GUILayout.Space(10);
    }

    private void DrawReorderableSteps()
    {
        if (stepsProperty == null)
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
            stepsProperty.arraySize++;
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
public static class SnapFlowEditorGizmos
{
    public static bool ShowGizmos = true;

    static SnapFlowEditorGizmos()
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
        }
    }
}