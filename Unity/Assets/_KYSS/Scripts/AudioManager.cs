using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AudioManager : UdonSharpBehaviour
{
    [Header("Assign all audio sources here (no Play On Awake)")]
    public AudioSource[] sources;

    [Header("Optional")]
    public bool stopResetsTime = true;   // If true, Stop() resets to start. If false, we Pause previous instead.

    private int _current = -1;

    private void Start()
    {
        StopAll();
    }

    public void PlayIndex(int index)
    {
        if (sources == null || index < 0 || index >= sources.Length) return;

        // If already playing this index, restart it
        if (_current == index)
        {
            if (sources[index] != null)
            {
                sources[index].Stop();
                sources[index].Play();
            }
            return;
        }

        // Stop previous
        if (_current >= 0 && _current < sources.Length && sources[_current] != null)
        {
            if (stopResetsTime) sources[_current].Stop();
            else sources[_current].Pause();
        }

        // Play new
        AudioSource s = sources[index];
        if (s != null)
        {
            // Make sure only this one can be heard
            StopAllExcept(index);
            s.Play();
            _current = index;
        }
    }

    public void StopCurrent()
    {
        if (_current >= 0 && _current < sources.Length && sources[_current] != null)
        {
            sources[_current].Stop();
        }
        _current = -1;
    }

    public void StopAll()
    {
        if (sources == null) return;
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null) sources[i].Stop();
        }
        _current = -1;
    }

    public void StopAllExcept(int keepIndex)
    {
        if (sources == null) return;
        for (int i = 0; i < sources.Length; i++)
        {
            if (i == keepIndex) continue;
            if (sources[i] != null) sources[i].Stop();
        }
    }

    public void ToggleIndex(int index)
    {
        if (sources == null || index < 0 || index >= sources.Length) return;

        AudioSource s = sources[index];
        if (s == null) return;

        // If this is the current and playing -> stop it.
        if (_current == index && s.isPlaying)
        {
            StopCurrent();
        }
        else
        {
            PlayIndex(index);
        }
    }

    public void SetVolume(float volume01)
    {
        if (sources == null) return;
        float v = Mathf.Clamp01(volume01);
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null) sources[i].volume = v;
        }
    }

    // ---------- Optional: UI Button bridge ----------
    // Use this with a Unity UI Button (see UI proxy script below).
    public int uiRequestedIndex = -1;
    public void _UIPlayRequested()
    {
        if (uiRequestedIndex >= 0) PlayIndex(uiRequestedIndex);
    }
}
