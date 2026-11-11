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

    bool _isPopupOpen;
    GameObject _selectedBeforePopup;

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
        bool popupOpen = _popupService != null && _popupService.IsAnyPopupOpen;
        if (popupOpen != _isPopupOpen)
        {
            _isPopupOpen = popupOpen;
            OnPopupStateChanged(popupOpen);
        }

        if (_isPopupOpen)
        {
            // 팝업이 열려 있는 동안은 팝업 안의 UI만 선택되도록 유지
            KeepSelectionInPopup();
            return;
        }

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
        }
    }

    /// <summary>
    /// 팝업이 열리면 뒤의 Title 버튼을 잠가 WASD·방향키 이동, Submit, 강조 연출이 동작하지 않게 하고
    /// 닫히면 팝업을 열기 전에 선택돼 있던 버튼으로 돌아간다.
    /// </summary>
    void OnPopupStateChanged(bool popupOpen)
    {
        if (popupOpen)
        {
            _selectedBeforePopup = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null); // 버튼 Deselect → 강조 이펙트 끄기
        }

        SetTitleButtonsInteractable(!popupOpen);

        if (popupOpen)
        {
            KeepSelectionInPopup();
        }
        else
        {
            var restore = _selectedBeforePopup != null ? _selectedBeforePopup : _startButton.gameObject;
            EventSystem.current.SetSelectedGameObject(restore);
            _selectedBeforePopup = null;
        }
    }

    void SetTitleButtonsInteractable(bool interactable)
    {
        _startButton.interactable = interactable;
        _settingsButton.interactable = interactable;
        if (_quitButton != null) _quitButton.interactable = interactable;
    }

    void KeepSelectionInPopup()
    {
        var popup = _popupService.TopPopup;
        if (popup == null) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(popup.transform)) return;

        var first = popup.GetComponentInChildren<Selectable>();
        if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
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
        _quitButton.gameObject.SetActive(false);
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

        // 마우스를 올리면 그 버튼을 선택 상태로 바꿔 강조 이펙트가 키보드 선택과 같이 따라오게 한다
        // (마우스 강조와 키보드 선택이 서로 다른 버튼에 동시에 표시되던 문제)
        button
            .OnPointerEnterAsObservable()
            .Where(_ => button.interactable && EventSystem.current.currentSelectedGameObject != button.gameObject)
            .Subscribe(_ => EventSystem.current.SetSelectedGameObject(button.gameObject))
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
