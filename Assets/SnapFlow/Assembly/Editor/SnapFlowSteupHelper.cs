using UnityEditor;
using UnityEngine;
using BNG;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;

[CustomEditor(typeof(GameObject))]
public class SnapFlowSetupHelper : Editor
{
    private AssemblyStepManager assemblyManager;
    private int selectedStepIndex = 0;
    private AssignmentType assignType = AssignmentType.ObjectToGrab;
    private List<string> stepLabels = new();
    private List<AssemblyStepManager.AssemblyStep> steps;
    private Vector2 scrollPos;

    private enum AssignmentType { ObjectToGrab, TargetSnapZone }

    public override void OnInspectorGUI()
    {
        GameObject go = (GameObject)target;
        if (Application.isPlaying) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("SnapFlow Setup Helper", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawSetupButtons(go);

        EditorGUILayout.Space();
        DrawManagerSelection(go);

        EditorGUILayout.EndScrollView();
    }

    private void DrawSetupButtons(GameObject go)
    {
        var grab = go.GetComponent<Grabbable>();
        var snap = go.GetComponent<SnapZone>();
        var grabevent = go.GetComponent<GrabbableUnityEvents>();

        if (snap == null)
        {
            if (grab == null)
            {
                if (GUILayout.Button("Setup Grabbable"))
                {
                    Undo.AddComponent<Grabbable>(go);
                    Undo.AddComponent<BoxCollider>(go);
                    Undo.AddComponent<Rigidbody>(go);
                    Undo.AddComponent<GrabbableUnityEvents>(go);
                    
                    var g = go.GetComponent<Grabbable>();
                    g.GrabButton = GrabButton.Grip;
                    g.Grabtype = HoldType.HoldDown;
                    g.GrabMechanic = GrabType.Snap;
                    g.GrabPhysics = GrabPhysics.FixedJoint;
                }
            }
            else
            {
                if (GUILayout.Button("Remove Grabbable Setup"))
                {
                    Undo.DestroyObjectImmediate(go.GetComponent<GrabbableUnityEvents>());
                    Undo.DestroyObjectImmediate(go.GetComponent<Grabbable>());
                    if (go.GetComponent<Rigidbody>()) Undo.DestroyObjectImmediate(go.GetComponent<Rigidbody>());
                    if (go.GetComponent<BoxCollider>()) Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());
                }
            }
        }

        if (grabevent == null)
        {
            if (snap == null)
            {
                if (GUILayout.Button("Setup SnapZone"))
                {
                    Undo.AddComponent<SnapZone>(go);
                    if (go.GetComponent<Grabbable>() == null) Undo.AddComponent<Grabbable>(go);
                    if (go.GetComponent<BoxCollider>() == null) Undo.AddComponent<BoxCollider>(go);
                    if (go.GetComponent<Rigidbody>() == null) Undo.AddComponent<Rigidbody>(go);
                    Undo.AddComponent<GrabbablesInTrigger>(go);
                    Undo.AddComponent<GrabAction>(go);

                    var col = go.GetComponent<BoxCollider>();
                    col.isTrigger = true;

                    var g = go.GetComponent<Grabbable>();
                    g.GrabButton = GrabButton.Grip;
                    g.Grabtype = HoldType.HoldDown;
                    g.GrabMechanic = GrabType.Snap;
                    g.GrabPhysics = GrabPhysics.FixedJoint;

                    go.GetComponent<Rigidbody>().isKinematic = true;
                }
            }
            else
            {
                if (GUILayout.Button("Remove SnapZone Setup"))
                {
                    Undo.DestroyObjectImmediate(go.GetComponent<SnapZone>());
                    if (go.GetComponent<GrabbablesInTrigger>()) Undo.DestroyObjectImmediate(go.GetComponent<GrabbablesInTrigger>());
                    if (go.GetComponent<GrabAction>()) Undo.DestroyObjectImmediate(go.GetComponent<GrabAction>());
                    Undo.DestroyObjectImmediate(go.GetComponent<Grabbable>());
                    if (go.GetComponent<Rigidbody>()) Undo.DestroyObjectImmediate(go.GetComponent<Rigidbody>());
                    if (go.GetComponent<BoxCollider>()) Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());
                }
            }
        }
    }

    private void DrawManagerSelection(GameObject go)
    {
        var managers = FindObjectsOfType<AssemblyStepManager>();
        if (managers.Length == 0)
        {
            EditorGUILayout.HelpBox("No AssemblyStepManager found in scene.", MessageType.Warning);
            return;
        }

        string[] managerNames = managers.Select(m => m.name).ToArray();
        int currentManagerIndex = assemblyManager != null ? managers.ToList().IndexOf(assemblyManager) : 0;
        int newManagerIndex = EditorGUILayout.Popup("Assembly Manager", currentManagerIndex, managerNames);

        if (assemblyManager != managers[newManagerIndex])
        {
            assemblyManager = managers[newManagerIndex];
            RefreshStepList();
        }

        if (steps == null || steps.Count == 0)
        {
            EditorGUILayout.HelpBox("No steps defined in selected Assembly Manager.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step Assignment", EditorStyles.boldLabel);

        selectedStepIndex = EditorGUILayout.Popup("Select Step", selectedStepIndex, stepLabels.ToArray());
        assignType = (AssignmentType)EditorGUILayout.EnumPopup("Assign As", assignType);

        if (GUILayout.Button("Assign"))
        {
            AssignSelectedStep(go);
        }
    }

    private void AssignSelectedStep(GameObject go)
    {
        var step = steps[selectedStepIndex];

        if (assignType == AssignmentType.ObjectToGrab)
        {
            Undo.RecordObject(assemblyManager, "Assign ObjectToGrab");
            step.objectToGrab = go.GetComponent<Grabbable>();
        }
        else
        {
            Undo.RecordObject(assemblyManager, "Assign TargetSnapZone");
            step.targetSnapZone = go.GetComponent<SnapZone>();
        }

        EditorUtility.SetDirty(assemblyManager);
        Debug.Log($"Assigned '{go.name}' to Step {selectedStepIndex + 1} as {assignType}.");
    }

    private void RefreshStepList()
    {
        steps = assemblyManager.steps;
        stepLabels = steps.Select((s, i) => $"Step {i + 1}: {s.StepDescription}").ToList();
    }
}