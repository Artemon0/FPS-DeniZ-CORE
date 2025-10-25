using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

[RequireComponent(typeof(NavMeshAgent), typeof(AudioSource))]
public class NextBotController : MonoBehaviour
{
    public float speed = 3.5f;
    public float musicRange = 10f;
    public float catchRange = 2f;
    public float catchVerticalRange = 3f;

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

    private double timer = 0;
    

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        agent.speed = speed;

        // Set the angular speed for fast turns
        agent.angularSpeed = angularSpeed;

        // Set a higher deceleration rate to stop faster
        agent.acceleration = decelerationRate;

        // Enable rotation update
        agent.updateRotation = true;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (!player || hasTriggered) return;

        // Immediately update the destination to the player's position
        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);

        if (backgroundMusic && distance <= musicRange && !isPlayingMusic)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
            isPlayingMusic = true;
        }

        if (isPlayingMusic && distance > musicRange)
        {
            audioSource.Stop();
            isPlayingMusic = false;
        }

        // Catch Player
        if (distance <= catchRange && Mathf.Abs(GetTransformPosY() - GetPlayerPosition().y) <= catchVerticalRange)
        {
            TriggerCatch();
        }
    }

    void TriggerCatch()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        // Stop all AudioSources in the scene
        foreach (AudioSource source in FindObjectsOfType<AudioSource>())
        {
            source.Stop();
        }

        // Play the jumpscare sound with a temporary AudioSource
        if (jumpscareSound)
        {
            GameObject tempAudio = new GameObject("JumpscareAudio");
            AudioSource jumpscareSource = tempAudio.AddComponent<AudioSource>();
            jumpscareSource.clip = jumpscareSound;
            jumpscareSource.Play();
            Destroy(tempAudio, jumpscareSound.length);
        }

        // Show jumpscare image
        if (jumpscareImage)
        {
            GameObject canvasGO = new GameObject("JumpscareCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject imageGO = new GameObject("JumpscareImage");
            imageGO.transform.SetParent(canvasGO.transform, false);
            Image img = imageGO.AddComponent<Image>();
            RectTransform rect = imageGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Sprite sprite = Sprite.Create(jumpscareImage, new Rect(0, 0, jumpscareImage.width, jumpscareImage.height),
                new Vector2(0.5f, 0.5f));
            img.sprite = sprite;
        }

        Invoke(nameof(RestartLevel), restartDelay);
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private Transform GetPlayerTransform()
    {
        return player.transform;
    }
    private float GetTransformPosY()
    {
        return transform.position.y;
    }
    
    private Vector3 GetPlayerPosition()
    {
        return player.position;
    }
    
}