using System;
using System.Collections.Generic;
using UnityEngine;
using BNG;
using UnityEngine.Events;
using TMPro;
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

    public List<AssemblyStep> steps = new();
    public AudioClip stepCompleteSound;
    public AudioClip errorSound;
    public Material HighlightMaterial;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI stepDescriptionText;

    private Material _grabInitialMaterial;
    private Material _SnapInitialMaterial;

    
    
    public int _currentStep = 0;


    private void Awake()
    {
        this.gameObject.transform.name = "Snap Flow Manager";
    }

    private void Start()
    {
        SetupStep(_currentStep);
        UpdateProgress();

        


    }
    
    
    private void SetupStep(int stepIndex)
    {
        if (stepIndex >= steps.Count)
        {
            Debug.Log("All steps completed!");
            return;
        }

        // Disable all snap zones
        foreach (var s in steps)
        {
            if (s.targetSnapZone != null)
                s.targetSnapZone.enabled = false;
        }

        var step = steps[stepIndex];

        // Enable only current step's snap zone
        if (step.targetSnapZone != null)
            step.targetSnapZone.enabled = true;

        // Store initial materials
        _grabInitialMaterial = step.objectToGrab.GetComponent<Renderer>().material;
        _SnapInitialMaterial = step.targetSnapZone.GetComponent<Renderer>().material;

        // Highlight object to grab
        HighlightObject(step.objectToGrab, true);

        // Add grab event listener
        var grabEvents = step.objectToGrab.GetComponent<GrabbableUnityEvents>();
        if (grabEvents)
            grabEvents.onGrab.AddListener((GrabbableUnityEvents) => OnObjectGrabbed(stepIndex));
        else
            Debug.LogWarning("GrabbableEvents script missing on " + step.objectToGrab.name);

        // Add snap event listener
        step.targetSnapZone.OnSnapEvent.AddListener((GrabbableEvent) => OnObjectSnapped(GrabbableEvent, stepIndex));
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

            TriggerErrorHaptics(); // 🎮 Vibrate both controllers

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
            Debug.Log("Assembly Completed!");
    }

    private void HighlightObject(Grabbable obj, bool highlight)
    {
        var meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer)
        {
            meshRenderer.material = highlight ? HighlightMaterial : _grabInitialMaterial;
        }
    }

    private void HighlightSnapZone(SnapZone zone, bool highlight)
    {
        var meshRenderer = zone.GetComponent<MeshRenderer>();
        if (meshRenderer)
        {
            meshRenderer.material = highlight ? HighlightMaterial : _SnapInitialMaterial;
        }
    }

    private void UpdateProgress()
    {
        if (progressBar) progressBar.value = (float)_currentStep / steps.Count;
        if (progressText) progressText.text = $"Step {(_currentStep + 1)}/{steps.Count}";
        if (_currentStep < steps.Count)
        {
            if(stepDescriptionText) stepDescriptionText.text = steps[_currentStep].StepDescription;
        }
        
    }

    private void ResetObject(Grabbable obj)
    {
        Debug.Log("Resetting incorrect object: " + obj.name);
        obj.transform.position = obj.GetComponent<Rigidbody>().position;
        obj.transform.rotation = obj.GetComponent<Rigidbody>().rotation;
    }

    private void TriggerErrorHaptics(float frequency = 0.7f,float amplitude = 0.7f, float duration = 0.15f)
    {
        if (InputBridge.Instance != null)
        {
            InputBridge.Instance.VibrateController(frequency, amplitude, duration,ControllerHand.Left);
            InputBridge.Instance.VibrateController(frequency, amplitude, duration,ControllerHand.Right);
        }
    }
}