using System.Collections.Generic;
using UnityEngine;
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    [SerializeField] private List<AudioSource> sources;
    [SerializeField] private List<AudioClip> clips;
    [SerializeField] private int Pool_Size;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        CompletePool(Pool_Size);
    }
    private void CompletePool(int num)
    {
        for (int i = 0; i < Pool_Size; i++)
        {
            AddSoundSource();
        }
    }
    private void AddSoundSource()
    {
        var AudioSource = this.gameObject.AddComponent<AudioSource>();
        sources.Add(AudioSource);
    }
    private AudioSource GetAudioSourceFromList()
    {
        return sources.Find(x => x.isPlaying == false);
    }
    public void PlayClip(AudioClip clip, float volumen, bool loop)
    {
        if(PauseManager.instance != null)
        {
            if (PauseManager.instance.on_pause) return;
        }
        var audioSource = GetAudioSourceFromList();
        if (audioSource == null) return;
        audioSource.volume = volumen;
        audioSource.loop = loop;
        audioSource.clip = clip;
        audioSource.Play();
    }
    public void PlayMusic(AudioClip clip, float volumen, bool loop)
    {
        var audioSource = GetAudioSourceFromList();
        if (audioSource == null) return;
        audioSource.volume = volumen;
        audioSource.loop = loop;
        audioSource.clip = clip;
        audioSource.Play();
    }
    public void PlayClipMenu(AudioClip clip, float volumen, bool loop)
    {
        var audioSource = GetAudioSourceFromList();
        if (audioSource == null) return;
        audioSource.volume = volumen;
        audioSource.loop = loop;
        audioSource.clip = clip;
        audioSource.Play();
    }
    public AudioClip GetAudioClip(string clip)
    {
        return clips.Find(x => x.name == clip);
    }
}
