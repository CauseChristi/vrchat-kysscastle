using UdonSharp;
using UnityEngine;

public enum Team { None = -1, Red = 0, Blue = 1 }

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SharedTypes : UdonSharpBehaviour
{
    // Empty – just exists so UdonSharp is happy
}
