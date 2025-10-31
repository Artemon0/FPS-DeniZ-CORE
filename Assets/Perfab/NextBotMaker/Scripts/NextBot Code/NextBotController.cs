using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using TMPro;

[RequireComponent(typeof(NavMeshAgent), typeof(AudioSource))]
public class NextBotController : MonoBehaviour
{
    public float speed = 3.5f;
    public float musicRange = 10f;
    public float catchRange = 2f;
    public float catchVerticalRange = 3f;
    public float timerNotKill = 3f;

    public AudioClip backgroundMusic;
    public AudioClip jumpscareSound;
    public Texture2D jumpscareImage;
    public float restartDelay = 1f;

    public float angularSpeed = 1000f; // Adjust this value for faster turning
    public float decelerationRate = 8f; // Increase this value to make the bot stop faster

    Transform player;
    NavMeshAgent agent;
    AudioSource audioSource;
    bool isPlayingMusic = false;
    bool hasTriggered = false;

    private double timer = 0.0;

    private static Canvas gameOverCanvas;
    private static TextMeshProUGUI gameOverText;
    private static Canvas jumpscareCanvas;
    private static Image jumpscareImageUI;

    private float nextPathUpdateTime;
    private const float PATH_UPDATE_RATE = 0.2f; // Обновлять путь каждые 0.2 секунды

    private Vector3 lastPlayerPosition;
    private const float UPDATE_THRESHOLD = 0.1f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        agent.speed = speed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = decelerationRate;
        agent.updateRotation = true;
        lastPlayerPosition = Vector3.zero;

        // Создаём UI только один раз для всех NextBot'ов
        if (gameOverCanvas == null)
        {
            CreateGameOverUI();
        }
        if (jumpscareCanvas == null)
        {
            CreateJumpscareUI();
        }
    }

    void CreateJumpscareUI()
    {
        // Используем пул объектов или проверяем существование
        if (jumpscareCanvas != null) return;

        GameObject canvasGO = new GameObject("JumpscareCanvas");
        jumpscareCanvas = canvasGO.AddComponent<Canvas>();
        jumpscareCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imageGO = new GameObject("JumpscareImage");
        imageGO.transform.SetParent(jumpscareCanvas.transform, false);
        jumpscareImageUI = imageGO.AddComponent<Image>();

        RectTransform rect = jumpscareImageUI.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Создаём спрайт один раз при инициализации
        if (jumpscareImage != null)
        {
            jumpscareImageUI.sprite = Sprite.Create(jumpscareImage,
                new Rect(0, 0, jumpscareImage.width, jumpscareImage.height),
                new Vector2(0.5f, 0.5f));
        }

        jumpscareCanvas.gameObject.SetActive(false);
    }

    void CreateGameOverUI()
    {
        GameObject canvasGO = new GameObject("GameOverCanvas");
        gameOverCanvas = canvasGO.AddComponent<Canvas>();
        gameOverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("GameOverText");
        textGO.transform.SetParent(gameOverCanvas.transform, false);

        gameOverText = textGO.AddComponent<TextMeshProUGUI>();
        gameOverText.fontSize = 90;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.red;

        RectTransform rt = gameOverText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 200);
        rt.anchoredPosition = Vector2.zero;

        gameOverCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < timerNotKill) hasTriggered = false;

        if (!player || hasTriggered) return;

        Vector3 currentPlayerPos = player.position;
        float distance = Vector3.Distance(transform.position, currentPlayerPos);

        // Обновляем путь только если игрок значительно переместился
        if (Time.time >= nextPathUpdateTime && 
            Vector3.Distance(currentPlayerPos, lastPlayerPosition) > UPDATE_THRESHOLD)
        {
            agent.SetDestination(currentPlayerPos);
            nextPathUpdateTime = Time.time + PATH_UPDATE_RATE;
            lastPlayerPosition = currentPlayerPos;
        }

        // Оптимизация проверки расстояния для музыки
        if (backgroundMusic)
        {
            if (distance <= musicRange && !isPlayingMusic)
            {
                audioSource.clip = backgroundMusic;
                audioSource.loop = true;
                audioSource.Play();
                isPlayingMusic = true;
            }
            else if (isPlayingMusic && distance > musicRange)
            {
                audioSource.Stop();
                isPlayingMusic = false;
            }
        }

        // Catch Player
        if (distance <= catchRange && timer > timerNotKill)
        {
            TriggerCatch();
        }
    }

    void TriggerCatch()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        // Останавливаем только текущий источник звука вместо поиска всех
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Воспроизводим звук джампскера
        if (jumpscareSound)
        {
            audioSource.clip = jumpscareSound;
            audioSource.loop = false;
            audioSource.Play();
        }

        // Показываем джампскер
        if (jumpscareImage && jumpscareCanvas != null)
        {
            ShowJumpscareImage();
        }

        Invoke(nameof(Timer), restartDelay);
    }

    void ShowJumpscareImage()
    {
        if (jumpscareCanvas != null)
        {
            jumpscareCanvas.gameObject.SetActive(true);
        }
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [SerializeField] float timeToShow = 3f;
    void Timer()
    {
        timer -= 3f + restartDelay;

        if (gameOverText != null)
        {
            gameOverText.text = "You survived " + (int)timer + " seconds!";
            gameOverCanvas.gameObject.SetActive(true);
        }

        Invoke(nameof(RestartLevel), timeToShow);
    }
}