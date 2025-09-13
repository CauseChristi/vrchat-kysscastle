using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class NetworkedAudio : UdonSharpBehaviour
{
    public AudioSource src;

    public void PlayLocal()
    {
        if (src != null) src.Play();
    }

    public void PlayForAll()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(RPC_Play));
    }

    public void RPC_Play()
    {
        if (src != null) src.Play();
    }
}
