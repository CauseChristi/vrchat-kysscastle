
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StoolKon : UdonSharpBehaviour
{
    public GameObject[] KonStools;
    public UdonBehaviour MikoUdon;
    [UdonSynced] public bool Enabled;

    public override void OnDeserialization()
    {
        ToggleKonStool(Enabled);
    }

    public override void Interact()
    {
        if (!Networking.LocalPlayer.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        Enabled = !Enabled;
        RequestSerialization();
        ToggleKonStool(Enabled);
    }

    public void ToggleKonStool(bool enabled)
    {
        // Loop through each KonStool in the array and set its active state to the value of the 'enabled' parameter
        foreach (GameObject Stool in KonStools)
        {
            Stool.SetActive(enabled);
        }

        // Set the 'KonEnabled' program variable on the 'MikoUdon' UdonBehaviour to the value of the 'enabled' parameter
        MikoUdon.SetProgramVariable("KonEnabled", enabled);
    }
}
