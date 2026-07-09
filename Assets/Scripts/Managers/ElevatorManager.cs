using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class ElevatorManager : MonoBehaviour
{
    [System.Serializable]
    private class HallCallSchedule
    {
        public float triggerTime;
        public int floor;
        [Tooltip("1 = up, -1 = down")]
        public int direction = 1;
    }

    [System.Serializable]
    private class LevelDefinition
    {
        public string name;
        [TextArea] public string description;
        [Min(10f)] public float timeLimit = 45f;
        public HallCallSchedule[] calls;
    }

    [SerializeField] private ElevatorController[] elevators;
    [SerializeField] private int totalFloors = 4;
    [Header("Level Gameplay")]
    [SerializeField] private bool autoPlayLevels = true;
    [SerializeField] private LevelDefinition[] levels;
    [SerializeField] private TextMeshProUGUI levelStatusText;

    private readonly HashSet<string> pendingHallCalls = new HashSet<string>();
    private int currentLevelIndex = -1;
    private int nextCallIndex = 0;
    private int issuedCallsThisLevel = 0;
    private int servedCallsThisLevel = 0;
    private float levelElapsed = 0f;
    private bool levelActive = false;
    private bool levelCampaignFinished = false;

    private void Awake()
    {
        if ((levels == null || levels.Length == 0) && totalFloors >= 2)
        {
            levels = CreateDefaultLevels();
        }

        foreach (var elevator in elevators)
        {
            elevator.OnArrivedAtFloor += HandleElevatorArrived;
        }
    }

    private void OnDestroy()
    {
        foreach (var elevator in elevators)
        {
            if (elevator != null)
                elevator.OnArrivedAtFloor -= HandleElevatorArrived;
        }
    }

    private void Start()
    {
        if (!autoPlayLevels || levels == null || levels.Length == 0)
        {
            UpdateLevelStatus();
            return;
        }

        StartLevel(0);
    }

    private void Update()
    {
        if (!levelActive)
            return;

        levelElapsed += Time.deltaTime;
        TriggerScheduledCalls();
        UpdateLevelStatus();

        if (HasLevelCompleted())
        {
            CompleteCurrentLevel();
            return;
        }

        if (levelElapsed >= levels[currentLevelIndex].timeLimit)
        {
            FailCurrentLevel();
        }
    }

    public bool RequestElevator(int floor, int direction)
    {
        if (floor < 0 || floor >= totalFloors)
            return false;

        if (direction != 1 && direction != -1)
            return false;

        string key = GetHallCallKey(floor, direction);

        if (pendingHallCalls.Contains(key))
            return false;

        ElevatorController chosen = FindBestElevator(floor, direction);

        if (chosen == null)
            return false;

        pendingHallCalls.Add(key);
        chosen.AddRequest(floor);

        Debug.Log($"Request at floor {floor} dir {direction} assigned to {chosen.name}");
        return true;
    }

    private ElevatorController FindBestElevator(int floor, int direction)
    {
        var idleElevators = elevators
            .Where(e => e != null && e.IsIdle)
            .OrderBy(e => e.DistanceToFloor(floor))
            .ToList();

        if (idleElevators.Count > 0)
            return idleElevators[0];

        var movingCompatible = elevators
            .Where(e => e != null && e.CanTakeRequestOnTheWay(floor, direction))
            .OrderBy(e => e.DistanceToFloor(floor))
            .ThenBy(e => e.PendingRequestCount)
            .ToList();

        if (movingCompatible.Count > 0)
            return movingCompatible[0];

        var fallback = elevators
            .Where(e => e != null)
            .OrderBy(e => e.PendingRequestCount)
            .ThenBy(e => e.DistanceToFloor(floor))
            .ToList();

        return fallback.Count > 0 ? fallback[0] : null;
    }

    private void HandleElevatorArrived(int floor)
    {
        int servedNow = 0;

        if (pendingHallCalls.Remove(GetHallCallKey(floor, 1)))
            servedNow++;

        if (pendingHallCalls.Remove(GetHallCallKey(floor, -1)))
            servedNow++;

        if (servedNow > 0 && levelActive)
            servedCallsThisLevel += servedNow;
    }

    private string GetHallCallKey(int floor, int direction)
    {
        return $"{floor}_{direction}";
    }

    private void StartLevel(int index)
    {
        if (index < 0 || index >= levels.Length)
            return;

        currentLevelIndex = index;
        nextCallIndex = 0;
        issuedCallsThisLevel = 0;
        servedCallsThisLevel = 0;
        levelElapsed = 0f;
        levelActive = true;

        Debug.Log($"Started Level {currentLevelIndex + 1}: {levels[currentLevelIndex].name}");
        UpdateLevelStatus();
    }

    private void TriggerScheduledCalls()
    {
        var current = levels[currentLevelIndex];
        if (current.calls == null || current.calls.Length == 0)
            return;

        while (nextCallIndex < current.calls.Length && current.calls[nextCallIndex].triggerTime <= levelElapsed)
        {
            var call = current.calls[nextCallIndex];
            if (IsValidScheduledCall(call))
            {
                if (RequestElevator(call.floor, call.direction))
                {
                    issuedCallsThisLevel++;
                }
            }
            nextCallIndex++;
        }
    }

    private bool IsValidScheduledCall(HallCallSchedule call)
    {
        return call != null &&
               call.floor >= 0 &&
               call.floor < totalFloors &&
               (call.direction == 1 || call.direction == -1);
    }

    private void CompleteCurrentLevel()
    {
        levelActive = false;
        Debug.Log($"Completed Level {currentLevelIndex + 1}: {levels[currentLevelIndex].name}");

        if (currentLevelIndex + 1 < levels.Length)
        {
            StartLevel(currentLevelIndex + 1);
            return;
        }

        levelCampaignFinished = true;
        UpdateLevelStatus();
    }

    private void FailCurrentLevel()
    {
        levelActive = false;
        Debug.LogWarning($"Level failed: {levels[currentLevelIndex].name}. Restarting...");
        StartLevel(currentLevelIndex);
    }

    private void UpdateLevelStatus()
    {
        if (levelStatusText == null)
            return;

        if (!autoPlayLevels || levels == null || levels.Length == 0)
        {
            levelStatusText.text = "Free Play Mode";
            return;
        }

        if (levelCampaignFinished)
        {
            levelStatusText.text = "All Levels Completed";
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Length)
        {
            levelStatusText.text = "Preparing Levels...";
            return;
        }

        var level = levels[currentLevelIndex];
        var remaining = Mathf.Max(0f, level.timeLimit - levelElapsed);
        string title = $"Level {currentLevelIndex + 1}/{levels.Length}: {level.name}";
        string objective = $"Served {servedCallsThisLevel}/{issuedCallsThisLevel} Calls";
        string timer = $"Time Left: {remaining:0.0}s";
        levelStatusText.text = $"{title}\n{objective}\n{timer}";
    }

    private bool HasLevelCompleted()
    {
        var current = levels[currentLevelIndex];
        int scheduledCount = current.calls == null ? 0 : current.calls.Length;
        bool allCallsEvaluated = nextCallIndex >= scheduledCount;
        bool noPendingCalls = pendingHallCalls.Count == 0;
        bool allIssuedServed = servedCallsThisLevel >= issuedCallsThisLevel;
        return allCallsEvaluated && noPendingCalls && allIssuedServed;
    }

    private LevelDefinition[] CreateDefaultLevels()
    {
        return new[]
        {
            new LevelDefinition
            {
                name = "Morning Warm-Up",
                description = "A light call flow to get familiar with elevator timing.",
                timeLimit = 40f,
                calls = new[]
                {
                    new HallCallSchedule { triggerTime = 0f, floor = 0, direction = 1 },
                    new HallCallSchedule { triggerTime = 3f, floor = 1, direction = 1 },
                    new HallCallSchedule { triggerTime = 7f, floor = 3, direction = -1 },
                    new HallCallSchedule { triggerTime = 10f, floor = 2, direction = -1 },
                    new HallCallSchedule { triggerTime = 14f, floor = 0, direction = 1 },
                    new HallCallSchedule { triggerTime = 18f, floor = 1, direction = -1 }
                }
            },
            new LevelDefinition
            {
                name = "Office Rush Hour",
                description = "Concurrent requests force smarter elevator distribution.",
                timeLimit = 50f,
                calls = new[]
                {
                    new HallCallSchedule { triggerTime = 0f, floor = 0, direction = 1 },
                    new HallCallSchedule { triggerTime = 1f, floor = 3, direction = -1 },
                    new HallCallSchedule { triggerTime = 2f, floor = 1, direction = 1 },
                    new HallCallSchedule { triggerTime = 4f, floor = 2, direction = -1 },
                    new HallCallSchedule { triggerTime = 6f, floor = 3, direction = -1 },
                    new HallCallSchedule { triggerTime = 8f, floor = 1, direction = -1 },
                    new HallCallSchedule { triggerTime = 10f, floor = 2, direction = 1 },
                    new HallCallSchedule { triggerTime = 13f, floor = 3, direction = -1 },
                    new HallCallSchedule { triggerTime = 16f, floor = 0, direction = 1 }
                }
            },
            new LevelDefinition
            {
                name = "Evening Peak",
                description = "Sustained mixed-direction traffic tests full system efficiency.",
                timeLimit = 60f,
                calls = new[]
                {
                    new HallCallSchedule { triggerTime = 0f, floor = 3, direction = -1 },
                    new HallCallSchedule { triggerTime = 2f, floor = 2, direction = -1 },
                    new HallCallSchedule { triggerTime = 4f, floor = 1, direction = -1 },
                    new HallCallSchedule { triggerTime = 6f, floor = 0, direction = 1 },
                    new HallCallSchedule { triggerTime = 8f, floor = 2, direction = 1 },
                    new HallCallSchedule { triggerTime = 10f, floor = 3, direction = -1 },
                    new HallCallSchedule { triggerTime = 12f, floor = 1, direction = 1 },
                    new HallCallSchedule { triggerTime = 14f, floor = 0, direction = 1 },
                    new HallCallSchedule { triggerTime = 17f, floor = 2, direction = -1 },
                    new HallCallSchedule { triggerTime = 20f, floor = 3, direction = -1 },
                    new HallCallSchedule { triggerTime = 23f, floor = 1, direction = -1 },
                    new HallCallSchedule { triggerTime = 27f, floor = 0, direction = 1 }
                }
            }
        };
    }
}