using UnityEngine;

public class WaterPhysics : MonoBehaviour
{
    public Rigidbody rb;
    public Transform[] floatPoints;

    [Header("Buoyancy")]
    public float buoyancyStrength = 15f;
    public float floatHeight = 1.65f;

    [Header("Damping")]
    [Range(0f, 1f)] public float verticalDamp = 0.2f;

    [Header("Fin")]
    [Range(0f, 1f)] public float finGrip = 0.4f;
    public float finGripDeadzone = 0.01f;

    [Header("Drag")]
    public float linearDrag = 0.8f;      // Slows forward momentum naturally
    public float angularDragPitch = 6f;  // Stiff — board resists pitching
    public float angularDragRoll = 4f;   // Medium — allows banking but snaps back
    public float angularDragYaw = 0.5f;  // Loose — board turns freely

    [Header("Raycast")]
    public LayerMask waterLayer;

    // Public so AI can read the board's current state
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

            // 1. Buoyancy
            Vector3 buoyancy = Vector3.up * (buoyancyStrength * d);

            // 2. Vertical damping (velocity only, no depth scaling)
            float perfectVertStop = (pointVel.y * rb.mass) / Time.fixedDeltaTime / floatPoints.Length;
            Vector3 vertDamp = Vector3.up * (-perfectVertStop * verticalDamp);

            // 3. Fin lateral grip (scales with depth — no grip near surface)
            Vector3 lateralForce = Vector3.zero;
            Vector3 localVel = transform.InverseTransformDirection(pointVel);
            float slip = localVel.x;

            if (Mathf.Abs(slip) > finGripDeadzone)
            {
                float perfectLateralStop = (slip * rb.mass) / Time.fixedDeltaTime / floatPoints.Length;
                lateralForce = transform.right * (-perfectLateralStop * finGrip * Mathf.Clamp01(d));
            }

            rb.AddForceAtPosition(buoyancy + vertDamp + lateralForce, point.position);
        }

        // 4. Per-axis angular drag — applied in LOCAL space so it's always
        //    relative to the board's own pitch/roll/yaw axes, not world axes.
        //    This is what makes the board feel like a surfboard and not a box.
        if (pointsInWater > 0)
        {
            Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);
            localAngVel.x -= localAngVel.x * angularDragRoll  * Time.fixedDeltaTime;
            localAngVel.y -= localAngVel.y * angularDragYaw   * Time.fixedDeltaTime;
            localAngVel.z -= localAngVel.z * angularDragPitch * Time.fixedDeltaTime;
            rb.angularVelocity = transform.TransformDirection(localAngVel);

            // Linear drag — only horizontal, don't fight buoyancy vertically
            Vector3 vel = rb.linearVelocity;
            vel.x -= vel.x * linearDrag * Time.fixedDeltaTime;
            vel.z -= vel.z * linearDrag * Time.fixedDeltaTime;
            rb.linearVelocity = vel;
        }

        // 5. Expose state for AI to read
        Vector3 localBoardVel = transform.InverseTransformDirection(rb.linearVelocity);
        currentSpeed  = localBoardVel.x;   // Positive = moving forward (nose axis)
        sidewaysSlip  = localBoardVel.z;   // Positive = sliding sideways
    }
}