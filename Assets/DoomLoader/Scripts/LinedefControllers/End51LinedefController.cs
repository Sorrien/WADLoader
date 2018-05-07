using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class End51LinedefController : MonoBehaviour, Pokeable
{
    AudioSource audioSource;
    public string CurrentTexture;

    public bool activated = false;

    public bool Poke(GameObject caller)
    {
        if (GameManager.Instance.deathmatch)
            return false;

        if (activated)
            return false;

        activated = true;

        TextureLoader.Instance.SetSwitchTexture(GetComponent<MeshRenderer>(), true);
        audioSource.Play();

        if (MapLoader.CurrentMap == "E1M3")
            GameManager.Instance.ChangeMap = "E1M9";

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
        return false; //would be funny tho
    }
}
