using System.Collections.Generic;
using System.Linq;
using BNG;
using SnapFlow.Assembly;
using SnapFlow.Disassembly;
using UnityEditor;
using UnityEngine;

namespace SnapFlow
{
    [CustomEditor(typeof(GameObject))]
    public class SnapFlowSetupHelper : Editor
    {
        private enum ManagerType { Assembly, Disassembly }
        private enum AssignmentType { ObjectToGrab, TargetSnapZone, ObjectToRemove, SourceSnapZone }

        private ManagerType _selectedManagerType = ManagerType.Assembly;
        private AssignmentType _assignType = AssignmentType.ObjectToGrab;

        private AssemblyStepManager _assemblyManager;
        private DisassemblyManager _disassemblyManager;

        private int _selectedStepIndex;
        private List<string> _stepLabels = new();
        private Vector2 _scrollPos;

        
       
        public override void OnInspectorGUI()
        {
            GameObject go = (GameObject)target;

            if (go.name == "Snap Flow - Assembly" || go.name == "Snap Flow - Disassembly" || Application.isPlaying)
            {

                DrawDefaultInspector();
                return;
            }
                

            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Snap Flow Setup Helper", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            
            DrawSetupButtons(go);
            EditorGUILayout.Space();
            DrawManagerSelection(go);

            EditorGUILayout.EndScrollView();
        }

