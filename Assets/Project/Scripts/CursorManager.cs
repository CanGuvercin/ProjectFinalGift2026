using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }
    
    [SerializeField] private float hideDelay = 5f;
    
    private Vector3 _lastMousePosition;
    private float _idleTimer;
    private bool _cursorVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        HideCursor();
        _lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        if (Input.mousePosition != _lastMousePosition)
        {
            _lastMousePosition = Input.mousePosition;
            _idleTimer = 0f;
            
            if (!_cursorVisible)
                ShowCursor();
        }
        else if (_cursorVisible)
        {
            _idleTimer += Time.unscaledDeltaTime;
            
            if (_idleTimer >= hideDelay)
                HideCursor();
        }
    }

    private void ShowCursor()
    {
        _cursorVisible = true;
        Cursor.visible = true;
    }

    private void HideCursor()
    {
        _cursorVisible = false;
        Cursor.visible = false;
    }
}