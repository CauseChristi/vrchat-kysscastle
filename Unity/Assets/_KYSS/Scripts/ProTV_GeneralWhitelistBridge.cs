using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using ArchiTech.ProTV;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ProTV_GeneralWhitelistBridge : TVAuthPlugin
{
    [Header("Sources")]
    [SerializeField] private TVManager tvRef;       // drag your TVManager here (NO field named "tv")
    [SerializeField] private GeneralWhitelist provider;

    [Header("Options")]
    public bool caseInsensitive = true;
    public bool useSeparateLists = true;
    public bool everyoneIsSuperIfNoSplit = false;

    private bool _ready;

    public override void Start()
    {
        // Wire the inspector ref into the base field to avoid field-hiding.
        if (tv == null && tvRef != null) tv = tvRef;

        if (init) return;         // 'init' is defined in the base class
        base.Start();             // let ProTV initialize its plugin bits
        _ready = true;
    }

    public override void _TvReady()
    {
        // Ensure base tv is set (handles cases where Start order differs).
        if (tv == null && tvRef != null) tv = tvRef;

        if (tv != null) tv._Reauthorize();
    }

    public override bool _IsAuthorizedUser(VRCPlayerApi who)
    {
        if (!_ready || provider == null || !Utilities.IsValid(who)) return false;

        string name = who.displayName;
        if (caseInsensitive) name = name.ToLower();

        if (useSeparateLists)
        {
            // auth OR supers counts as authorized
            if (Contains(GetAuth(), name)) return true;
            if (Contains(GetSupers(), name)) return true;
            return false;
        }
        else
        {
            return Contains(GetAuth(), name);
        }
    }

    public override bool _IsSuperUser(VRCPlayerApi who)
    {
        if (!_ready || provider == null || !Utilities.IsValid(who)) return false;

        string name = who.displayName;
        if (caseInsensitive) name = name.ToLower();

        if (useSeparateLists)
        {
            return Contains(GetSupers(), name);
        }
        else
        {
            if (!everyoneIsSuperIfNoSplit) return false;
            return Contains(GetAuth(), name);
        }
    }

    // ---- helpers ----
    private bool Contains(string[] arr, string name)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++)
        {
            var s = arr[i];
            if (s == null) continue;
            if (caseInsensitive ? s.ToLower() == name : s == name) return true;
        }
        return false;
    }

    private string[] GetAuth()
    {
        var arr = provider != null ? provider.GetAuthorized() : null;
        return arr ?? new string[0];
    }

    private string[] GetSupers()
    {
        var arr = provider != null ? provider.GetSuperUsers() : null;
        return arr ?? new string[0];
    }
}
