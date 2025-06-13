using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 5f;

    [Header("Dash Settings")]
    [SerializeField]
    private float dashSpeed = 12f;
    [SerializeField]
    private float dashDuration = 0.2f;
    [SerializeField, Range(0f, 1f)]
    private float dashControlFactor = 0.5f;

    private Vector2 moveInput = Vector2.zero;
    private Vector2 dashDirection = Vector2.up;
    private Rigidbody2D rb;
    private bool isDashing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Called when "Move" action triggers.
    /// </summary>
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    /// <summary>
    /// Called when "Dash" action triggers.
    /// </summary>
    public void OnDash(InputValue value)
    {
        if (value.isPressed && !isDashing)
            StartCoroutine(PerformDash());
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        // Capture initial dash direction or default up
        dashDirection = moveInput.sqrMagnitude > 0f ? moveInput.normalized : Vector2.up;

        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        isDashing = false;
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            // Base dash + limited mid-dash control
            Vector2 controlVel = moveInput * moveSpeed * dashControlFactor;
            rb.velocity = dashDirection * dashSpeed + controlVel;
        }
        else
        {
            rb.velocity = moveInput * moveSpeed;
        }
    }
}

/*
Setup Instructions:
1. Input System Package:
   - Ensure "Input System" is installed via Package Manager.

2. Input Actions Asset:
   - Open "PlayerControls" Input Actions asset.
   - In your Action Map (e.g., "Movement"), ensure you have:
     • "Move" action (Value, Vector2) bound to WASD/arrow keys and left stick.
     • "Dash" action (Button) bound to <Keyboard>/space and <Gamepad>/buttonSouth (A).

3. PlayerInput Component:
   - On your Player GameObject:
     • Assign the "PlayerControls" asset.
     • Set Behavior to "Send Messages".
     • Default Map to match your Action Map name.
     • Verify "OnMove" and "OnDash" appear under the corresponding section.

4. Tweak in Inspector:
   - Move Speed: normal movement.
   - Dash Speed: dash velocity.
   - Dash Duration: how long the dash lasts.
   - Dash Control Factor: fraction of normal movement you retain mid-dash (0 = no control, 1 = full control).

5. Test:
   - Play and use WASD/arrow keys to move.
   - Press Space (or Gamepad A) to dash; while dashing you can slightly adjust direction based on input.
*/

