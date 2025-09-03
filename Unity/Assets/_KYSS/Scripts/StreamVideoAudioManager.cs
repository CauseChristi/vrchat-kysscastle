using UdonSharp;
using UnityEngine;
using VRC.SDKBase;                    // VRCUrl
using VRC.SDK3.Video.Components;      // VRCUnityVideoPlayer

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StreamVideoAudioManager : UdonSharpBehaviour
{
    [Header("Shared Player")]
    public VRCUnityVideoPlayer player;

    [Header("Per-Button Setup (same length arrays)")]
    public VRCUrl[] urls;         // one per button
    public AudioSource[] outputs; // one per button (spatialize + VRCSpatialAudio as you like)

    // in StreamVideoAudioManager
    public StreamVideoRelay relay;   // assign in Inspector


    [Header("Options")]
    public bool autoStopOnSwitch = true; // stop before switching URL
    public bool loopTrack = false;       // replay same index on end
    public bool useMuteNotVolume = false; // gate others with mute instead of volume=0
    [Range(0f,1f)] public float selectedVolume = 1f; // volume for the active output
    public bool debugLogs = true;

    private int _current = -1;
    private bool _isPlaying = false;
    private float[] _origVolumes; // remember per-source default volumes

    private void Start()
    {
        if (outputs != null && outputs.Length > 0)
        {
            _origVolumes = new float[outputs.Length];
            for (int i = 0; i < outputs.Length; i++)
            {
                var s = outputs[i];
                _origVolumes[i] = (s != null) ? s.volume : 1f;
            }
        }

        GateAllOff();
        _current = -1;
        _isPlaying = false;

        if (debugLogs) Debug.Log("[StreamMgr] Ready. Player=" + (player ? "OK" : "NULL"));
    }

    // -------- Public Controls --------

    public void PlayIndex(int index)
    {
        if (!IsValid(index)) { if (debugLogs) Debug.LogWarning("[StreamMgr] PlayIndex invalid: " + index); return; }
        if (player == null) { if (debugLogs) Debug.LogWarning("[StreamMgr] No player set."); return; }

        if (debugLogs) Debug.Log("[StreamMgr] Request Play index=" + index);

        // Clean handoff if something is playing
        if (autoStopOnSwitch && _isPlaying && relay != null) relay.StopPlayback();

        // Route audio: make only this output audible
        GateOnly(index);

        // Kick playback
        var url = urls[index];
        if (url == null || string.IsNullOrEmpty(url.Get()))
        {
            if (debugLogs) Debug.LogWarning("[StreamMgr] URL missing at index " + index);
            return;
        }

        _current = index;
        _isPlaying = true;
        if (relay != null) relay.PlayUrl(url);
    }

    public void ToggleIndex(int index)
    {
        if (!IsValid(index)) return;

        if (_current == index && _isPlaying)
            StopCurrent();
        else
            PlayIndex(index);
    }

    public void StopCurrent()
    {
        if (player != null) player.Stop();
        _isPlaying = false;
        _current = -1;
        GateAllOff();
        if (debugLogs) Debug.Log("[StreamMgr] Stopped current.");
    }

    public void StopAll()
    {
        if (player != null) player.Stop();
        _isPlaying = false;
        _current = -1;
        GateAllOff();
        if (debugLogs) Debug.Log("[StreamMgr] Stopped all.");
    }

    public void SetGlobalVolume(float volume01)
    {
        float v = Mathf.Clamp01(volume01);
        for (int i = 0; i < outputs.Length; i++)
            if (outputs[i] != null) outputs[i].volume = v;
    }

    // -------- Player Callbacks --------

    public override void OnVideoReady()
    {
        if (debugLogs) Debug.Log("[StreamMgr] OnVideoReady (index " + _current + ")");
    }

    public override void OnVideoStart()
    {
        _isPlaying = true;
        if (debugLogs) Debug.Log("[StreamMgr] OnVideoStart (index " + _current + ")");
    }

    public override void OnVideoEnd()
    {
        if (debugLogs) Debug.Log("[StreamMgr] OnVideoEnd (index " + _current + ")");
        if (loopTrack && _current >= 0) PlayIndex(_current);
        else _isPlaying = false;
    }

    // -------- Helpers --------

    private bool IsValid(int index)
    {
        if (urls == null || outputs == null) return false;
        if (index < 0 || index >= urls.Length) return false;
        if (outputs.Length != urls.Length)
        {
            if (debugLogs) Debug.LogWarning("[StreamMgr] urls.length != outputs.length. Please match them.");
            return false;
        }
        return true;
    }

    private void GateOnly(int index)
    {
        for (int i = 0; i < outputs.Length; i++)
        {
            var s = outputs[i];
            if (s == null) continue;

            if (i == index)
            {
                if (useMuteNotVolume)
                {
                    s.mute = false;
                    s.volume = _origVolumes != null ? _origVolumes[i] : selectedVolume;
                }
                else
                {
                    s.mute = false;
                    s.volume = selectedVolume;
                }
                // ensure component stays enabled (don’t disable)
                s.enabled = true;
            }
            else
            {
                if (useMuteNotVolume)
                {
                    s.mute = true;
                }
                else
                {
                    s.mute = false;
                    s.volume = 0f;
                }
                s.enabled = true; // keep enabled so the player keeps routing audio
            }
        }
    }

    private void GateAllOff()
    {
        for (int i = 0; i < outputs.Length; i++)
        {
            var s = outputs[i];
            if (s == null) continue;

            if (useMuteNotVolume)
            {
                s.mute = true;
            }
            else
            {
                s.mute = false;
                s.volume = 0f;
            }
            s.enabled = true;
        }
    }

    public void _OnVideoReady() { if (debugLogs) Debug.Log("[StreamMgr] OnVideoReady (index " + _current + ")"); }
    public void _OnVideoStart() { _isPlaying = true; if (debugLogs) Debug.Log("[StreamMgr] OnVideoStart (index " + _current + ")"); }
    public void _OnVideoEnd()
    {
        if (debugLogs) Debug.Log("[StreamMgr] OnVideoEnd (index " + _current + ")");
        if (loopTrack && _current >= 0) PlayIndex(_current); else _isPlaying = false;
    }
}
