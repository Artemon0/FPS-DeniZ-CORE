using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

public class RuntimeMenuCreatorScene : MonoBehaviour
{
    [Header("Behaviour")]
    [Tooltip("Если задан — будет активирован вместо загрузки сцены.")]
    public GameObject gameRoot;

    [Tooltip("Имя сцены для загрузки. Можно заполнить вручную или через SceneAsset в редакторе.")]
    public string sceneName = "";

    [Header("UI")]
    public Transform uiParent;
    public Font uiFont;
    public Vector2 buttonSize = new Vector2(400, 100);
    public int spacing = 20;

    GameObject runtimeCanvasGO;
    GameObject panelGO;

    void Start()
    {
        EnsureEventSystemSafe();
        CreateMenu();
    }

    void CreateMenu()
    {
        // создаём Canvas, если не задан uiParent
        if (uiParent == null)
        {
            runtimeCanvasGO = new GameObject("RuntimeMenuCanvas");
            var canvas = runtimeCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = runtimeCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            runtimeCanvasGO.AddComponent<GraphicRaycaster>();
            uiParent = runtimeCanvasGO.transform;

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(uiParent, false);
            var bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
        }

        // Panel
        panelGO = new GameObject("MenuPanel");
        panelGO.transform.SetParent(uiParent, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(buttonSize.x + 80, buttonSize.y * 2 + spacing + 80);
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);

        // Play
        var playBtn = CreateButton("PlayButton", "Play", panelGO.transform, uiFont, buttonSize);
        playBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, spacing / 2f + buttonSize.y / 2f);
        playBtn.onClick.AddListener(OnPlayPressed);

        // Exit
        var exitBtn = CreateButton("ExitButton", "Exit", panelGO.transform, uiFont, buttonSize);
        exitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -spacing / 2f - buttonSize.y / 2f);
        exitBtn.onClick.AddListener(OnExitPressed);
    }

    void OnPlayPressed()
    {
        if (gameRoot != null)
        {
            gameRoot.SetActive(true);
            if (runtimeCanvasGO != null) runtimeCanvasGO.SetActive(false);
            else panelGO.SetActive(false);
            return;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Debug.LogWarning("RuntimeMenuCreatorScene: Не указан gameRoot или sceneName.");
    }

    void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    Button CreateButton(string name, string label, Transform parent, Font font, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        var defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (defaultSprite != null) img.sprite = defaultSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = colors;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 28;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.black;
        txt.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");

        return btn;
    }

    void EnsureEventSystemSafe()
    {
        var existing = FindObjectOfType<EventSystem>();
        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType == null)
            inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");

        if (existing == null)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(null);
            go.AddComponent<EventSystem>();
            if (inputSystemModuleType != null)
                go.AddComponent(inputSystemModuleType);
            else
                go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            return;
        }

        if (inputSystemModuleType != null)
        {
            var inputModule = existing.GetComponent(inputSystemModuleType);
            if (inputModule == null)
            {
                var sit = existing.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (sit != null) DestroyImmediate(sit);
                existing.gameObject.AddComponent(inputSystemModuleType);
            }
        }
        else
        {
            if (existing.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
                existing.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}