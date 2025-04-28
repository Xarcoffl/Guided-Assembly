using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using BNG;
using Slider = UnityEngine.UI.Slider;



public class DisassemblyManager : MonoBehaviour
{
    [Serializable]
    public class DisassemblyStep
    {
        public Grabbable objectToRemove;
        public SnapZone sourceSnapZone;
        public string StepDescription;
        public UnityEvent onUnSnapEvents;
    }

    public List<DisassemblyStep> steps = new();
    public AudioClip stepCompleteSound;
    public Material HighlightMaterial;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI stepDescriptionText;
    public UnityEvent onDisassemblyComplete;

    public int _currentStep;
    private Material _originalSnapMaterial;
    public HashSet<int> completedSteps = new();

    private void Start()
    {
        _currentStep = steps.Count - 1;
        SetupStep(_currentStep);
        UpdateProgress();
    }

    private void SetupStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= steps.Count)
        {
            Debug.Log("All steps disassembled!");
            return;
        }

        foreach (var step in steps)
        {
            if (step.sourceSnapZone != null)
                step.sourceSnapZone.StartingItem = step.objectToRemove;
            step.sourceSnapZone.CanRemoveItem = false;
           
        }

        var currentStep = steps[stepIndex];

        if (currentStep.sourceSnapZone != null)
        {
            currentStep.sourceSnapZone.CanRemoveItem = true;
            _originalSnapMaterial = currentStep.sourceSnapZone.GetComponent<Renderer>()?.material;
            HighlightSnapZone(currentStep.sourceSnapZone, true);

            currentStep.sourceSnapZone.OnDetachEvent.RemoveAllListeners();
            currentStep.sourceSnapZone.OnDetachEvent.AddListener((unsnapped) => OnObjectUnsnapped(unsnapped, stepIndex));
        }
    }

    private void OnObjectUnsnapped(Grabbable obj, int stepIndex)
    {
        var step = steps[stepIndex];

        if (obj != step.objectToRemove)
        {
            Debug.LogWarning("Wrong object removed: " + obj.name);
            TriggerErrorFeedback(obj.transform.position);
            return;
        }

        if (completedSteps.Contains(stepIndex))
            return;

        Debug.Log("Correct object removed: " + obj.name);
        completedSteps.Add(stepIndex);
        HighlightSnapZone(step.sourceSnapZone, false);
        step.onUnSnapEvents?.Invoke();

        if (stepCompleteSound) AudioSource.PlayClipAtPoint(stepCompleteSound, step.sourceSnapZone.transform.position);

        _currentStep--;
        UpdateProgress();

        if (_currentStep >= 0)
            SetupStep(_currentStep);
        else
        {
            Debug.Log("Disassembly Completed!");
            onDisassemblyComplete?.Invoke();
        }
    }

    private void HighlightSnapZone(SnapZone zone, bool highlight)
    {
        var meshRenderer = zone.GetComponent<MeshRenderer>();
        if (meshRenderer)
        {
            meshRenderer.material = highlight ? HighlightMaterial : _originalSnapMaterial;
        }
    }

    private void UpdateProgress()
    {
        if (progressBar)
        {
            float val = steps.Count == 0 ? 1 : (float)(steps.Count - _currentStep - 1) / steps.Count;
            progressBar.value = val;
        }

        if (progressText)
        {
            string label = $"Step {(_currentStep + 1)}/{steps.Count} (Disassembly Mode)";
            progressText.text = label;
        }

        if (_currentStep >= 0 && _currentStep < steps.Count && stepDescriptionText)
            stepDescriptionText.text = steps[_currentStep].StepDescription;
    }

    private void TriggerErrorFeedback(Vector3 position)
    {
        /*
        if (errorSound)
            AudioSource.PlayClipAtPoint(errorSound, position);
            */

        if (InputBridge.Instance != null)
        {
            InputBridge.Instance.VibrateController(0.7f, 0.7f, 0.15f, ControllerHand.Left);
            InputBridge.Instance.VibrateController(0.7f, 0.7f, 0.15f, ControllerHand.Right);
        }
    }
}