using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Animator))]
public class LoopShrekPlayable : MonoBehaviour
{
    [Header("Target")]
    public string targetName = "Shrek";
    public GameObject targetObject;

    [Header("Clip")]
    public AnimationClip clip;
    public float playbackSpeed = 1f;

    PlayableGraph graph;
    AnimationPlayableOutput playableOutput;
    AnimationClipPlayable clipPlayable;
    Animator targetAnimator;
    bool active = false;

    void Start()
    {
        if (targetObject == null)
        {
            var found = GameObject.Find(targetName);
            if (found == null)
            {
                Debug.LogError($"LoopShrekPlayable: Не найден объект '{targetName}' и targetObject не назначен.");
                enabled = false;
                return;
            }
            targetObject = found;
        }

        targetAnimator = targetObject.GetComponent<Animator>();
        if (targetAnimator == null)
        {
            Debug.LogError("LoopShrekPlayable: У целевого объекта нет Animator компонента. Добавь Animator.");
            enabled = false;
            return;
        }

        if (clip == null)
        {
            Debug.LogError("LoopShrekPlayable: AnimationClip не назначен в инспекторе.");
            enabled = false;
            return;
        }

        CreateAndPlay();
    }

    void CreateAndPlay()
    {
        graph = PlayableGraph.Create($"LoopShrekPlayable_{targetObject.name}");
        playableOutput = AnimationPlayableOutput.Create(graph, "Animation", targetAnimator);

        clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetApplyPlayableIK(false);
        clipPlayable.SetSpeed(playbackSpeed);

        playableOutput.SetSourcePlayable(clipPlayable);
        graph.Play();
        active = true;
    }

    void Update()
    {
        if (!active) return;

        // ручная петля: если время проигрывания >= длины клипа — сбрасываем на 0
        double time = clipPlayable.GetTime();
        double length = clip.length / Mathf.Max(0.0001f, playbackSpeed);
        if (time >= length)
        {
            clipPlayable.SetTime(0.0);
        }
    }

    void OnDisable()
    {
        StopAndDestroy();
    }

    void OnDestroy()
    {
        StopAndDestroy();
    }

    void StopAndDestroy()
    {
        if (!active) return;
        if (graph.IsValid())
        {
            graph.Stop();
            graph.Destroy();
        }
        active = false;
    }

    void OnGUI()
    {
        if (clip != null && targetObject != null)
            GUI.Label(new Rect(10, 10, 500, 20), $"Playing '{clip.name}' on {targetObject.name}");
    }
}