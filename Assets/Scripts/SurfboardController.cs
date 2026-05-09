using UnityEngine;
using UnityEngine.InputSystem;

// This controller is a TEMPORARY test harness only.
// It mimics how the AI will eventually control the board:
// by applying torque at specific points (weight shift),
// and forward force only along the flat nose direction.
// The AI will replace the keyboard inputs — nothing else changes.

public class SurfboardController : MonoBehaviour
{
    public Rigidbody rb;

    [Header("Propulsion")]
    public float forwardForce = 50f;

    [Header("Weight Shift — this is how the AI will steer")]
    public float pitchTorque = 4f;   // Nose up/down (W/S)
    public float rollTorque  = 3f;   // Rail-to-rail lean (A/D)

    void FixedUpdate()
    {
        float drive = 0f;
        float pitch = 0f;
        float roll  = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.isPressed) drive =  1f;
            if (Keyboard.current.wKey.isPressed)     pitch =  1f;
            if (Keyboard.current.sKey.isPressed)     pitch = -1f;
            if (Keyboard.current.aKey.isPressed)     roll  = -1f;
            if (Keyboard.current.dKey.isPressed)     roll  =  1f;
        }

        // Forward — flat nose direction only, never sends board skyward
        Vector3 flatNose = transform.right;
        flatNose.y = 0f;
        rb.AddForce(flatNose.normalized * drive * forwardForce);

        // Weight shift torque — applied in LOCAL space so it's always
        // relative to the board no matter how it's oriented.
        // The AI will output these same two values (-1 to 1) to steer.
        rb.AddRelativeTorque(Vector3.forward * pitch * -pitchTorque);
        rb.AddRelativeTorque(Vector3.right   * roll  * -rollTorque);
    }
}