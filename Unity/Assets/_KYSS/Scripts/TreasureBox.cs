using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Enums;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TreasureBox : UdonSharpBehaviour
{
    [Header("=== Required Key (pick ONE linking style) ===")]
    public VRC_Pickup requiredKeyPickup;
    public GameObject requiredKeyRoot;

    [Header("=== Lid & Gem ===")]
    public Transform lid;
    public GameObject gemObject;
    [Tooltip("Delta from CLOSED rotation. Default: Y -45°")]
    public Vector3 lidOpenDelta = new Vector3(0f, -45f, 0f);
    [Tooltip("Degrees per second for the lid motion")]
    public float lidLerpSpeed = 120f;

    [Header("=== FX on Unlock ===")]
    public AudioSource unlockSfx;
    public ParticleSystem unlockParticles;
    public GameObject particleObject;

    [Header("=== Optional External Trigger ===")]
    public UdonSharpBehaviour beginReceiver;
    public bool replayBeginForLateJoiners = false;

    [Header("=== Interaction Prompt (optional) ===")]
    [Tooltip("Optional child root (with its own VRCInteractable + collider) we can SetActive(true/false). Leave null to skip.")]
    public GameObject promptRoot;

    [Tooltip("Optional world-space TMP label we control dynamically.")]
    public TMP_Text promptText;

    [Tooltip("TMP text while LOCKED (if promptText set).")]
    public string interactionTextLocked = "Use the correct key";

    [Tooltip("TMP text while UNLOCKED (if promptText set).")]
    public string interactionTextUnlocked = "";

    [Tooltip("If true, we hide promptRoot and TMP label when unlocked.")]
    public bool hidePromptWhenUnlocked = true;



    [Header("=== Key Disable on Unlock ===")]
    [Tooltip("If set, this object is disabled on unlock. If null, we try 'requiredKeyRoot' then 'requiredKeyPickup.gameObject'.")]
    public GameObject keyRootToDisable;

    [Header("=== Debug ===")]
    public bool debugLogs = false;

    [UdonSynced] private bool _isUnlockedSynced = false;

    // Lid rotation state
    private Quaternion _lidClosedRot;
    private Quaternion _lidTargetRot;
    private bool _isLerpingLid;

    // Locals
    private bool _appliedOnce;   // SFX/particles guard
    private bool _beginInvoked;  // Begin() guard

    private void Start()
    {
        if (lid != null)
        {
            _lidClosedRot = lid.localRotation;
            _lidTargetRot = _lidClosedRot;
        }

        // Initialize prompt for locked/unlocked
        SetPromptLocked(!_isUnlockedSynced);

        // Apply visuals for initial state (late join aware)
        ApplyState(_isUnlockedSynced, false, _isUnlockedSynced);
    }

    private void Update()
    {
        if (!_isLerpingLid || lid == null) return;

        float maxStep = lidLerpSpeed * Time.deltaTime;
        lid.localRotation = Quaternion.RotateTowards(lid.localRotation, _lidTargetRot, maxStep);

        if (Quaternion.Angle(lid.localRotation, _lidTargetRot) <= 0.1f)
        {
            lid.localRotation = _lidTargetRot;
            _isLerpingLid = false;
        }
    }

    public override void Interact() 
    { 
        // Just show some text on hover
    }

    public override void OnDeserialization()
    {
        ApplyState(_isUnlockedSynced, true, false);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (_isUnlockedSynced) return;
        if (!Utilities.IsValid(Networking.LocalPlayer)) return;

        if (IsCorrectKeyCollider(other.gameObject))
        {
            if (debugLogs) Debug.Log($"[TreasureBox] Unlock by {Networking.LocalPlayer.displayName}");

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _isUnlockedSynced = true;
            RequestSerialization();

            ApplyState(true, true, false);
        }
        else if (debugLogs)
        {
            Debug.Log($"[TreasureBox] Wrong key/collider.");
        }
    }

    private bool IsCorrectKeyCollider(GameObject hit)
    {
        if (requiredKeyPickup != null)
        {
            VRC_Pickup p = hit.GetComponent<VRC_Pickup>();
            if (p == null)
            {
                Transform t = hit.transform.parent;
                while (t != null && p == null)
                {
                    p = t.GetComponent<VRC_Pickup>();
                    t = t.parent;
                }
            }
            if (p == requiredKeyPickup) return true;
        }

        if (requiredKeyRoot != null)
        {
            Transform t = hit.transform;
            Transform root = requiredKeyRoot.transform;
            while (t != null)
            {
                if (t == root) return true;
                t = t.parent;
            }
        }
        return false;
    }

    private void ApplyState(bool isOpen, bool playFx, bool isLateJoin)
    {
        // Target rotation & animate
        if (lid != null)
        {
            Quaternion openRot = _lidClosedRot * Quaternion.Euler(lidOpenDelta);
            _lidTargetRot = isOpen ? openRot : _lidClosedRot;
            _isLerpingLid = true;
        }

        // Gem & particles parent
        if (gemObject != null) gemObject.SetActive(isOpen);
        if (particleObject != null) particleObject.SetActive(isOpen);

        // SFX / particles (once per client)
        if (isOpen && playFx && !_appliedOnce)
        {
            if (unlockSfx != null) unlockSfx.Play();
            if (unlockParticles != null) unlockParticles.Play(true);
            _appliedOnce = true;
        }
        if (!isOpen)
        {
            _appliedOnce = false;
            if (unlockParticles != null)
                unlockParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _beginInvoked = false;
        }

        // Interaction prompt state
        SetPromptLocked(!isOpen);

        // Disable key object (all clients will do this when they see open=true)
        if (isOpen) DisableKeyObject();

        // Optional Begin()
        if (beginReceiver != null && isOpen && !_beginInvoked)
        {
            if (!isLateJoin || (isLateJoin && replayBeginForLateJoiners))
            {
                beginReceiver.SendCustomEvent("Begin");
                _beginInvoked = true;
            }
        }
    }

    private void SetPromptLocked(bool locked)
    {
        // 1) Toggle entire child with VRCInteractable
        if (promptRoot != null)
        {
            if (locked)
                promptRoot.SetActive(true);
            else if (hidePromptWhenUnlocked)
                promptRoot.SetActive(false);
        }

        // 2) Toggle TMP text
        if (promptText != null)
        {
            if (locked)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = interactionTextLocked;
            }
            else
            {
                if (hidePromptWhenUnlocked)
                {
                    promptText.gameObject.SetActive(false);
                }
                else
                {
                    promptText.gameObject.SetActive(true);
                    promptText.text = interactionTextUnlocked;
                }
            }
        }
    }




    private void DisableKeyObject()
    {
        GameObject toDisable = keyRootToDisable;
        if (toDisable == null && requiredKeyRoot != null) toDisable = requiredKeyRoot;
        if (toDisable == null && requiredKeyPickup != null) toDisable = requiredKeyPickup.gameObject;

        if (toDisable != null && toDisable.activeSelf)
            toDisable.SetActive(false);

        gameObject.SetActive(false);
    }

    // Local-only reset for testing (NOT networked)
    public void ResetLockLocal()
    {
        if (debugLogs) Debug.Log($"[TreasureBox] Local reset on {gameObject.name}.");
        _isUnlockedSynced = false;
        ApplyState(false, false, false);
        if (lid != null)
        {
            lid.localRotation = _lidClosedRot;
            _isLerpingLid = false;
        }
        // Re-enable key if we disabled it (dev convenience)
        GameObject toEnable = keyRootToDisable ?? requiredKeyRoot ?? (requiredKeyPickup != null ? requiredKeyPickup.gameObject : null);
        if (toEnable != null) toEnable.SetActive(true);
    }
}
