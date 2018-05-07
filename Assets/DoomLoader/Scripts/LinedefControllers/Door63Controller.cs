using UnityEngine;
using System.Collections.Generic;

public class Door63Controller : MonoBehaviour, Pokeable
{
    AudioSource audioSource;
    public List<SlowRepeatableDoorController> sectorControllers;
    public string CurrentTexture;

    float activationTime = 0f;
    public bool activated = false;

    public bool Poke(GameObject caller)
    {
        if (activated) return false;
        activated = true;
        activationTime = 1f;

        foreach(SlowRepeatableDoorController sectorController in sectorControllers)
            if (sectorController.CurrentState == SlowRepeatableDoorController.State.Closed)
                sectorController.CurrentState = SlowRepeatableDoorController.State.Opening;

        TextureLoader.Instance.SetSwitchTexture(GetComponent<MeshRenderer>(), true);
        audioSource.Play();
        return true;
    }

    void Update()
    {
        if (GameManager.Paused)
            return;

        if (activated)
        {
            if (activationTime > 0f)
                activationTime -= Time.deltaTime;
            else
            {
                activated = false;
                TextureLoader.Instance.SetSwitchTexture(GetComponent<MeshRenderer>(), false);
            }
        }
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