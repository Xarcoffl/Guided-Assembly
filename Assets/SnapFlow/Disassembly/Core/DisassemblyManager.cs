using System;
using System.Collections.Generic;
using BNG;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

using Slider = UnityEngine.UI.Slider;



namespace SnapFlow.Disassembly
{

    public class DisassemblyManager : MonoBehaviour
    {
        [Serializable]
        public class DisassemblyStep
        {
            public Grabbable objectToRemove;
            public SnapZone sourceSnapZone;
            public string stepDescription;
            public UnityEvent onUnSnapEvents;
        }

        public List<DisassemblyStep> steps = new();
        public AudioClip stepCompleteSound;
        public AudioClip errorSound;
        public Material highlightMaterial;
        public Slider progressBar;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI stepDescriptionText;
        public UnityEvent onDisassemblyComplete;

        public int _currentStep;
        private Dictionary<MeshRenderer, Material[]> cachedMaterials = new();
        public HashSet<int> CompletedSteps = new();

        public bool debuglog = false;
        public bool showgizmos = false;
        public bool AssemblyEvents = false;

        private void Start()
        {
            if (steps == null || steps.Count == 0)
            {
                Debug.LogWarning("[SnapFlow] No disassembly steps configured!");
                return;
            }

            CacheInitialMaterials();
            _currentStep = steps.Count - 1;
            SetupStep(_currentStep);
            UpdateProgress();
        }

        private void CacheInitialMaterials()
        {
            foreach (var step in steps)
            {
                if (step.sourceSnapZone != null)
                {
                    var renderers = step.sourceSnapZone.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer != null && !cachedMaterials.ContainsKey(renderer))
                        {
                            cachedMaterials[renderer] = (Material[])renderer.materials.Clone();
                        }
                    }
                }
            }
        }

        private void SetupStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count)
            {
                if (debuglog) Debug.Log("[SnapFlow] All steps disassembled!");
                return;
            }

            // Configure all snap zones
            foreach (var step in steps)
            {
                if (step.sourceSnapZone != null)
                {
                    step.sourceSnapZone.StartingItem = step.objectToRemove;
                    step.sourceSnapZone.CanRemoveItem = false;
                }
            }

            var currentStep = steps[stepIndex];

            if (currentStep.sourceSnapZone != null)
            {
                currentStep.sourceSnapZone.CanRemoveItem = true;
                HighlightSnapZone(currentStep.sourceSnapZone, true);

                currentStep.sourceSnapZone.OnDetachEvent.RemoveAllListeners();
                currentStep.sourceSnapZone.OnDetachEvent.AddListener((unsnapped) => OnObjectUnsnapped(unsnapped, stepIndex));
            }
        }

        private void OnObjectUnsnapped(Grabbable obj, int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= steps.Count)
                return;

            var step = steps[stepIndex];

            if (obj != step.objectToRemove)
            {
                if (debuglog) Debug.LogWarning("[SnapFlow] Wrong object removed: " + obj.name);
                TriggerErrorFeedback(obj.transform.position);
                return;
            }

            if (CompletedSteps.Contains(stepIndex))
                return;

            if (debuglog) Debug.Log("[SnapFlow] Correct object removed: " + obj.name);
            CompletedSteps.Add(stepIndex);
            HighlightSnapZone(step.sourceSnapZone, false);
            step.onUnSnapEvents?.Invoke();

            PlaySound(stepCompleteSound, step.sourceSnapZone?.transform.position ?? Vector3.zero);

            _currentStep--;
            UpdateProgress();

            if (_currentStep >= 0)
                SetupStep(_currentStep);
            else
            {
                if (debuglog) Debug.Log("[SnapFlow] Disassembly Completed!");
                onDisassemblyComplete?.Invoke();
            }
        }

        private void HighlightSnapZone(SnapZone zone, bool highlight)
        {
            if (zone == null) return;

            var renderers = zone.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                if (highlight && highlightMaterial != null)
                {
                    renderer.material = highlightMaterial;
                }
                else if (!highlight && cachedMaterials.ContainsKey(renderer))
                {
                    renderer.materials = (Material[])cachedMaterials[renderer].Clone();
                }
            }
        }

        private void UpdateProgress()
        {
            if (progressBar != null)
            {
                float val = steps.Count == 0 ? 1 : (float)(steps.Count - _currentStep - 1) / steps.Count;
                progressBar.value = Mathf.Clamp01(val);
            }

            if (progressText != null)
            {
                string label = $"Step {Mathf.Max(_currentStep + 1, 0)}/{steps.Count} (Disassembly Mode)";
                progressText.text = label;
            }

            if (_currentStep >= 0 && _currentStep < steps.Count && stepDescriptionText != null)
                stepDescriptionText.text = steps[_currentStep].stepDescription ?? "";
        }

        private void PlaySound(AudioClip clip, Vector3 position)
        {
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, position);
        }

        private void TriggerErrorFeedback(Vector3 position)
        {
            PlaySound(errorSound, position);

            if (InputBridge.Instance != null)
            {
                InputBridge.Instance.VibrateController(0.7f, 0.7f, 0.15f, ControllerHand.Left);
                InputBridge.Instance.VibrateController(0.7f, 0.7f, 0.15f, ControllerHand.Right);
            }
        }
    }
}