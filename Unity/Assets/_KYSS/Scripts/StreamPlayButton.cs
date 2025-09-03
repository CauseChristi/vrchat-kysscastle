using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("Udon/Media/Stream Play Button")]
public class StreamPlayButton : UdonSharpBehaviour
{
    public StreamVideoAudioManager manager;
    [Tooltip("Index into manager.urls / manager.outputs")]
    public int index = 0;
    public bool toggle = true; // if false, always PlayIndex; if true, ToggleIndex

    public override void Interact()
    {
        if (manager == null) return;
        if (toggle) manager.ToggleIndex(index);
        else manager.PlayIndex(index);
    }
}
