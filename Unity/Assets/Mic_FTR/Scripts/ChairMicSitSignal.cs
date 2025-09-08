
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ChairMicSitSignal : UdonSharpBehaviour
{

    public VoiceAmp voiceAmp;

    void Start()
    {
        if (voiceAmp.isThereChairsInTrigger == false)
            voiceAmp.isThereChairsInTrigger = true;
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        voiceAmp.OnChairSit(player);
    }
}
