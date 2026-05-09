using UnityEngine;

public class WaveRideFeasibilityTester : MonoBehaviour
{
    public Rigidbody rb;

    [Header("Drive")]
    public float forwardForce = 80f;
    public float targetSpeed = 6f;

    [Header("Balance")]
    public float uprightTorque = 10f;
    public float angularDamping = 2f;

    [Header("Debug")]
    public float speedWithWave;
    public float uprightDot;

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 nose = transform.right;
        nose.y = 0f;

        if (nose.sqrMagnitude > 0.001f)
            nose.Normalize();

        speedWithWave = Vector3.Dot(rb.linearVelocity, Vector3.right);

        if (speedWithWave < targetSpeed)
        {
            rb.AddForce(nose * forwardForce, ForceMode.Force);
        }

        uprightDot = Vector3.Dot(transform.up, Vector3.up);

        Vector3 correctionAxis = Vector3.Cross(transform.up, Vector3.up);
        rb.AddTorque(correctionAxis * uprightTorque, ForceMode.Acceleration);
        rb.AddTorque(-rb.angularVelocity * angularDamping, ForceMode.Acceleration);
    }
}
