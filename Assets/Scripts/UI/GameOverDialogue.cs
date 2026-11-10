using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 기본 게임 오버 처리 (v0.0.2). 키링 제출 후 자본이 마이너스여서 게임 오버되면(GameFlowManager → Result 씬)
/// 대사창이 화면 아래에서 부드럽게 올라오며 게임 오버 대사를 보여 준다.
/// panel은 화면 아래 가운데에 붙여(anchor·pivot y = 0) 두고, 그 위치가 다 올라온 위치가 된다.
/// </summary>
public class GameOverDialogue : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("게임 오버 대사 (임시)")]
    [SerializeField] private string message = "Bankrupt!";
    [Tooltip("씬 Fade In이 끝난 뒤 올라오도록 기다리는 시간")]
    [SerializeField, Min(0f)] private float delay = 0.8f;
    [SerializeField, Min(0.01f)] private float slideSeconds = 0.6f;

    private Vector2 shownPosition;

    private void Awake()
    {
        if (panel == null) return;
        shownPosition = panel.anchoredPosition;
        panel.gameObject.SetActive(false);
    }

    private void Start()
    {
        var result = SceneFlow.LastResult;
        if (panel == null || result == null || result.Value.isWin) return;

        if (dialogueText != null) dialogueText.text = message;
        StartCoroutine(SlideUp());
    }

    private IEnumerator SlideUp()
    {
        // 화면 아래(대사창 높이만큼 밖)에서 시작
        Vector2 hiddenPosition = new Vector2(shownPosition.x, -panel.rect.height - 20f);
        panel.anchoredPosition = hiddenPosition;
        panel.gameObject.SetActive(true);

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        for (float t = 0f; t < slideSeconds; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / slideSeconds, 3f); // ease-out: 빠르게 올라와 천천히 멈춤
            panel.anchoredPosition = Vector2.LerpUnclamped(hiddenPosition, shownPosition, k);
            yield return null;
        }
        panel.anchoredPosition = shownPosition;
    }
}
