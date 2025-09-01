using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ToggleObjectsByTag : UdonSharpBehaviour
{
    [Header("Setup")]
    [Tooltip("Objects to enable/disable (manually assign in Inspector).")]
    public GameObject[] targetObjects;

    [Tooltip("If true, objects will be enabled; if false, disabled.")]
    public bool enableObjects = true;

    [Header("Options")]
    [Tooltip("Run automatically at Start?")]
    public bool runOnStart = true;

    void Start()
    {
        if (runOnStart)
        {
            ApplyToggle();
        }
    }

    public void ApplyToggle()
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogWarning("[ToggleObjectsByTag] No objects assigned!");
            return;
        }

        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
                obj.SetActive(enableObjects);
        }
    }

    public void EnableAll()
    {
        enableObjects = true;
        ApplyToggle();
    }

    public void DisableAll()
    {
        enableObjects = false;
        ApplyToggle();
    }
}
