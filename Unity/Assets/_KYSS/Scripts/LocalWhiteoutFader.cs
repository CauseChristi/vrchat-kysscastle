using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[AddComponentMenu("Udon/Effects/Local Whiteout Fader")]
public class LocalWhiteoutFader : UdonSharpBehaviour
{
    [Header("Plane Setup")]
    public GameObject whiteoutPlane;      // Quad with our UnlitColorAlpha shader (white, alpha 0)
    public float distance = 0.25f;        // meters in front of head
    public float planeScale = 3f;         // scale so it covers view
    public bool lockRotationToHead = true;

    [Header("Fade")]
    public float fadeInSeconds = 0.75f;
    public float fadeOutSeconds = 0.75f;
    public Color baseColor = Color.white; // alpha controlled by fade
    public bool disableWhenTransparent = true;

    [Header("Render Priority")]
    public int renderQueue = 4000;        // 4000 = Overlay (on top)

    // --- Private ---
    private VRCPlayerApi _localPlayer;
    private Renderer _rend;
    private Material _mat;

    private bool _ready = false;

    private const int STATE_IDLE = 0;
    private const int STATE_FADING_IN = 1;
    private const int STATE_FADING_OUT = 2;

    private int _state = STATE_IDLE;
    private float _t = 0f;
    private float _duration = 0f;
    private float _currentAlpha = 0f;

    // one-frame pin after teleport
    private int _pinOpaqueFrames = 0;

    private float Ease(float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        return x * x * (3f - 2f * x); // smoothstep
    }

    public void Start()
    {
        _localPlayer = Networking.LocalPlayer;

        if (whiteoutPlane == null)
        {
            Debug.LogError("[LocalWhiteoutFader] Assign Whiteout Plane (a Quad with unlit transparent white).");
            return;
        }

        _rend = whiteoutPlane.GetComponent<Renderer>();
        if (_rend == null)
        {
            Debug.LogError("[LocalWhiteoutFader] Whiteout Plane needs a Renderer.");
            return;
        }

        // Unique material instance
        _mat = _rend.material;
        if (_mat != null) _mat.renderQueue = renderQueue;

        whiteoutPlane.transform.localScale = new Vector3(planeScale, planeScale, 1f);

        SetAlphaImmediate(0f); // also applies material color
        if (disableWhenTransparent) whiteoutPlane.SetActive(false); // start disabled

        _ready = true;
    }

    // Called by TriggerTeleport right after TeleportTo
    public void HoldOpaqueOneFrame() { _pinOpaqueFrames = 1; }

    private void Update()
    {
        if (!_ready) return;

        // Pin fully opaque for exactly one frame (prevents the 1-frame peek)
        if (_pinOpaqueFrames > 0)
        {
            _pinOpaqueFrames--;
            SetAlphaImmediate(1f);
            return; // skip fading this frame
        }

        // Fade state machine
        if (_state == STATE_IDLE) return;

        if (_duration <= 0f)
        {
            if (_state == STATE_FADING_IN) SetAlphaImmediate(1f);
            else if (_state == STATE_FADING_OUT) SetAlphaImmediate(0f);
            _state = STATE_IDLE;
            return;
        }

        _t += Time.deltaTime / _duration;
        if (_t >= 1f) _t = 1f;

        float eased = Ease(_t);
        if (_state == STATE_FADING_IN)
        {
            SetAlpha(eased);
            if (_t >= 1f) _state = STATE_IDLE;
        }
        else // FADING_OUT
        {
            SetAlpha(1f - eased);
            if (_t >= 1f) _state = STATE_IDLE;
        }
    }

    private void LateUpdate()
    {
        if (!_ready) return;
        if (_localPlayer == null) return;

        var head = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 forward = head.rotation * Vector3.forward;

        // Drive the plane itself
        whiteoutPlane.transform.position = head.position + forward * distance;
        if (lockRotationToHead)
            whiteoutPlane.transform.rotation = head.rotation;
        whiteoutPlane.transform.localScale = new Vector3(planeScale, planeScale, 1f);
    }

    private void ApplyColor(float a)
    {
        if (_mat == null) return;
        Color c = baseColor; c.a = a;
        if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", c);
        else _mat.color = c;
    }

    private void SetAlphaImmediate(float a)
    {
        _t = 1f;
        _state = STATE_IDLE;
        _currentAlpha = Mathf.Clamp01(a);
        ApplyColor(_currentAlpha);
        if (disableWhenTransparent) whiteoutPlane.SetActive(_currentAlpha > 0f);
    }

    private void SetAlpha(float a)
    {
        float clamped = Mathf.Clamp01(a);
        if (Mathf.Approximately(clamped, _currentAlpha)) return;
        _currentAlpha = clamped;

        if (disableWhenTransparent)
        {
            if (clamped <= 0f)
            {
                ApplyColor(0f);
                if (whiteoutPlane.activeSelf) whiteoutPlane.SetActive(false);
                return;
            }
            if (!whiteoutPlane.activeSelf) whiteoutPlane.SetActive(true);
        }

        ApplyColor(clamped);
    }

    // External API
    public void FadeIn()
    {
        if (!_ready) return;
        if (disableWhenTransparent && !whiteoutPlane.activeSelf) whiteoutPlane.SetActive(true);
        _state = STATE_FADING_IN;
        _t = (_currentAlpha <= 0f) ? 0f : _currentAlpha;   // resume from current alpha
        _duration = fadeInSeconds * (1f - _t);
        if (_duration <= 0f) SetAlphaImmediate(1f);
    }

    public void FadeOut()
    {
        if (!_ready) return;
        _state = STATE_FADING_OUT;
        _t = (1f - _currentAlpha);                         // resume from current alpha
        _duration = fadeOutSeconds * (1f - _t);
        if (_duration <= 0f) SetAlphaImmediate(0f);
    }

    // For SendCustomEvent wiring if needed
    public void _FadeIn()  { FadeIn(); }
    public void _FadeOut() { FadeOut(); }
}
