using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandWaterDetection : MonoBehaviour
{
    [Header("Physics Settings")]
    public float pushStrength = 10f;

    [Header("Surface Detection")]
    public string surfaceTag = "Surface";

    [Header("Audio Feedback")]
    public AudioClip splashSound;
    public AudioClip moveSound;
    
    private AudioSource moveSource;
    private AudioSource splashSource;

    // Tracking Variables
    private HandPresence handPresence;
    private Vector3 previousPosition;
    private Vector3 smoothVelocity; 
    private float currentHandSpeed;
    private bool isInsideWater = false;

    void Start()
    {
        previousPosition = transform.position;
        handPresence = GetComponentInParent<HandPresence>();
        moveSource = GetComponent<AudioSource>();
        if (moveSource == null)
        {
            moveSource = gameObject.AddComponent<AudioSource>();
        }
        moveSource.spatialBlend = 1.0f;
        moveSource.clip = moveSound;
        moveSource.loop = true;
        moveSource.volume = 0; // Start silent
        moveSource.playOnAwake = false;
        
        // We create a secondary source so fading the move sound doesn't kill the splash
        splashSource = gameObject.AddComponent<AudioSource>();
        splashSource.spatialBlend = 1.0f;
        splashSource.volume = 0.5f;

        // Start the loop immediately (at volume 0) so it's ready to fade in
        if (moveSound != null) moveSource.Play();
    }

    void Update()
    {
        Vector3 rawVelocity = (transform.position - previousPosition) / Time.deltaTime;
        smoothVelocity = Vector3.Lerp(smoothVelocity, rawVelocity, Time.deltaTime * 20); 
        currentHandSpeed = smoothVelocity.magnitude;
        
        previousPosition = transform.position;

        if (!isInsideWater)
        {
            moveSource.volume = Mathf.Lerp(moveSource.volume, 0, Time.deltaTime * 5);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(surfaceTag))
        {
            isInsideWater = true;
            
            RippleEffect.Instance.RippleAtPoint(transform.position);

            if (splashSound != null)
            {
                splashSource.pitch = Random.Range(0.85f, 1.15f);
                splashSource.PlayOneShot(splashSound);
            }

            TriggerHapticPulse(0.8f, 0.15f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(surfaceTag))
        {
            isInsideWater = true;
            Rigidbody rb = other.attachedRigidbody;

            RippleEffect.Instance.RippleAtPoint(transform.position);

            if (rb != null && currentHandSpeed > 0.1f)
            {
                rb.AddForce(smoothVelocity.normalized * pushStrength * currentHandSpeed, ForceMode.Impulse);
            }

            ProcessContinuousFeedback();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(surfaceTag))
        {
            isInsideWater = false;
        }
    }

    private void ProcessContinuousFeedback()
    {
        float intensity = Mathf.Clamp01(currentHandSpeed / 0.2f);
        float targetVolume = intensity > 0.05f ? Mathf.Lerp(0.1f, 1.0f, intensity*2) : 0f;
        
        moveSource.volume = Mathf.Lerp(moveSource.volume, targetVolume, Time.deltaTime * 10);
        moveSource.pitch = Mathf.Lerp(0.8f, 1.2f, intensity);

        if (intensity > 0.05f)
        {
            TriggerHapticPulse(intensity * 0.4f, Time.deltaTime + 0.02f);
        }
    }

    private void TriggerHapticPulse(float amplitude, float duration)
    {
        if (handPresence == null) return;

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(handPresence.controllerCharacteristics, devices);

        if (devices.Count > 0)
        {
            InputDevice device = devices[0];
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }
}