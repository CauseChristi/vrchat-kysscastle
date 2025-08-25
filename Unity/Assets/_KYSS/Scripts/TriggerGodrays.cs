// TriggerGodrays.cs
// UdonSharp – Unity 2022.3 (VRChat)
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TriggerGodrays : UdonSharpBehaviour
{
    [Header("Godrays Scaling (Z only)")]
    [Tooltip("Root transform to scale on Z.")]
    public Transform godraysRoot;

    [Tooltip("Z scale when OFF (near zero).")]
    public float godraysOffZ = 0.01f;

    [Tooltip("Z scale when ON (full length).")]
    public float godraysOnZ = 1.0f;

    [Tooltip("Seconds to lerp between OFF/ON.")]
    public float transitionSeconds = 2.0f;

    [Tooltip("If true, starts disabled (and scaled to OFF).")]
    public bool startDisabled = true;

    // Internals
    private Vector3 _baseScale;
    private bool _lerping;
    private bool _disableOnEnd;
    private float _startZ;
    private float _targetZ;
    private float _t;                // elapsed seconds

    private void Start()
    {
        if (godraysRoot == null) return;
        _baseScale = godraysRoot.localScale;

        if (startDisabled)
        {
            SetZ(godraysOffZ);
            godraysRoot.gameObject.SetActive(false);
        }
        else
        {
            // Ensure Z is at ON value if starting enabled
            if (!godraysRoot.gameObject.activeSelf) godraysRoot.gameObject.SetActive(true);
            SetZ(godraysOnZ);
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal || godraysRoot == null) return;
        EnableGodrays();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!player.isLocal || godraysRoot == null) return;
        DisableGodrays();
    }

    private void EnableGodrays()
    {
        godraysRoot.gameObject.SetActive(true);
        BeginLerp(CurrentZ(), godraysOnZ, false);
    }

    private void DisableGodrays()
    {
        BeginLerp(CurrentZ(), godraysOffZ, true);
    }

    private void BeginLerp(float fromZ, float toZ, bool disableAtEnd)
    {
        if (transitionSeconds <= 0f)
        {
            SetZ(toZ);
            if (disableAtEnd) godraysRoot.gameObject.SetActive(false);
            _lerping = false;
            return;
        }

        _startZ = fromZ;
        _targetZ = toZ;
        _t = 0f;
        _disableOnEnd = disableAtEnd;
        _lerping = true;
    }

    private float CurrentZ()
    {
        return (godraysRoot != null) ? godraysRoot.localScale.z : 0f;
    }

    private void SetZ(float z)
    {
        var s = (_baseScale == Vector3.zero) ? godraysRoot.localScale : _baseScale;
        s.z = z;
        godraysRoot.localScale = s;
    }

    private void Update()
    {
        if (!_lerping || godraysRoot == null) return;

        _t += Time.deltaTime;
        float t01 = Mathf.Clamp01(_t / Mathf.Max(0.0001f, transitionSeconds));
        float z = Mathf.Lerp(_startZ, _targetZ, t01);
        SetZ(z);

        if (t01 >= 1f)
        {
            _lerping = false;
            if (_disableOnEnd) godraysRoot.gameObject.SetActive(false);
        }
    }
}
