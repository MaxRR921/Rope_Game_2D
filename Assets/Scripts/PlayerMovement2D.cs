using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 5f;

    private Vector2 moveInput = Vector2.zero;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Sent via PlayerInput (Send Messages) when "Move" triggers
    public void OnMove(InputValue value)
    {
        // Read the Vector2 input (WASD, arrow keys or stick)
        moveInput = value.Get<Vector2>();
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }
}

/*
Setup Instructions:

1. Input System Package:
   - Ensure "Input System" is installed via Package Manager.

2. Input Actions Asset:
   - Create (or open) your Input Actions asset, e.g., "PlayerControls".
   - Add an Action Map named "Gameplay".
   - Add a new Action "Move", set Action Type to "Value" and Control Type to "Vector2".

3. Bindings for "Move":
   a. Add a "2D Vector Composite" binding:
      • Up: path "<Keyboard>/w"
      • Down: path "<Keyboard>/s"
      • Left: path "<Keyboard>/a"
      • Right: path "<Keyboard>/d"
   b. (Optional) Add arrow keys as separate bindings:
      • "<Keyboard>/upArrow", 
      • "<Keyboard>/downArrow",
      • "<Keyboard>/leftArrow",
      • "<Keyboard>/rightArrow"
   c. Add Gamepad stick binding: "<Gamepad>/leftStick"

4. Player GameObject Setup:
   - Attach a Rigidbody2D component.
   - Add a PlayerInput component:
       • Assign the "PlayerControls" asset.
       • Set Behavior to "Send Messages".
       • Set Default Map to "Gameplay".
   - Attach this PlayerMovement2D.cs script.

5. Tweak:
   - Adjust Move Speed in the Inspector.

6. Test:
   - Enter Play Mode; WASD/arrow keys or gamepad left stick should move the player.
*/

