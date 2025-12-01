using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // Propiedades públicas de solo lectura para inputs
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool DownPressed { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool CrouchReleased { get; private set; }
    public bool CrouchHeld { get; private set; }

    // --- NUEVOS INPUTS DE COMBATE ---
    public bool PrimaryFirePressed { get; private set; } // Click Izquierdo (Hold para automático)
    public bool QuickMeleePressed { get; private set; }  // Tecla E
    public float WeaponScroll { get; private set; }      // Rueda del ratón
    public bool DashPressed { get; private set; } // Left Shift

    void Update()
    {
        // Capturar inputs de movimiento
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(horizontal, vertical).normalized;

        // Capturar inputs de salto
        JumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        JumpReleased = Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W);
        JumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);

        // Capturar input de descenso rápido
        DownPressed = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        // Capturar inputs de agacharse
        CrouchPressed = Input.GetKeyDown(KeyCode.LeftControl);
        CrouchReleased = Input.GetKeyUp(KeyCode.LeftControl);
        CrouchHeld = Input.GetKey(KeyCode.LeftControl);

        // --- NUEVA LÓGICA DE ATAQUE ---
        // Click izquierdo (GetMouseButton permite dejarlo presionado para armas automáticas)
        PrimaryFirePressed = Input.GetMouseButton(0);

        // Melee rápido con E
        QuickMeleePressed = Input.GetKeyDown(KeyCode.E);

        // Rueda del ratón
        WeaponScroll = Input.GetAxis("Mouse ScrollWheel");

        // Dash con Left Shift
        DashPressed = Input.GetKeyDown(KeyCode.LeftShift);
    }

    public void ClearInputs()
    {
        MoveInput = Vector2.zero;
        JumpPressed = false;
        JumpReleased = false;
        JumpHeld = false;
        DownPressed = false;
        CrouchPressed = false;
        PrimaryFirePressed = false;
        QuickMeleePressed = false;
        WeaponScroll = 0f;
        DashPressed = false;
    }
}