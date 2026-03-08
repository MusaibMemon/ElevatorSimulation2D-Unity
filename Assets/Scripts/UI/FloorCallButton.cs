using UnityEngine;

public class FloorCallButton : MonoBehaviour
{
    [SerializeField] private ElevatorManager elevatorManager;
    [SerializeField] private int floor;
    [SerializeField] private int direction; // 1 = up, -1 = down

    public void CallElevator()
    {
        if (elevatorManager == null)
        {
            Debug.LogError($"{name}: ElevatorManager reference missing.");
            return;
        }

        elevatorManager.RequestElevator(floor, direction);
    }
}