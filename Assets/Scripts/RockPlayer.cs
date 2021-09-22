using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource sound0;
    [SerializeField] private AudioSource sound1;
    [SerializeField] private AudioSource sound2;
    [SerializeField] private AudioSource sound3;
    [SerializeField] private AudioSource sound4;
    public List<AudioClip> clips0 = new List<AudioClip>();
    public List<AudioClip> clips1 = new List<AudioClip>();
    public List<AudioClip> clips2 = new List<AudioClip>();
    public List<AudioClip> clips3 = new List<AudioClip>();
    public List<AudioClip> clips4 = new List<AudioClip>();

    public void Play(int size)
    {
        (AudioSource source, AudioClip clip) toPlay = size switch {
            0 => (sound0, clips0[UnityEngine.Random.Range(0, clips0.Count)]),
            1 => (sound1, clips1[UnityEngine.Random.Range(0, clips1.Count)]),
            2 => (sound2, clips2[UnityEngine.Random.Range(0, clips2.Count)]),
            3 => (sound3, clips3[UnityEngine.Random.Range(0, clips3.Count)]),
            4 => (sound4, clips4[UnityEngine.Random.Range(0, clips4.Count)]),
            _ => (null, null)
        };
        
        if(toPlay.source != null && toPlay.clip != null)
        {
            if(toPlay.source.time >= toPlay.clip.length * 0.5f || !toPlay.source.isPlaying)
                toPlay.source.PlayOneShot(toPlay.clip);
        }
    }
}
