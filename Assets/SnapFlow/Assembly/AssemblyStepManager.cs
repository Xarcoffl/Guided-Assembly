using System;
using System.Collections.Generic;
using UnityEngine;
using BNG;
using UnityEngine.Events;
using TMPro;
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
    }

    [Header("Assembly Configuration")]
    public List<AssemblyStep> steps = new();
    public AudioClip stepCompleteSound;
    public AudioClip errorSound;
    public Material HighlightMaterial;

    [Header("UI")]
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI stepDescriptionText;

    [Header("Events")]
    public UnityEvent onAssemblyComplete;

    private int _currentStep = 0;

    private Dictionary<Renderer, Material> originalMaterials = new();
    private Dictionary<Grabbable, (Vector3, Quaternion)> initialTransforms = new();

    private void Start()
    {
        CacheInitialTransforms();
        SetupStep(_currentStep);
        UpdateProgress();
    }

    private void CacheInitialTransforms()
    {
        foreach (var step in steps)
        {
            if (step.objectToGrab != null)
            {
                initialTransforms[step.objectToGrab] = (step.objectToGrab.transform.position, step.objectToGrab.transform.rotation);
                CacheOriginalMaterial(step.objectToGrab.GetComponent<Renderer>());
            }

            if (step.targetSnapZone != null)
            {
                CacheOriginalMaterial(step.targetSnapZone.GetComponent<Renderer>());
            }
        }
    }

    private void CacheOriginalMaterial(Renderer renderer)
    {
        if (renderer != null && !originalMaterials.ContainsKey(renderer))
        {
            originalMaterials[renderer] = renderer.material;
        }
    }

    private void SetupStep(int stepIndex)
    {
        if (stepIndex >= steps.Count)
        {
            Debug.Log("All steps completed!");
            onAssemblyComplete?.Invoke();
            return;
        }

        foreach (var s in steps)
        {
            if (s.targetSnapZone != null)
                s.targetSnapZone.enabled = false;
        }

        var step = steps[stepIndex];

        if (step.targetSnapZone != null)
        {
            step.targetSnapZone.enabled = true;
            step.targetSnapZone.OnSnapEvent.RemoveAllListeners();
            step.targetSnapZone.OnSnapEvent.AddListener((snappedObj) => OnObjectSnapped(snappedObj, stepIndex));
        }

        if (step.objectToGrab != null)
        {
            var grabEvents = step.objectToGrab.GetComponent<GrabbableUnityEvents>();
            if (grabEvents)
            {
                grabEvents.onGrab.RemoveAllListeners();
                grabEvents.onGrab.AddListener((_) => OnObjectGrabbed(stepIndex));
            }
            else
            {
                Debug.LogWarning("GrabbableEvents script missing on " + step.objectToGrab.name);
            }

            HighlightObject(step.objectToGrab, true);
        }
    }

    private void OnObjectGrabbed(int stepIndex)
    {
        var step = steps[stepIndex];

        Debug.Log("Object grabbed");
        HighlightObject(step.objectToGrab, false);
        HighlightSnapZone(step.targetSnapZone, true);

        step.onGrabEvents?.Invoke();
    }

    private void OnObjectSnapped(Grabbable snappedObject, int stepIndex)
    {
        var step = steps[stepIndex];

        if (snappedObject != step.objectToGrab)
        {
            Debug.LogWarning("Wrong object placed in snap zone: " + snappedObject.name);

            if (errorSound) AudioSource.PlayClipAtPoint(errorSound, snappedObject.transform.position);
            TriggerErrorHaptics();
            ResetObject(snappedObject);
            return;
        }

        Debug.Log("Correct object snapped");

        HighlightSnapZone(step.targetSnapZone, false);
        step.onSnapEvents?.Invoke();

        if (stepCompleteSound) AudioSource.PlayClipAtPoint(stepCompleteSound, step.targetSnapZone.transform.position);

        _currentStep++;
        UpdateProgress();

        if (_currentStep < steps.Count)
            SetupStep(_currentStep);
        else
        {
            Debug.Log("Assembly Completed!");
            onAssemblyComplete?.Invoke();
        }
    }

    private void HighlightObject(Grabbable obj, bool highlight)
    {
        var renderer = obj ? obj.GetComponent<Renderer>() : null;
        if (renderer)
        {
            renderer.material = highlight ? HighlightMaterial : GetOriginalMaterial(renderer);
        }
    }

    private void HighlightSnapZone(SnapZone zone, bool highlight)
    {
        var renderer = zone ? zone.GetComponent<Renderer>() : null;
        if (renderer)
        {
            renderer.material = highlight ? HighlightMaterial : GetOriginalMaterial(renderer);
        }
    }

    private Material GetOriginalMaterial(Renderer renderer)
    {
        return originalMaterials.TryGetValue(renderer, out var mat) ? mat : renderer.material;
    }

    private void UpdateProgress()
    {
        if (progressBar) progressBar.value = (float)_currentStep / steps.Count;
        if (progressText) progressText.text = $"Step {_currentStep + 1}/{steps.Count}";
        if (stepDescriptionText && _currentStep < steps.Count)
        {
            stepDescriptionText.text = steps[_currentStep].StepDescription;
        }
    }

    private void ResetObject(Grabbable obj)
    {
        if (obj == null || !initialTransforms.ContainsKey(obj)) return;

        var rb = obj.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var (pos, rot) = initialTransforms[obj];
        obj.transform.SetPositionAndRotation(pos, rot);
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