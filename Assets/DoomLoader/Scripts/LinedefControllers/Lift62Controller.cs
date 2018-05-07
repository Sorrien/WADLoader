using UnityEngine;
using System.Collections.Generic;

public class Lift62Controller : MonoBehaviour, Pokeable
{
    public List<Slow3sLiftController> liftControllers;

    public bool Poke(GameObject caller)
    {
        foreach (Slow3sLiftController liftController in liftControllers)
            if (liftController.CurrentState == Slow3sLiftController.State.AtTop)
            {
                liftController.CurrentState = Slow3sLiftController.State.Lowering;
                return true;
            }

        return false;
    }

    public bool AllowMonsters()
    {
        return false;
    }
}
