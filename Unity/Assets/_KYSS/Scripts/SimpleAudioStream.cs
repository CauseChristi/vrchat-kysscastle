using UdonSharp;
using UnityEngine;
using VRC.SDKBase;                    // VRCUrl
using VRC.SDK3.Video.Components;      // VRCUnityVideoPlayer

public class SimpleAudioStream : UdonSharpBehaviour
{
    [Header("Refs")]
    public VRCUnityVideoPlayer player;
    private AudioSource audioSource;   // auto-found

    [Header("Playlist (fill in Inspector)")]
    public VRCUrl[] playlist;
    public int startIndex = 0;

    [Header("Options")]
    public bool autoPlay = true;
    public bool loopTrack = true;      // loop current track
    public bool loopPlaylist = true;   // when using Next/Prev
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float spatialBlend = 0f;

    private int _index = 0;
    private bool _ready;
    private bool _isPlaying;

    private void Start()
    {
        // auto-find AudioSource on same GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.spatialBlend = spatialBlend;
        }

        if (playlist != null && playlist.Length > 0)
        {
            _index = Mathf.Clamp(startIndex, 0, playlist.Length - 1);
            if (autoPlay) PlayCurrent();
        }
    }

    private void PlayCurrent()
    {
        if (player == null || playlist == null || playlist.Length == 0) return;

        VRCUrl url = playlist[_index];
        if (url == null) return;

        string s = url.Get();
        if (string.IsNullOrEmpty(s)) return;

        _ready = false;
        _isPlaying = true;
        player.PlayURL(url);
    }

    // Public controls
    public void Play()  { PlayCurrent(); }
    public void Pause() { if (player != null) { player.Pause(); _isPlaying = false; } }
    public void Stop()  { if (player != null) { player.Stop();  _isPlaying = false; } }

    public void Toggle()
    {
        if (_isPlaying) Stop();
        else PlayCurrent();
    }

    public void Next()
    {
        if (playlist == null || playlist.Length == 0) return;
        _index++;
        if (_index >= playlist.Length)
        {
            if (loopPlaylist) _index = 0;
            else { _index = playlist.Length - 1; return; }
        }
        PlayCurrent();
    }

    public void Prev()
    {
        if (playlist == null || playlist.Length == 0) return;
        _index--;
        if (_index < 0)
        {
            if (loopPlaylist) _index = playlist.Length - 1;
            else { _index = 0; return; }
        }
        PlayCurrent();
    }

    public void PlayIndex(int i)
    {
        if (playlist == null || playlist.Length == 0) return;
        _index = Mathf.Clamp(i, 0, playlist.Length - 1);
        PlayCurrent();
    }

    public void SetUrl(VRCUrl newUrl)
    {
        if (newUrl == null) return;
        string s = newUrl.Get();
        if (string.IsNullOrEmpty(s)) return;

        playlist = new VRCUrl[] { newUrl };
        _index = 0;
        PlayCurrent();
    }

    public override void OnVideoReady()
    { 
        Debug.Log("[Stream] OnVideoReady");
        _ready = true; 
    }
    public override void OnVideoEnd()
    {
        Debug.Log("[Stream] OnVideoEnd");
        if (loopTrack) { PlayCurrent(); return; }
        Next();
    }
    
    public override void OnVideoStart(){ Debug.Log("[Stream] OnVideoStart"); }

}

