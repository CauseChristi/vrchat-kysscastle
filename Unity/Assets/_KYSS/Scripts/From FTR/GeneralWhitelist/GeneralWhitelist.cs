using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GeneralWhitelist : UdonSharpBehaviour
{
    [Header("Whitelist Settings")]
    [Tooltip("Optional: pre-seeded usernames (one per element).")]
    [HideInInspector] public string[] Whitelist;

    [SerializeField] private string[] whitelistArray;   // your inspector list (already present)
    private string[] mergedWhitelist;                   // make sure this exists if you merge sources
    private readonly string[] EMPTY = new string[0];

    [Tooltip("Optional: URL to a plain-text list (one username per line).")]
    public VRCUrl whitelistURL;

    [Tooltip("Optional: Text file (Resources or any asset) with one name per line.")]
    public TextAsset whitelistTextFile;

    [Header("Scene Toggles (applied to LOCAL player only)")]
    public GameObject[] objectsToDisableIfWhitelisted;
    public GameObject[] objectsToEnableIfWhitelisted;
    public Collider[]   collidersToEnableIfWhitelisted;
    public GraphicRaycaster[] raycastersToEnableIfWhitelisted;

    [Header("UI (optional)")]
    public Button refreshButton;
    public TextMeshProUGUI refreshButtonLabel; // assign if your button label isn’t named exactly "Text (TMP)"

    [Header("Advanced (optional)")]
    public UdonSharpBehaviour[] udonBehavioursToNotify; // calls EnableWhitelisted on success

    // Public, read-only view of the built whitelist (lowercased, trimmed values).
    // Don't modify this at runtime from other scripts; let the manager own it.
    
    
    public string[] GetAuthorized()
    {
        // If you don’t build a merged list yet, this will fall back to the inspector list.
        return (mergedWhitelist != null && mergedWhitelist.Length > 0) ? mergedWhitelist :
            (whitelistArray != null ? whitelistArray : EMPTY);
    }

    public string[] GetSuperUsers()
    {
        // You don’t keep a separate super list → return empty; the TV bridge will treat none as super.
        return EMPTY;
    }

    // Internal working buffer
    private string[] _work = new string[0];

    void Start()
    {
        RebuildLocalSources();
        if (!IsNullOrEmpty(whitelistURL.Get()))
            RefreshWhitelist();   // fetch remote + merge
        else
            ApplyLocalPlayerToggles(); // use local-only sources
    }

    // --- Public API for other scripts ----------------------------------------

    // Check by VRCPlayerApi
    public bool IsPlayerWhitelisted(VRCPlayerApi player)
    {
        if (player == null) return false;
        return IsNameWhitelisted(player.displayName);
    }

    // Check by name (case-insensitive; trims spaces)
    public bool IsNameWhitelisted(string name)
    {
        if (Whitelist == null || name == null) return false;
        string q = Sanitize(name);
        for (int i = 0; i < Whitelist.Length; i++)
        {
            if (Whitelist[i] == q) return true;
        }
        return false;
    }

    // If you want a defensive copy:
    public string[] GetWhitelistCopy()
    {
        if (Whitelist == null) return new string[0];
        string[] copy = new string[Whitelist.Length];
        for (int i = 0; i < Whitelist.Length; i++) copy[i] = Whitelist[i];
        return copy;
    }

    // --- Building / merging ---------------------------------------------------

    private void RebuildLocalSources()
    {
        _work = new string[0];

        // 1) Serialized array
        if (whitelistArray != null)
            AddMany(whitelistArray);

        // 2) TextAsset (one per line)
        if (whitelistTextFile != null && !IsNullOrEmpty(whitelistTextFile.text))
            AddMany(SplitLines(whitelistTextFile.text));

        // Commit to public Whitelist
        Whitelist = _work;
    }

    // Called by button (or programmatically)
    public void RefreshWhitelistButton()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, "RefreshWhitelist");
    }

    public void RefreshWhitelist()
    {
        SetButtonState(true, "Refreshing...");
        VRCStringDownloader.LoadUrl(whitelistURL, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        // Merge remote lines into the current set
        AddMany(SplitLines(result.Result));
        Whitelist = _work; // publish

        // UI & follow-ups
        SetButtonState(false, "Whitelist Refreshed");
        SendCustomEventDelayedSeconds(nameof(ResetButtonState), 4f);

        ApplyLocalPlayerToggles();

        // Optional callback for other Udon behaviors
        for (int i = 0; i < udonBehavioursToNotify.Length; i++)
        {
            UdonSharpBehaviour b = udonBehavioursToNotify[i];
            if (b != null) b.SendCustomEvent("EnableWhitelisted");
        }
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        SetButtonState(false, "Refresh Failed");
        SendCustomEventDelayedSeconds(nameof(ResetButtonState), 4f);
        Debug.LogWarning("[GeneralWhitelist] URL string load failed: " + result.Error);
        // Still apply local sources so things keep working
        ApplyLocalPlayerToggles();
    }

    public void ResetButtonState()
    {
        SetButtonState(false, "Refresh Whitelist");
    }

    // --- Local application (optional UX you already had) ---------------------

    private void ApplyLocalPlayerToggles()
    {
        bool allowed = IsPlayerWhitelisted(Networking.LocalPlayer);

        // disable/enable gameobjects
        ToggleList(objectsToDisableIfWhitelisted, !allowed); // inverted
        ToggleList(objectsToEnableIfWhitelisted,  allowed);

        // enable components
        ToggleColliders(collidersToEnableIfWhitelisted, allowed);
        ToggleRaycasters(raycastersToEnableIfWhitelisted, allowed);

        Debug.Log("[GeneralWhitelist] Local player is " + (allowed ? "" : "NOT ") + "whitelisted.");
    }

    // --- Helpers --------------------------------------------------------------

    private string[] SplitLines(string blob)
    {
        // Supports CRLF/LF; ignores empty/comment lines (# at column 0)
        if (IsNullOrEmpty(blob)) return new string[0];
        // Manual split to stay Udon-friendly:
        // (UnityEngine's Split with string[] delimiters is okay, but this is simpler)
        string normalized = blob.Replace("\r\n", "\n").Replace("\r", "\n");
        return normalized.Split('\n');
    }

    private void AddMany(string[] raw)
    {
        if (raw == null) return;
        for (int i = 0; i < raw.Length; i++)
        {
            string s = Sanitize(raw[i]);
            if (IsNullOrEmpty(s)) continue;
            if (s.StartsWith("#")) continue; // allow comment lines
            if (!Contains(_work, s))
                _work = Append(_work, s);
        }
    }

    private string Sanitize(string s)
    {
        if (s == null) return "";
        // trim + lowercase for consistent comparisons
        s = s.Trim();
        // Some Udon builds are picky with culture-dependent calls; use ToLowerInvariant-like:
        return s.ToLower();
    }

    private bool Contains(string[] arr, string value)
    {
        if (arr == null || value == null) return false;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == value) return true;
        return false;
    }

    private string[] Append(string[] arr, string value)
    {
        int len = arr == null ? 0 : arr.Length;
        string[] n = new string[len + 1];
        for (int i = 0; i < len; i++) n[i] = arr[i];
        n[len] = value;
        return n;
    }

    private bool IsNullOrEmpty(string s)
    {
        return s == null || s.Length == 0;
    }

    private void ToggleList(GameObject[] list, bool active)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            GameObject go = list[i];
            if (go != null) go.SetActive(active);
        }
    }

    private void ToggleColliders(Collider[] list, bool enabled)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            Collider c = list[i];
            if (c != null) c.enabled = enabled;
        }
    }

    private void ToggleRaycasters(GraphicRaycaster[] list, bool enabled)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            GraphicRaycaster r = list[i];
            if (r != null) r.enabled = enabled;
        }
    }

    private void SetButtonState(bool busy, string label)
    {
        if (refreshButton != null) refreshButton.interactable = !busy;
        if (refreshButtonLabel != null) refreshButtonLabel.text = label;
        // Optional: if your Button’s child is named "Text (TMP)"
        if (refreshButton != null && refreshButtonLabel == null)
        {
            Transform t = refreshButton.transform.Find("Text (TMP)");
            if (t != null)
            {
                TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = label;
            }
        }
    }
}
