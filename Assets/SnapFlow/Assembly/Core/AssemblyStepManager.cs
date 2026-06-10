using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BNG;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

namespace SnapFlow.Assembly
{
    public class AssemblyStepManager : MonoBehaviour
    {
        [Serializable]
        public class AssemblyStep
        {
            public Grabbable objectToGrab;
            public SnapZone targetSnapZone;
            public string StepDescription;
            public UnityEvent onGrabEvents;
            public UnityEvent onSnapEvents;

            [HideInInspector] public Vector3 initialPosition;
            [HideInInspector] public Quaternion initialRotation;
        }

        public List<AssemblyStep> steps = new();
        public AudioClip stepCompleteSound;
        public AudioClip errorSound;
        public Material highlightObjMaterial;
        public Material highlightSnapMaterial;
        public Slider progressBar;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI stepDescriptionText;
        public UnityEvent onAssemblyComplete;

        public Grabber Primary;
        public Grabber Secondary;
        
        // Material caching system
        private Dictionary<MeshRenderer, Material[]> CachedMaterials = new();
        private Dictionary<Grabbable, Vector3> CachedObjectPositions = new();
        private Dictionary<Grabbable, Quaternion> CachedObjectRotations = new();


        public bool showgizmos = false;
        public bool debuglog = false;
        public bool AssemblyEvents = false;

        private int _currentStep = 0;
        private bool _currentStepSnapped = false;
        Dictionary<Grabbable, AssemblyStep> stepLookup;
        


        private void Start()
        {
            if (steps == null || steps.Count == 0)
            {
                Debug.LogWarning("[SnapFlow] No assembly steps configured!");
                return;
            }

            CacheInitialTransforms();
            CacheInitialMaterials();
            InitializeStepLookup();
            SetupStep(_currentStep);
            UpdateProgressUI();
        }


        private void CacheInitialTransforms()
        {
            foreach (var step in steps)
            {
                if (step.objectToGrab != null)
                {
                    step.initialPosition = step.objectToGrab.transform.position;
                    step.initialRotation = step.objectToGrab.transform.rotation;
                    
                    CachedObjectPositions[step.objectToGrab] = step.initialPosition;
                    CachedObjectRotations[step.objectToGrab] = step.initialRotation;
                }
            }
        }

        private void CacheInitialMaterials()
        {
            foreach (var step in steps)
            {
                // Cache object materials
                if (step.objectToGrab != null)
                {
                    var renderers = step.objectToGrab.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer != null && !CachedMaterials.ContainsKey(renderer))
                        {
                            // Store copies of materials to avoid affecting shared materials
                            CachedMaterials[renderer] = (Material[])renderer.materials.Clone();
                        }
                    }
                }

