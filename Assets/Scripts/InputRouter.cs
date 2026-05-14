using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Singleton de input centralizado. Reemplaza los InputActions individuales
/// de cada ObjectDisplayController — UN solo raycast por tap en lugar de 24.
/// Colocar en un GameObject persistente en la escena (ej. GameManager).
/// </summary>
public class InputRouter : MonoBehaviour
{
    public static InputRouter Instance { get; private set; }

    private InputAction touchPressAction;
    private InputAction touchPositionAction;
    private InputAction mousePressAction;
    private InputAction mousePositionAction;

    private Camera mainCamera;
    private InventarioToggleController inventarioToggle;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCamera = Camera.main;

        touchPressAction    = new InputAction("TouchPress",    InputActionType.Button, "<Touchscreen>/primaryTouch/press");
        touchPositionAction = new InputAction("TouchPosition", InputActionType.Value,  "<Touchscreen>/primaryTouch/position");
        mousePressAction    = new InputAction("MousePress",    InputActionType.Button, "<Mouse>/leftButton");
        mousePositionAction = new InputAction("MousePosition", InputActionType.Value,  "<Mouse>/position");

        touchPressAction.Enable();
        touchPositionAction.Enable();
        mousePressAction.Enable();
        mousePositionAction.Enable();

        touchPressAction.performed += OnTouchPress;
        mousePressAction.performed += OnMousePress;
    }

    void Start()
    {
        inventarioToggle = FindObjectOfType<InventarioToggleController>();

        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();
    }

    private bool EstaInventarioAbierto()
    {
        if (inventarioToggle == null)
            inventarioToggle = FindObjectOfType<InventarioToggleController>();
        return inventarioToggle != null && inventarioToggle.EstaPanelVisible();
    }

    private void OnTouchPress(InputAction.CallbackContext ctx)
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        int touchId = touchscreen.primaryTouch.touchId.ReadValue();
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
            return;

        DetectarYRutar(touchPositionAction.ReadValue<Vector2>());
    }

    private void OnMousePress(InputAction.CallbackContext ctx)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        DetectarYRutar(mousePositionAction.ReadValue<Vector2>());
    }

    private void DetectarYRutar(Vector2 posicionPantalla)
    {
        if (EstaInventarioAbierto()) return;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(posicionPantalla);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            hit.collider.GetComponent<ObjectDisplayController>()?.OnHit();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // Guard: si este era el duplicado destruido en Awake, los campos son null
        if (touchPressAction == null) return;

        touchPressAction.performed -= OnTouchPress;
        mousePressAction.performed -= OnMousePress;

        touchPressAction.Disable();
        touchPositionAction.Disable();
        mousePressAction.Disable();
        mousePositionAction.Disable();

        touchPressAction.Dispose();
        touchPositionAction.Dispose();
        mousePressAction.Dispose();
        mousePositionAction.Dispose();
    }
}
