using Cysharp.Threading.Tasks;
using R3.Triggers;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// v0.0.1 Title 씬: 버전 텍스트 표시 + 게임 시작 버튼으로 새 게임 시작 (Counter 씬).
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;

    [SerializeField] Button _startButton;
    [SerializeField] GameObject _startButtonEffect;
    [SerializeField] Button _settingsButton;
    [SerializeField] GameObject _settingsButtonEffect;
    [SerializeField] Button _quitButton;
    [SerializeField] GameObject _quitButtonEffect;

    PopupService _popupService;

    private void Start()
    {
        _popupService = FindAnyObjectByType<PopupService>();

        if (versionText != null)
            versionText.text = "v" + Application.version;

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
        }

        SubscribeEvents();
        WaitAndPlayBgm().Forget();
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
        }
    }

    void SubscribeEvents()
    {
        _startButton
            .OnSubmitAsObservable()
            .Subscribe(_ => OnStartButtonClicked())
            .AddTo(this);

        _startButton
            .OnPointerClickAsObservable()
            .Subscribe(_ => OnStartButtonClicked())
            .AddTo(this);

        SubscribeButtonAnim(_startButton, _startButtonEffect);

        _settingsButton
            .OnSubmitAsObservable()
            .Subscribe(_ => OnSettingsButtonClicked())
            .AddTo(this);

        _settingsButton
            .OnPointerClickAsObservable()
            .Subscribe(_ => OnSettingsButtonClicked())
            .AddTo(this);

        SubscribeButtonAnim(_settingsButton, _settingsButtonEffect);

#if UNITY_WEBGL
        _quitButton.SetActive(false);
        _quitButtonEffect.SetActive(false);
#else
        _quitButton
            .OnSubmitAsObservable()
            .Subscribe(_ => OnQuitButtonClicked())
            .AddTo(this);

        _quitButton
            .OnPointerClickAsObservable()
            .Subscribe(_ => OnQuitButtonClicked())
            .AddTo(this);

        SubscribeButtonAnim(_quitButton, _quitButtonEffect);
#endif
    }

    /// <summary>
    /// 버튼 애니메이션 연출 재생 구독
    /// </summary>
    void SubscribeButtonAnim(Button button, GameObject buttonEffect)
    {
        Vector2 effectOrigin = buttonEffect.GetComponent<RectTransform>().anchoredPosition;
        buttonEffect.SetActive(false);
        button
            .OnSelectAsObservable()
            .Subscribe(e =>
            {
                AudioService.Instance.PlaySfx("sfx_ui_select");
                ScaleButton(e.selectedObject, buttonEffect, effectOrigin);
            })
            .AddTo(this);

        button
            .OnDeselectAsObservable()
            .Subscribe(_ => buttonEffect.SetActive(false))
            .AddTo(this);
    }

    void ScaleButton(GameObject button, GameObject buttonEffect, Vector2 effectOrigin)
    {
        DOTween.Sequence()
            .Append(button.transform
                .DOScale(Vector2.one * 1.1f, 0.2f)
                .SetEase(Ease.OutQuad)
            )
            .Append(button.transform
                .DOScale(Vector2.one, 0.2f)
                .SetEase(Ease.InQuad)
            )
            .SetTarget(this)
            .SetLink(this.gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() => button.transform.localScale = Vector2.one)
            .Play();

        buttonEffect.gameObject.SetActive(true);
        buttonEffect.GetComponent<RectTransform>()
            .DOAnchorPos(Vector2.one * 12f, 0.2f)
            .SetEase(Ease.InOutBounce)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this)
            .SetLink(buttonEffect, LinkBehaviour.KillOnDisable)
            .OnKill(() =>
            {
                // buttonEffect gameObject destroy 체크
                if (buttonEffect != null)
                {
                    buttonEffect.GetComponent<RectTransform>().anchoredPosition = effectOrigin;
                }
            })
            .SetRelative(true)
            .Play();

    }

    async UniTask WaitAndPlayBgm()
    {
        await UniTask.WaitForEndOfFrame();
        await UniTask.Yield();

        AudioService.Instance.PlayBgm("bgm_title");
    }

    void OnStartButtonClicked()
    {
        this.DOPause();
        SceneFlow.StartNewGame();
        AudioService.Instance.PlaySfx("sfx_ui_btn_click");
    }

    void OnSettingsButtonClicked()
    {
        _popupService.ShowPopup("popup_title_settings");
        AudioService.Instance.PlaySfx("sfx_ui_btn_click");
    }

    void OnQuitButtonClicked()
    {
        this.DOPause();
        AudioService.Instance.PlaySfx("sfx_ui_btn_click");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
