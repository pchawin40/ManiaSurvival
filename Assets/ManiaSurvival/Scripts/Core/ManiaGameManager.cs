using System.Collections.Generic;
using UnityEngine;

public enum ManiaGameState
{
    WaitingToStart,
    Playing,
    MonsterWon,
    SurvivorsWon
}

public class ManiaGameManager : MonoBehaviour
{
    public static ManiaGameManager Instance { get; private set; }

    [Header("Round")]
    public float roundDuration = 180f;
    [Tooltip("If true, the round starts immediately on scene load. Leave false so players must click Start first.")]
    public bool startOnAwake = false;

    [Header("Match Start")]
    [Tooltip("How many players must click Start before the round begins. Use 1 for solo testing.")]
    public int playersRequiredToStart = 1;

    [Header("Scene References")]
    public List<UnitHealth> survivors = new List<UnitHealth>();
    public ManiaGameUI gameUI;

    [Header("Debug")]
    public bool logStateChanges = true;

    public ManiaGameState State { get; private set; } = ManiaGameState.WaitingToStart;
    public float TimeRemaining { get; private set; }
    public int MonsterKills { get; private set; }
    public int PlayersReadyCount { get; private set; }
    public int PlayersRequiredToStart => Mathf.Max(1, playersRequiredToStart);
    public bool IsPlaying => State == ManiaGameState.Playing;

    public int AliveSurvivorCount
    {
        get
        {
            int aliveCount = 0;

            for (int i = 0; i < survivors.Count; i++)
            {
                UnitHealth survivor = survivors[i];

                if (survivor != null && survivor.gameObject.CompareTag("Survivor") && !survivor.IsDead)
                {
                    aliveCount++;
                }
            }

            return aliveCount;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ManiaGameManager found. Keep only one in the scene.");
            return;
        }

        Instance = this;
        TimeRemaining = roundDuration;
    }

    private void Start()
    {
        RefreshSurvivorList();

        if (gameUI == null)
        {
            gameUI = FindFirstObjectByType<ManiaGameUI>();
        }

        if (startOnAwake)
        {
            BeginRound();
        }
        else
        {
            ChangeState(ManiaGameState.WaitingToStart);
        }
    }

    private void Update()
    {
        if (State != ManiaGameState.Playing)
        {
            return;
        }

        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);

        if (TimeRemaining <= 0f)
        {
            EndRound(ManiaGameState.SurvivorsWon);
            return;
        }

        if (AliveSurvivorCount <= 0)
        {
            EndRound(ManiaGameState.MonsterWon);
            return;
        }

        if (gameUI != null)
        {
            gameUI.Refresh(this);
        }
    }

    public void ReturnToWaitingScreen()
    {
        RefreshSurvivorList();
        TimeRemaining = roundDuration;
        MonsterKills = 0;
        PlayersReadyCount = 0;

        for (int i = 0; i < survivors.Count; i++)
        {
            if (survivors[i] != null)
            {
                survivors[i].ResetHealth();
            }
        }

        ChangeState(ManiaGameState.WaitingToStart);
    }

    public bool TryRegisterLocalPlayerReady()
    {
        if (State != ManiaGameState.WaitingToStart)
        {
            return false;
        }

        PlayersReadyCount++;

        if (logStateChanges)
        {
            Debug.Log("Player ready: " + PlayersReadyCount + " / " + PlayersRequiredToStart);
        }

        if (PlayersReadyCount < PlayersRequiredToStart)
        {
            if (gameUI != null)
            {
                gameUI.Refresh(this);
            }

            return false;
        }

        BeginRound();
        return true;
    }

    public void BeginRound()
    {
        RefreshSurvivorList();

        LocalRoleController roleController = FindFirstObjectByType<LocalRoleController>();
        if (roleController != null)
        {
            roleController.ApplyControlMode();
        }

        TimeRemaining = roundDuration;
        MonsterKills = 0;
        PlayersReadyCount = 0;

        for (int i = 0; i < survivors.Count; i++)
        {
            if (survivors[i] != null)
            {
                survivors[i].ResetHealth();
            }
        }

        ChangeState(ManiaGameState.Playing);
    }

    public void RegisterSurvivor(UnitHealth survivor)
    {
        if (survivor == null || survivors.Contains(survivor) || !survivor.gameObject.CompareTag("Survivor"))
        {
            return;
        }

        survivors.Add(survivor);
    }

    public void ReportSurvivorDeath(UnitHealth survivor, GameObject damageSource)
    {
        bool killerIsMonster = damageSource != null
            && (damageSource.GetComponentInParent<PredatorClassManager>() != null
                || damageSource.CompareTag("Monster")
                || damageSource.CompareTag("Predator"));

        if (killerIsMonster)
        {
            MonsterKills++;
        }

        if (State == ManiaGameState.Playing && AliveSurvivorCount <= 0)
        {
            EndRound(ManiaGameState.MonsterWon);
        }

        if (gameUI != null)
        {
            gameUI.Refresh(this);
        }
    }

    public void EndRound(ManiaGameState result)
    {
        if (State != ManiaGameState.Playing)
        {
            return;
        }

        ChangeState(result);
    }

    public void RefreshSurvivorList()
    {
        survivors.RemoveAll(survivor => survivor == null || !survivor.gameObject.CompareTag("Survivor"));

        GameObject[] foundSurvivors = GameObject.FindGameObjectsWithTag("Survivor");

        for (int i = 0; i < foundSurvivors.Length; i++)
        {
            UnitHealth survivor = foundSurvivors[i].GetComponent<UnitHealth>();

            if (survivor == null)
            {
                survivor = foundSurvivors[i].GetComponentInParent<UnitHealth>();
            }

            RegisterSurvivor(survivor);
        }
    }

    private void ChangeState(ManiaGameState newState)
    {
        State = newState;

        if (newState == ManiaGameState.WaitingToStart)
        {
            PlayersReadyCount = 0;
        }

        if (logStateChanges)
        {
            Debug.Log("Mania game state: " + State);
        }

        if (gameUI != null)
        {
            gameUI.Refresh(this);
        }
    }
}
