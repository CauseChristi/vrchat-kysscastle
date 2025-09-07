using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Networked sliding door. Call Open() / Close() from other scripts.
/// Ownership is taken by the caller; open/close state is synced.
/// Plays existing AudioSources at the right times:
/// - Opening started   -> openingSfx.Play()
/// - Reached open      -> openedSfx.Play()
/// - Closing started   -> closingSfx.Play()
/// - Reached closed    -> closedSfx.Play()
/// Move duration is total time in seconds.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DoorSlide : UdonSharpBehaviour
{
    [Header("Slide")]
    public Vector3 slideDelta = new Vector3(1f, 0f, 0f);

    [Tooltip("Total time (seconds) to fully open/close.")]
    public float moveDuration = 1.5f;

    public bool startOpened = false;
    public bool publishInitialStateOnStart = true;

    [Header("Audio Sources (assign existing components)")]
    [Tooltip("Played when movement toward OPEN begins.")]
    public AudioSource openingSfx;

    [Tooltip("Played once fully OPEN.")]
    public AudioSource openedSfx;

    [Tooltip("Played when movement toward CLOSED begins.")]
    public AudioSource closingSfx;

    [Tooltip("Played once fully CLOSED.")]
    public AudioSource closedSfx;

    [Header("Audio Options")]
    [Tooltip("If true, stops all other SFX before playing the current phase's SFX.")]
    public bool stopOthersOnPhase = true;

    [UdonSynced] private bool _netIsOpen;

    private Vector3 _closedLocalPos;
    private Vector3 _openLocalPos;

    private bool _isMoving;
    private bool _wantsOpenLocal;
    private float _moveStartTime;
    private Vector3 _moveStartPos;
    private Vector3 _moveTargetPos;

    public bool IsOpen; // Udon-friendly

    private VRCPlayerApi _local;

    void Start()
    {
        _local = Networking.LocalPlayer;

        _closedLocalPos = transform.localPosition;
        _openLocalPos   = _closedLocalPos + slideDelta;

        if (publishInitialStateOnStart)
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(_local, gameObject);

            _netIsOpen = startOpened;
            RequestSerialization();
            SnapToState(_netIsOpen);

            // Optional: play the arrival SFX for initial state to mirror prior behavior.
            if (_netIsOpen) PlayPhase(openedSfx);
            else            PlayPhase(closedSfx);
        }
        else
        {
            SnapToState(false);
            PlayPhase(closedSfx);
        }
    }

    void Update()
    {
        if (!_isMoving) return;

        float t = (Time.time - _moveStartTime) / moveDuration;
        if (t >= 1f)
        {
            transform.localPosition = _moveTargetPos;
            _isMoving = false;

            if (_wantsOpenLocal)
            {
                IsOpen = true;
                PlayPhase(openedSfx);
            }
            else
            {
                IsOpen = false;
                PlayPhase(closedSfx);
            }
        }
        else
        {
            transform.localPosition = Vector3.Lerp(_moveStartPos, _moveTargetPos, t);
        }
    }

    public void Begin() { Open(); }

    // === Public API ===
    public void Open()
    {
        EnsureOwnership();
        if (_netIsOpen && IsOpen && !_isMoving) return;

        _netIsOpen = true;
        RequestSerialization();
        BeginMove(true);
    }

    public void Close()
    {
        EnsureOwnership();
        if (!_netIsOpen && !IsOpen && !_isMoving) return;

        _netIsOpen = false;
        RequestSerialization();
        BeginMove(false);
    }

    public void Toggle()
    {
        if (_netIsOpen) Close(); else Open();
    }

    public override void OnDeserialization()
    {
        BeginMove(_netIsOpen);
    }

    // === Internals ===
    private void BeginMove(bool openTarget)
    {
        _wantsOpenLocal = openTarget;
        _moveStartPos   = transform.localPosition;
        _moveTargetPos  = openTarget ? _openLocalPos : _closedLocalPos;
        _moveStartTime  = Time.time;
        _isMoving       = true;

        // Phase start SFX
        if (openTarget) PlayPhase(openingSfx);
        else            PlayPhase(closingSfx);
    }

    private void SnapToState(bool openState)
    {
        _wantsOpenLocal = openState;
        _isMoving = false;
        transform.localPosition = openState ? _openLocalPos : _closedLocalPos;
        IsOpen = openState;
    }

    private void EnsureOwnership()
    {
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(_local, gameObject);
    }

    // === Audio helpers ===
    private void PlayPhase(AudioSource src)
    {
        if (stopOthersOnPhase) StopAllPhaseSfxExcept(src);
        if (src != null) src.Play();
    }

    private void StopAllPhaseSfxExcept(AudioSource keep)
    {
        if (openingSfx != null && openingSfx != keep) openingSfx.Stop();
        if (openedSfx  != null && openedSfx  != keep) openedSfx.Stop();
        if (closingSfx != null && closingSfx != keep) closingSfx.Stop();
        if (closedSfx  != null && closedSfx  != keep) closedSfx.Stop();
    }
}
