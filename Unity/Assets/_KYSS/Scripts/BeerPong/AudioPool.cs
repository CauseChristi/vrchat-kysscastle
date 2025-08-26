using UdonSharp;
using UnityEngine;

public class AudioPool : UdonSharpBehaviour
{
    public AudioSource[] pool;
    public float defaultVolume = 1f;

    public void PlayAt(AudioClip clip, Vector3 pos, float volume)
    {
        if (clip == null || pool == null || pool.Length == 0) return;

        for (int i = 0; i < pool.Length; i++)
        {
            var a = pool[i];
            if (a == null) continue;
            if (!a.isPlaying)
            {
                a.transform.position = pos;
                a.clip = clip;
                a.volume = (volume <= 0f) ? defaultVolume : volume;
                a.gameObject.SetActive(true);
                a.Play();
                return;
            }
        }
        // If all busy, reuse first
        var first = pool[0];
        if (first != null)
        {
            first.transform.position = pos;
            first.clip = clip;
            first.volume = (volume <= 0f) ? defaultVolume : volume;
            first.gameObject.SetActive(true);
            first.Play();
        }
    }
}
