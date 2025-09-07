using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RandomSpawnFromList : UdonSharpBehaviour
{
    [Header("Candidate sources (world-space)")]
    public Transform[] sources;

    [Tooltip("Enable for a couple of helpful Debug.Log messages.")]
    public bool debugLogs = false;

    [UdonSynced] private int _chosen = -1;
    private bool _applied = false;

    void Start()
    {
        if (_applied) return;

        if (sources == null || sources.Length == 0)
        {
            if (debugLogs) Debug.Log("[RandomSpawnFromList] No sources assigned.");
            return;
        }

        if (Networking.IsMaster)
        {
            PickAndBroadcast();
        }
        else
        {
            // Wait for the master's sync if we join early
            SendCustomEventDelayedFrames(nameof(__ApplyIfChosenLater), 2);
        }
    }

    public void __ApplyIfChosenLater()
    {
        if (_applied) return;

        if (_chosen >= 0)
        {
            ApplyFromIndex(_chosen);
        }
        else
        {
            // Try again a bit later until we receive the synced choice
            SendCustomEventDelayedFrames(nameof(__ApplyIfChosenLater), 10);
        }
    }

    public override void OnDeserialization()
    {
        if (!_applied && _chosen >= 0)
        {
            ApplyFromIndex(_chosen);
        }
    }

    private void PickAndBroadcast()
    {
        int count = sources.Length;
        int idx = -1;

        // Random try for a valid (non-null) entry
        for (int attempts = 0; attempts < count; attempts++)
        {
            int candidate = Random.Range(0, count);
            if (sources[candidate] != null)
            {
                idx = candidate;
                break;
            }
        }

        // Fallback: first non-null
        if (idx == -1)
        {
            for (int i = 0; i < count; i++)
            {
                if (sources[i] != null)
                {
                    idx = i;
                    break;
                }
            }
        }

        if (idx == -1)
        {
            if (debugLogs) Debug.Log("[RandomSpawnFromList] All entries were null.");
            return;
        }

        _chosen = idx;

        // Ensure ownership before serializing
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local != null && Networking.GetOwner(gameObject) != local)
        {
            Networking.SetOwner(local, gameObject);
        }

        RequestSerialization();
        ApplyFromIndex(_chosen);
    }

    private void ApplyFromIndex(int idx)
    {
        if (_applied) return;

        Transform src = sources[idx];
        if (src == null)
        {
            if (debugLogs) Debug.Log("[RandomSpawnFromList] Chosen source was null.");
            return;
        }

        transform.position = src.position;   // world position
        transform.rotation = src.rotation;   // world rotation

        _applied = true;

        if (debugLogs)
            Debug.Log($"[RandomSpawnFromList] Applied source index {idx} ({src.name}).");
    }
}
