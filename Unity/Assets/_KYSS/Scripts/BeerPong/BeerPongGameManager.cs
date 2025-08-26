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
    // Red cups are scored by Blue; Blue cups are scored by Red
    public CupTopTrigger[] redCupsTop;     // length 6 (these are Red’s cups → Blue scores here)
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
    public AudioClip sfxRoundWin;

    [Header("Confetti")]
    public ParticleSystem[] confettiPool; // simple pool; will be moved over winner

    [Header("UI")]
    public TextMeshProUGUI redNameText;
    public TextMeshProUGUI blueNameText;
    public TextMeshProUGUI redScoreText;
    public TextMeshProUGUI blueScoreText;

    [UdonSynced] private int _redScore;
    [UdonSynced] private int _blueScore;

    [UdonSynced] private int _redCupMask;  // bitmask of lit cups (0..5)
    [UdonSynced] private int _blueCupMask;

    [UdonSynced] private int _turnTeam = (int)Team.None; // None until both players ready

    private VRCPlayerApi _redPlayer;
    private VRCPlayerApi _bluePlayer;

    // --- Init ---
    private void Start()
    {
        ResetAllVisuals();
        UpdateUI();
        SetTurn(Team.None);
        SafeDisableBall(Team.Red);
        SafeDisableBall(Team.Blue);
    }

    // --- Zone hooks ---
    public void OnPlayerZoneJoined(Team team, VRCPlayerApi player)
    {
        if (team == Team.Red) _redPlayer = player;
        else if (team == Team.Blue) _bluePlayer = player;

        audioPool.PlayAt(sfxPlayerActivate, transform.position, 1f);

        UpdateUINames();
        MaybeStartGame();
    }

    public void OnPlayerZoneLeft(Team team, VRCPlayerApi player)
    {
        audioPool.PlayAt(sfxPlayerDeactivate, transform.position, 1f);

        if (team == Team.Red && _redPlayer != null && player.playerId == _redPlayer.playerId) _redPlayer = null;
        if (team == Team.Blue && _bluePlayer != null && player.playerId == _bluePlayer.playerId) _bluePlayer = null;

        // If current shooter left, pause turns (disable balls)
        if ((int)team == _turnTeam)
        {
            SetTurn(Team.None);
            SafeDisableBall(Team.Red);
            SafeDisableBall(Team.Blue);
        }
        UpdateUINames();
    }

    private void MaybeStartGame()
    {
        if (_turnTeam != (int)Team.None) return; // already running

        bool hasRed = _redPlayer != null;
        bool hasBlue = _bluePlayer != null;

        if (hasRed && hasBlue)
        {
            // Normal 2P start
            _redScore = 0; _blueScore = 0;
            _redCupMask = 0; _blueCupMask = 0;
            ResetAllVisuals();
            UpdateUI();
            SetTurn(Team.Red);
            audioPool.PlayAt(sfxGameStart, transform.position, 1f);
            return;
        }

        if (allowSoloPractice && (hasRed || hasBlue))
        {
            // Solo start: whoever is seated gets the first (and only) turn
            _redScore = 0; _blueScore = 0;
            _redCupMask = 0; _blueCupMask = 0;
            ResetAllVisuals();
            UpdateUI();

            Team soloTeam = hasRed ? Team.Red : Team.Blue;
            SetTurn(soloTeam);
            audioPool.PlayAt(sfxGameStart, transform.position, 1f);
        }
    }


    // --- Turn control ---
    private void SetTurn(Team team)
    {
        _turnTeam = (int)team;
        RequestSerialization();

        // Enable only the active team's ball
        SafeDisableBall(Team.Red);
        SafeDisableBall(Team.Blue);
        if (team == Team.Red) SafeEnableBall(Team.Red);
        if (team == Team.Blue) SafeEnableBall(Team.Blue);

        // Center must be reset at the start of a new possession
        if (centerGate != null) centerGate.ResetGate();
    }

    public bool IsPlayersTurn(VRCPlayerApi player, Team team)
    {
        if ((int)team != _turnTeam) return false;
        if (team == Team.Red) return _redPlayer != null && player.playerId == _redPlayer.playerId;
        if (team == Team.Blue) return _bluePlayer != null && player.playerId == _bluePlayer.playerId;
        return false;
    }

    private bool HasPlayer(Team t) => (t == Team.Red) ? (_redPlayer != null) : (_bluePlayer != null);

    public void NextTurn()
    {
        Team current = (Team)_turnTeam;
        if (current == Team.None) return;

        Team next = current == Team.Red ? Team.Blue : Team.Red;

        if (allowSoloPractice && !HasPlayer(next))
        {
            // Stay on the same team in solo mode; re-arm center gate and keep ball up
            if (centerGate != null) centerGate.ResetGate();
            SetTurn(current); // re-enables the same ball & re-arms gate
            return;
        }

        SetTurn(next);
    }


    // --- Ball event passthroughs (for SFX convenience) ---
    public void OnBallPickup(Team team, Vector3 pos)
    {
        audioPool.PlayAt(sfxBallPickup, pos, 1f);
    }
    public void OnBallBounce(Vector3 pos)
    {
        audioPool.PlayAt(sfxBallBounce, pos, 1f);
    }
    public void OnCenterSuccess(Vector3 pos)
    {
        audioPool.PlayAt(sfxCenterSuccess, pos, 1f);
    }
    public void OnCupSideHit(Vector3 pos)
    {
        audioPool.PlayAt(sfxHitCupSide, pos, 1f);
    }
    public void OnCupTopHit(Vector3 pos)
    {
        audioPool.PlayAt(sfxHitCupTop, pos, 1f);
    }

    // --- Scoring ---
    // Attacker is the opposite of the defendedTeam (the cups belong to defendedTeam)
    public void RegisterCupScored(Team defendedTeam, int cupIndex, Vector3 hitPos)
    {
        if (defendedTeam == Team.Red)
        {
            // Blue scored on Red’s cup
            if (((_redCupMask >> cupIndex) & 1) == 1) return; // already lit
            _redCupMask |= (1 << cupIndex);
            _blueScore++;
            LightCup(defendedTeam, cupIndex, true);
        }
        else if (defendedTeam == Team.Blue)
        {
            // Red scored on Blue’s cup
            if (((_blueCupMask >> cupIndex) & 1) == 1) return;
            _blueCupMask |= (1 << cupIndex);
            _redScore++;
            LightCup(defendedTeam, cupIndex, true);
        }

        RequestSerialization();
        UpdateUI();
        audioPool.PlayAt(sfxHitCupTop, hitPos, 1f);

        // Check win
        if (_redScore >= 6)
        {
            // Red lit all Blue cups → Red wins
            SpawnConfettiOver(_redPlayer);
            audioPool.PlayAt(sfxRoundWin, hitPos, 1f);
            EndRound();
            return;
        }
        if (_blueScore >= 6)
        {
            SpawnConfettiOver(_bluePlayer);
            audioPool.PlayAt(sfxRoundWin, hitPos, 1f);
            EndRound();
            return;
        }

        // Scored → possession switches
        NextTurn();
    }

    private void EndRound()
    {
        // Freeze turns & balls
        SetTurn(Team.None);
        SafeDisableBall(Team.Red);
        SafeDisableBall(Team.Blue);

        // Let people stay in zones; a new round starts when both present and you call MaybeStartGame() again
        // (Or auto-start after a delay if you like.)
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
        if (r != null)
        {
            r.sharedMaterial = lit ? cupMatLit : cupMatNormal;
        }
        if (applyNow)
        {
            // disable the top trigger after lighting (so it can’t be scored twice)
            CupTopTrigger top = defendedTeam == Team.Red ? redCupsTop[index] : blueCupsTop[index];
            if (top != null) top.SetScored(lit);
        }
    }

    private void UpdateUI()
    {
        if (redScoreText) redScoreText.text = _redScore.ToString();
        if (blueScoreText) blueScoreText.text = _blueScore.ToString();
    }

    private void UpdateUINames()
    {
        if (redNameText) redNameText.text = _redPlayer != null ? _redPlayer.displayName : "-";
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
            if (ps != null && !ps.isPlaying)
            {
                return ps;
            }
        }
        return null;
    }

    private void SafeEnableBall(Team team)
    {
        TeamBall b = (team == Team.Red) ? redBall : blueBall;
        if (b != null) b.SetBallEnabled(true);
    }
    private void SafeDisableBall(Team team)
    {
        TeamBall b = (team == Team.Red) ? redBall : blueBall;
        if (b != null) b.SetBallEnabled(false);
    }

    // --- Serialization apply (late joiners) ---
    public override void OnDeserialization()
    {
        ApplyCupMasksToVisuals();
        UpdateUI();
    }

    // --- Exposed utility for other scripts ---
    public bool CanPickupBall(VRCPlayerApi p, Team team)
    {
        return IsPlayersTurn(p, team);
    }

    public VRCPlayerApi GetTeamPlayer(Team team)
    {
        return team == Team.Red ? _redPlayer : _bluePlayer;
    }

    public Team CurrentTurnTeam()
    {
        return (Team)_turnTeam;
    }
}
