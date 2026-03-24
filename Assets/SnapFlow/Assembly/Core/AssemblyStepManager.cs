using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BNG;
using UnityEngine.Events;
using TMPro;
using UnityEditor;
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

        public enum SopMode
        {
            Strict,
            Practice
        }

        public SopMode sopMode = SopMode.Strict;


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
        
        private List<Material> InitialObjMaterials;
        private List<Material> InitialSnapMaterials;


        public bool showgizmos = false;
        public bool debuglog = false;
        public bool AssemblyEvents = false;

        private int _currentStep = 0;
        private bool _currentStepSnapped = false;
        Dictionary<Grabbable, AssemblyStep> stepLookup;
        


        private void Start()
        {
            CacheInitialTransforms();
            if (sopMode == SopMode.Practice)
            {
                RegisterPracticeReleaseListeners();
            }
            stepLookup = new Dictionary<Grabbable, AssemblyStep>();
            foreach (var step in steps)
            {
                if (step.objectToGrab != null)
                    stepLookup[step.objectToGrab] = step;
            }

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
                }
            }
        }

        private void CacheInitialObjMaterial()
        {
            
        }


        private void RegisterPracticeReleaseListeners()
        {
            foreach (var step in steps)
            {
                if (step.objectToGrab == null) continue;

                var events = step.objectToGrab.GetComponent<GrabbableUnityEvents>();
                if (events == null) continue;

                events.onRelease.RemoveAllListeners();
                events.onRelease.AddListener(() => OnPracticeObjectReleased(step.objectToGrab));
            }
        }

        private void OnPracticeObjectReleased(Grabbable releasedObject)
        {
            if (sopMode != SopMode.Practice)
                return;

            if (_currentStep < 0 || _currentStep >= steps.Count)
                return;
            
            var currentStep = steps[_currentStep];
            
            // Case 1: Wrong object
            if (releasedObject != currentStep.objectToGrab)
            {
                ResetObjectByReference(releasedObject);
                return;
            }

            // Case 2: Correct object but not snapped yet
            if (!_currentStepSnapped)
            {
                ResetObject(currentStep);
            }
        }

        private void ResetObjectByReference(Grabbable obj)
        {
            if (stepLookup.TryGetValue(obj, out var step))
                ResetObject(step);
        }
        
        

        private void SetupStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                Debug.Log("All steps completed.");
                return;
            }
// Turning off all the snapzone attached to the manager and letting only stepwise snapzone to be enabled 

            foreach (var s in steps)
                if (s.targetSnapZone != null)
                    s.targetSnapZone.enabled = false;

            var step = steps[stepIndex];
            if (step.targetSnapZone != null)
                step.targetSnapZone.enabled = true;

///////////////////////////////////////////////////////////////////////////////////////////////////////

            if (sopMode == SopMode.Strict)
            {
                foreach (var o in steps)
                    if (o.objectToGrab != null)
                        o.objectToGrab.GrabPhysics = GrabPhysics.None;

                if (step.objectToGrab != null)
                    step.objectToGrab.GrabPhysics = GrabPhysics.FixedJoint;
            }
            else
            {
                foreach (var o in steps)
                    if (o.objectToGrab != null)
                        o.objectToGrab.GrabPhysics = GrabPhysics.FixedJoint;
            }

/////////////////////////////////////////////////////////////////////////////////////////////////////////          
            HighlightObject(step.objectToGrab, true);

            var grabEvents = step.objectToGrab.GetComponent<GrabbableUnityEvents>();
            if (grabEvents)
            {
                grabEvents.onGrab.RemoveAllListeners();
                grabEvents.onGrab.AddListener((GrabbableUnityEvents) => OnObjectGrabbed(stepIndex));
            }
                


            step.targetSnapZone.OnSnapEvent.RemoveAllListeners();
            step.targetSnapZone.OnSnapEvent.AddListener((snapped) => OnObjectSnapped(snapped, stepIndex));
        }

        private void OnObjectGrabbed(int stepIndex)
        {
            var step = steps[stepIndex];
            HighlightObject(step.objectToGrab, false);
            HighlightSnapZone(step.targetSnapZone, true);
            step.onGrabEvents?.Invoke();
            Debug.Log($"Grabbed: {step.objectToGrab.name}");
        }
        

        private void OnObjectSnapped(Grabbable snappedObject, int stepIndex)
        {
            var step = steps[stepIndex];

            if (snappedObject != step.objectToGrab)
            {
                TriggerErrorFeedback(snappedObject.transform.position);
                ResetObjectByReference(snappedObject);
                return;
            }
            
            _currentStepSnapped = true; // ✅ IMPORTANT

            step.targetSnapZone.CanRemoveItem = false;

            HighlightSnapZone(step.targetSnapZone, false);
            step.onSnapEvents?.Invoke();
            PlaySound(stepCompleteSound, step.targetSnapZone.transform.position);

            _currentStep++;
            UpdateProgressUI();

            if (_currentStep < steps.Count)
            {
                _currentStepSnapped = false; // reset for next step
                SetupStep(_currentStep);
            }
            else
            {
                onAssemblyComplete?.Invoke();
            }
        }


        private void HighlightObject(Grabbable obj, bool highlight)
        {
            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                if (rend != null)
                    rend.material = highlight ? highlightObjMaterial : rend.sharedMaterial;
            }
        }

        private void HighlightSnapZone(SnapZone zone, bool highlight)
        {
            var renderers = zone.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                if (rend != null)
                    rend.material = highlight ? highlightSnapMaterial : rend.sharedMaterial;
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

            float val =
                (float)_currentStep / steps.Count;


            progressBar.value = val;
        }

        private void UpdateProgressText()
        {
            if (progressText == null) return;

            string label =
                _currentStep < steps.Count
                    ? $"Step {(_currentStep + 1)}/{steps.Count} (Assembly Mode)"
                    : $"Step {(_currentStep)}/{steps.Count} (Assembly Mode)";

            progressText.text = label;
        }

        private void UpdateStepDescription()
        {
            if (stepDescriptionText == null || _currentStep < 0 || _currentStep >= steps.Count)
                return;

            stepDescriptionText.text = steps[_currentStep].StepDescription;
        }

        [Obsolete("Obsolete")]
        private void ResetObject(AssemblyStep step)
        {
            if (step.objectToGrab == null) return;
            StartCoroutine(ResetNextPhysicsFrame(step));
        }

        [Obsolete("Obsolete")]
        private static IEnumerator ResetNextPhysicsFrame(AssemblyStep step)
        {
            yield return new WaitForFixedUpdate();

            Grabbable grab = step.objectToGrab;
            Transform t = grab.transform;
            Rigidbody rb = grab.GetComponent<Rigidbody>();

            grab.enabled = false;   // 🔒 stop BNG interference

            if (rb != null)
            {
                rb.isKinematic = true;
                
            }

            t.position = step.initialPosition;
            t.rotation = step.initialRotation;

            yield return null;

            if (rb != null)
                rb.isKinematic = false;

            grab.enabled = true;    // 🔓 re-enable grab
        }



        private void PlaySound(AudioClip clip, Vector3 position)
        {
            if (clip)
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