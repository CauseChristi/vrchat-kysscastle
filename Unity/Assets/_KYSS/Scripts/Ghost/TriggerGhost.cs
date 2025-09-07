using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TriggerGhost : UdonSharpBehaviour
{
    [Header("Ghost (disabled in scene)")]
    public GameObject ghostObject;

    [Header("Spawn Chance")]
    [Range(0f, 100f)]
    public float spawnChancePercent = 100f;

    [Header("Timing")]
    [Tooltip("If ghost has no AudioSource/Clip, use this lifetime (seconds).")]
    public float fallbackLifetime = 3f;

    [Tooltip("Extra padding added to audio clip length (seconds).")]
    public float paddingSeconds = 0.1f;

    [Header("Misc")]
    public bool debugLogs = false;

    private float despawnTime = -1f;
    private bool isActive = false;

    private void Start()
    {
        if (ghostObject != null && ghostObject.activeSelf)
        {
            ghostObject.SetActive(false); // Ensure starts disabled
        }
    }

    private void Update()
    {
        if (isActive && Time.time >= despawnTime)
        {
            if (ghostObject != null)
            {
                ghostObject.SetActive(false);
                if (debugLogs) Debug.Log("[TriggerGhost] Ghost disabled.");
            }
            isActive = false;
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) return;
        if (ghostObject == null) return;
        if (isActive) return;

        float roll = Random.Range(0f, 100f);
        if (roll > spawnChancePercent)
        {
            if (debugLogs) Debug.Log($"[TriggerGhost] Skipped (roll {roll:0.0} > chance {spawnChancePercent:0.0}).");
            return;
        }

        // Activate ghost
        ghostObject.SetActive(true);
        isActive = true;

        float life = GetClipLengthOrFallback(ghostObject) + paddingSeconds;
        despawnTime = Time.time + life;

        if (debugLogs) Debug.Log($"[TriggerGhost] Activated for ~{life:0.00}s (roll {roll:0.0}).");
    }

    private float GetClipLengthOrFallback(GameObject g)
    {
        AudioSource src = g.GetComponent<AudioSource>();
        if (src == null) src = g.GetComponentInChildren<AudioSource>();

        if (src != null && src.clip != null)
        {
            if (src.loop)
            {
                if (debugLogs) Debug.LogWarning("[TriggerGhost] Audio is looping; using fallback lifetime.");
                return Mathf.Max(0.05f, fallbackLifetime);
            }
            return Mathf.Max(0.05f, src.clip.length);
        }

        if (debugLogs) Debug.LogWarning("[TriggerGhost] No AudioSource/clip found; using fallback lifetime.");
        return Mathf.Max(0.05f, fallbackLifetime);
    }
}
