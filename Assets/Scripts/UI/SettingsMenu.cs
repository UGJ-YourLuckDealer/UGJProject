using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 설정 팝업 (임시 UI). Setting 버튼 또는 ESC로 열고 닫는다.
/// 전체 볼륨(AudioListener.volume, PlayerPrefs 저장) / Resume / Title(게임 중) / Quit(PC 빌드만).
/// 게임 씬에서는 열려 있는 동안 Time.timeScale = 0으로 일시정지한다.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";

    [SerializeField] private GameObject panel;
    [SerializeField] private Button openButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValue;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitButton;

    [Tooltip("Title 씬에서는 끈다 (이미 타이틀)")]
    [SerializeField] private bool showTitleButton = true;
    [Tooltip("열려 있는 동안 게임 일시정지")]
    [SerializeField] private bool pauseWhileOpen = true;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        ShowVolume(volume);

        if (openButton != null) openButton.onClick.AddListener(Toggle);
        if (resumeButton != null) resumeButton.onClick.AddListener(Close);
        if (titleButton != null)
        {
            titleButton.gameObject.SetActive(showTitleButton);
            titleButton.onClick.AddListener(GoToTitle);
        }
        if (quitButton != null)
        {
#if UNITY_WEBGL
            quitButton.gameObject.SetActive(false); // itch.io(WebGL)에서는 종료 버튼 숨김
#endif
            quitButton.onClick.AddListener(Quit);
        }

        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Toggle();
    }

    private void OnDestroy()
    {
        if (IsOpen && pauseWhileOpen) Time.timeScale = 1f; // 씬 전환 시 일시정지가 남지 않게
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);
        if (pauseWhileOpen) Time.timeScale = 0f;
    }

    public void Close()
    {
        if (panel == null) return;
        panel.SetActive(false);
        if (pauseWhileOpen) Time.timeScale = 1f;
        PlayerPrefs.Save();
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        ShowVolume(value);
    }

    private void ShowVolume(float value)
    {
        if (volumeValue != null)
            volumeValue.text = Mathf.RoundToInt(value * 100f).ToString(CultureInfo.InvariantCulture);
    }

    private void GoToTitle()
    {
        Close();
        SceneFlow.GoToTitle();
    }

    private void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
