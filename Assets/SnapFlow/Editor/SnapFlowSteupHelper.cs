using UnityEditor;
using UnityEngine;
using BNG;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;

[CustomEditor(typeof(GameObject))]
public class SnapFlowSteupHelper : Editor
{
    private AssemblyStepManager assemblyManager;
    private int selectedStepIndex = 0;
    private AssignmentType assignType = AssignmentType.ObjectToGrab;
    private List<string> stepLabels = new();
    private List<AssemblyStepManager.AssemblyStep> steps;

    private enum AssignmentType { ObjectToGrab, TargetSnapZone }

    public override void OnInspectorGUI()
    {
        GameObject go = (GameObject)target;

        if (Application.isPlaying) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("SnapFlow Setup Helper", EditorStyles.boldLabel);
     

        // Grabbable setup/remove toggle

        var grab = go.GetComponent<Grabbable>();
        var snap = go.GetComponent<SnapZone>();
        var grabevent = go.GetComponent<GrabbableUnityEvents>();

        if (snap == null)
        {
            if (grab == null)
            {
                if (GUILayout.Button("Setup Grabbable"))
                {
                    go.AddComponent<Grabbable>();
                    go.AddComponent<BoxCollider>();
                    go.AddComponent<Rigidbody>();
                    go.AddComponent<GrabbableUnityEvents>();
                    go.GetComponent<Grabbable>().GrabButton = GrabButton.Grip;
                    go.GetComponent<Grabbable>().Grabtype = HoldType.HoldDown;
                    go.GetComponent<Grabbable>().GrabMechanic = GrabType.Snap;
                    go.GetComponent<Grabbable>().GrabPhysics = GrabPhysics.FixedJoint;
                }
            }
            else
            {
                if (GUILayout.Button("Remove Grabbable Setup"))
                {
                    DestroyImmediate(go.GetComponent<GrabbableUnityEvents>());
                    DestroyImmediate(go.GetComponent<Grabbable>());
                   if (go.GetComponent<Rigidbody>()) DestroyImmediate(go.GetComponent<Rigidbody>());
                    if (go.GetComponent<BoxCollider>()) DestroyImmediate(go.GetComponent<BoxCollider>());
                }
            }
        }
        
        

        // SnapZone setup/remove toggle
        if (grabevent == null)
        {
            if (snap == null)
            {
                if (GUILayout.Button("Setup SnapZone"))
                {
                    go.AddComponent<SnapZone>();
                    if (go.GetComponent<Grabbable>() == null) go.AddComponent<Grabbable>();
                    if (go.GetComponent<BoxCollider>() == null) go.AddComponent<BoxCollider>();
                    if (go.GetComponent<Rigidbody>() == null) go.AddComponent<Rigidbody>();
                    go.AddComponent<GrabbablesInTrigger>();
                    go.AddComponent<GrabAction>();
                    go.GetComponent<BoxCollider>().isTrigger = true;
                    go.GetComponent<Grabbable>().GrabButton = GrabButton.Grip;
                    go.GetComponent<Grabbable>().Grabtype = HoldType.HoldDown;
                    go.GetComponent<Grabbable>().GrabMechanic = GrabType.Snap;
                    go.GetComponent<Grabbable>().GrabPhysics = GrabPhysics.FixedJoint;
                    go.GetComponent<SnapZone>().CanRemoveItem = false;
                    go.GetComponent<Rigidbody>().isKinematic = true;
                    
                }
            }
            else
            {
                if (GUILayout.Button("Remove SnapZone Setup"))
                {
                    DestroyImmediate(go.GetComponent<SnapZone>());
                    if (go.GetComponent<GrabbablesInTrigger>()) DestroyImmediate(go.GetComponent<GrabbablesInTrigger>());
                    if (go.GetComponent<GrabAction>()) DestroyImmediate(go.GetComponent<GrabAction>());
                    DestroyImmediate(go.GetComponent<Grabbable>());
                    if (go.GetComponent<Rigidbody>()) DestroyImmediate(go.GetComponent<Rigidbody>());
                    if (go.GetComponent<BoxCollider>()) DestroyImmediate(go.GetComponent<BoxCollider>());
                }
            }
        }
        
        
        

        EditorGUILayout.Space();

        // Find all managers
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

        selectedStepIndex = EditorGUILayout.Popup("Select Step", selectedStepIndex, stepLabels.ToArray());
        assignType = (AssignmentType)EditorGUILayout.EnumPopup("Assign As", assignType);
        
        
        

        if (GUILayout.Button("Assign"))
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
            Debug.Log($"Assigned '{go.name}' to Step {selectedStepIndex} as {assignType}.");
        }
        
    }
    

    private void RefreshStepList()
    {
        steps = assemblyManager.steps;
        stepLabels = steps.Select((s, i) => $"Step {i}: {s.StepDescription}").ToList();
    }
}
