
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ToggleMicAmp : UdonSharpBehaviour
{
    private Button toggle;
    [UdonSynced] private bool isMicAmpEnabled = false;

    [Header("Whitelist Settings")]
    public GeneralWhitelist generalWhitelist;
    public bool whitelistOnly = false;

    public VoiceAmp voiceAmp;

    void Start()
    {
        toggle = GetComponent<Button>();
        SetMicAmpEnabled();
    }

    public override void OnDeserialization()
    {
        SetMicAmpEnabled();
    }

    public void ToggleMicAmpEnabled()
    {
        if (whitelistOnly && !generalWhitelist.IsPlayerWhitelisted(Networking.LocalPlayer)) return;

        if (!Networking.LocalPlayer.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        isMicAmpEnabled = !isMicAmpEnabled;
        RequestSerialization();

        SetMicAmpEnabled();
    }

    public void SetMicAmpEnabled()
    {
        toggle.transform.Find("Background").Find("Checkmark").gameObject.SetActive(isMicAmpEnabled);
        voiceAmp.SetMicAmplifier(isMicAmpEnabled);
    }
}
