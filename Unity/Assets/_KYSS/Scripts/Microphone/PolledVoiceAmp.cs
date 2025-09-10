using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PolledVoiceAmp : UdonSharpBehaviour
{
    [Header("Amplified Voice Settings")]
    public float voiceDistanceFar = 75f;
    public float voiceGain = 15f;
    public MeshRenderer LEDIndicator;

    [Header("Mic Amplifier Settings")]
    [Tooltip("PlayerTag key used to mark who is currently amp’d on this client.")]
    public string tagString = "VoiceAmpZone";

    public GeneralWhitelist generalWhitelist;
    public bool whitelistedOnly = false;

    [Header("Scan Settings")]
    [Tooltip("How often to rescan players (seconds).")]
    public float scanInterval = 0.25f;
    [Tooltip("If true, uses head position for inside-check; else uses root position.")]
    public bool useHeadForInsideCheck = true;

    [Header("Defaults to restore when leaving")]
    public float defaultDistanceFar = 25f;
    public float defaultGain = 15f;
    public bool  defaultLowpass = true;

    [Header("Debug")]
    public bool debugMode = false;
    [HideInInspector] public bool isThereChairsInTrigger = false;

    // Internal state
    private bool isEnabled = false;
    private float _nextScan;
    private Collider _col;

    private VRCPlayerApi[] _players = new VRCPlayerApi[80];
    private bool[] _isAmped = new bool[80];

    // LED colors
    private readonly Color _amplifiedColor = Color.green;
    private Color _defaultEmissionColor;

    void Start()
    {
        _col = GetComponent<Collider>();
        if (_col == null)
        {
            if (debugMode) Debug.LogWarning("[VoiceAmp] No Collider found on this GameObject.");
        }

        if (LEDIndicator)
        {
            // Cache current emission color (material instance)
            _defaultEmissionColor = LEDIndicator.material.GetColor("_EmissionColor");
            if (!isEnabled) LEDIndicator.material.DisableKeyword("_EMISSION");
        }
    }

    public void SetMicAmplifier(bool enabled)
    {
        isEnabled = enabled;
        if (_col != null) _col.enabled = enabled;

        if (enabled)
        {
            if (LEDIndicator) LEDIndicator.material.EnableKeyword("_EMISSION");
        }
        else
        {
            if (LEDIndicator) LEDIndicator.material.DisableKeyword("_EMISSION");
            ResetAllAmpedPlayersVoices();
        }
    }

    void Update()
    {
        if (!isEnabled || _col == null) return;

        if (Time.time >= _nextScan)
        {
            _nextScan = Time.time + scanInterval;
            ScanPlayers();
        }
    }

    private void ScanPlayers()
    {
        // Fetch current players
        for (int i = 0; i < _players.Length; i++) { _players[i] = null; }
        VRCPlayerApi.GetPlayers(_players);

        bool anyAmped = false;

        for (int i = 0; i < _players.Length; i++)
        {
            VRCPlayerApi p = _players[i];
            if (p == null || !p.IsValid()) continue;

            // Whitelist gate (if requested)
            if (whitelistedOnly && (generalWhitelist == null || !generalWhitelist.IsPlayerWhitelisted(p)))
            {
                // If we previously amp’d them, reset.
                if (_isAmped[i]) ResetOne(p, i);
                continue;
            }

            // Determine if player is inside this zone on THIS client
            Vector3 testPos = useHeadForInsideCheck ? p.GetBonePosition(HumanBodyBones.Head) : p.GetPosition();
            bool inside = _col.bounds.Contains(testPos);

            // Special edge-case: if seated chairs may offset the collider check,
            // you can additionally check chair state and force inside/enter (opt-in).
            if (!inside && isThereChairsInTrigger)
            {
                // A light, optional extra: if head is below/above bounds slightly, fudge a little:
                // Expand bounds a bit to be forgiving while seated.
                Bounds expanded = _col.bounds;
                expanded.Expand(new Vector3(0.1f, 0.5f, 0.1f));
                inside = expanded.Contains(testPos);
            }

            if (inside)
            {
                if (!_isAmped[i])
                {
                    // Enter amp on this client
                    ApplyAmp(p, i);
                    anyAmped = true;
                }
                else
                {
                    anyAmped = true; // still amp’d
                }
            }
            else
            {
                if (_isAmped[i])
                {
                    // Exit amp on this client
                    ResetOne(p, i);
                }
            }
        }

        // LED state
        if (LEDIndicator && isEnabled)
        {
            LEDIndicator.material.SetColor("_EmissionColor", anyAmped ? _amplifiedColor : _defaultEmissionColor);
        }
    }

    private void ApplyAmp(VRCPlayerApi player, int index)
    {
        _isAmped[index] = true;
        // Tag is local-only and just helps us track locally
        player.SetPlayerTag(tagString, "true");
        player.SetVoiceDistanceFar(voiceDistanceFar);
        player.SetVoiceGain(voiceGain);
        player.SetVoiceLowpass(false);

        if (debugMode) Debug.Log($"[VoiceAmp] Amp ON → {player.displayName}");
    }

    private void ResetOne(VRCPlayerApi player, int index)
    {
        _isAmped[index] = false;
        // Clear tag and restore defaults
        player.SetPlayerTag(tagString, null);
        player.SetVoiceDistanceFar(defaultDistanceFar);
        player.SetVoiceGain(defaultGain);
        player.SetVoiceLowpass(defaultLowpass);

        if (debugMode) Debug.Log($"[VoiceAmp] Amp OFF → {player.displayName}");
    }

    public void ResetAllAmpedPlayersVoices()
    {
        // Clear local tags & restore defaults for anyone we think is amp’d
        for (int i = 0; i < _players.Length; i++)
        {
            VRCPlayerApi p = _players[i];
            if (p != null && _isAmped[i])
            {
                ResetOne(p, i);
            }
            _isAmped[i] = false;
        }
    }

    // Optional: if you still want a chair-based helper (kept from your original),
    // you can call this from your seating logic to force a re-scan immediately.
    public void OnChairSit(VRCPlayerApi player)
    {
        if (!isEnabled) return;
        _nextScan = 0f; // force immediate rescan next Update
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        // Best-effort cleanup: find index and mark as not amp’d.
        for (int i = 0; i < _players.Length; i++)
        {
            if (_players[i] != null && _players[i].playerId == player.playerId)
            {
                _isAmped[i] = false;
                break;
            }
        }
    }
}
