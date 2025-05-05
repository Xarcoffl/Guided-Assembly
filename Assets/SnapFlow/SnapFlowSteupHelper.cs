using UnityEditor;
using UnityEngine;
using BNG;
using System.Linq;
using System.Collections.Generic;
using SnapFlow.Disassembly;

[CustomEditor(typeof(GameObject))]
public class SnapFlowSetupHelper : Editor
{
    private enum ManagerType { Assembly, Disassembly }
    private enum AssignmentType { ObjectToGrab, TargetSnapZone, objectToRemove, sourceSnapZone }

    private ManagerType selectedManagerType = ManagerType.Assembly;
    private AssignmentType assignType = AssignmentType.ObjectToGrab;

    private AssemblyStepManager assemblyManager;
    private DisassemblyManager disassemblyManager;

    private int selectedStepIndex = 0;
    private List<string> stepLabels = new();
    private Vector2 scrollPos;

    public override void OnInspectorGUI()
    {
        GameObject go = (GameObject)target;

        if (go.transform.name == "Snap Flow - Assembly" || go.transform.name == "Snap Flow - Disassembly")
        {
            return;
        }
        if (Application.isPlaying) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Snap Flow Setup Helper", EditorStyles.boldLabel);

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

        if (grabevent == null && snap == null)
        {
            if (GUILayout.Button("Setup SnapZone"))
            {
                Undo.AddComponent<SnapZone>(go);
                if (!go.GetComponent<Grabbable>()) Undo.AddComponent<Grabbable>(go);
                if (!go.GetComponent<BoxCollider>()) Undo.AddComponent<BoxCollider>(go);
                if (!go.GetComponent<Rigidbody>()) Undo.AddComponent<Rigidbody>(go);
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
        else if (snap != null)
        {
            if (GUILayout.Button("Remove SnapZone Setup"))
            {
                Undo.DestroyObjectImmediate(go.GetComponent<SnapZone>());
                if (go.GetComponent<GrabbablesInTrigger>()) Undo.DestroyObjectImmediate(go.GetComponent<GrabbablesInTrigger>());
                if (go.GetComponent<GrabAction>()) Undo.DestroyObjectImmediate(go.GetComponent<GrabAction>());
                if (go.GetComponent<Grabbable>()) Undo.DestroyObjectImmediate(go.GetComponent<Grabbable>());
                if (go.GetComponent<Rigidbody>()) Undo.DestroyObjectImmediate(go.GetComponent<Rigidbody>());
                if (go.GetComponent<BoxCollider>()) Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());
            }
        }
    }

    private void DrawManagerSelection(GameObject go)
    {
        var assemblyManagers = FindObjectsOfType<AssemblyStepManager>();
        var disassemblyManagers = FindObjectsOfType<DisassemblyManager>();

        if (assemblyManagers.Length == 0 && disassemblyManagers.Length == 0)
        {
            EditorGUILayout.HelpBox("No SnapFlow Managers found in scene.", MessageType.Warning);
            return;
        }

        // Manager type toggle
        selectedManagerType = (ManagerType)EditorGUILayout.EnumPopup("Manager Type", selectedManagerType);

        if (selectedManagerType == ManagerType.Assembly)
        {
            if (assemblyManagers.Length == 0)
            {
                EditorGUILayout.HelpBox("No Assembly Managers found.", MessageType.Info);
                return;
            }

            string[] managerNames = assemblyManagers.Select(m => m.name).ToArray();
            int currentIndex = assemblyManager != null ? assemblyManagers.ToList().IndexOf(assemblyManager) : 0;
            int newIndex = EditorGUILayout.Popup("Assembly Manager", currentIndex, managerNames);

            if (assemblyManager != assemblyManagers[newIndex])
            {
                assemblyManager = assemblyManagers[newIndex];
                RefreshAssemblySteps();
            }

            DrawStepAssignmentUI(go, assemblyManager?.steps?.Count ?? 0);
        }
        else
        {
            if (disassemblyManagers.Length == 0)
            {
                EditorGUILayout.HelpBox("No Disassembly Managers found.", MessageType.Info);
                return;
            }

            string[] managerNames = disassemblyManagers.Select(m => m.name).ToArray();
            int currentIndex = disassemblyManager != null ? disassemblyManagers.ToList().IndexOf(disassemblyManager) : 0;
            int newIndex = EditorGUILayout.Popup("Disassembly Manager", currentIndex, managerNames);

            if (disassemblyManager != disassemblyManagers[newIndex])
            {
                disassemblyManager = disassemblyManagers[newIndex];
                RefreshDisassemblySteps();
            }

            DrawStepAssignmentUI(go, disassemblyManager?.steps?.Count ?? 0);
        }
    }

    private void DrawStepAssignmentUI(GameObject go, int stepCount)
    {
        if (stepCount == 0)
        {
            EditorGUILayout.HelpBox("No steps defined in selected manager.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step Assignment", EditorStyles.boldLabel);

        selectedStepIndex = EditorGUILayout.Popup("Select Step", selectedStepIndex, stepLabels.ToArray());

        string[] options = selectedManagerType == ManagerType.Assembly
            ? new[] { "ObjectToGrab", "TargetSnapZone" }
            : new[] { "objectToRemove", "sourceSnapZone" };

        assignType = (AssignmentType)EditorGUILayout.Popup("Assign As", (int)assignType, options);

        if (GUILayout.Button("Assign"))
        {
            AssignSelectedStep(go);
        }
    }

    private void AssignSelectedStep(GameObject go)
    {
        if (assignType == AssignmentType.ObjectToGrab && go.GetComponent<Grabbable>() == null)
        {
            Debug.LogWarning("Missing Grabbable component.");
            return;
        }
        if ((assignType == AssignmentType.TargetSnapZone || assignType == AssignmentType.sourceSnapZone) && go.GetComponent<SnapZone>() == null)
        {
            Debug.LogWarning("Missing SnapZone component.");
            return;
        }

        if (selectedManagerType == ManagerType.Assembly)
        {
            Undo.RecordObject(assemblyManager, "Assign Step Object");
            var step = assemblyManager.steps[selectedStepIndex];

            if (assignType == AssignmentType.ObjectToGrab)
                step.objectToGrab = go.GetComponent<Grabbable>();
            else
                step.targetSnapZone = go.GetComponent<SnapZone>();

            EditorUtility.SetDirty(assemblyManager);
        }
        else
        {
            Undo.RecordObject(disassemblyManager, "Assign Step Object");
            var step = disassemblyManager.steps[selectedStepIndex];

            if (assignType == AssignmentType.objectToRemove)
                step.objectToRemove = go.GetComponent<Grabbable>();
            else
                step.sourceSnapZone = go.GetComponent<SnapZone>();

            EditorUtility.SetDirty(disassemblyManager);
        }

        EditorGUIUtility.PingObject(go);
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.Frame(go.GetComponent<Renderer>()?.bounds ?? new Bounds(go.transform.position, Vector3.one), false);
        }

        Debug.Log($"Assigned '{go.name}' to {selectedManagerType} Step {selectedStepIndex + 1} as {assignType}.");
    }

    private void RefreshAssemblySteps()
    {
        var steps = assemblyManager.steps;
        stepLabels = steps.Select((s, i) => $"Step {i + 1}: {s.StepDescription}").ToList();
    }

    private void RefreshDisassemblySteps()
    {
        var steps = disassemblyManager.steps;
        stepLabels = steps.Select((s, i) => $"Step {i + 1}: {s.stepDescription}").ToList();
    }
}