using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class NetworkedAnimatorStarter : UdonSharpBehaviour
{
    [Header("Animator")]
    public Animator animator;
    [Tooltip("Animator layer to play on (usually 0).")]
    public int layerIndex = 0;
    [Tooltip("State to play (must exist in the controller on the layer above).")]
    public string stateName;

    [Header("Clip Reference (optional but recommended)")]
    [Tooltip("If set, used to calculate clip length for accurate late-join syncing.")]
    public AnimationClip clip;

    [Header("Playback")]
    [Tooltip("Ignore a new Start request if already playing.")]
    public bool doNotRestartIfPlaying = true;

    // --- Synced state ---
    [UdonSynced] private bool _isPlaying;
    [UdonSynced] private float _serverStartTime; // seconds since instance start
    [UdonSynced] private float _clipLength;      // filled from 'clip' or cached once

    // Local cache
    private bool _hasInitialized;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (clip != null) _clipLength = clip.length;
        _hasInitialized = true;

        // Ensure consistent animator settings
        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        // Apply current state on join (handles late join into already playing)
        ApplyState();
    }

    public void StartNetworked()
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;

        if (doNotRestartIfPlaying && _isPlaying)
            return;

        // Take ownership so we can serialize
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null && !Networking.IsOwner(gameObject))
            Networking.SetOwner(local, gameObject);

        // Determine/confirm clip length if not set
        if (_clipLength <= 0f)
        {
            if (clip != null) _clipLength = clip.length;
            else
            {
                // Try to find by state name once (best-effort fallback)
                var rac = animator.runtimeAnimatorController;
                if (rac != null)
                {
                    var clips = rac.animationClips;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (clips[i] != null && clips[i].name == stateName)
                        {
                            _clipLength = clips[i].length;
                            break;
                        }
                    }
                }
            }
            if (_clipLength <= 0f) _clipLength = 1f; // safe fallback
        }

        _serverStartTime = (float)Networking.GetServerTimeInSeconds();
        _isPlaying = true;

        // Apply locally immediately…
        ApplyState();
        // …then sync for everyone else
        RequestSerialization();
    }

    public void StopNetworked()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null && !Networking.IsOwner(gameObject))
            Networking.SetOwner(local, gameObject);

        _isPlaying = false;
        RequestSerialization();
        ApplyState();
    }

    public override void OnDeserialization()
    {
        // When new state arrives, play/update locally
        ApplyState();
    }

    private void ApplyState()
    {
        if (!_hasInitialized || animator == null) return;

        if (_isPlaying)
        {
            // How long since the authoritative start?
            float elapsed = Mathf.Max(0f, (float)Networking.GetServerTimeInSeconds() - _serverStartTime);

            // Convert to normalized time within the state (loops if longer than length)
            float normalizedTime = (_clipLength > 0f) ? (elapsed / _clipLength) : 0f;

            // Play the state at the correct point
            animator.speed = 1f;
            animator.Play(stateName, layerIndex, normalizedTime % 1f);
        }
        else
        {
            // Optional: reset animator or pause where it is
            // animator.Play(stateName, layerIndex, 0f);
            // animator.speed = 0f;
        }
    }
}
