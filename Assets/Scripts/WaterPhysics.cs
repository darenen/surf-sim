using UnityEngine;

public class WaterPhysics : MonoBehaviour
{
    public Rigidbody rb;
    public Transform[] floatPoints;

    [Header("Buoyancy")]
    public float buoyancyStrength = 15f;
    public float floatHeight = 1.65f;
    [Range(0f, 1f)] public float normalLiftInfluence = 0.25f;

    [Header("Damping")]
    public float verticalDamping = 5f;

    [Header("Fin Grip")]
    public float finGrip = 1f;
    public float finGripDeadzone = 0.01f;

    [Header("Board Drag")]
    public float forwardDrag = 10f;
    public float sidewaysDrag = 5f;
    public float verticalWaterDrag = 0.3f;

    [Header("Rail Bite / Carving")]
    public float railBiteStrength = 0.8f;
    public float railCarvePivotOffset = 1.2f;
    public float speedForFullRailBite = 8f;
    public bool invertRailCarve = false;

    [Header("Angular Drag")]
    public float angularDragRoll = 2f;
    public float angularDragYaw = 0.8f;
    public float angularDragPitch = 3f;

    [Header("Safety")]
    public float maxVelocity = 15f;
    public float maxAngularVelocity = 12f;

    [Header("Raycast")]
    public LayerMask waterLayer;
    public float raycastStartHeight = 5f;
    public float raycastDistance = 20f;

    [HideInInspector] public float currentSpeed;
    [HideInInspector] public float sidewaysSlip;
    [HideInInspector] public int pointsInWater;
    [HideInInspector] public float railAmount;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb == null || floatPoints == null || floatPoints.Length == 0)
            return;

        pointsInWater = 0;

        float totalSubmergence = 0f;
        Vector3 normalSum = Vector3.zero;

        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;

            ApplyFloatPoint(point, ref totalSubmergence, ref normalSum);
        }

        float immersion = pointsInWater > 0 ? totalSubmergence / pointsInWater : 0f;

        if (pointsInWater > 0)
        {
            ApplyLocalDrag(immersion);
            ApplyAngularDrag(immersion);
            ApplyRailBite(immersion);
        }

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxVelocity);
        rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxAngularVelocity);

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        currentSpeed = localVel.x;
        sidewaysSlip = localVel.z;
    }

    void ApplyFloatPoint(Transform point, ref float totalSubmergence, ref Vector3 normalSum)
    {
        Vector3 rayOrigin = point.position + Vector3.up * raycastStartHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, waterLayer, QueryTriggerInteraction.Collide))
            return;

        float heightError = hit.point.y + floatHeight - point.position.y;

        if (heightError <= 0f)
            return;


        float submergence = Mathf.Clamp01(heightError / Mathf.Max(0.001f, floatHeight));

        pointsInWater++;
        totalSubmergence += submergence;

        Vector3 waterNormal = hit.normal;
        if (waterNormal.y < 0.1f)
            waterNormal = Vector3.up;

        waterNormal.Normalize();
        normalSum += waterNormal;

        Vector3 pointVel = rb.GetPointVelocity(point.position);


        Vector3 flatWaveDir = Vector3.right;
        float speedWithWave = Vector3.Dot(rb.linearVelocity, flatWaveDir);
        float catchFactor = Mathf.Clamp01(speedWithWave / 5f);

        float effectiveNormalLift = normalLiftInfluence * catchFactor;

        Vector3 liftDirection = Vector3.Slerp(Vector3.up, waterNormal, effectiveNormalLift).normalized;


        Vector3 buoyancy = liftDirection * (buoyancyStrength * submergence);

        Vector3 verticalDamp = Vector3.up * (-pointVel.y * verticalDamping * submergence);

        Vector3 localPointVel = transform.InverseTransformDirection(pointVel);
        float slip = localPointVel.z;

        Vector3 finForce = Vector3.zero;

        if (Mathf.Abs(slip) > finGripDeadzone)
        {
            Vector3 sideDir = FlatSideDirection();
            finForce = sideDir * (-slip * finGrip * rb.mass * submergence);
        }

        rb.AddForceAtPosition(buoyancy + verticalDamp + finForce, point.position, ForceMode.Force);
    }

    void ApplyLocalDrag(float immersion)
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        localVel.x = Damp(localVel.x, forwardDrag * immersion);
        localVel.y = Damp(localVel.y, verticalWaterDrag * immersion);
        localVel.z = Damp(localVel.z, sidewaysDrag * immersion);

        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    void ApplyAngularDrag(float immersion)
    {
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);

        localAngVel.x = Damp(localAngVel.x, angularDragRoll * immersion);
        localAngVel.y = Damp(localAngVel.y, angularDragYaw * immersion);
        localAngVel.z = Damp(localAngVel.z, angularDragPitch * immersion);

        rb.angularVelocity = transform.TransformDirection(localAngVel);
    }

    void ApplyRailBite(float immersion)
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        float forwardSpeed = localVel.x;
        float speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.001f, speedForFullRailBite));

        railAmount = Mathf.Clamp(transform.forward.y, -1f, 1f);

        if (Mathf.Abs(railAmount) < 0.02f || speed01 <= 0.01f)
            return;

        float direction = invertRailCarve ? -1f : 1f;
        float signedSpeedPower = forwardSpeed * Mathf.Abs(forwardSpeed);

        Vector3 sideDir = FlatSideDirection();
        Vector3 carveForce = sideDir * (railAmount * signedSpeedPower * railBiteStrength * speed01 * immersion * direction);

        Vector3 forcePoint = rb.worldCenterOfMass - transform.right * railCarvePivotOffset;

        rb.AddForceAtPosition(carveForce, forcePoint, ForceMode.Acceleration);
    }

    float Damp(float value, float drag)
    {
        return value * Mathf.Exp(-drag * Time.fixedDeltaTime);
    }

    Vector3 FlatSideDirection()
    {
        Vector3 side = transform.forward;
        side.y = 0f;

        if (side.sqrMagnitude < 0.001f)
            side = transform.forward;

        return side.normalized;
    }
}
