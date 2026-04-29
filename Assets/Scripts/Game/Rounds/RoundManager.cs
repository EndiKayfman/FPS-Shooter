using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class RoundManager : MonoBehaviour
{
    [SerializeField] HudController hud;

    [Tooltip("Пустой массив = авто-сбор всех Health в сцене при старте.")]
    [SerializeField] Health[] fighters;

    [SerializeField] SpawnPoint[] spawnPoints;
    [SerializeField] bool autoGather = true;

    [SerializeField] float roundSeconds = 115f;
    [SerializeField] float interRoundSeconds = 4f;
    [SerializeField] int winsRequired = 4;

    public bool FightsAreLive => _fightActive && !_resolvingRound;

    bool _fightActive;
    bool _resolvingRound;

    float _clock;

    int _alphaWins;
    int _betaWins;

    readonly List<Health> _aliveStaging = new();

    void Awake()
    {
        if (autoGather && (fighters == null || fighters.Length == 0))
            fighters = FindObjectsByType<Health>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (spawnPoints == null || spawnPoints.Length == 0)
            spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    void Start()
    {
        SubscribeDeathEvents();
        hud?.ShowScores(_alphaWins, _betaWins);
        StartCoroutine(OpenMatchCoroutine());
    }

    IEnumerator OpenMatchCoroutine()
    {
        hud?.Announce("Расстановка команд");
        Redeploy(resetHealth: false);
        hud?.Announce("Стартовый отсчёт");
        yield return new WaitForSecondsRealtime(interRoundSeconds);

        ActivateFightWave();
    }

    void Update()
    {
        if (!_fightActive || _resolvingRound)
            return;

        _clock -= Time.deltaTime;
        hud?.ShowTimerSeconds(_clock);

        if (_clock > 0f)
            return;

        BeginResolveRound(RoundOutcomeResolver.TimerWinner(fighters));
    }

    void HandleDeathSignal(Health _)
    {
        if (!_fightActive || _resolvingRound)
            return;

        _aliveStaging.Clear();
        foreach (var h in fighters.Where(f => f != null && f.IsAlive))
            _aliveStaging.Add(h);

        var verdict = RoundOutcomeResolver.EliminationWinner(_aliveStaging);
        if (verdict == RoundResult.Undecided)
            return;

        BeginResolveRound(verdict);
    }

    void BeginResolveRound(RoundResult rr)
    {
        if (_resolvingRound)
            return;

        _resolvingRound = true;
        _fightActive = false;
        StartCoroutine(RoundAftermath(rr));
    }

    IEnumerator RoundAftermath(RoundResult rr)
    {
        BumpScore(rr);
        hud?.Announce(RoundSentence(rr));
        hud?.ShowScores(_alphaWins, _betaWins);

        if (ReachedSeriesWin(out var ending))
        {
            hud?.Announce(ending);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _resolvingRound = false;
            yield break;
        }

        hud?.Announce("Разминка между раундами");
        yield return new WaitForSecondsRealtime(interRoundSeconds);

        Redeploy(resetHealth: true);
        hud?.ClearBanner();
        ActivateFightWave();

        _resolvingRound = false;
    }

    void ActivateFightWave()
    {
        _aliveStaging.Clear();
        foreach (var h in fighters.Where(f => f != null && f.IsAlive))
            _aliveStaging.Add(h);

        _fightActive = true;
        _clock = roundSeconds;

        hud?.ShowScores(_alphaWins, _betaWins);
        hud?.ClearBanner();
        hud?.ShowTimerSeconds(_clock);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void BumpScore(RoundResult rr)
    {
        switch (rr)
        {
            case RoundResult.TeamAlphaWin:
                _alphaWins++;
                break;
            case RoundResult.TeamBetaWin:
                _betaWins++;
                break;
            case RoundResult.Tie:
                break;
        }
    }

    static string RoundSentence(RoundResult rr)
    {
        return rr switch
        {
            RoundResult.TeamAlphaWin => "Раунд за ALPHA",
            RoundResult.TeamBetaWin => "Раунд за BETA",
            RoundResult.Tie => "Раунд — ничья",
            RoundResult.Undecided => "",
            _ => ""
        };
    }

    bool ReachedSeriesWin(out string msg)
    {
        msg = "";
        if (_alphaWins >= winsRequired)
        {
            msg = "Серию выигрывает ALPHA";
            return true;
        }

        if (_betaWins >= winsRequired)
        {
            msg = "Серию выигрывает BETA";
            return true;
        }

        return false;
    }

    void SubscribeDeathEvents()
    {
        foreach (var fighter in fighters.Where(f => f != null))
        {
            fighter.Died -= HandleDeathSignal;
            fighter.Died += HandleDeathSignal;
        }
    }

    void Redeploy(bool resetHealth)
    {
        SubscribeDeathEvents();

        var alphaSpawns = spawnPoints.Where(s => s != null && s.Team == CombatTeam.Alpha).ToArray();
        var betaSpawns = spawnPoints.Where(s => s != null && s.Team == CombatTeam.Beta).ToArray();

        Shuffle(alphaSpawns);
        Shuffle(betaSpawns);

        DeployTeam(BuildTeam(CombatTeam.Alpha), alphaSpawns);
        DeployTeam(BuildTeam(CombatTeam.Beta), betaSpawns);

        foreach (var h in fighters)
        {
            if (h == null) continue;

            if (resetHealth)
                h.ResetToFull();

            foreach (var w in h.transform.root.GetComponentsInChildren<HitscanWeapon>(true))
                w.ResetCooldown();

            foreach (var meshAgent in h.transform.root.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            {
                meshAgent.enabled = true;
                meshAgent.isStopped = false;
                meshAgent.velocity = Vector3.zero;
                meshAgent.ResetPath();
                meshAgent.Warp(meshAgent.gameObject.transform.position);
            }

            foreach (var bot in h.transform.root.GetComponentsInChildren<FpsMobController>(true))
                bot.PrepareForRound();
        }
    }

    List<Health> BuildTeam(CombatTeam side)
    {
        return fighters
            .Where(h => h != null && h.TryGetComponent<TeamMember>(out var tm) && tm.Team == side)
            .OrderByDescending(h => h.gameObject.name)
            .ToList();
    }

    static void Shuffle(SpawnPoint[] arr)
    {
        for (var i = arr.Length - 1; i > 0; i--)
        {
            var j = UnityEngine.Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    static void DeployTeam(List<Health> roster, SpawnPoint[] nests)
    {
        if (nests.Length == 0 || roster.Count == 0)
            return;

        for (var i = 0; i < roster.Count; i++)
        {
            var root = roster[i].transform.root;
            var nest = nests[i % nests.Length];

            if (root.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var nav))
                nav.enabled = false;

            nest.ApplyTo(root);

            if (root.TryGetComponent(out nav))
            {
                nav.enabled = true;
                nav.Warp(nav.transform.position);
                nav.velocity = Vector3.zero;
                nav.ResetPath();
            }
        }
    }
}
