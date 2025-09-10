using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BeerPongGameManager : UdonSharpBehaviour
{
    [Header("Rules")]
    public bool allowSoloPractice = true;

    [Header("Zones")]
    public PlayerZone redZone;
    public PlayerZone blueZone;

    [Header("Balls")]
    public TeamBall redBall;
    public TeamBall blueBall;

    [Header("Center Gate")]
    public CenterGate centerGate;

    [Header("Cups (Defended by Team)")]
    public CupTopTrigger[] redCupsTop;
    public Renderer[]      redCupsRender;
    public CupTopTrigger[] blueCupsTop;
    public Renderer[]      blueCupsRender;

    [Header("Cup Materials")]
    public Material cupMatNormal;
    public Material cupMatLit;

    [Header("Networked Audio")]
    public NetworkedAudio sfxGameStartAll;
    public NetworkedAudio sfxPointAwardAll;

    [Header("Confetti")]
    public ParticleSystem[] confettiPool;

    [Header("UI")]
    public TextMeshProUGUI redNameText;
    public TextMeshProUGUI blueNameText;
    public TextMeshProUGUI redScoreText;
    public TextMeshProUGUI blueScoreText;

    [UdonSynced] private int _redScore;
    [UdonSynced] private int _blueScore;
    [UdonSynced] private int _redCupMask;  // red's cups lit (blue attacks)
    [UdonSynced] private int _blueCupMask; // blue's cups lit (red attacks)

    private VRCPlayerApi _redPlayer;
    private VRCPlayerApi _bluePlayer;
    private int _lastSessionRedId = -1;
    private int _lastSessionBlueId = -1;
    private bool _started;

    private void Start()
    {
        ValidateSetup();
        ResetAllVisuals();
        UpdateUI();
        UpdateBallEnables();
        BroadcastSeatsChanged();
    }

    private void ValidateSetup()
    {
        if (redCupsTop == null || blueCupsTop == null || redCupsRender == null || blueCupsRender == null)
            Debug.LogError("[BPGM] Cup arrays not assigned.");
        if (redCupsTop != null && redCupsRender != null && redCupsTop.Length != redCupsRender.Length)
            Debug.LogError("[BPGM] Red cup arrays length mismatch.");
        if (blueCupsTop != null && blueCupsRender != null && blueCupsTop.Length != blueCupsRender.Length)
            Debug.LogError("[BPGM] Blue cup arrays length mismatch.");
    }

    // --- Zone hooks ---
    public void OnPlayerZoneJoined(Team team, VRCPlayerApi player)
    {
        if (team == Team.Red) _redPlayer = player; else _bluePlayer = player;
        UpdateUINames();
        UpdateBallEnables();
        BroadcastSeatsChanged();

        MaybeStartOrResetSession();
    }

    public void OnPlayerZoneLeft(Team team, VRCPlayerApi player)
    {
        if (team == Team.Red && _redPlayer != null && player.playerId == _redPlayer.playerId) _redPlayer = null;
        if (team == Team.Blue && _bluePlayer != null && player.playerId == _bluePlayer.playerId) _bluePlayer = null;

        UpdateUINames();
        UpdateBallEnables();
        BroadcastSeatsChanged();

        // If both gone, mark session ended so the next join can reset
        if (_redPlayer == null && _bluePlayer == null) _started = false;
    }

    private void MaybeStartOrResetSession()
    {
        // If solo not allowed and we don't have both, bail
        bool bothPresent = (_redPlayer != null && _bluePlayer != null);
        if (!allowSoloPractice && !bothPresent) return;

        int redId  = (_redPlayer  != null) ? _redPlayer.playerId  : -1;
        int blueId = (_bluePlayer != null) ? _bluePlayer.playerId : -1;

        // Reset session if first start OR either seat changed to someone new
        bool seatChanged = (redId != _lastSessionRedId) || (blueId != _lastSessionBlueId);

        if (!_started || seatChanged)
        {
            EnsureOwner();

            _redScore = 0; _blueScore = 0;
            _redCupMask = 0; _blueCupMask = 0;
            ResetAllVisuals();
            UpdateUI();
            RequestSerialization();

            _started = true;
            _lastSessionRedId = redId;
            _lastSessionBlueId = blueId;

            if (sfxGameStartAll != null) sfxGameStartAll.PlayForAll();
        }
    }

    private void EnsureOwner()
    {
        var lp = Networking.LocalPlayer;
        if (lp != null && !Networking.IsOwner(gameObject))
            Networking.SetOwner(lp, gameObject);
    }

    private void UpdateBallEnables()
    {
        if (redBall)  redBall.SetBallEnabled(_redPlayer  != null);
        if (blueBall) blueBall.SetBallEnabled(_bluePlayer != null);
    }

    private void BroadcastSeatsChanged()
    {
        if (redBall)  redBall.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TeamBall.RPC_SeatsChanged));
        if (blueBall) blueBall.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TeamBall.RPC_SeatsChanged));
    }

    // --- Scoring (no turns) ---
    public void RegisterCupScored(Team defendedTeam, int cupIndex, Vector3 hitPos)
    {
        EnsureOwner();

        bool changed = false;

        if (defendedTeam == Team.Red)
        {
            if (((_redCupMask >> cupIndex) & 1) == 0)
            {
                _redCupMask |= (1 << cupIndex);
                changed = true;
                LightCup(Team.Red, cupIndex, true);
            }
        }
        else
        {
            if (((_blueCupMask >> cupIndex) & 1) == 0)
            {
                _blueCupMask |= (1 << cupIndex);
                changed = true;
                LightCup(Team.Blue, cupIndex, true);
            }
        }

        if (!changed) return;

        RequestSerialization();
        UpdateUI();

        // Check completion of the defended set
        if (defendedTeam == Team.Red)
        {
            int full = (1 << redCupsTop.Length) - 1;
            if (_redCupMask == full)
            {
                _blueScore++;
                RequestSerialization();
                UpdateUI();

                // Everyone celebrates + confetti over Blue
                if (sfxPointAwardAll != null) sfxPointAwardAll.PlayForAll();
                RPC_SpawnConfettiBlue();

                ResetCups(Team.Red);
            }
        }
        else
        {
            int full = (1 << blueCupsTop.Length) - 1;
            if (_blueCupMask == full)
            {
                _redScore++;
                RequestSerialization();
                UpdateUI();

                if (sfxPointAwardAll != null) sfxPointAwardAll.PlayForAll();
                RPC_SpawnConfettiRed();

                ResetCups(Team.Blue);
            }
        }
    }

    private void ResetCups(Team defendedTeam)
    {
        EnsureOwner();

        if (defendedTeam == Team.Red)
        {
            _redCupMask = 0;
            for (int i = 0; i < redCupsRender.Length; i++) LightCup(Team.Red, i, false);
        }
        else
        {
            _blueCupMask = 0;
            for (int i = 0; i < blueCupsRender.Length; i++) LightCup(Team.Blue, i, false);
        }
        RequestSerialization();
        UpdateUI();
    }

    // --- Visual helpers ---
    private void ResetAllVisuals()
    {
        for (int i = 0; i < redCupsRender.Length; i++) LightCup(Team.Red, i, false, applyNow:false);
        for (int i = 0; i < blueCupsRender.Length; i++) LightCup(Team.Blue, i, false, applyNow:false);
        ApplyCupMasksToVisuals();
    }

    private void ApplyCupMasksToVisuals()
    {
        for (int i = 0; i < redCupsRender.Length; i++)
            LightCup(Team.Red, i, ((_redCupMask >> i) & 1) == 1);
        for (int i = 0; i < blueCupsRender.Length; i++)
            LightCup(Team.Blue, i, ((_blueCupMask >> i) & 1) == 1);
    }

    private void LightCup(Team defendedTeam, int index, bool lit, bool applyNow = true)
    {
        Renderer r = defendedTeam == Team.Red ? redCupsRender[index] : blueCupsRender[index];
        if (r != null) r.sharedMaterial = lit ? cupMatLit : cupMatNormal;

        if (applyNow)
        {
            CupTopTrigger top = defendedTeam == Team.Red ? redCupsTop[index] : blueCupsTop[index];
            if (top != null) top.SetScored(lit);
        }
    }

    private void UpdateUI()
    {
        if (redScoreText)  redScoreText.text  = _redScore.ToString();
        if (blueScoreText) blueScoreText.text = _blueScore.ToString();
    }

    private void UpdateUINames()
    {
        if (redNameText)  redNameText.text  = _redPlayer  != null ? _redPlayer.displayName  : "-";
        if (blueNameText) blueNameText.text = _bluePlayer != null ? _bluePlayer.displayName : "-";
    }

    // --- Confetti RPCs (everyone spawns over current player positions) ---
    public void RPC_SpawnConfettiRed()
    {
        SpawnConfettiOver(GetTeamPlayer(Team.Red));
    }

    public void RPC_SpawnConfettiBlue()
    {
        SpawnConfettiOver(GetTeamPlayer(Team.Blue));
    }

    private void SpawnConfettiOver(VRCPlayerApi p)
    {
        if (p == null) return;
        ParticleSystem ps = GetFreeConfetti();
        if (ps == null) return;

        Vector3 pos = p.GetPosition(); pos.y += 2.0f;
        ps.transform.position = pos;
        ps.gameObject.SetActive(true);
        ps.Play();
    }

    private ParticleSystem GetFreeConfetti()
    {
        for (int i = 0; i < confettiPool.Length; i++)
        {
            var ps = confettiPool[i];
            if (ps != null && !ps.isPlaying) return ps;
        }
        return null;
    }

    public override void OnDeserialization()
    {
        ApplyCupMasksToVisuals();
        UpdateUI();
    }

    // --- Pickup gate: only seated player may pick up their team's ball ---
    public bool CanPickupBall(VRCPlayerApi p, Team team)
    {
        if (p == null) return false;
        if (team == Team.Red)  return _redPlayer  != null && p.playerId == _redPlayer.playerId;
        if (team == Team.Blue) return _bluePlayer != null && p.playerId == _bluePlayer.playerId;
        return false;
    }

    public VRCPlayerApi GetTeamPlayer(Team team) => (team == Team.Red) ? _redPlayer : _bluePlayer;
}
