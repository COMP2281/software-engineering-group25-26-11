using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandWaterDetector : MonoBehaviour
{
    [Header("Physics Settings")]
    public float pushStrength = 10f; 
    private Vector3 previousPosition; 

    [Header("Surface Detection")]
    public string surfaceTag = "Water";
    
    [Header("Audio Feedback")]
    public AudioClip splashSound;
    private AudioSource audioSource;
    private HandPresence handPresence; 
    public float vibrationAmplitude = 0.5f; 
    public float vibrationDuration = 0.1f;

    void Start()
    {
        previousPosition = transform.position;

        // Find HandPresence for controller info
        handPresence = GetComponentInParent<HandPresence>();

        // Setup Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // Automatically add an AudioSource if one is missing
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Make sound 3D (coming from the hand)
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(surfaceTag))
        {
            // Play Sound
            if (audioSource != null && splashSound != null)
            {
                // PlayOneShot allows multiple splashes to overlap naturally
                audioSource.PlayOneShot(splashSound); 
            }

            // Vibrate Controller
            TriggerHapticPulse();
        }
    }

    private void TriggerHapticPulse()
    {        
        if (handPresence != null)
        {   
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(handPresence.controllerCharacteristics, devices);

            if (devices.Count > 0)
            {
                InputDevice device = devices[0];
                device.SendHapticImpulse(0, vibrationAmplitude, vibrationDuration);
            }
        }
    }
}