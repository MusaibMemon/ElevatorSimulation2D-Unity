using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ElevatorManager : MonoBehaviour
{
    [SerializeField] private ElevatorController[] elevators;
    [SerializeField] private int totalFloors = 4;

    private readonly HashSet<string> pendingHallCalls = new HashSet<string>();

    private void Awake()
    {
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

    public void RequestElevator(int floor, int direction)
    {
        if (floor < 0 || floor >= totalFloors)
            return;

        string key = GetHallCallKey(floor, direction);

        if (pendingHallCalls.Contains(key))
            return;

        ElevatorController chosen = FindBestElevator(floor, direction);

        if (chosen == null)
            return;

        pendingHallCalls.Add(key);
        chosen.AddRequest(floor);

        Debug.Log($"Request at floor {floor} dir {direction} assigned to {chosen.name}");
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
        pendingHallCalls.Remove(GetHallCallKey(floor, 1));
        pendingHallCalls.Remove(GetHallCallKey(floor, -1));
    }

    private string GetHallCallKey(int floor, int direction)
    {
        return $"{floor}_{direction}";
    }
}