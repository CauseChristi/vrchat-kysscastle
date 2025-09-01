using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("Udon/Audio/Audio Play Button")]
public class AudioPlayButton : UdonSharpBehaviour
{
    public AudioManager manager;
    [Tooltip("Which AudioSource index to play from the manager.")]
    public int index = 0;

    public override void Interact()
    {
        if (manager != null) manager.PlayIndex(index);
    }
}
