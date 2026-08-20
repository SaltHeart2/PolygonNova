using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    AudioSource musicSource;
    AudioSource oneShotSource;
    Dictionary<SFXType, AudioSource> loopSources;

    [Header("Músicas del fondo")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip gameOverMusic;
    public AudioClip victoryMusic;

    enum MusicState
    {
        Menu,
        Gameplay,
        GameOver,
        Victory
    }

    public enum SFXType
    {
        PlayerNormalShot,
        PlayerEnergyCharge,
        PlayerEnergyShot,
        PlayerDualSwords,
        PlayerMineDrop,

        BossDash,
        BossLaser,
        BossEnergyBall,
        BossRays,
        BossBlackHole
    }

    [System.Serializable]
    public class SFXEntry
    {
        public SFXType type;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [Header("Lista de sonidos")]
    public List<SFXEntry> sfxList;
    Dictionary<SFXType, SFXEntry> sfxDict;
    
    [Header("Configuración de Sonido")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.4f;


    void Awake()
    {
        Instance = this;

        // Añadimos dinámicamente el componente AudioSource
        musicSource = gameObject.AddComponent<AudioSource>();
        oneShotSource = gameObject.AddComponent<AudioSource>();
        loopSources = new Dictionary<SFXType, AudioSource>();

        // Crear AudioSource específica para algunas ataques
        foreach (var entry in sfxList)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = entry.clip;
            src.loop = true;
            src.volume = entry.volume * sfxVolume;

            loopSources.Add(entry.type, src);
        }

        // La música del fondo debe repetir cuando se acaba
        musicSource.loop = true;

        // Configuración de volumen
        musicSource.volume = musicVolume;
        oneShotSource.volume = sfxVolume;

        // Construir el diccionario de sonido
        sfxDict = new Dictionary<SFXType, SFXEntry>();
        foreach (var sfx in sfxList)
        {
            if (!sfxDict.ContainsKey(sfx.type))
                sfxDict.Add(sfx.type, sfx);
        }
    }

    // Reproducir la música del fondo
    public void PlayMusic(AudioClip clip, bool loop)
    {
        if (clip == null) return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    // Los métodos para reproducir la músca según el caso
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic, true);
    }
    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic, true);
    }
    public void PlayGameOverMusic()
    {
        PlayMusic(gameOverMusic, false);
    }
    public void PlayVictoryMusic()
    {
        PlayMusic(victoryMusic, false);
    }

    // Reproducir sonidos sólo una vez (disparo, golpes, etc.)
    public void PlaySFX(SFXType type)
    {
        if (!sfxDict.ContainsKey(type)) return;

        SFXEntry sfx = sfxDict[type];
        oneShotSource.PlayOneShot(sfx.clip, sfx.volume);
    }

    // Reproducir el sonido contínuo (láser, bola de energía, etc.)
    public void PlaySFXLoop(SFXType type)
    {
        if (!loopSources.ContainsKey(type))
            return;

        AudioSource src = loopSources[type];

        if (!src.isPlaying)
            src.Play();
    }

    // Detener el sonido contínuo correspondiente
    public void StopLoopSFX(SFXType type)
    {
        if (!loopSources.ContainsKey(type))
            return;

        loopSources[type].Stop();
    }

    // Detener todos los sonidos
    public void StopAllLoopSFX()
    {
        foreach (var src in loopSources.Values)
            src.Stop();
    }
}