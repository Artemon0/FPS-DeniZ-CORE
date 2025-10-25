using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor.SceneManagement;

public class NextBotMaker : EditorWindow
{
    Texture2D botTexture;
    float botSpeed = 3.5f;

    AudioClip chaseMusic;
    AudioClip jumpscareSound;
    float musicRange = 10f;
    float catchRange = 2f;

    Texture2D jumpscareImage;
    float restartDelay = 1f;

    float angularSpeed = 1000f;
    float decelerationRate = 8f;

    Vector3 botSize = new Vector3(3f, 3f, 0.01f);

    [MenuItem("Window/NextBotMaker")]
    public static void ShowWindow()
    {
        GetWindow<NextBotMaker>("NextBot Maker");
    }

    void OnGUI()
    {
        GUILayout.Label("Create Your NextBot", EditorStyles.boldLabel);

        botTexture = (Texture2D)EditorGUILayout.ObjectField("Bot Image", botTexture, typeof(Texture2D), false);
        botSpeed = EditorGUILayout.FloatField("Speed", botSpeed);

        GUILayout.Space(10);
        GUILayout.Label("Sound Settings (Optional)", EditorStyles.boldLabel);
        chaseMusic = (AudioClip)EditorGUILayout.ObjectField("Chase Music", chaseMusic, typeof(AudioClip), false);
        musicRange = EditorGUILayout.FloatField("Music Range", musicRange);
        jumpscareSound = (AudioClip)EditorGUILayout.ObjectField("Jumpscare Sound", jumpscareSound, typeof(AudioClip), false);

        GUILayout.Space(10);
        GUILayout.Label("Jumpscare Settings (Optional)", EditorStyles.boldLabel);
        jumpscareImage = (Texture2D)EditorGUILayout.ObjectField("Jumpscare Image", jumpscareImage, typeof(Texture2D), false);
        catchRange = EditorGUILayout.FloatField("Catch Range", catchRange);
        restartDelay = EditorGUILayout.FloatField("Restart Delay", restartDelay);

        GUILayout.Space(10);
        GUILayout.Label("Movement Settings", EditorStyles.boldLabel);
        angularSpeed = EditorGUILayout.FloatField("Angular Speed", angularSpeed);
        decelerationRate = EditorGUILayout.FloatField("Deceleration Rate", decelerationRate);

        GUILayout.Space(10);
        GUILayout.Label("Size Settings (Recommended: 3,3,0.01)", EditorStyles.boldLabel);
        botSize = EditorGUILayout.Vector3Field("Size (X, Y, Z)", botSize);

        GUILayout.Space(10);
        if (GUILayout.Button("Make NextBot"))
        {
            MakeNextBot();
        }
    }

    void MakeNextBot()
    {
        GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bot.name = "NextBot";
        bot.transform.position = Vector3.zero;
        bot.transform.localScale = botSize;

        if (botTexture)
        {
            Material mat = new Material(Shader.Find("Unlit/Texture"));
            mat.mainTexture = botTexture;
            bot.GetComponent<Renderer>().material = mat;
        }

        Rigidbody rb = bot.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        NavMeshAgent agent = bot.AddComponent<NavMeshAgent>();
        agent.speed = botSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = decelerationRate;

        Collider col = bot.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        bot.AddComponent<BoxCollider>();

        bot.AddComponent<AudioSource>();

        NextBotController controller = bot.AddComponent<NextBotController>();
        controller.speed = botSpeed;
        controller.musicRange = musicRange;
        controller.catchRange = catchRange;
        controller.backgroundMusic = chaseMusic;
        controller.jumpscareSound = jumpscareSound;
        controller.jumpscareImage = jumpscareImage;
        controller.restartDelay = restartDelay;
        controller.angularSpeed = angularSpeed;
        controller.decelerationRate = decelerationRate;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
