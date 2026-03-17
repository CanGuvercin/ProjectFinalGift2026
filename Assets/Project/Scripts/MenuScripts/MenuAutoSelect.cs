using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MenuAutoSelect : MonoBehaviour
{
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private Selectable fallbackSelected;

    private void OnEnable()
    {
        StartCoroutine(SelectDelayed());
    }
    
    private IEnumerator SelectDelayed()
    {
        // Bir frame bekle - butonların interactable durumu set edilsin
        yield return null;
        
        if (EventSystem.current == null) yield break;
        
        if (firstSelected != null && firstSelected.gameObject.activeInHierarchy && firstSelected.interactable)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }
        else if (fallbackSelected != null && fallbackSelected.gameObject.activeInHierarchy && fallbackSelected.interactable)
        {
            EventSystem.current.SetSelectedGameObject(fallbackSelected.gameObject);
        }
    }
}