        private void DrawSetupButtons(GameObject go)
        {
            var hasGrab = HasComponent<Grabbable>(go);
            var hasSnap = HasComponent<SnapZone>(go);
            var hasEvents = HasComponent<GrabbableUnityEvents>(go);

            

            if (!hasSnap)
            {
                if (!hasGrab)
                {
                    if (GUILayout.Button(new GUIContent("Setup Grabbable", "Adds necessary components to make this object grabbable.")))
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
                    if (GUILayout.Button(new GUIContent("Remove Grabbable Setup", "Removes all components related to grabbable functionality.")))
                    {
                        Undo.DestroyObjectImmediate(go.GetComponent<GrabbableUnityEvents>());
                        Undo.DestroyObjectImmediate(go.GetComponent<Grabbable>());
                        TryDestroy<Rigidbody>(go);
                        TryDestroy<BoxCollider>(go);
                    }
                }
            }

            if (!hasSnap && !hasEvents)
            {
                if (GUILayout.Button(new GUIContent("Setup SnapZone", "Adds SnapZone and supporting components for snapping functionality.")))
                {
                    Undo.AddComponent<SnapZone>(go);
                    if (!hasGrab) Undo.AddComponent<Grabbable>(go);
                    if (!HasComponent<BoxCollider>(go)) Undo.AddComponent<BoxCollider>(go);
                    if (!HasComponent<Rigidbody>(go)) Undo.AddComponent<Rigidbody>(go);
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
            }
            else if (hasSnap)
            {
                if (GUILayout.Button(new GUIContent("Remove SnapZone Setup", "Removes SnapZone and associated snapping components.")))
                {
                    TryDestroy<SnapZone>(go);
                    TryDestroy<GrabbablesInTrigger>(go);
                    TryDestroy<GrabAction>(go);
                    TryDestroy<Grabbable>(go);
                    TryDestroy<Rigidbody>(go);
                    TryDestroy<BoxCollider>(go);
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

            _selectedManagerType = (ManagerType)EditorGUILayout.EnumPopup(new GUIContent("Manager Type", "Choose whether to assign to Assembly or Disassembly manager."), _selectedManagerType);

            if (_selectedManagerType == ManagerType.Assembly)
            {
                if (assemblyManagers.Length == 0)
                {
                    EditorGUILayout.HelpBox("No Assembly Managers found.", MessageType.Info);
                    return;
                }

                string[] names = assemblyManagers.Select(m => m.name).ToArray();
                int current = _assemblyManager != null ? assemblyManagers.ToList().IndexOf(_assemblyManager) : 0;
                int selected = EditorGUILayout.Popup("Assembly Manager", current, names);

                if (_assemblyManager != assemblyManagers[selected])
                {
                    _assemblyManager = assemblyManagers[selected];
                    RefreshAssemblySteps();
                }

                DrawStepAssignmentUI(go, _assemblyManager?.steps?.Count ?? 0);
            }
            else
            {
                if (disassemblyManagers.Length == 0)
                {
                    EditorGUILayout.HelpBox("No Disassembly Managers found.", MessageType.Info);
                    return;
                }

                string[] names = disassemblyManagers.Select(m => m.name).ToArray();
                int current = _disassemblyManager != null ? disassemblyManagers.ToList().IndexOf(_disassemblyManager) : 0;
                int selected = EditorGUILayout.Popup("Disassembly Manager", current, names);

                if (_disassemblyManager != disassemblyManagers[selected])
                {
                    _disassemblyManager = disassemblyManagers[selected];
                    RefreshDisassemblySteps();
                }

                DrawStepAssignmentUI(go, _disassemblyManager?.steps?.Count ?? 0);
            }
        }

        private void DrawStepAssignmentUI(GameObject go, int stepCount)
        {
            if (stepCount == 0)
            {
                EditorGUILayout.HelpBox("No steps defined in selected manager.", MessageType.Info);
                return;
            }

            GUILayout.Space(5);
            GUILayout.Label("Step Assignment", EditorStyles.boldLabel);

            _selectedStepIndex = Mathf.Clamp(_selectedStepIndex, 0, _stepLabels.Count - 1);
            _selectedStepIndex = EditorGUILayout.Popup(new GUIContent("Select Step", "Choose the step you want to assign this object to."), _selectedStepIndex, _stepLabels.ToArray());

            string[] options = _selectedManagerType == ManagerType.Assembly
                ? new[] { "ObjectToGrab", "TargetSnapZone" }
                : new[] { "objectToRemove", "sourceSnapZone" };

            _assignType = (AssignmentType)EditorGUILayout.Popup(new GUIContent("Assign As", "Select how this object will be used in the step."), (int)_assignType, options);

            if (GUILayout.Button(new GUIContent("Assign", "Assigns the selected object to the selected step.")))
            {
                AssignSelectedStep(go);
            }
        }

        private void AssignSelectedStep(GameObject go)
        {
            if (!IsValidAssignment(go))
            {
                Debug.LogWarning("Missing required component for selected assignment type.");
                return;
            }

            if (_selectedManagerType == ManagerType.Assembly && _assemblyManager != null)
            {
                if (_selectedStepIndex >= 0 && _selectedStepIndex < _assemblyManager.steps.Count)
                {
                    var step = _assemblyManager.steps[_selectedStepIndex];
                    Undo.RecordObject(_assemblyManager, "Assign Assembly Step Object");

                    if (_assignType == AssignmentType.ObjectToGrab)
                        step.objectToGrab = go.GetComponent<Grabbable>();
                    else
                        step.targetSnapZone = go.GetComponent<SnapZone>();

                    EditorUtility.SetDirty(_assemblyManager);
                }
            }
            else if (_selectedManagerType == ManagerType.Disassembly && _disassemblyManager != null)
            {
                if (_selectedStepIndex >= 0 && _selectedStepIndex < _disassemblyManager.steps.Count)
                {
                    var step = _disassemblyManager.steps[_selectedStepIndex];
                    Undo.RecordObject(_disassemblyManager, "Assign Disassembly Step Object");

                    if (_assignType == AssignmentType.ObjectToRemove)
                        step.objectToRemove = go.GetComponent<Grabbable>();
                    else
                        step.sourceSnapZone = go.GetComponent<SnapZone>();

                    EditorUtility.SetDirty(_disassemblyManager);
                }
            }

            EditorGUIUtility.PingObject(go);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Frame(go.GetComponent<Renderer>()?.bounds ?? new Bounds(go.transform.position, Vector3.one), false);
            }

            Debug.Log($"Assigned '{go.name}' to {_selectedManagerType} Step {_selectedStepIndex + 1} as {_assignType}.");
        }

        private bool IsValidAssignment(GameObject go)
        {
            return _assignType switch
            {
                AssignmentType.ObjectToGrab or AssignmentType.ObjectToRemove => HasComponent<Grabbable>(go),
                AssignmentType.TargetSnapZone or AssignmentType.SourceSnapZone => HasComponent<SnapZone>(go),
                _ => false
            };
        }

        private void RefreshAssemblySteps()
        {
            var steps = _assemblyManager?.steps;
            _stepLabels = steps?.Select((s, i) => $"Step {i + 1}: {s.StepDescription}").ToList() ?? new List<string>();
        }

        private void RefreshDisassemblySteps()
        {
            var steps = _disassemblyManager?.steps;
            _stepLabels = steps?.Select((s, i) => $"Step {i + 1}: {s.stepDescription}").ToList() ?? new List<string>();
        }

        private bool HasComponent<T>(GameObject go) where T : Component => go.GetComponent<T>() != null;

        private void TryDestroy<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp != null)
            {
                Undo.DestroyObjectImmediate(comp);
            }
        }
    }
}