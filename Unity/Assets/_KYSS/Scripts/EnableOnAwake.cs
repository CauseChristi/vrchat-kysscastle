using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EnableOnAwake : UdonSharpBehaviour
{
    [Header("Objects to Enable on Awake")]
    public GameObject[] objectsToEnable;

    private void Awake()
    {
        if (objectsToEnable == null || objectsToEnable.Length == 0) return;

        for (int i = 0; i < objectsToEnable.Length; i++)
        {
            if (objectsToEnable[i] != null)
            {
                objectsToEnable[i].SetActive(true);
            }
        }
    }
}
