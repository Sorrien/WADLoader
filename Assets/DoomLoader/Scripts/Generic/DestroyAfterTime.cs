using UnityEngine;

public class DestroyAfterTime : MonoBehaviour 
{
    public float _lifeTime = 1;
    float time = 0f;

    void Update()
    {
        if (GameManager.Paused)
            return;

        time += Time.deltaTime;

        if (time >= _lifeTime)
            Destroy(gameObject);
    }
}
