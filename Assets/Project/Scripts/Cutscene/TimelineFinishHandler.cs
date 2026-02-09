using UnityEngine;
using UnityEngine.Playables;

public class TimelineFinishHandler : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private CutsceneChief cutsceneChief;

    private void OnEnable()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped += OnTimelineStopped;
        }
    }

    private void OnDisable()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (cutsceneChief != null)
        {
            cutsceneChief.AdvanceState();
        }
    }
}