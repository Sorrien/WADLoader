using UnityEngine;

public class Door31Controller : MonoBehaviour, Pokeable
{
    public SlowOneshotDoorController sectorController;

    public bool Poke(GameObject caller)
    {
        if (sectorController.CurrentState == SlowOneshotDoorController.State.Closed)
        {
            sectorController.CurrentState = SlowOneshotDoorController.State.Opening;
            return true;
        }

        return false;
    }

    public bool AllowMonsters()
    {
        return false;
    }
}
