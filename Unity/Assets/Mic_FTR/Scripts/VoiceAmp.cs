using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class VoiceAmp : UdonSharpBehaviour
{

    [Header("Amplified Voice Settings")]
    public float voiceDistanceFar = 75;
    public float voiceGain = 15;
    public MeshRenderer LEDIndicator;

    [Header("Mic Amplifier Settings")]
    public string tagString = "";

    public bool whitelistedOnly = false;
    bool playerIsWhitelisted = false;
    public string[] whitelist = new string[] { };

    bool isEnabled = true;

    [Header("Debug")]
    public bool debugMode = false;
    [HideInInspector] public bool isThereChairsInTrigger = false;

    // LED Colors
    readonly Color amplified = Color.green;
    Color defaultEnabled;

    VRCPlayerApi[] players = new VRCPlayerApi[80];
    VRCPlayerApi[] amplifiedPlayers = new VRCPlayerApi[80];

    void Start()
    {
        if (LEDIndicator)
        {
            defaultEnabled = LEDIndicator.material.GetColor("_EmissionColor");
            if (!isEnabled) LEDIndicator.material.DisableKeyword("_EMISSION");
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (isEnabled)
        {
            if (whitelistedOnly && !IsPlayerWhitelisted(player)) return;
            PlayerEntersAmp(player, tagString, "true");
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (isEnabled)
        {
            if (whitelistedOnly && !IsPlayerWhitelisted(player)) return;
            PlayerExitsAmp(player, tagString);
        }
    }

    private void SetPlayerVoiceSettings(VRCPlayerApi player, float distance, float gain, bool lowpass, string tag, string tagValue)
    {
        player.SetVoiceDistanceFar(distance);
        player.SetVoiceGain(gain);
        player.SetVoiceLowpass(lowpass);
        player.SetPlayerTag(tag, tagValue);
    }

    private bool IsPlayerWhitelisted(VRCPlayerApi player)
    {
        foreach (string playerInList in whitelist)
        {
            if (player.displayName == playerInList || playerIsWhitelisted)
                return true;
        }
        return false;
    }

    public void PlayerEntersAmp(VRCPlayerApi player, string tag, string tagValue)
    {
        if (player.GetPlayerTag(tag) == "true") return;

        SetPlayerVoiceSettings(player, voiceDistanceFar, voiceGain, false, tag, tagValue);

        AddPlayerToAmplifiedList(player);
    }

    public void PlayerExitsAmp(VRCPlayerApi player, string tag)
    {
        if (player.GetPlayerTag(tag) == null) return;

        SetPlayerVoiceSettings(player, 25, 15, true, tag, null);

        RemovePlayerFromAmplifiedList(player);
    }

    private void AddPlayerToAmplifiedList(VRCPlayerApi player)
    {
        if (Array.IndexOf(amplifiedPlayers, player) == -1)
        {
            for (int i = 0; i < amplifiedPlayers.Length; i++)
            {
                if (amplifiedPlayers[i] == null)
                {
                    amplifiedPlayers[i] = player;
                    break;
                }
            }

            int count = 0;
            foreach (VRCPlayerApi p in amplifiedPlayers)
            {
                if (p != null) count++;
            }

            if (LEDIndicator != null && isEnabled && count > 0)
                LEDIndicator.material.SetColor("_EmissionColor", amplified);

            if (debugMode)
            {
                Debug.Log("Added " + player.displayName + " to the amplified list.");
                Debug.Log("Amplified players: " + count);
            }
        }
    }

    private void RemovePlayerFromAmplifiedList(VRCPlayerApi player)
    {
        if (Array.IndexOf(amplifiedPlayers, player) != -1)
        {
            bool hasRemainingPlayers = false;
            for (int i = 0; i < amplifiedPlayers.Length; i++)
            {
                if (amplifiedPlayers[i] == player)
                    amplifiedPlayers[i] = null;
                else if (amplifiedPlayers[i] != null)
                    hasRemainingPlayers = true;
            }

            if (debugMode)
            {
                Debug.Log("Removed " + player.displayName + " from the amplified list.");

                int count = 0;
                foreach (VRCPlayerApi p in amplifiedPlayers)
                {
                    if (p != null) count++;
                }

                Debug.Log("Amplified players: " + count);
            }

            if (LEDIndicator != null && isEnabled && !hasRemainingPlayers)
                LEDIndicator.material.SetColor("_EmissionColor", defaultEnabled);
        }
    }

    public void SetMicAmplifier(bool isEnabled)
    {
        this.isEnabled = isEnabled;
        gameObject.GetComponent<Collider>().enabled = isEnabled;

        if (isEnabled)
        {
            if (LEDIndicator) LEDIndicator.material.EnableKeyword("_EMISSION");
        }
        else
        {
            if (LEDIndicator) LEDIndicator.material.DisableKeyword("_EMISSION");
            ResetAllAmpedPlayersVoices();
        }
    }

    public void EnableWhitelisted()
    {
        playerIsWhitelisted = true;
    }

    public void ResetAllAmpedPlayersVoices()
    {
        VRCPlayerApi.GetPlayers(players);
        foreach (VRCPlayerApi player in players)
        {
            if (player == null || player.GetPlayerTag(tagString) == null) continue;

            if (debugMode) Debug.Log("Resetting " + player.displayName + "'s voice");
            SetPlayerVoiceSettings(player, 25, 15, true, tagString, null);
        }

        amplifiedPlayers = new VRCPlayerApi[80];
    }

    public void OnChairSit(VRCPlayerApi player)
    {
        if (!isThereChairsInTrigger || player.GetPlayerTag(tagString) == "true" || !isEnabled) return;

        Bounds ampBounds = gameObject.GetComponent<Collider>().bounds;
        if (ampBounds.Contains(player.GetBonePosition(HumanBodyBones.Head)))
            PlayerEntersAmp(player, tagString, "true");
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (Array.IndexOf(amplifiedPlayers, player) != -1)
            RemovePlayerFromAmplifiedList(player);
    }
}