using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 설정 팝업 (임시 UI). Setting 버튼 또는 ESC로 열고 닫는다.
/// 전체 볼륨(AudioListener.volume, PlayerPrefs 저장) / Resume / Title(게임 중) / Quit(PC 빌드만).
/// 게임 씬에서는 열려 있는 동안 Time.timeScale = 0으로 일시정지한다.
/// 마우스를 직접 읽는 팀원 스크립트보다 먼저 실행되도록 실행 순서를 앞으로 둔다 (Setting 버튼을 누른 클릭이 대화로 새지 않게).
/// </summary>
[DefaultExecutionOrder(-1000)]
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
    [Tooltip("Title 씬에서는 끈다 (Title 화면에 별도 Quit 버튼이 있음)")]
    [SerializeField] private bool showQuitButton = true;
    [Tooltip("열려 있는 동안 게임 일시정지")]
    [SerializeField] private bool pauseWhileOpen = true;
    [Tooltip("열려 있는 동안 꺼 둘 컴포넌트 (마우스를 UI와 상관없이 직접 읽어 팝업 뒤로 클릭이 새는 팀원 스크립트)")]
    [SerializeField] private string[] blockWhileOpen =
    {
        "DialogueManager", "DeskInteraction",
        "ObjectInteraction", "ObjectDragManager", "ObjectDeleteManager", "KeyringMaterialObject",
    };

    private readonly List<Behaviour> blocked = new List<Behaviour>();
    private Coroutine unblockRoutine;
    private bool pressingOpenButton;

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

            // Title 버튼을 숨기면 Quit 버튼을 그 자리로 올려 빈 공간을 없앤다
            if (!showTitleButton && quitButton != null)
                ((RectTransform)quitButton.transform).anchoredPosition = ((RectTransform)titleButton.transform).anchoredPosition;
        }
        if (quitButton != null)
        {
            // Title 화면에 별도 Quit이 있거나, 웹(WebGL)이면 팝업의 종료 버튼 숨김
            quitButton.gameObject.SetActive(showQuitButton && QuitButton.IsQuitSupported);
            quitButton.onClick.AddListener(Quit);
        }

        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Toggle();

        GuardOpenButtonClick();
    }

    // Setting 버튼은 ButtonClickDelay로 조금 늦게 열리므로, 누르는 순간 바로 게임 입력을 잠가
    // 같은 클릭이 뒤의 대화 넘기기·재료 집기로 처리되지 않게 한다.
    private void GuardOpenButtonClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || openButton == null || IsOpen) return;

        if (mouse.leftButton.wasPressedThisFrame && IsPointerOver(openButton, mouse.position.ReadValue()))
        {
            pressingOpenButton = true;
            BlockGameInput();
        }
        else if (pressingOpenButton && mouse.leftButton.wasReleasedThisFrame)
        {
            pressingOpenButton = false;
            // 버튼 위에서 떼면 곧 열린다(그때부터는 Close가 잠금을 푼다). 열리지 않으면 잠금을 푼다.
            if (unblockRoutine != null) StopCoroutine(unblockRoutine);
            unblockRoutine = StartCoroutine(UnblockIfNotOpened());
        }
    }

    private static bool IsPointerOver(Selectable target, Vector2 screenPosition)
    {
        var rect = (RectTransform)target.transform;
        var canvas = target.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return target.isActiveAndEnabled && target.interactable
            && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, cam);
    }

    private IEnumerator UnblockIfNotOpened()
    {
        yield return new WaitForSecondsRealtime(0.5f); // ButtonClickDelay(0.15초)보다 넉넉히
        unblockRoutine = null;
        if (!IsOpen) yield return UnblockNextFrame();
    }

    private void OnDestroy()
    {
        if (IsOpen && pauseWhileOpen) Time.timeScale = 1f; // 씬 전환 시 일시정지가 남지 않게
        if (IsOpen || blocked.Count > 0) GameSession.IsUIBlockingInput = false; // 팝업을 연 채(또는 닫는 중에) 씬이 바뀐 경우
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
        BlockGameInput();
    }

    public void Close()
    {
        if (panel == null) return;
        panel.SetActive(false);
        if (pauseWhileOpen) Time.timeScale = 1f;
        PlayerPrefs.Save();
        // 닫는 클릭이 같은 프레임에 뒤의 대화·재료로 전달되지 않도록 다음 프레임에 되돌린다
        if (unblockRoutine != null) StopCoroutine(unblockRoutine);
        unblockRoutine = StartCoroutine(UnblockNextFrame());
    }

    private void BlockGameInput()
    {
        if (unblockRoutine != null) { StopCoroutine(unblockRoutine); unblockRoutine = null; }
        GameSession.IsUIBlockingInput = true;
        if (blocked.Count > 0) return; // 이미 꺼 둔 상태

        var names = new HashSet<string>(blockWhileOpen);
        foreach (var b in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (b.enabled && names.Contains(b.GetType().Name))
            {
                b.enabled = false;
                blocked.Add(b);
            }
    }

    private IEnumerator UnblockNextFrame()
    {
        yield return null;
        foreach (var b in blocked)
            if (b != null) b.enabled = true;
        blocked.Clear();
        GameSession.IsUIBlockingInput = false;
        unblockRoutine = null;
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
