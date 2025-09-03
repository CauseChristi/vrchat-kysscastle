using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SimpleAudioStreamButton : UdonSharpBehaviour
{
    public SimpleAudioStream targetStream;

    public override void Interact()
    {
        if (targetStream != null)
        {
            targetStream.Toggle();
        }
    }
}
