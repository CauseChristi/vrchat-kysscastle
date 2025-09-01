using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[AddComponentMenu("Udon/Logic/Enable & Disable On Trigger")]
public class TriggerEnableDisable : UdonSharpBehaviour
{
    [Header("On Enter: what to toggle")]
    public GameObject[] enableOnEnter;
    public GameObject[] disableOnEnter;

    [Header("Optional: On Exit behavior")]
    public bool useExit = false;
    public GameObject[] enableOnExit;
    public GameObject[] disableOnExit;

    [Header("Options")]
    [Tooltip("If true, only the local player's entry will fire the logic.")]
    public bool localOnly = true;

    [Tooltip("If true, fire once and then ignore further entries.")]
    public bool triggerOnce = false;

    private bool _fired;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (triggerOnce && _fired) return;
        if (localOnly && !player.isLocal) return;

        SetActiveList(enableOnEnter, true);
        SetActiveList(disableOnEnter, false);

        if (triggerOnce) _fired = true;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!useExit) return;
        if (localOnly && !player.isLocal) return;

        SetActiveList(enableOnExit, true);
        SetActiveList(disableOnExit, false);
    }

    private void SetActiveList(GameObject[] list, bool value)
    {
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            GameObject go = list[i];
            if (go != null) go.SetActive(value);
        }
    }
}
