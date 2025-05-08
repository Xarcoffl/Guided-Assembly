// Editor/HierarchySnapFlowSetup.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BNG;
using SnapFlow.Assembly;
using SnapFlow.Disassembly;

[InitializeOnLoad]
public static class HierarchySnapFlowSetup
{
    static Texture2D gearIcon;

    static HierarchySnapFlowSetup()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        gearIcon = EditorGUIUtility.IconContent("CustomTool@2x").image as Texture2D;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null) return;

        if (go.GetComponent<Renderer>() == null ||
            (go.GetComponent<AssemblyStepManager>() != null && go.GetComponent<DisassemblyManager>() != null))
            return;

        Rect buttonRect = new Rect(selectionRect.xMax - 20, selectionRect.yMin, 18, selectionRect.height);
        GUIContent buttonContent = new GUIContent(gearIcon, "Snap Flow Setup");

        if (GUI.Button(buttonRect, buttonContent, GUIStyle.none))
        {
            ShowCustomMenu(go);
        }
    }

    static void ShowCustomMenu(GameObject go)
    {
        GenericMenu menu = new GenericMenu();

        var hasGrab = go.GetComponent<Grabbable>();
        var hasSnap = go.GetComponent<SnapZone>();
        var hasEvents = go.GetComponent<GrabbableUnityEvents>();

        if (!hasSnap)
        {
            if (!hasGrab)
            {
                menu.AddItem(new GUIContent("SnapFlow/Snap Flow Objects/Grabbable/Add Grab"), false, () => setupGrabbable(go));
                menu.AddDisabledItem(new GUIContent("SnapFlow/Snap Flow Objects/Grabbable/Remove Grab"));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("SnapFlow/Snap Flow Objects/Grabbable/Add Grab"));
                menu.AddItem(new GUIContent("SnapFlow/Snap Flow Objects/Grabbable/Remove Grab"), false, () => removeGrabbable(go));
            }
        }

        if (!hasSnap && !hasEvents)
        {
            menu.AddItem(new GUIContent("SnapFlow/Snap Flow Objects/Snapzone/Add Zone"), false, () => setupSnapzone(go));
            menu.AddDisabledItem(new GUIContent("SnapFlow/Snap Flow Objects/Snapzone/Remove Zone"));
        }
        else if (hasSnap)
        {
            menu.AddDisabledItem(new GUIContent("SnapFlow/Snap Flow Objects/Snapzone/Add Zone"));
            menu.AddItem(new GUIContent("SnapFlow/Snap Flow Objects/Snapzone/Remove Zone"), false, () => removeSnapzone(go));
        }

        menu.AddItem(new GUIContent("SnapFlow/Assign Snap Flow"), false, () => SnapFlowSetupPanel.Open(go));
        menu.ShowAsContext();
    }

    static void setupGrabbable(GameObject go)
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

    static void setupSnapzone(GameObject go)
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
        go.GetComponent<GrabAction>().OnGrabEvent.AddListener(go.GetComponent<SnapZone>().GrabEquipped);
    }

    static void removeGrabbable(GameObject go)
    {
        Undo.DestroyObjectImmediate(go.GetComponent<GrabbableUnityEvents>());
        Undo.DestroyObjectImmediate(go.GetComponent<Grabbable>());
        Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());
        Undo.DestroyObjectImmediate(go.GetComponent<Rigidbody>());
    }

    static void removeSnapzone(GameObject go)
    {
        Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());
        Undo.DestroyObjectImmediate(go.GetComponent<Rigidbody>());
        Undo.DestroyObjectImmediate(go.GetComponent<GrabbablesInTrigger>());
        Undo.DestroyObjectImmediate(go.GetComponent<GrabAction>());
        Undo.DestroyObjectImmediate(go.GetComponent<Grabbable>());
        Undo.DestroyObjectImmediate(go.GetComponent<SnapZone>());
    }

    public class SnapFlowSetupPanel : EditorWindow
    {
        GameObject targetObject;
        AssemblyStepManager assemblyManager;
        DisassemblyManager disassemblyManager;

        enum ManagerType { Assembly, Disassembly }
        ManagerType selectedManagerType = ManagerType.Assembly;

        int selectedStepIndex = 0;
        string[] stepOptions = new string[0];

        public static void Open(GameObject go)
        {
            var window = GetWindow<SnapFlowSetupPanel>("Snap Flow Object Assign Panel");
            window.targetObject = go;
            window.Initialize();
            window.Show();
        }

        void Initialize()
        {
            assemblyManager = FindObjectOfType<AssemblyStepManager>();
            disassemblyManager = FindObjectOfType<DisassemblyManager>();
            RefreshStepOptions();
        }

        void RefreshStepOptions()
        {
            stepOptions = new string[0];

            if (selectedManagerType == ManagerType.Assembly && assemblyManager != null && assemblyManager.steps != null)
            {
                List<string> names = new List<string>();
                for (int i = 0; i < assemblyManager.steps.Count; i++)
                {
                    string label = string.IsNullOrEmpty(assemblyManager.steps[i].StepDescription)
                        ? $"Step {i + 1}"
                        : $"Step {i + 1}: {assemblyManager.steps[i].StepDescription}";
                    names.Add(label);
                }
                stepOptions = names.ToArray();
            }
            else if (selectedManagerType == ManagerType.Disassembly && disassemblyManager != null && disassemblyManager.steps != null)
            {
                List<string> names = new List<string>();
                for (int i = 0; i < disassemblyManager.steps.Count; i++)
                {
                    string label = string.IsNullOrEmpty(disassemblyManager.steps[i].stepDescription)
                        ? $"Step {i + 1}"
                        : $"Step {i + 1}: {disassemblyManager.steps[i].stepDescription}";
                    names.Add(label);
                }
                stepOptions = names.ToArray();
            }
        }

        void OnGUI()
        {
            if (targetObject == null)
            {
                EditorGUILayout.HelpBox("No GameObject selected.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("SnapFlow Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
            EditorGUILayout.Space();

            selectedManagerType = (ManagerType)EditorGUILayout.EnumPopup("Manager Type", selectedManagerType);
            EditorGUILayout.Space();

            if ((selectedManagerType == ManagerType.Assembly && assemblyManager == null) ||
                (selectedManagerType == ManagerType.Disassembly && disassemblyManager == null))
            {
                EditorGUILayout.HelpBox("No manager found in scene.", MessageType.Warning);
                if (GUILayout.Button($"Add Snap Flow - {selectedManagerType} Manager to Scene"))
                {
                    GameObject go = new GameObject($"Snap Flow - {selectedManagerType} Manager");
                    Undo.RegisterCreatedObjectUndo(go, "Create Manager");
                    if (selectedManagerType == ManagerType.Assembly)
                        assemblyManager = go.AddComponent<AssemblyStepManager>();
                    else
                        disassemblyManager = go.AddComponent<DisassemblyManager>();
                    RefreshStepOptions();
                }
                return;
            }

            if (stepOptions.Length == 0)
            {
                EditorGUILayout.HelpBox("No steps found. Add at least one step.", MessageType.Info);
                if (GUILayout.Button("Add Step"))
                {
                    if (selectedManagerType == ManagerType.Assembly)
                    {
                        Undo.RecordObject(assemblyManager, "Add Assembly Step");
                        assemblyManager.steps.Add(new AssemblyStepManager.AssemblyStep());
                        EditorUtility.SetDirty(assemblyManager);
                    }
                    else
                    {
                        Undo.RecordObject(disassemblyManager, "Add Disassembly Step");
                        disassemblyManager.steps.Add(new DisassemblyManager.DisassemblyStep());
                        EditorUtility.SetDirty(disassemblyManager);
                    }
                    RefreshStepOptions();
                }
                return;
            }

            EditorGUILayout.LabelField("Select Step:");
            selectedStepIndex = EditorGUILayout.Popup(selectedStepIndex, stepOptions);
            EditorGUILayout.Space();

            if (GUILayout.Button("Assign as ObjectToGrab"))
            {
                if (selectedManagerType == ManagerType.Assembly)
                {
                    Undo.RecordObject(assemblyManager, "Assign Object to Grab");
                    assemblyManager.steps[selectedStepIndex].objectToGrab = targetObject.GetComponent<Grabbable>();
                    EditorUtility.SetDirty(assemblyManager);
                }
                else
                {
                    Undo.RecordObject(disassemblyManager, "Assign Object to Remove");
                    disassemblyManager.steps[selectedStepIndex].objectToRemove = targetObject.GetComponent<Grabbable>();
                    EditorUtility.SetDirty(disassemblyManager);
                }
                Debug.Log($"{targetObject.name} assigned as Object to Grab to {stepOptions[selectedStepIndex]}");
            }

            if (GUILayout.Button("Assign as SnapZone"))
            {
                if (selectedManagerType == ManagerType.Assembly)
                {
                    Undo.RecordObject(assemblyManager, "Assign SnapZone");
                    assemblyManager.steps[selectedStepIndex].targetSnapZone = targetObject.GetComponent<SnapZone>();
                    EditorUtility.SetDirty(assemblyManager);
                }
                else
                {
                    Undo.RecordObject(disassemblyManager, "Assign SourceSnapZone");
                    disassemblyManager.steps[selectedStepIndex].sourceSnapZone = targetObject.GetComponent<SnapZone>();
                    EditorUtility.SetDirty(disassemblyManager);
                }
                Debug.Log($"{targetObject.name} assigned as SnapZone to {stepOptions[selectedStepIndex]}");
            }
        }
    }
}