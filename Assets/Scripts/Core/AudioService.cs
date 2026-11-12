
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class AudioService : MonoBehaviour
{
    public const string VOLUME_SAVE_KEY = "MasterVolume";
    const string MASTER_VOLUME_PARAM = "masterVol";

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
                    _instance.Initialize();
                }
            }

            return _instance;
        }
    }

    public float MasterVolume => _masterVolume;

    static AudioService _instance;

    [SerializeField] ResourceDB _resourceDB;
    [SerializeField] AudioMixer _audioMixer;
    [SerializeField] AudioMixerGroup _sfxMixerGroup;
    [SerializeField] AudioMixerGroup _bgmMixerGroup;

    AudioSource _bgmSource;
    ObjectPool<AudioSource> _sfxPool;
    struct ActiveSfx
    {
        public AudioSource source;
        public string concurrencyGroup;
        public int startFrame;
    }

    List<ActiveSfx> _activeSfxSources;
    bool _initialized;

    float _masterVolume;

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

        if (_initialized)
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
            _activeSfxSources = new List<ActiveSfx>();
            _instance = this;

            LoadVolumeSettings().Forget();
            CreateBgmSource();
            CreateSfxPool();
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

    async UniTask LoadVolumeSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat(VOLUME_SAVE_KEY, 1f);

        await UniTask.WaitForEndOfFrame();

        SetMasterVolume(_masterVolume);
    }

    void CreateBgmSource()
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
        source.Stop();
        source.generator = null;
        source.enabled = false;
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = volume;

        // 0.01f ~ 1f 범위로 제한
        // !0이 들어가면 Log10(0)이 되어 NaN이 되므로 0.01f로 클램프
        float audioMixerValue = Mathf.Clamp(_masterVolume, 0.01f, 1f);
        _audioMixer.SetFloat(MASTER_VOLUME_PARAM, Mathf.Log10(audioMixerValue) * 20);
        PlayerPrefs.SetFloat(VOLUME_SAVE_KEY, _masterVolume);
    }

    /// <summary>동시 재생 수 제한 없이 SFX를 재생합니다.</summary>
    public void PlaySfx(string key)
    {
        TryPlaySfx(key, int.MaxValue);
    }

    /// <summary>
    /// 같은 그룹의 활성 재생 수가 maxConcurrent 미만이면 재생을 요청합니다.
    /// 그룹을 지정하지 않으면 key를 그룹으로 사용합니다.
    /// </summary>
    public bool TryPlaySfx(string key, int maxConcurrent, string concurrencyGroup = null)
    {
        if (!_initialized || !isActiveAndEnabled || string.IsNullOrEmpty(key) || maxConcurrent <= 0 ||
            !_resourceDB.sfxDict.TryGetValue(key, out IAudioGenerator clip) || clip == null)
        {
            return false;
        }

        ReleaseFinishedSfx();
        string group = string.IsNullOrEmpty(concurrencyGroup) ? key : concurrencyGroup;
        int count = 0;
        foreach (var active in _activeSfxSources)
        {
            if (active.concurrencyGroup == group && ++count >= maxConcurrent)
            {
                return false;
            }
        }

        var source = _sfxPool.Get();
        source.generator = clip;
        source.Play();
        _activeSfxSources.Add(new ActiveSfx { source = source, concurrencyGroup = group, startFrame = Time.frameCount });
        return true;
    }

    void Update()
    {
        if (_initialized)
        {
            ReleaseFinishedSfx();
        }
    }

    void ReleaseFinishedSfx()
    {
        for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
        {
            var active = _activeSfxSources[i];
            // 시작 프레임과 리스너 일시정지 중에는 재생 상태만으로 종료를 판단하지 않습니다.
            if (active.startFrame == Time.frameCount ||
                (AudioListener.pause && !active.source.ignoreListenerPause) || active.source.isPlaying)
            {
                continue;
            }

            _activeSfxSources.RemoveAt(i);
            _sfxPool.Release(active.source);
        }
    }

    void OnDisable()
    {
        ClearSfx();
    }

    public void ClearSfx()
    {
        if (_activeSfxSources == null || _sfxPool == null) return;

        while (_activeSfxSources.Count > 0)
        {
            int index = _activeSfxSources.Count - 1;
            var source = _activeSfxSources[index].source;
            _activeSfxSources.RemoveAt(index);
            _sfxPool.Release(source);
        }
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
