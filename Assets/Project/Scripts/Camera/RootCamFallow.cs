using UnityEngine;

public class RootCamFallow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Pixel Perfect")]
    [SerializeField] private float pixelsPerUnit = 16f;

    private void Awake()
{
    if (target == null)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }
    
    // BAŞLANGIÇ POZİSYONUNU SNAP'LE
    Vector3 startPos = transform.position;
    startPos.x = Mathf.Round(startPos.x * pixelsPerUnit) / pixelsPerUnit;
    startPos.y = Mathf.Round(startPos.y * pixelsPerUnit) / pixelsPerUnit;
    transform.position = startPos;
    
    Debug.Log($"[CAMERA] Start position snapped to: {startPos}");
}

    private void LateUpdate()
    {
        if (target == null) return;

        // Target pozisyonunu ÖNCE snap'le
        Vector3 targetPos = target.position;
        float snappedTargetX = Mathf.Round(targetPos.x * pixelsPerUnit) / pixelsPerUnit;
        float snappedTargetY = Mathf.Round(targetPos.y * pixelsPerUnit) / pixelsPerUnit;
        
        Vector3 snappedTarget = new Vector3(snappedTargetX, snappedTargetY, targetPos.z);
        
        // Smooth follow snap'lenmiş hedef ile
        Vector3 desiredPosition = snappedTarget + offset;
        Vector3 smoothPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        // Final pozisyonu da snap'le
        float finalX = Mathf.Round(smoothPosition.x * pixelsPerUnit) / pixelsPerUnit;
        float finalY = Mathf.Round(smoothPosition.y * pixelsPerUnit) / pixelsPerUnit;
        
        transform.position = new Vector3(finalX, finalY, offset.z);
    }
}