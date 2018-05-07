using UnityEngine;
using System.Collections.Generic;

public class Floor20LinedefController : MonoBehaviour, Pokeable
{
    AudioSource audioSource;
    public List<Floor20SectorController> sectorControllers;
    public string CurrentTexture;

    public bool activated = false;

    public bool Poke(GameObject caller)
    {
        if (activated) return false;
        activated = true;

        foreach (Floor20SectorController sectorController in sectorControllers)
            if (sectorController.CurrentState == Floor20SectorController.State.AtBottom)
                sectorController.CurrentState = Floor20SectorController.State.Rising;

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
