using R3.Triggers;
using UnityEngine;
using UnityEngine.UI;
using R3;
using TMPro;
using System.Globalization;

public class SettingsPopupUI : MonoBehaviour
{
    [SerializeField] Image _backgroundPanel;

    [SerializeField] Slider _volumeSlider;
    [SerializeField] TMP_Text _volumeText;

    void Awake()
    {
        _backgroundPanel
            .OnPointerClickAsObservable()
            .Subscribe(_ => Hide())
            .AddTo(this);

        _volumeSlider.value = AudioService.Instance.MasterVolume;
        _volumeSlider
            .OnValueChangedAsObservable()
            .Subscribe(OnVolumeChanged)
            .AddTo(this);
    }

    void OnVolumeChanged(float value)
    {
        AudioService.Instance.SetMasterVolume(value);
        _volumeText.text = Mathf.RoundToInt(value * 100f)
            .ToString(CultureInfo.InvariantCulture);
    }

    void Hide()
    {
        PlayerPrefs.Save();
        Destroy(this.gameObject);
    }
}