using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class VRCPickupToggles : UdonSharpBehaviour
{
    [Header("Objects to Toggle")]
    public GameObject[] targetObjects;

    public override void OnPickup()
    {
        // Enable all target objects when picked up
        foreach (var obj in targetObjects)
        {
            if (obj != null) obj.SetActive(true);
        }
    }

    public override void OnDrop()
    {
        // Disable all target objects when dropped
        foreach (var obj in targetObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
