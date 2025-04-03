using System;
using System.Collections.Generic;
using UnityEngine;
using BNG;
using Unity.VisualScripting;
using UnityEngine.Events;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

public class AssemblyStepManager : MonoBehaviour
{
    [Serializable]
    public class AssemblyStep
    {
        public Grabbable objectToGrab;
        public SnapZone targetSnapZone;
        public UnityEvent onGrabEvents;
        public UnityEvent onSnapEvents;
        /*public UnityEvent btwStepEvents;*/
    }

    
    public List<AssemblyStep> steps = new();
    public AudioClip stepCompleteSound;
    

    public AudioClip errorSound;
    public Material HighlightMaterial;
    public Slider progressBar;
    public Text progressText;

    private int _currentStep = 0;

    private void Start()
    {
        SetupStep(_currentStep);
    }

    private void SetupStep(int stepIndex)
    {
        if (stepIndex >= steps.Count)
        {
            Debug.Log("All steps completed!");
            return;
        }

        var step = steps[stepIndex];
        HighlightObject(step.objectToGrab, true);

        var grabEvents = step.objectToGrab.GetComponent<GrabbableUnityEvents>();
        if (grabEvents)
            grabEvents.onGrab.AddListener((GrabbableEvent) => OnObjectGrabbed(stepIndex));


        else
            Debug.LogWarning("GrabbableEvents script missing on " + step.objectToGrab.name);

        step.targetSnapZone.OnSnapEvent.AddListener((GrabbableEvent) => OnObjectSnapped(stepIndex));
    }

    private void OnObjectGrabbed(int stepIndex)
    {
        Debug.Log("OnObjectGrabbed");
        var step = steps[stepIndex];
        HighlightObject(step.objectToGrab, false);
        HighlightSnapZone(step.targetSnapZone, true);
        
        if ( step.onGrabEvents != null) {
            step.onGrabEvents.Invoke();
        }
        
    }

    

    private void OnObjectSnapped(int stepIndex)
    {
        Debug.Log("OnObjectSnapped");
        var step = steps[stepIndex];
        HighlightSnapZone(step.targetSnapZone, false);
        
        if ( step.onSnapEvents != null) {
            step.onSnapEvents.Invoke();
        }

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
        var renderer = obj.GetComponent<Renderer>();
        var initialmaterial = renderer.material;
        if (renderer)
        {
            renderer.material = highlight ? HighlightMaterial : initialmaterial;
        }
        // if (renderer) renderer.material.color = highlight ? Color.green : Color.white;
    }

    private void HighlightSnapZone(SnapZone zone, bool highlight)
    {
        var renderer = zone.GetComponent<Renderer>();
        var initialmaterial = renderer.material;
        if (renderer)
        {
            renderer.material = highlight ? HighlightMaterial : initialmaterial;
        }
        // if (renderer) renderer.material.color = highlight ? Color.blue : Color.white;
    }

    private void UpdateProgress()
    {
        if (progressBar) progressBar.value = (float)_currentStep / steps.Count;

        if (progressText) progressText.text = $"Step {_currentStep}/{steps.Count}";
    }
}