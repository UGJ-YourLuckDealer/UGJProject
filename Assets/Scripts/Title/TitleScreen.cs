using Cysharp.Threading.Tasks;
using R3.Triggers;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// v0.0.1 Title 씬: 버전 텍스트 표시 + 게임 시작 버튼으로 새 게임 시작 (Counter 씬).
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;
    [SerializeField] Button _settingsButton;

    PopupService _popupService;

    private void Start()
    {
        _popupService = FindAnyObjectByType<PopupService>();

        if (versionText != null)
            versionText.text = "v" + Application.version;

        SubscribeEvents();

        WaitAndPlayBgm().Forget();
    }

    void SubscribeEvents()
    {
        _settingsButton
            .OnPointerClickAsObservable()
            .Subscribe(_ => _popupService.ShowPopup("popup_title_settings"))
            .AddTo(this);
    }

    async UniTask WaitAndPlayBgm()
    {
        await UniTask.WaitForEndOfFrame();
        await UniTask.Yield();

        AudioService.Instance.PlayBgm("bgm_title");
    }

    public void OnStartButtonClicked()
    {
        SceneFlow.StartNewGame();
        AudioService.Instance.PlaySfx("sfx_ui_btn_click");
    }
}
