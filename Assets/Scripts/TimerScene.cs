using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TimerSceneBridge : MonoBehaviour
{
    [Header("Scenes")] [Tooltip("Имя сцены, из которой стартуем (может быть та же сцена).")]
    public string fromSceneName;

    [Tooltip("Имя сцены, которую нужно загрузить и в которой показать timer.")]
    public string toSceneName;

    [Header("NextBot")] [Tooltip("Если пусто — создастся новый NextBotController при старте.")]
    public NextBotController nextBotPrefab;

    [Header("UI")] [Tooltip("Шрифт для отображения текста, можно оставить пустым — будет использован Arial.")]
    public Font uiFont;

    [Tooltip("Формат вывода (например F2).")]
    public string format = "F2";

    // Ведущий экземпляр NextBotController, который будет помечен DontDestroyOnLoad
    private NextBotController persistentBot;

    // Ссылка на созданный UI Text в загруженной сцене
    private Text timerTextInstance;

    void Start()
    {
        // Если у нас уже есть объект-носитель (например, при повторном запуске), используем его
        persistentBot = FindObjectOfType<NextBotController>();
        if (persistentBot == null)
        {
            // Создать игровой объект с NextBotController
            GameObject botGO;
            if (nextBotPrefab != null)
            {
                // Инстансить префаб (если задан)
                botGO = Instantiate(nextBotPrefab.gameObject);
                persistentBot = botGO.GetComponent<NextBotController>();
            }
            else
            {
                // Создать "с нуля"
                botGO = new GameObject("NextBot_Persistent");
                persistentBot = botGO.AddComponent<NextBotController>();
            }

            // Защита от случайного удаления при смене сцен
            DontDestroyOnLoad(persistentBot.gameObject);
        }

        // Подписываемся на событие загрузки сцены, чтобы создать UI там
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Отписка для безопасности
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Вызовите этот метод (например, из UI кнопки или из кода), чтобы загрузить сцену toSceneName
    public void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(toSceneName))
        {
            Debug.LogError("TimerSceneBridge: toSceneName не задано.");
            return;
        }

        // Асинхронная загрузка сцены (замена текущей)
        SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Single);
    }

    // Сработает после загрузки сцены — создаём Canvas + Text и привязываем к persistentBot.timer
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != toSceneName) return;

        // Найти существующий Canvas в сцене, иначе создать новый
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("TimerCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Создать текст для отображения timer
        GameObject textGO = new GameObject("TimerText");
        textGO.transform.SetParent(canvas.transform, false);
        timerTextInstance = textGO.AddComponent<Text>();
        timerTextInstance.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        timerTextInstance.fontSize = 36;
        timerTextInstance.alignment = TextAnchor.UpperLeft;
        timerTextInstance.color = Color.white;

        RectTransform rt = timerTextInstance.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(10f, -10f);
        rt.sizeDelta = new Vector2(400f, 100f);

        // Создать вспомогательный MonoBehaviour для обновления UI каждый кадр в новой сцене
        TimerUIUpdater updater = textGO.AddComponent<TimerUIUpdater>();
        updater.Setup(persistentBot, timerTextInstance, format);
    }

    // Вспомогательный класс-апдейтер для обновления текстового поля значением timer.
    private class TimerUIUpdater : MonoBehaviour
    {
        private NextBotController bot;
        private Text txt;
        private string fmt;

        public void Setup(NextBotController bot, Text txt, string format)
        {
            this.bot = bot;
            this.txt = txt;
            this.fmt = format;
        }

        void Update()
        {
            if (bot == null || txt == null) return;
            txt.text = bot.timer.ToString(fmt);
        }
    }
}