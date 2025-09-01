using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BeerPongGameManager : UdonSharpBehaviour
{
    [Header("Rules")]
    public bool allowSoloPractice = true; // (kept for convenience; now we auto-enable whichever side has a player)

    [Header("Zones")]
    public PlayerZone redZone;
    public PlayerZone blueZone;

    [Header("Balls")]
    public TeamBall redBall;
    public TeamBall blueBall;

    [Header("Center Gate")]
    public CenterGate centerGate;

    [Header("Cups (Defended by Team)")]
    // Red cups are *defended by Red* (Blue scores on them)
    public CupTopTrigger[] redCupsTop;     // length 6
    public Renderer[]      redCupsRender;  // visuals, length 6
    public CupTopTrigger[] blueCupsTop;    // length 6
    public Renderer[]      blueCupsRender; // visuals, length 6

    [Header("Cup Materials")]
    public Material cupMatNormal;
    public Material cupMatLit;

    [Header("Audio")]
    public AudioPool audioPool;
    public AudioClip sfxPlayerActivate;
    public AudioClip sfxPlayerDeactivate;
    public AudioClip sfxGameStart;
    public AudioClip sfxBallPickup;
    public AudioClip sfxBallBounce;
    public AudioClip sfxHitCupSide;
    public AudioClip sfxHitCupTop;
    public AudioClip sfxCenterSuccess;
    public AudioClip sfxRoundWin; // use for point-award celebration now

    [Header("Confetti")]
    public ParticleSystem[] confettiPool;

    [Header("UI")]
    public TextMeshProUGUI redNameText;
    public TextMeshProUGUI blueNameText;
    public TextMeshProUGUI redScoreText;
    public TextMeshProUGUI blueScoreText;

    [UdonSynced] private int _redScore;
    [UdonSynced] private int _blueScore;

    [UdonSynced] private int _redCupMask;  // which of Red's cups are lit (Blue attacks these)
    [UdonSynced] private int _blueCupMask; // which of Blue's cups are lit (Red attacks these)

    private VRCPlayerApi _redPlayer;
    private VRCPlayerApi _bluePlayer;
    private bool _started; // simple guard so we only "start" (reset) once until both seats empty

    // --- Init ---
    private void Start()
    {
        ResetAllVisuals();
        UpdateUI();
        UpdateBallEnables(); // nothing active until a player joins
    }

    // --- Zone hooks ---
    public void OnPlayerZoneJoined(Team team, VRCPlayerApi player)
    {
        if (team == Team.Red) _redPlayer = player; else _bluePlayer = player;
        audioPool.PlayAt(sfxPlayerActivate, transform.position, 1f);
        UpdateUINames();

        MaybeStartOrResume();
        UpdateBallEnables();
    }

    public void OnPlayerZoneLeft(Team team, VRCPlayerApi player)
    {
        audioPool.PlayAt(sfxPlayerDeactivate, transform.position, 1f);

        if (team == Team.Red && _redPlayer != null && player.playerId == _redPlayer.playerId) _redPlayer = null;
        if (team == Team.Blue && _bluePlayer != null && player.playerId == _bluePlayer.playerId) _bluePlayer = null;

        UpdateUINames();
        UpdateBallEnables();

        // If both gone, consider the session ended so the next join can reset everything
        if (_redPlayer == null && _bluePlayer == null) _started = false;
    }

    private void MaybeStartOrResume()
    {
        if (_started) return;
        if (!allowSoloPractice && (_redPlayer == null || _bluePlayer == null)) return;

        // Reset scores and cups whenever a new session begins
        _redScore = 0; _blueScore = 0;
        _redCupMask = 0; _blueCupMask = 0;
        ResetAllVisuals();
        UpdateUI();
        RequestSerialization();

        _started = true;
        audioPool.PlayAt(sfxGameStart, transform.position, 1f);
    }

    private void UpdateBallEnables()
    {
        if (redBall)  redBall.SetBallEnabled(_redPlayer != null);
        if (blueBall) blueBall.SetBallEnabled(_bluePlayer != null);
    }

    // --- Ball event passthroughs (for SFX convenience) ---
    public void OnBallPickup(Team team, Vector3 pos)   { audioPool.PlayAt(sfxBallPickup, pos, 1f); }
    public void OnBallBounce(Vector3 pos)               { audioPool.PlayAt(sfxBallBounce, pos, 1f); }
    public void OnCenterSuccess(Vector3 pos)            { audioPool.PlayAt(sfxCenterSuccess, pos, 1f); }
    public void OnCupSideHit(Vector3 pos)               { audioPool.PlayAt(sfxHitCupSide, pos, 1f); }
    public void OnCupTopHit(Vector3 pos)                { audioPool.PlayAt(sfxHitCupTop, pos, 1f); }

    // --- Scoring (no turns) ---
    // A cup top lit = mark the defended team's mask. When *all 6* of a defended team are lit, the attacker earns +1 point.
    public void RegisterCupScored(Team defendedTeam, int cupIndex, Vector3 hitPos)
    {
        bool changed = false;

        if (defendedTeam == Team.Red)
        {
            // Blue attacked Red's cups
            if (((_redCupMask >> cupIndex) & 1) == 0)
            {
                _redCupMask |= (1 << cupIndex);
                changed = true;
                LightCup(Team.Red, cupIndex, true);
            }
        }
        else if (defendedTeam == Team.Blue)
        {
            // Red attacked Blue's cups
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
        audioPool.PlayAt(sfxHitCupTop, hitPos, 1f);

        // After any cup lights, check if *all* of that set is now lit
        if (defendedTeam == Team.Red)
        {
            int fullMask = (1 << redCupsTop.Length) - 1; // expects 6 cups
            if (_redCupMask == fullMask)
            {
                // Blue completed all Red cups → Blue scores +1
                _blueScore++;
                RequestSerialization();
                UpdateUI();

                // Celebrate near Blue player
                SpawnConfettiOver(_bluePlayer);
                audioPool.PlayAt(sfxRoundWin, hitPos, 1f);

                // Reset Red's cups for the next point
                ResetCups(Team.Red);
            }
        }
        else // defendedTeam == Team.Blue
        {
            int fullMask = (1 << blueCupsTop.Length) - 1;
            if (_blueCupMask == fullMask)
            {
                // Red completed all Blue cups → Red scores +1
                _redScore++;
                RequestSerialization();
                UpdateUI();

                SpawnConfettiOver(_redPlayer);
                audioPool.PlayAt(sfxRoundWin, hitPos, 1f);

                ResetCups(Team.Blue);
            }
        }
    }

    private void ResetCups(Team defendedTeam)
    {
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
            // disable/enable the scoring top trigger accordingly
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

    private void SpawnConfettiOver(VRCPlayerApi p)
    {
        if (p == null) return;
        ParticleSystem ps = GetFreeConfetti();
        if (ps == null) return;

        Vector3 pos = p.GetPosition();
        pos.y += 2.0f;
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

    // --- Serialization apply (late joiners) ---
    public override void OnDeserialization()
    {
        ApplyCupMasksToVisuals();
        UpdateUI();
    }

    // --- Pickup permission: ball tied to the player seated in that team's zone ---
    public bool CanPickupBall(VRCPlayerApi p, Team team)
    {
        if (p == null) return false;
        if (team == Team.Red)  return _redPlayer  != null && p.playerId == _redPlayer.playerId;
        if (team == Team.Blue) return _bluePlayer != null && p.playerId == _bluePlayer.playerId;
        return false;
    }

    public VRCPlayerApi GetTeamPlayer(Team team) => (team == Team.Red) ? _redPlayer : _bluePlayer;
}
