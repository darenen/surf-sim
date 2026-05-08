using UnityEngine;
using UnityEngine.InputSystem; // This tells the code to use the New Input System!

public class SurfboardController : MonoBehaviour
{
    public Rigidbody rb;
    public float forwardForce = 50f;
    public float turnForce = 50f;

    void FixedUpdate()
    {
        float moveForward = 0f;
        float turn = 0f;

        // Make sure a keyboard is actually plugged in/detected
        if (Keyboard.current != null)
        {
            // Forward and Backward (W/S or Up/Down)
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveForward = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveForward = -1f;

            // Turning Left and Right (A/D or Left/Right)
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) turn = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) turn = 1f;
        }

        // Push the board
        rb.AddForce(transform.forward * moveForward * forwardForce);

        // Twist the board
        rb.AddTorque(transform.up * turn * turnForce);
    }
}