// TriggerMirror.cs
// UdonSharp – Unity 2022.3 (VRChat)
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TriggerMirror : UdonSharpBehaviour
{
    [Header("Actual Mirror")]
    [Tooltip("Game object for the actual VRChat mirror.")]
    public GameObject mirror;
    private MeshRenderer mr;

    private void Start()
    {
        if (mirror == null) return;
        mr = gameObject.GetComponent<MeshRenderer>();
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        EnableMirror();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        DisableMirror();
    }

    private void EnableMirror()
    {
        mirror.SetActive(true);
        if (mr != null) mr.enabled = false;
    }

    private void DisableMirror()
    {
        if (mr != null) mr.enabled = true;
        mirror.SetActive(false);
    }
}
