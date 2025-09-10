using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UIButtonStartAnimation : UdonSharpBehaviour
{
    public Animator animator;

    public AnimAction action = AnimAction.SetTrigger;

    [Tooltip("For SetTrigger/Bool modes, this is the parameter name.\nFor Play/CrossFade, this is the state name.")]
    public string parameterOrStateName = "Play";

    [Header("State Options (Play/CrossFade)")]
    public int layerIndex = 0;
    public float crossFadeDuration = 0.15f;

    [Header("Extras")]
    public AudioSource clickSfx;
    public bool debugLogs = false;

    public void OnUIButtonClick()
    {
        if (clickSfx != null) clickSfx.Play();

        if (animator == null)
        {
            if (debugLogs) Debug.Log("[UIButtonStartAnimation] No Animator assigned.");
            return;
        }

        switch (action)
        {
            case AnimAction.SetTrigger:
                animator.SetTrigger(parameterOrStateName);
                break;

            case AnimAction.SetBoolTrue:
                animator.SetBool(parameterOrStateName, true);
                break;

            case AnimAction.SetBoolFalse:
                animator.SetBool(parameterOrStateName, false);
                break;

            case AnimAction.ToggleBool:
                bool current = animator.GetBool(parameterOrStateName);
                animator.SetBool(parameterOrStateName, !current);
                break;

            case AnimAction.PlayState:
                animator.Play(parameterOrStateName, layerIndex, 0f);
                break;

            case AnimAction.CrossFadeState:
                animator.CrossFade(parameterOrStateName, crossFadeDuration, layerIndex, 0f);
                break;
        }

        if (debugLogs) Debug.Log($"[UIButtonStartAnimation] Action {action} on '{parameterOrStateName}'");
    }
}

/// <summary>
/// Must be declared outside the class — nested types are not supported in UdonSharp.
/// </summary>
public enum AnimAction
{
    SetTrigger,
    SetBoolTrue,
    SetBoolFalse,
    ToggleBool,
    PlayState,
    CrossFadeState
}