                // Cache snap zone materials
                if (step.targetSnapZone != null)
                {
                    var renderers = step.targetSnapZone.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer != null && !CachedMaterials.ContainsKey(renderer))
                        {
                            CachedMaterials[renderer] = (Material[])renderer.materials.Clone();
                        }
                    }
                }
            }
        }

        private void InitializeStepLookup()
        {
            stepLookup = new Dictionary<Grabbable, AssemblyStep>();
            foreach (var step in steps)
            {
                if (step.objectToGrab != null)
                    stepLookup[step.objectToGrab] = step;
            }
        }




        private void SetupStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                if (debuglog) Debug.Log("[SnapFlow] All steps completed.");
                return;
            }

            // Disable all snap zones except current
            foreach (var s in steps)
                if (s.targetSnapZone != null)
                    s.targetSnapZone.enabled = false;

            var step = steps[stepIndex];
            if (step.targetSnapZone != null)
                step.targetSnapZone.enabled = true;

            // Only current object is grabbable (Strict mode)
            foreach (var s in steps)
            {
                if (s.objectToGrab != null)
                    s.objectToGrab.GrabPhysics = GrabPhysics.None;
            }

            if (step.objectToGrab != null)
                step.objectToGrab.GrabPhysics = GrabPhysics.FixedJoint;

            HighlightObject(step.objectToGrab, true);

            // Register grab event
            var grabEvents = step.objectToGrab?.GetComponent<GrabbableUnityEvents>();
            if (grabEvents != null)
            {
                grabEvents.onGrab.RemoveAllListeners();
                grabEvents.onGrab.AddListener((GrabbableUnityEvents) => OnObjectGrabbed(stepIndex));
            }

            // Register snap event
            if (step.targetSnapZone != null)
            {
                step.targetSnapZone.OnSnapEvent.RemoveAllListeners();
                step.targetSnapZone.OnSnapEvent.AddListener((snapped) => OnObjectSnapped(snapped, stepIndex));
            }
        }

        private void OnObjectGrabbed(int stepIndex)
        {
            var step = steps[stepIndex];
            HighlightObject(step.objectToGrab, false);
            HighlightSnapZone(step.targetSnapZone, true);
            step.onGrabEvents?.Invoke();
            if (debuglog) Debug.Log($"[SnapFlow] Grabbed: {step.objectToGrab.name}");
        }
        

        private void OnObjectSnapped(Grabbable snappedObject, int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count)
                return;

            var step = steps[stepIndex];

            if (snappedObject != step.objectToGrab)
            {
                if (debuglog) Debug.LogWarning($"[SnapFlow] Wrong object snapped: {snappedObject.name}");
                TriggerErrorFeedback(snappedObject.transform.position);
                ResetObjectByReference(snappedObject);
                return;
            }
            
            _currentStepSnapped = true;

            if (step.targetSnapZone != null)
                step.targetSnapZone.CanRemoveItem = false;

            HighlightSnapZone(step.targetSnapZone, false);
            step.onSnapEvents?.Invoke();
            PlaySound(stepCompleteSound, step.targetSnapZone?.transform.position ?? Vector3.zero);

            if (AssemblyEvents) Debug.Log($"[SnapFlow] Step {stepIndex + 1} completed: {snappedObject.name}");

            _currentStep++;
            UpdateProgressUI();

            if (_currentStep < steps.Count)
            {
                _currentStepSnapped = false;
                SetupStep(_currentStep);
            }
            else
            {
                if (debuglog) Debug.Log("[SnapFlow] Assembly completed!");
                onAssemblyComplete?.Invoke();
            }
        }


        private void HighlightObject(Grabbable obj, bool highlight)
        {
            if (obj == null) return;

            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                if (highlight && highlightObjMaterial != null)
                {
                    rend.material = highlightObjMaterial;
                }
                else if (!highlight && CachedMaterials.ContainsKey(rend))
                {
                    // Restore original materials
                    rend.materials = (Material[])CachedMaterials[rend].Clone();
                }
            }
        }

        private void HighlightSnapZone(SnapZone zone, bool highlight)
        {
            if (zone == null) return;

            var renderers = zone.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                if (highlight && highlightSnapMaterial != null)
                {
                    rend.material = highlightSnapMaterial;
                }
                else if (!highlight && CachedMaterials.ContainsKey(rend))
                {
                    rend.materials = (Material[])CachedMaterials[rend].Clone();
                }
            }
        }

        private void UpdateProgressUI()
        {
            UpdateProgressBar();
            UpdateProgressText();
            UpdateStepDescription();
        }

        private void UpdateProgressBar()
        {
            if (progressBar == null) return;

            float val = (float)_currentStep / steps.Count;
            progressBar.value = Mathf.Clamp01(val);
        }

        private void UpdateProgressText()
        {
            if (progressText == null) return;

            string label = $"Step {Mathf.Min(_currentStep + 1, steps.Count)}/{steps.Count} (Assembly Mode)";
            progressText.text = label;
        }

        private void UpdateStepDescription()
        {
            if (stepDescriptionText == null || _currentStep < 0 || _currentStep >= steps.Count)
                return;

            string description = steps[_currentStep].StepDescription ?? "";
            stepDescriptionText.text = description;
        }

        private void ResetObjectByReference(Grabbable obj)
        {
            if (stepLookup.TryGetValue(obj, out var step))
                ResetObject(step);
        }

        private void ResetObject(AssemblyStep step)
        {
            if (step.objectToGrab == null) return;
            StartCoroutine(ResetObjectNextPhysicsFrame(step));
        }

        private IEnumerator ResetObjectNextPhysicsFrame(AssemblyStep step)
        {
            Grabbable grab = step.objectToGrab;
            if (grab == null) yield break;

            Transform t = grab.transform;
            Rigidbody rb = grab.GetComponent<Rigidbody>();

            // Disable grabbing temporarily
            grab.enabled = false;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            yield return new WaitForFixedUpdate();

            // Reset transform
            t.position = CachedObjectPositions.ContainsKey(grab) 
                ? CachedObjectPositions[grab] 
                : step.initialPosition;
            t.rotation = CachedObjectRotations.ContainsKey(grab) 
                ? CachedObjectRotations[grab] 
                : step.initialRotation;

            yield return null;

            if (rb != null)
                rb.isKinematic = false;

            // Re-enable grabbing
            grab.enabled = true;
        }



        private void PlaySound(AudioClip clip, Vector3 position)
        {
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, position);
        }

        private void TriggerErrorFeedback(Vector3 position)
        {
            PlaySound(errorSound, position);
            TriggerErrorHaptics();
        }

        private void TriggerErrorHaptics(float frequency = 0.7f, float amplitude = 0.7f, float duration = 0.15f)
        {
            if (InputBridge.Instance != null)
            {
                InputBridge.Instance.VibrateController(frequency, amplitude, duration, ControllerHand.Left);
                InputBridge.Instance.VibrateController(frequency, amplitude, duration, ControllerHand.Right);
            }
        }
    }
}