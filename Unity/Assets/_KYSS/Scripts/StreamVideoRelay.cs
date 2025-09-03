using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Video.Components;

public class StreamVideoRelay : UdonSharpBehaviour
{
    public VRCUnityVideoPlayer player;                 // drag your VRCUnityVideoPlayer here
    public StreamVideoAudioManager manager;            // drag your manager here

    private void Start()
    {
        if (player == null) player = GetComponent<VRCUnityVideoPlayer>();
    }

    // Manager calls these so the play/stop originate on the player object
    public void PlayUrl(VRCUrl url)
    {
        if (player != null && url != null && url.Get() != "") player.PlayURL(url);
    }
    public void StopPlayback()
    {
        if (player != null) player.Stop();
    }

    // Forward the video events back to the manager
    public override void OnVideoReady()  { if (manager != null) manager._OnVideoReady(); }
    public override void OnVideoStart()  { if (manager != null) manager._OnVideoStart(); }
    public override void OnVideoEnd()    { if (manager != null) manager._OnVideoEnd(); }
}
