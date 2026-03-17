using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuAutoSelect : MonoBehaviour
{
    [SerializeField] private Selectable firstSelected;

    private void OnEnable()
    {
        if (firstSelected != null && EventSystem.current != null) //
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }
}