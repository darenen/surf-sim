using UnityEngine;

public class WaterPhysics : MonoBehaviour
{
    public Rigidbody rb;
    public Transform[] floatPoints;

    [Header("Buoyancy")]
    public float buoyancyStrength = 15f;
    public float floatHeight = 1.65f;

    [Header("Damping")]
    public float verticalDamping = 5f;   // Simple drag coefficient, NOT a perfect stop
                                         // Raise this if board bounces, lower if it feels glued

    [Header("Fin")]
    [Range(0f, 1f)] public float finGrip = 0.4f;
    public float finGripDeadzone = 0.01f;

    [Header("Drag")]
    public float linearDrag = 0.8f;
    public float angularDragPitch = 3f;
    public float angularDragRoll = 2f;
    public float angularDragYaw = 0.5f;

    [Header("Safety")]
    public float maxVelocity = 15f;

    [Header("Raycast")]
    public LayerMask waterLayer;

    [HideInInspector] public float currentSpeed;
    [HideInInspector] public float sidewaysSlip;
    [HideInInspector] public int pointsInWater;

    void FixedUpdate()
    {
        pointsInWater = 0;

        foreach (Transform point in floatPoints)
        {
            if (!Physics.Raycast(point.position, Vector3.down, out RaycastHit hit, 100f, waterLayer))
                continue;

            float d = floatHeight - hit.distance;
            if (d <= 0f) continue;

            pointsInWater++;
            Vector3 pointVel = rb.GetPointVelocity(point.position);
            float clampedD = Mathf.Clamp01(d); // Never more than 1 — kills explosion at source

            // 1. Buoyancy — simple spring, force scales with depth
            Vector3 buoyancy = Vector3.up * (buoyancyStrength * clampedD);

            // 2. Vertical damping — F = -c*v style, NOT a perfect stop.
            //    This CANNOT explode because it only ever opposes velocity
            //    proportionally. No division by Time.fixedDeltaTime means
            //    no frame-rate dependent force spikes.
            Vector3 vertDamp = Vector3.up * (-pointVel.y * verticalDamping);

            // 3. Fin lateral grip — same safe pattern, proportional drag not perfect stop
            Vector3 lateralForce = Vector3.zero;
            Vector3 localVel = transform.InverseTransformDirection(pointVel);
            float slip = localVel.x;

            if (Mathf.Abs(slip) > finGripDeadzone)
            {
                lateralForce = transform.right * (-slip * finGrip * rb.mass * clampedD);
            }

            rb.AddForceAtPosition(buoyancy + vertDamp + lateralForce, point.position);
        }

        if (pointsInWater > 0)
        {
            // Per-axis angular drag in local space
            Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
            localAngVel.x -= localAngVel.x * angularDragRoll  * Time.fixedDeltaTime;
            localAngVel.y -= localAngVel.y * angularDragYaw   * Time.fixedDeltaTime;
            localAngVel.z -= localAngVel.z * angularDragPitch * Time.fixedDeltaTime;
            rb.angularVelocity = transform.TransformDirection(localAngVel);

            Vector3 vel = rb.linearVelocity;
            vel.x -= vel.x * linearDrag * Time.fixedDeltaTime;
            vel.z -= vel.z * linearDrag * Time.fixedDeltaTime;
            rb.linearVelocity = vel;
        }

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxVelocity);

        Vector3 localBoardVel = transform.InverseTransformDirection(rb.linearVelocity);
        currentSpeed = localBoardVel.x;
        sidewaysSlip = localBoardVel.z;
    }
}