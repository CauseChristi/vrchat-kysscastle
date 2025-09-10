using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UdonSharp;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class VRCPickupRespawn : UdonSharpBehaviour
{
    // Audio / Visuals
	public AudioSource respawnSound; // Assign in inspector
    public ParticleSystem respawnEffect; // Assign in inspector

	// Original Transform Data
    private Vector3 originalPosition;
    private Quaternion originalRotation;

	// Components
    private VRCPickup vrcPickup;
    private Rigidbody rb;
	
	
	// Internal Timer
	private bool touched = false;
	public float timerDuration = 10f;
	private float timeElapsed = 0f;

	// Mini API
	public bool lockSpawnAfterFirstCapture = true; // optional safety
	private bool _spawnCapturedOnce = false;
	
	[UdonSynced, FieldChangeCallback(nameof(TimerActive))] 
	private bool _timerActive = false;

	public bool TimerActive 
    {
        get => _timerActive;
        set
        {
            _timerActive = value;
        }
    }

	[UdonSynced, FieldChangeCallback(nameof(UsesGravity))]
    public bool usesGravity;

    public bool UsesGravity
    {
        get => usesGravity;
        set
        {
            usesGravity = value;
        }
    }

	[UdonSynced, FieldChangeCallback(nameof(IsKinematic))]
    private bool isKinematic;

    public bool IsKinematic
    {
        get => isKinematic;
        set
        {
            isKinematic = value;
            ApplyKinematicState();
        }
    }

	[Header("Disable Until Pickup (i.e. Particle Trails)")]
	public GameObject[] gameObjectsList;


	// === START AND UPDATE ========================================
    void Start()
    {
		//Debug.Log("[VRCPickupRespawn] Setting default values...");

		// Get initial data
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        vrcPickup = GetComponent<VRCPickup>();
        rb = (Rigidbody)GetComponent(typeof(Rigidbody));

		// Disable Listed Game Objects
		DisableGameObjectsList();

        // Disable the Rigidbody
        DisableRigidbody();
		ApplyKinematicState();
    }

    void Update()
    {
		if (vrcPickup != null)
		{
			// --- ITEM IS BEING HELD --- //
			if (vrcPickup.IsHeld)
			{
				if (!touched)
					//Debug.Log("[VRCPickupRespawn] Object Grabbed...");
					touched = true;
				
				// Kill the timer
				if (TimerActive)
					ResetTimer();
				
				// Enable Rigidbody
				if (rb.isKinematic)
					EnableRigidbody();

				// Enable Listed Game Objects
				EnableGameObjectsList();
			}

			// --- ITEM IS NOT BEING HELD --- //
			else
			{
				if (TimerActive)
				{
					timeElapsed += Time.deltaTime; // Increment the timer by the time passed since last frame

					if (timeElapsed >= timerDuration)
					{
						TimerFinished();
					}
				}
				else
				{
					if (touched && gameObject.activeInHierarchy)
					{
						//Debug.Log("[VRCPickupRespawn] Object Dropped!");
						StartTimer();
					}
				}

				
			}

			
		}
    }

	// === RESPAWN FUNCTION ========================================
    private void RespawnObject()
    {

		//Debug.Log("[VRCPickupRespawn] Respawning Object...");

		// Blink back to spawn spot
		gameObject.SetActive(false);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
		gameObject.SetActive(true);
        
        // Play sound and particle effects
        if (respawnSound != null)
        {
            respawnSound.Play();
        }
        if (respawnEffect != null)
        {
            respawnEffect.Play();
        }

		// Disable Listed Game Objects
		DisableGameObjectsList();

        // Disable Rigidbody
        DisableRigidbody();

		// Reset touch flag
		touched = false;

        //Debug.Log("[VRCPickupRespawn] Object Respawned!");
    }
	
	// === RIGIDBODY FUNCTIONS ========================================

	private void ApplyKinematicState()
    {
		if (rb != null)
        {
            rb.isKinematic = isKinematic;
        }
    }

    public void SetKinematic(bool state)
    {
		UnityEngine.Debug.Log(" >>> SET KINEMATIC: " + state);
        
        IsKinematic = state;

		// Kinematic ON, frozen in mid air
		if (state)
		{
			rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
			if (usesGravity)
				rb.useGravity = false;
		}

		// Kinematic OFF, rag doll baby!
		else
		{
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			if (usesGravity)
				rb.useGravity = true;
		}
    }

	private void EnableRigidbody()
	{
		if (rb != null)
        {
			//Debug.Log("[VRCPickupRespawn] Enabling Rigidbody...");
			SetKinematic(false);
		}
	}

	private void DisableRigidbody()
	{
		if (rb != null)
        {
			//Debug.Log("[VRCPickupRespawn] Disabling Rigidbody...");
			SetKinematic(true);
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
	}


	// === TIMER FUNCTIONS ========================================

	public void StartTimer()
    {
		//Debug.Log("[VRCPickupRespawn] Timer Started...");
        timeElapsed = 0f; // Reset the time
        TimerActive = true; // Activate the timer
    }

	public void ResetTimer()
    {
		//Debug.Log("[VRCPickupRespawn] Timer Canceled!");
        timeElapsed = 0f; // Reset the time
        TimerActive = false; // Deactivate the timer
    }

	private void TimerFinished()
    {
    	//Debug.Log("[VRCPickupRespawn] Timer Finished!");
		ResetTimer();
		RespawnObject();
    }


	// === GAME OBJECT FUNCTIONS ===================================

	public void EnableGameObjectsList()
	{
		foreach (GameObject go in gameObjectsList) {
			go.SetActive(true);
		}
	}

	public void DisableGameObjectsList()
	{
		foreach (GameObject go in gameObjectsList) {
			go.SetActive(false);
		}
	}

	// === API FUNCTIONS ==================================

	// Call this to set a new respawn pose (e.g., after random spawn picked)
	public void SetRespawnPose(Vector3 pos, Quaternion rot)
	{
		if (lockSpawnAfterFirstCapture && _spawnCapturedOnce) return;
		originalPosition = pos;
		originalRotation = rot;
		_spawnCapturedOnce = true;
	}

	// Convenience: set the current transform as the respawn pose
	public void CaptureCurrentAsSpawn()
	{
		SetRespawnPose(transform.position, transform.rotation);
	}












}
