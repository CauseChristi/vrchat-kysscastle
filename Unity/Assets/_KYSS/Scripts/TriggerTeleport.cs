using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TriggerTeleport : UdonSharpBehaviour
{
    [Header("Destination")]
    public Transform target;

    [Header("Whiteout (optional)")]
    public LocalWhiteoutFader whiteoutFader;
    public float whiteoutDelay = 0.75f;     // seconds before teleport (fade-in duration)

    [Tooltip("Optional extra local-space offset applied from the target.")]
    public Vector3 localOffset = Vector3.zero;

    [Header("SFX")]
    public GameObject sfxObject;

    [Header("Activation")]
    public bool useOnTriggerEnter = true;

    [Header("Cooldown")]
    public float cooldownSeconds = 0f;

    [Header("Orientation")]
    public bool faceTarget = true;
    public bool matchYawOnly = true;

    [Header("Misc")]
    public bool debugLogs = false;

    private float _nextAllowedTime = 0f;
    private bool _teleportPending = false;

    private Vector3 _queuedPos;
    private Quaternion _queuedRot;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!useOnTriggerEnter) return;
        TryStartTeleport(player);
    }

    private void TryStartTeleport(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return;
        if (!player.isLocal) return;
        if (_teleportPending) return;
        if (Time.time < _nextAllowedTime) return;
        if (target == null)
        {
            if (debugLogs) Debug.Log("[TriggerTeleport] No target assigned.");
            return;
        }

        // Play SFX
        if (sfxObject != null) sfxObject.SetActive(true);

        // Compute destination
        _queuedPos = target.TransformPoint(localOffset);
        if (faceTarget)
        {
            if (matchYawOnly)
            {
                Vector3 e = target.rotation.eulerAngles;
                _queuedRot = Quaternion.Euler(0f, e.y, 0f);
            }
            else _queuedRot = target.rotation;
        }
        else _queuedRot = player.GetRotation();

        // Run sequence
        if (whiteoutFader != null && whiteoutDelay > 0f)
        {
            _teleportPending = true;

            // keep fade-in duration in sync with the delay
            whiteoutFader.fadeInSeconds = whiteoutDelay;

            whiteoutFader.FadeIn();
            if (debugLogs) Debug.Log("[TriggerTeleport] Fade-in started; queuing teleport...");
            SendCustomEventDelayedSeconds(nameof(DoTeleportNow), whiteoutDelay);
        }
        else
        {
            DoImmediateTeleport();
        }
    }

    // Called via SendCustomEventDelayedSeconds
    public void DoTeleportNow()
    {
        Networking.LocalPlayer.TeleportTo(_queuedPos, _queuedRot);

        // Reset pending now that we've teleported
        _teleportPending = false;

        // Hold full white for this frame, then start fading out next frame
        if (whiteoutFader != null)
        {
            whiteoutFader.HoldOpaqueOneFrame();
            SendCustomEventDelayedFrames(nameof(BeginFadeOut), 1);
        }

        if (cooldownSeconds > 0f) _nextAllowedTime = Time.time + cooldownSeconds;

        if (debugLogs)
            Debug.Log($"[TriggerTeleport] Teleported to {_queuedPos} rot {_queuedRot.eulerAngles}");
    }

    public void BeginFadeOut()
    {
        if (whiteoutFader != null) whiteoutFader.FadeOut();
    }

    private void DoImmediateTeleport()
    {
        Networking.LocalPlayer.TeleportTo(_queuedPos, _queuedRot);
        if (cooldownSeconds > 0f) _nextAllowedTime = Time.time + cooldownSeconds;

        if (debugLogs)
            Debug.Log($"[TriggerTeleport] Teleported (immediate) to {_queuedPos} rot {_queuedRot.eulerAngles}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Vector3 p = target.TransformPoint(localOffset);
        Gizmos.DrawWireSphere(p, 0.25f);
        Vector3 fwd = (matchYawOnly ? Quaternion.Euler(0f, target.eulerAngles.y, 0f) : target.rotation) * Vector3.forward;
        Gizmos.DrawLine(p, p + fwd * 0.75f);
    }
#endif
}
