using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortSprite : MonoBehaviour
{
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // Y aşağı indikçe öne geçsin
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }
}
