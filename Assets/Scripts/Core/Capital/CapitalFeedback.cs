using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// 돈 증감 피드백 (v0.0.2). 자본이 바뀌면 +/- 숫자 이펙트, 금액 숫자 강조, SFX를 재생한다.
/// CapitalUI 프리팹의 MoneyBar에 붙는다. 효과음 파일을 비워 두면 임시 효과음을 코드로 생성해 쓴다.
/// </summary>
public class CapitalFeedback : MonoBehaviour
{
    [Header("숫자 UI")]
    [SerializeField] private RectTransform moneyText;      // 강조(크기) 연출 대상
    [SerializeField] private TMP_Text deltaTemplate;        // +/- 이펙트 원본 (비활성)
    [SerializeField] private Color gainColor = new Color32(0x5B, 0xE8, 0x5B, 0xFF);
    [SerializeField] private Color lossColor = new Color32(0xFF, 0x5A, 0x4A, 0xFF);
    [SerializeField, Min(0.1f)] private float floatSeconds = 0.8f;
    [SerializeField] private float floatDistance = 50f;
    [SerializeField] private float punchScale = 1.15f;

    [Header("SFX (비우면 임시 효과음)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gainClip;
    [SerializeField] private AudioClip lossClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.6f;

    private float lastValue;
    private bool hasValue;
    private Coroutine punchRoutine;
    private static AudioClip generatedGain, generatedLoss;

    private void OnEnable() => GameEvents.CapitalChanged += OnCapitalChanged;
    private void OnDisable() => GameEvents.CapitalChanged -= OnCapitalChanged;

    private void Start()
    {
        // 씬에 들어올 때의 금액은 변화가 아니므로 기준값으로만 기억한다
        var capital = FindAnyObjectByType<CapitalSystem>();
        if (capital != null && !hasValue) { lastValue = capital.Current; hasValue = true; }
        if (deltaTemplate != null) deltaTemplate.gameObject.SetActive(false);
    }

    private void OnCapitalChanged(float value)
    {
        if (!hasValue) { lastValue = value; hasValue = true; return; }
        float delta = value - lastValue;
        lastValue = value;
        if (Mathf.Abs(delta) < 0.005f) return;

        bool gain = delta > 0f;
        SpawnDelta(delta, gain);
        if (moneyText != null && isActiveAndEnabled)
        {
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(Punch());
        }
        PlaySfx(gain);
    }

    private void SpawnDelta(float delta, bool gain)
    {
        if (deltaTemplate == null || !isActiveAndEnabled) return;
        var t = Instantiate(deltaTemplate, deltaTemplate.transform.parent);
        t.gameObject.SetActive(true);
        t.text = (gain ? "+" : "-") + Mathf.Abs(delta).ToString("0.00", CultureInfo.InvariantCulture);
        t.color = gain ? gainColor : lossColor;
        StartCoroutine(Float(t));
    }

    private IEnumerator Float(TMP_Text t)
    {
        var rt = t.rectTransform;
        Vector2 start = rt.anchoredPosition;
        Color c = t.color;
        for (float e = 0f; e < floatSeconds; e += Time.unscaledDeltaTime)
        {
            float k = e / floatSeconds;
            rt.anchoredPosition = start + Vector2.down * (floatDistance * (1f - (1f - k) * (1f - k)));
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            t.color = c;
            yield return null;
        }
        Destroy(t.gameObject);
    }

    private IEnumerator Punch()
    {
        const float up = 0.08f, down = 0.15f;
        for (float e = 0f; e < up; e += Time.unscaledDeltaTime)
        { moneyText.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, e / up); yield return null; }
        for (float e = 0f; e < down; e += Time.unscaledDeltaTime)
        { moneyText.localScale = Vector3.one * Mathf.Lerp(punchScale, 1f, e / down); yield return null; }
        moneyText.localScale = Vector3.one;
        punchRoutine = null;
    }

    private void PlaySfx(bool gain)
    {
        if (audioSource == null) return;
        var clip = gain ? (gainClip != null ? gainClip : GeneratedGain) : (lossClip != null ? lossClip : GeneratedLoss);
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    // ---- 임시 효과음 (효과음 파일이 들어오기 전까지) ----
    private static AudioClip GeneratedGain => generatedGain ??= Tones("sfx_money_gain_temp", new[] { 988f, 1319f });
    private static AudioClip GeneratedLoss => generatedLoss ??= Tones("sfx_money_loss_temp", new[] { 523f, 392f });

    private static AudioClip Tones(string name, float[] freqs)
    {
        const int rate = 44100;
        const float noteSec = 0.08f;
        int perNote = (int)(rate * noteSec);
        var data = new float[perNote * freqs.Length];
        for (int n = 0; n < freqs.Length; n++)
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Min(1f, i / (rate * 0.005f)) * (1f - (float)i / perNote); // 짧은 어택 + 감쇠
                data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * 0.5f;
            }
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { generatedGain = null; generatedLoss = null; }
}
