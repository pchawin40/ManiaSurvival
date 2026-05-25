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
    public bool startOnAwake = true;

    [Header("Scene References")]
    public List<SurvivorHealth> survivors = new List<SurvivorHealth>();
    public ManiaGameUI gameUI;

    [Header("Debug")]
    public bool logStateChanges = true;

    public ManiaGameState State { get; private set; } = ManiaGameState.WaitingToStart;
    public float TimeRemaining { get; private set; }
    public int MonsterKills { get; private set; }

    public int AliveSurvivorCount
    {
        get
        {
            int aliveCount = 0;

            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i] != null && survivors[i].IsAlive)
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

    public void BeginRound()
    {
        RefreshSurvivorList();

        TimeRemaining = roundDuration;
        MonsterKills = 0;

        for (int i = 0; i < survivors.Count; i++)
        {
            if (survivors[i] != null)
            {
                survivors[i].ResetHealth();
            }
        }

        ChangeState(ManiaGameState.Playing);
    }

    public void RegisterSurvivor(SurvivorHealth survivor)
    {
        if (survivor == null || survivors.Contains(survivor))
        {
            return;
        }

        survivors.Add(survivor);
    }

    public void ReportSurvivorDeath(SurvivorHealth survivor, GameObject damageSource)
    {
        if (damageSource != null && damageSource.GetComponentInParent<MonsterAttack>() != null)
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
        survivors.RemoveAll(survivor => survivor == null);

        SurvivorHealth[] foundSurvivors = FindObjectsByType<SurvivorHealth>(FindObjectsSortMode.None);

        for (int i = 0; i < foundSurvivors.Length; i++)
        {
            RegisterSurvivor(foundSurvivors[i]);
        }
    }

    private void ChangeState(ManiaGameState newState)
    {
        State = newState;

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
