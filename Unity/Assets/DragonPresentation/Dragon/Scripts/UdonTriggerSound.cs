
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UdonTriggerSound : UdonSharpBehaviour
{
		public AudioSource audioSource;
		public bool allowEarlyRestart = false;

    void OnEnable()
    {
        if (audioSource != null && (allowEarlyRestart || !audioSource.isPlaying))
        {
            audioSource.Play();
        }
    }
		
}
