using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;

public class PosterLoader : UdonSharpBehaviour
{
    [Header("Target")]
    public Renderer targetRenderer;
    public int materialIndex = 0;
    [Tooltip("Built-in RP: _MainTex (default). URP Lit: _BaseMap.")]
    public string textureProperty = "_MainTex";

    [Header("Source")]
    public VRCUrl imageUrl;

    [Header("Options")]
    public bool instantiateMaterial = true;

    [Header("Download Timing")]
    [Tooltip("Delay (in seconds) before starting the download.")]
    public float startDelay = 0f;

    // internals
    private VRCImageDownloader _downloader;
    private Material _mat;
    private IVRCImageDownload _active;
    private float _startTime;
    private bool _started;

    void Start()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) { Debug.LogWarning("[PosterLoader] No Renderer."); return; }

        // material choice
        if (instantiateMaterial)
        {
            var mats = targetRenderer.materials;
            if (materialIndex >= 0 && materialIndex < mats.Length) _mat = mats[materialIndex];
        }
        else
        {
            var mats = targetRenderer.sharedMaterials;
            if (materialIndex >= 0 && materialIndex < mats.Length) _mat = mats[materialIndex];
        }

        if (_mat == null) return;

        if (_downloader == null) _downloader = new VRCImageDownloader();

        _startTime = Time.time + startDelay;
    }

    void Update()
    {
        if (!_started && Time.time >= _startTime)
        {
            _started = true;
            Refresh();
        }
    }

    public void Refresh()
    {
        if (_downloader == null || _mat == null || imageUrl == null) return;

        if (_active != null) { _active.Dispose(); _active = null; }

        var info = new TextureInfo();
        info.MaterialProperty = textureProperty;

        _active = _downloader.DownloadImage(imageUrl, _mat, (IUdonEventReceiver)this, info);
    }

    public void OnImageLoadSuccess(IVRCImageDownload _) { }
    public void OnImageLoadError(IVRCImageDownload _) { }

    void OnDestroy()
    {
        if (_active != null) _active.Dispose();
        if (_downloader != null) _downloader.Dispose();
    }
}
