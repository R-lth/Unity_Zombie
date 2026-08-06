using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameFlowUI : MonoBehaviour
{
    private TMP_FontAsset mapleFont;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI escapeGuideText;
    private GameObject resultPanel;
    private TextMeshProUGUI resultTitle;
    private TextMeshProUGUI resultDescription;
    private Button restartButton;
    private Button titleButton;

    private void Awake()
    {
        BuildUI();
    }

    private void Start()
    {
        restartButton.onClick.AddListener(GameManager.Instance.RestartGame);
        titleButton.onClick.AddListener(GameManager.Instance.ReturnToTitle);
        GameManager.Instance.StateChanged += HandleStateChanged;
        HandleStateChanged(GameManager.Instance.State);
    }

    private void OnDestroy()
    {
        if (GameManager.Current != null)
        {
            GameManager.Current.StateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        bool isResult = state is GameState.Won or GameState.Lost;
        objectiveText.gameObject.SetActive(!isResult && state != GameState.Title);
        escapeGuideText.gameObject.SetActive(state == GameState.EscapeReady);
        resultPanel.SetActive(isResult);

        switch (state)
        {
            case GameState.Playing:
                objectiveText.text = "목표: 보스를 처치하세요";
                break;

            case GameState.EscapeReady:
                objectiveText.text = "목표: 탈출 지점으로 이동하세요";
                escapeGuideText.text = "보스 처치 완료! 초록색 탈출 지점으로 이동하세요.";
                break;

            case GameState.Won:
                resultTitle.text = "탈출 성공!";
                resultDescription.text = "보스를 처치하고 안전하게 탈출했습니다.";
                break;

            case GameState.Lost:
                resultTitle.text = "게임 오버";
                resultDescription.text = "플레이어가 좀비에게 쓰러졌습니다.";
                break;
        }
    }

    private void BuildUI()
    {
        mapleFont = Resources.Load<TMP_FontAsset>("Fonts/Maplestory Bold SDF");

        if (mapleFont == null)
        {
            Debug.LogWarning("Maplestory Bold SDF 폰트를 불러오지 못했습니다.", this);
        }

        RectTransform root = CreateRect("Game Flow UI", transform);
        Stretch(root);
        root.SetAsLastSibling();

        objectiveText = CreateText(
            "Objective Text",
            root,
            new Vector2(760f, 54f),
            new Vector2(0f, -36f),
            30f);
        SetTopCenter(objectiveText.rectTransform);

        escapeGuideText = CreateText(
            "Escape Guide Text",
            root,
            new Vector2(900f, 50f),
            new Vector2(0f, -90f),
            25f);
        escapeGuideText.color = new Color(0.35f, 1f, 0.4f);
        SetTopCenter(escapeGuideText.rectTransform);

        resultPanel = new GameObject("Result Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.SetParent(root, false);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(620f, 340f);
        resultPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        resultTitle = CreateText(
            "Result Title",
            panelRect,
            new Vector2(540f, 70f),
            new Vector2(0f, 90f),
            48f);

        resultDescription = CreateText(
            "Result Description",
            panelRect,
            new Vector2(540f, 60f),
            new Vector2(0f, 25f),
            24f);

        restartButton = CreateButton(
            "Restart Button",
            "다시 시작",
            panelRect,
            new Vector2(-115f, -95f));
        titleButton = CreateButton(
            "Title Button",
            "타이틀로",
            panelRect,
            new Vector2(115f, -95f));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject target = new GameObject(name, typeof(RectTransform));
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        float fontSize)
    {
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        if (mapleFont != null)
        {
            text.font = mapleFont;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(
        string name,
        string label,
        Transform parent,
        Vector2 position)
    {
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(190f, 58f);
        rect.anchoredPosition = position;

        Image image = target.GetComponent<Image>();
        image.color = new Color(0.15f, 0.55f, 0.25f, 1f);

        TextMeshProUGUI buttonText = CreateText(
            "Text",
            rect,
            rect.sizeDelta,
            Vector2.zero,
            24f);
        buttonText.text = label;

        return target.GetComponent<Button>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetTopCenter(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
    }
}
