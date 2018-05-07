using UnityEngine;
using System.Collections.Generic;

public class Door103Controller : MonoBehaviour, Pokeable
{
    AudioSource audioSource;
    public List<SlowOneshotDoorController> sectorControllers;
    public string CurrentTexture;

    public bool activated = false;

    public bool Poke(GameObject caller)
    {
        if (activated) return false;
        activated = true;

        foreach(SlowOneshotDoorController sectorController in sectorControllers)
            if (sectorController.CurrentState == SlowOneshotDoorController.State.Closed)
                sectorController.CurrentState = SlowOneshotDoorController.State.Opening;

        TextureLoader.Instance.SetSwitchTexture(GetComponent<MeshRenderer>(), true);
        audioSource.Play();
        return true;
    }

    void Awake()
    {
        GameObject audioPosition = new GameObject("Audio Position");
        audioPosition.transform.position = GetComponent<MeshFilter>().mesh.bounds.center;
        audioPosition.transform.SetParent(transform, true);
        audioSource = audioPosition.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.clip = SoundLoader.Instance.LoadSound("DSSWTCHN");
    }

    public bool AllowMonsters()
    {
        return false;
    }
}