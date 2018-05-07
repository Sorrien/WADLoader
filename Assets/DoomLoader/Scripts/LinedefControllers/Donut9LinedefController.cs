using UnityEngine;
using System.Collections.Generic;

public class Donut9LinedefController : MonoBehaviour, Pokeable
{
    public Donut9SectorController sectorController;

    AudioSource audioSource;
    public string CurrentTexture;

    public bool activated = false;

    public bool Poke(GameObject caller)
    {
        if (activated)
            return false;

        activated = true;

        TextureLoader.Instance.SetSwitchTexture(GetComponent<MeshRenderer>(), true);
        audioSource.Play();

        if (sectorController.CurrentState == Donut9SectorController.State.Waiting)
            sectorController.CurrentState = Donut9SectorController.State.Active;

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
