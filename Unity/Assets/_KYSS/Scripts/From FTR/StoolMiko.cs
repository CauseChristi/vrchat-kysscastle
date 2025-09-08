
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StoolMiko : UdonSharpBehaviour
{
    public GameObject[] MikoStools;
    public GameObject KonButton;
    public UdonBehaviour KonButtonUdon;

    [UdonSynced] public bool Enabled = false;
    [UdonSynced] public bool KonEnabled = false;

    // When the stools are deserialized, apply the toggle state.
    public override void OnDeserialization()
    {
        ToggleStools(Enabled);
    }

    public override void Interact()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Enabled = !Enabled;
        RequestSerialization();
        ToggleStools(Enabled);
    }

    public void ToggleStools(bool enabled)
    {
        // Set the active state of the stools
        foreach (GameObject Stool in MikoStools)
            Stool.SetActive(enabled);

        // If the Kon button is enabled, send a custom event to the Kon button's UdonBehaviour
        if (KonEnabled && !enabled)
            KonButtonUdon.SendCustomEvent("_interact");

        // Set the active state of the Kon button
        KonButton.SetActive(enabled);
    }
}
