using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using BNG;
using PlasticGui;

[CustomEditor(typeof(AssemblyStepManager))]
public class SnapFlowEditor : Editor
{
    private static readonly Color SnapFlowBlue = new Color(0.2f, 0.5f, 0.8f);
    private static readonly Color SnapFlowOrange = new Color(1f, 0.6f, 0.2f);
    private Texture2D snapFlowLogo;

    private SerializedProperty stepsProperty;
    private SerializedProperty stepCompleteSound;
    private SerializedProperty errorSound;
    private SerializedProperty HighlightMaterial;
    private SerializedProperty progressBar;
    private SerializedProperty progressText;

    private void OnEnable()
    {
        snapFlowLogo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/snapflow.png", typeof(Texture2D));
        stepsProperty = serializedObject.FindProperty("steps");
        stepCompleteSound = serializedObject.FindProperty("stepCompleteSound");
        HighlightMaterial = serializedObject.FindProperty("HighlightMaterial");
        errorSound = serializedObject.FindProperty("errorSound");
        progressBar = serializedObject.FindProperty("progressBar");
        progressText = serializedObject.FindProperty("progressText");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawHeader();
        DrawGeneralSettings();
        DrawStepList();
        DrawFooter();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        GUILayout.Space(10);
        if (snapFlowLogo)
        {
            
            GUILayout.Label(snapFlowLogo, GUILayout.Height(80));
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = SnapFlowBlue;
            GUILayout.Label("SnapFlow - Assembly Step Manager", titleStyle);
        }
        else
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = SnapFlowBlue;
            GUILayout.Label("SnapFlow - Assembly Step Manager", titleStyle);
        }
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
        GUILayout.Space(10);
    }

    private void DrawStepList()
    {
        if (stepsProperty == null || stepsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No steps defined. Add steps to configure the assembly process.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < stepsProperty.arraySize; i++)
            {
                SerializedProperty step = stepsProperty.GetArrayElementAtIndex(i);
                DrawStep(step, i);
            }
        }
    }

    private void DrawStep(SerializedProperty step, int index)
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 5, 5) };
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Step " + (index + 1), EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(step.FindPropertyRelative("objectToGrab"), new GUIContent("Object to Grab", "The object that must be grabbed in this step"));
        EditorGUILayout.PropertyField(step.FindPropertyRelative("targetSnapZone"), new GUIContent("Snap Zone", "Where the object should be snapped"));
        EditorGUILayout.PropertyField(step.FindPropertyRelative("onGrabEvents"), new GUIContent("Grab Events","List of Events that will be triggered while grabbing this step's Grabbable object"));
        EditorGUILayout.PropertyField(step.FindPropertyRelative("onSnapEvents"), new GUIContent("Snap Events","List of Events that will be triggered while snapping this step's Grabbable object"));
        
        
        if (GUILayout.Button("Remove Step", GUILayout.Height(25)))
        {
            stepsProperty.DeleteArrayElementAtIndex(index);
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(5);
    }

    private void DrawFooter()
    {
        GUILayout.Space(10);
        GUI.backgroundColor = SnapFlowOrange;
        if (GUILayout.Button("Add New Step", GUILayout.Height(30)))
        {
            stepsProperty.arraySize++;
        }
        GUI.backgroundColor = Color.red;
    }
}