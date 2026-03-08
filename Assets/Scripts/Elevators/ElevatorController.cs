using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    public enum ElevatorState
    {
        Idle,
        MovingUp,
        MovingDown
    }

    [Header("References")]
    [SerializeField] private Transform cabin;
    [SerializeField] private Transform[] floorPoints;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] private string elevatorName = "Lift";
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDelay = 0.5f;

    private readonly List<int> requests = new List<int>();
    private readonly HashSet<int> requestSet = new HashSet<int>();

    private bool isProcessing = false;

    public int CurrentFloor { get; private set; } = 0;
    public ElevatorState State { get; private set; } = ElevatorState.Idle;

    public event Action<int> OnArrivedAtFloor;

    public bool IsIdle => State == ElevatorState.Idle && !isProcessing && requests.Count == 0;

    public int Direction
    {
        get
        {
            if (State == ElevatorState.MovingUp) return 1;
            if (State == ElevatorState.MovingDown) return -1;
            return 0;
        }
    }

    public int PendingRequestCount => requests.Count;

    private void Start()
    {
        if (floorPoints == null || floorPoints.Length == 0)
        {
            Debug.LogError($"{name}: No floor points assigned.");
            enabled = false;
            return;
        }

        Vector3 startPos = cabin.position;
        startPos.y = floorPoints[CurrentFloor].position.y;
        cabin.position = startPos;

        UpdateStatusText();
    }

    public void AddRequest(int floor)
    {
        if (floor < 0 || floor >= floorPoints.Length)
            return;

        if (requestSet.Contains(floor))
            return;

        requestSet.Add(floor);
        requests.Add(floor);

        if (!isProcessing)
        {
            StartCoroutine(ProcessRequests());
        }

        UpdateStatusText();
    }

    public bool CanTakeRequestOnTheWay(int requestedFloor, int requestedDirection)
    {
        if (IsIdle) return false;

        if (Direction == 1)
        {
            bool sameDirection = requestedDirection >= 0;
            bool onTheWay = requestedFloor >= CurrentFloor;
            return sameDirection && onTheWay;
        }

        if (Direction == -1)
        {
            bool sameDirection = requestedDirection <= 0;
            bool onTheWay = requestedFloor <= CurrentFloor;
            return sameDirection && onTheWay;
        }

        return false;
    }

    public float DistanceToFloor(int floor)
    {
        return Mathf.Abs(CurrentFloor - floor);
    }

    private IEnumerator ProcessRequests()
    {
        isProcessing = true;

        while (requests.Count > 0)
        {
            int nextFloor = GetNextFloorFromQueue();
            requestSet.Remove(nextFloor);
            requests.Remove(nextFloor);

            yield return MoveToFloor(nextFloor);
            yield return new WaitForSeconds(stopDelay);

            CurrentFloor = nextFloor;
            OnArrivedAtFloor?.Invoke(CurrentFloor);
            UpdateStatusText();
        }

        State = ElevatorState.Idle;
        isProcessing = false;
        UpdateStatusText();
    }

    private int GetNextFloorFromQueue()
    {
        if (requests.Count == 1)
            return requests[0];

        if (State == ElevatorState.MovingUp)
        {
            var upward = requests.Where(r => r >= CurrentFloor).OrderBy(r => r).ToList();
            if (upward.Count > 0) return upward[0];

            var downward = requests.OrderByDescending(r => r).ToList();
            return downward[0];
        }

        if (State == ElevatorState.MovingDown)
        {
            var downward = requests.Where(r => r <= CurrentFloor).OrderByDescending(r => r).ToList();
            if (downward.Count > 0) return downward[0];

            var upward = requests.OrderBy(r => r).ToList();
            return upward[0];
        }

        return requests.OrderBy(r => Mathf.Abs(r - CurrentFloor)).First();
    }

    private IEnumerator MoveToFloor(int targetFloor)
    {
        float targetY = floorPoints[targetFloor].position.y;

        if (Mathf.Approximately(cabin.position.y, targetY))
        {
            CurrentFloor = targetFloor;
            UpdateStatusText();
            yield break;
        }

        State = targetFloor > CurrentFloor ? ElevatorState.MovingUp : ElevatorState.MovingDown;
        UpdateStatusText();

        while (Mathf.Abs(cabin.position.y - targetY) > 0.01f)
        {
            Vector3 pos = cabin.position;
            pos.y = Mathf.MoveTowards(pos.y, targetY, moveSpeed * Time.deltaTime);
            cabin.position = pos;

            UpdateStatusText(GetClosestFloorIndex());
            yield return null;
        }

        Vector3 finalPos = cabin.position;
        finalPos.y = targetY;
        cabin.position = finalPos;

        CurrentFloor = targetFloor;
        UpdateStatusText();
    }

    private int GetClosestFloorIndex()
    {
        float bestDistance = float.MaxValue;
        int bestFloor = 0;

        for (int i = 0; i < floorPoints.Length; i++)
        {
            float dist = Mathf.Abs(cabin.position.y - floorPoints[i].position.y);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestFloor = i;
            }
        }

        return bestFloor;
    }

    private void UpdateStatusText(int shownFloor = -1)
    {
        if (statusText == null) return;

        int floorToShow = shownFloor >= 0 ? shownFloor : CurrentFloor;

        string floorLabel = floorToShow == 0 ? "G" : floorToShow.ToString();
        string stateLabel = State switch
        {
            ElevatorState.Idle => "Idle",
            ElevatorState.MovingUp => "Up",
            ElevatorState.MovingDown => "Down",
            _ => "Idle"
        };

        statusText.text = $"{elevatorName} | Floor: {floorLabel} | {stateLabel}";
    }
}