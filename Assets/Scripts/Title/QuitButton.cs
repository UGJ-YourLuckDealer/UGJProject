using UnityEngine;
using UnityEngine.UI;
using R3;

/// <summary>
/// Title 씬 Quit(게임 종료) 버튼. PC 빌드에서만 보이고, 웹(WebGL) 빌드에서는 스스로 숨긴다.
/// 에디터에서는 Play를 멈춘다.
/// </summary>
[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_WEBGL
        this.gameObject.SetActive(false);
        return;
#endif

        this.GetComponent<Button>()
            .OnClickAsObservable()
            .Take(1)
            .Subscribe(_ => this.Quit())
            .AddTo(this);
    }

    public void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
