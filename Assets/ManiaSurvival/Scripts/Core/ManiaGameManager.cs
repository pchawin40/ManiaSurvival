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
    public List<UnitHealth> survivors = new List<UnitHealth>();
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
