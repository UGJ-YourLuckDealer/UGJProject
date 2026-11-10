using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class AudioService : MonoBehaviour
{
    public static AudioService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioService>();
                if (_instance == null)
                {
                    var go = new GameObject("AudioService");
                    _instance = go.AddComponent<AudioService>();
                }
            }

            return _instance;
        }
    }

    static AudioService _instance;

    [SerializeField] ResourceDB _resourceDB;
    [SerializeField] AudioMixerGroup _sfxMixerGroup;
    [SerializeField] AudioMixerGroup _bgmMixerGroup;

    AudioSource _bgmSource;
    ObjectPool<AudioSource> _sfxPool;
    bool _initialized;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (_instance == null)
        {
            if (_resourceDB == null)
            {
                Debug.LogWarning("ResourceDB is not assigned.");
                return;
            }

            _initialized = true;
            _instance = this;


            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    void CreateBGMSource()
    {
        var bgmSource = new GameObject("BGM Source");
        bgmSource.transform.SetParent(transform);
        _bgmSource = bgmSource.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.outputAudioMixerGroup = _bgmMixerGroup;
    }

    void CreateSfxPool()
    {
        _sfxPool = new ObjectPool<AudioSource>(
            createFunc: CreateSfxSource,
            actionOnGet: GetSfxSource,
            actionOnRelease: ReleaseSfxSource
        );
    }

    AudioSource CreateSfxSource()
    {
        var sfxSource = new GameObject("SFX Source");
        sfxSource.transform.SetParent(transform);
        var audioSource = sfxSource.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.outputAudioMixerGroup = _sfxMixerGroup;
        return audioSource;
    }

    void GetSfxSource(AudioSource source)
    {
        source.enabled = true;
    }

    void ReleaseSfxSource(AudioSource source)
    {
        source.enabled = false;
    }

    /// <summary>
    /// SFX를 재생 (5초 이하만 처리)
    /// </summary>
    public void PlaySfx(string key)
    {
        if (_resourceDB.sfxDict.TryGetValue(key, out IAudioGenerator clip))
        {
            var source = _sfxPool.Get();
            source.generator = clip;
            source.Play();

            // 5초 이하 SFX만 처리
            StartCoroutine(ReleaseSfxSourceAfterPlay(5, source));
        }
    }

    IEnumerator ReleaseSfxSourceAfterPlay(float duration, AudioSource source)
    {
        yield return new WaitForSeconds(duration);
        _sfxPool.Release(source);
    }

    /// <summary>
    /// BGM을 재생(무한 루프)
    /// </summary>
    public void PlayBgm(string key)
    {
        if (_resourceDB.bgmDict.TryGetValue(key, out AudioClip clip))
        {
            _bgmSource.generator = clip;
            _bgmSource.Play();
        }
    }

    /// <summary>
    /// BGM을 중지
    /// </summary>
    public void StopBgm()
    {
        _bgmSource.Stop();
    }
}
