using System.Collections.Generic;
using UnityEngine;

public class PooledParticleSystem : MonoBehaviour, IPoolable
{
    public bool inPool {get => _inPool; set => _inPool = value;}
    bool _inPool = false;
    public event OnReturnToPool onReturnToPool;
    public ParticleSystem partSystem;
    bool isAlive = false;
    [SerializeField] private AudioSource audioSource;
    public List<AudioClip> clips = new List<AudioClip>();
    public bool resetTransforms = false;
    private void Update() {
        if(isAlive)
        {
            if(!partSystem.IsAlive())
            {
                if(resetTransforms)
                {
                    transform.localScale = Vector3.one;
                    for(int i = 0; i < transform.childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        child.localScale = Vector3.one;
                    }
                }

                isAlive = false;
                onReturnToPool?.Invoke();
            }
        }
    }

    public void Clear() => partSystem.Clear();
    public void Play() 
    {
        if(audioSource != null)
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Count)]);

        isAlive = true;
        partSystem.Play();
    }
    public void Pause() => partSystem.Pause();
    public void Stop() => partSystem.Stop();

    public void SetSize(float val)
    {
        var main = partSystem.main;
        main.startSizeMultiplier = val;
    }
}
