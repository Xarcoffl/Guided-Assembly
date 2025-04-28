using System;
using System.Collections.Generic;
using UnityEngine;
using BNG;
using UnityEngine.Events;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

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

    public enum StepMode { Assembly, Disassembly }
    public StepMode currentMode = StepMode.Assembly;

    public List<AssemblyStep> steps = new();
    public AudioClip stepCompleteSound;
    public AudioClip errorSound;
    public Material HighlightMaterial;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI stepDescriptionText;
    public UnityEvent onAssemblyComplete;

    private int _currentStep = 0;
    private HashSet<int> disassembledSteps = new();


  

    private void Start()
    {
        CacheInitialTransforms();

        if (currentMode == StepMode.Disassembly)
        {
            _currentStep = steps.Count - 1;
            foreach (var s in steps)
                if (s.targetSnapZone) s.targetSnapZone.StartingItem = s.objectToGrab;
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

    private void SetupStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= steps.Count)
        {
            Debug.Log("All steps completed.");
            return;
        }

        foreach (var s in steps)
            if (s.targetSnapZone != null) s.targetSnapZone.enabled = false;

        var step = steps[stepIndex];
        if (step.targetSnapZone != null)
            step.targetSnapZone.enabled = true;

        if (currentMode == StepMode.Assembly)
        {
            HighlightObject(step.objectToGrab, true);

            var grabEvents = step.objectToGrab.GetComponent<GrabbableUnityEvents>();
            if (grabEvents)
                grabEvents.onGrab.AddListener((GrabbableUnityEvents) => OnObjectGrabbed(stepIndex));

            step.targetSnapZone.OnSnapEvent.RemoveAllListeners();
            step.targetSnapZone.OnSnapEvent.AddListener((snapped) => OnObjectSnapped(snapped, stepIndex));
        }
        else
        {
            HighlightSnapZone(step.targetSnapZone, true);
            step.targetSnapZone.OnDetachEvent.RemoveAllListeners();
            step.targetSnapZone.OnDetachEvent.AddListener((unsnapped) => OnObjectUnsnapped(unsnapped, stepIndex));
        }
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
            Debug.LogWarning("Wrong object placed!");
            TriggerErrorFeedback(snappedObject.transform.position);
            ResetObject(step);
            return;
        }

        HighlightSnapZone(step.targetSnapZone, false);
        step.onSnapEvents?.Invoke();
        PlaySound(stepCompleteSound, step.targetSnapZone.transform.position);

        _currentStep++;
        UpdateProgressUI();

        if (_currentStep < steps.Count)
            SetupStep(_currentStep);
        else
            onAssemblyComplete?.Invoke();
    }

    private void OnObjectUnsnapped(Grabbable obj, int stepIndex)
    {
        var step = steps[stepIndex];
        if (obj != step.objectToGrab)
        {
            Debug.LogWarning("Wrong object removed!");
            TriggerErrorFeedback(obj.transform.position);
            return;
        }

        if (disassembledSteps.Contains(stepIndex)) return;

        HighlightSnapZone(step.targetSnapZone, false);
        step.onSnapEvents?.Invoke();
        disassembledSteps.Add(stepIndex);
        PlaySound(stepCompleteSound, step.targetSnapZone.transform.position);

        _currentStep--;
        UpdateProgressUI();

        if (_currentStep >= 0)
            SetupStep(_currentStep);
        else
            onAssemblyComplete?.Invoke();
    }

    private void HighlightObject(Grabbable obj, bool highlight)
    {
        var renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var rend in renderers)
        {
            if (rend != null)
                rend.material = highlight ? HighlightMaterial : rend.sharedMaterial;
        }
    }

    private void HighlightSnapZone(SnapZone zone, bool highlight)
    {
        var renderers = zone.GetComponentsInChildren<MeshRenderer>();
        foreach (var rend in renderers)
        {
            if (rend != null)
                rend.material = highlight ? HighlightMaterial : rend.sharedMaterial;
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

        float val = currentMode == StepMode.Assembly ?
            (float)_currentStep / steps.Count :
            (float)(steps.Count - _currentStep - 1) / steps.Count;

        progressBar.value = val;
    }

    private void UpdateProgressText()
    {
        if (progressText == null) return;

        string label = currentMode == StepMode.Assembly ?
            $"Step {(_currentStep + 1)}/{steps.Count} (Assembly Mode)" :
            $"Step {(_currentStep + 1)}/{steps.Count} (Disassembly Mode)";

        progressText.text = label;
    }

    private void UpdateStepDescription()
    {
        if (stepDescriptionText == null || _currentStep < 0 || _currentStep >= steps.Count)
            return;

        stepDescriptionText.text = steps[_currentStep].StepDescription;
    }

    private void ResetObject(AssemblyStep step)
    {
        if (step.objectToGrab == null) return;

        step.objectToGrab.transform.position = step.initialPosition;
        step.objectToGrab.transform.rotation = step.initialRotation;
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