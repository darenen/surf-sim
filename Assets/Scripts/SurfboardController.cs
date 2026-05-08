using UnityEngine;
using UnityEngine.InputSystem;

public class SurfboardController : MonoBehaviour
{
    public Rigidbody rb;

    [Header("Forces")]
    public float forwardForce = 50f;
    public float pitchForce = 10f;
    public float rollForce = 3f;

    [Header("Stability")]
    public float angularDamp = 0.85f; // 0=no damping, 1=instant stop. Try 0.8-0.95

    void FixedUpdate()
    {
        float drive = 0f;
        float pitch = 0f;
        float roll = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.isPressed) drive = 1f;
            if (Keyboard.current.wKey.isPressed) pitch = 1f;
            if (Keyboard.current.sKey.isPressed) pitch = -1f;
            if (Keyboard.current.aKey.isPressed) roll = -1f;
            if (Keyboard.current.dKey.isPressed) roll = 1f;
        }

        // --- Forward Force ---
        Vector3 flatForward = transform.right;
        flatForward.y = 0f;
        rb.AddForce(flatForward.normalized * drive * forwardForce);

        // --- Pitch & Roll Torque ---
        rb.AddTorque(transform.forward * pitch * -pitchForce);
        rb.AddTorque(transform.right * roll * -rollForce);

        // --- Angular Damping ---
        // Bleeds off spinning each frame so torque can't accumulate endlessly.
        // This is the key fix — Unity's built-in angularDrag is linear,
        // this multiplicative version is much more responsive and controllable.
        rb.angularVelocity *= angularDamp;
    }
}