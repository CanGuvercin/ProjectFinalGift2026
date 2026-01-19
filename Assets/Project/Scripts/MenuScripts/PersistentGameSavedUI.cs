using UnityEngine;

public class PersistentGameSavedUI : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameSaved] Canvas marked as DontDestroyOnLoad");
    }
}