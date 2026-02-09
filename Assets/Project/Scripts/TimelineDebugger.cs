using UnityEngine;
using UnityEngine.Playables;

public class TimelineDebugger : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private CutsceneChief cutsceneChief;

    private void OnEnable()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped += OnTimelineStopped;
            Debug.Log("🎬 [TimelineDebugger] Timeline listener registered!");
        }
        else
        {
            Debug.LogError("❌ [TimelineDebugger] PlayableDirector is NULL!");
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
        Debug.Log("🎬 [TimelineDebugger] TIMELINE STOPPED!");
        Debug.Log($"Current CutsceneChief state: {cutsceneChief != null}");
        
        if (cutsceneChief != null)
        {
            Debug.Log("✅ [TimelineDebugger] Calling AdvanceState...");
            cutsceneChief.AdvanceState();
        }
        else
        {
            Debug.LogError("❌ [TimelineDebugger] CutsceneChief is NULL!");
        }
    }
}