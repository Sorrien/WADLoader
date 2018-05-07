using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera Instance;
    PlayerControls playerControls;

    void Awake()
    {
        Instance = this;

        playerControls = GetComponentInParent<PlayerControls>();
    }

    void Start()
    {
    }

    void Update()
    {
        if (GameManager.Paused)
            return;

        if (Options.HeadBob)
        {
            if (bopActive)
                interp = Mathf.Lerp(interp, 1, Time.deltaTime * 5);
            else
                interp = Mathf.Lerp(interp, 0, Time.deltaTime * 6);

            transform.localPosition = new Vector3(0, .35f + Mathf.Sin(Time.time * 10) * .15f * interp, 0);
        }

        transform.localRotation = Quaternion.Euler(playerControls.viewDirection.x, 0, 0);
    }

    float interp;
    public bool bopActive;
}
