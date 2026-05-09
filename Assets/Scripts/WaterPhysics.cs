using UnityEngine;

public class WaterPhysics : MonoBehaviour
{
    public Rigidbody rb;
    public Transform[] floatPoints;



    [Header("Buoyancy")]
    public float buoyancyStrength = 80f;
    public float floatHeight = 1.65f;
    public float verticalDamping = 8f;


    [Header("Slope Drive")]
    public float slopeDriveStrength = 1f;
    public float maxSlopeDriveAcceleration = 8f;

    [Header("Board Drag")]
    public float forwardDrag = 0.35f;
    public float sidewaysDrag = 2.5f;
    public float verticalWaterDrag = 0.5f;

    [Header("Fin")]
    public Transform finPoint;
    public float finGripStrength = 1.5f;
    public float speedForFullFin = 4f;
    public float finSlipDeadzone = 0.02f;
    public float maxFinAcceleration = 4f;

    [Header("Rail Bite / Carving")]
    public float railBiteStrength = 1.5f;
    public float railCarvePivotOffset = 1.2f;
    public float speedForFullRailBite = 6f;
    public float maxRailCarveAcceleration = 5f;
    public bool invertRailCarve = false;

    [Header("Angular Drag")]
    public float angularDragRoll = 1f;
    public float angularDragYaw = 0.5f;
    public float angularDragPitch = 3f;

    [Header("Safety")]
    public float maxVelocity = 18f;
    public float maxAngularVelocity = 12f;

    [Header("Raycast")]
    public LayerMask waterLayer;
    public float raycastStartHeight = 5f;
    public float raycastDistance = 20f;

    [Header("Debug")]
    [HideInInspector] public int pointsInWater;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public float sidewaysSlip;
    [HideInInspector] public float slopeDriveAcceleration;
    [HideInInspector] public float finSlip;
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
        slopeDriveAcceleration = 0f;
        finSlip = 0f;

        float totalSubmergence = 0f;
        Vector3 weightedWaterNormal = Vector3.zero;

        foreach (Transform point in floatPoints)
        {
            if (point == null)
                continue;

            ApplyFloatPoint(point, ref totalSubmergence, ref weightedWaterNormal);
        }

        float immersion = pointsInWater > 0 ? totalSubmergence / pointsInWater : 0f;
        Vector3 averageWaterNormal = weightedWaterNormal.sqrMagnitude > 0.001f
            ? weightedWaterNormal.normalized
            : Vector3.up;

        if (pointsInWater > 0)
        {
            ApplySlopeDrive(averageWaterNormal, immersion);
            ApplyFinGrip(immersion);
            ApplyRailBite(immersion);
            ApplyLocalDrag(immersion);
            ApplyAngularDrag(immersion);
        }

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxVelocity);
        rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxAngularVelocity);

        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        currentSpeed = localVelocity.x;
        sidewaysSlip = localVelocity.z;
    }

    void ApplyFloatPoint(Transform point, ref float totalSubmergence, ref Vector3 weightedWaterNormal)
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
        weightedWaterNormal += waterNormal * submergence;

        Vector3 pointVelocity = rb.GetPointVelocity(point.position);
        float verticalSpeed = Vector3.Dot(pointVelocity, Vector3.up);

        Vector3 buoyancy = Vector3.up * (buoyancyStrength * submergence);
        Vector3 damping = Vector3.up * (-verticalSpeed * verticalDamping * submergence);

        rb.AddForceAtPosition(buoyancy + damping, point.position, ForceMode.Force);
    }



    void ApplySlopeDrive(Vector3 waterNormal, float immersion)
    {
        Vector3 nose = FlatNoseDirection();

        if (nose == Vector3.zero)
            return;

        Vector3 gravityDownSlope = Vector3.ProjectOnPlane(Physics.gravity, waterNormal);

        if (gravityDownSlope.sqrMagnitude < 0.001f)
            return;

        float driveAlongNose = Vector3.Dot(gravityDownSlope, nose);

        if (driveAlongNose <= 0f)
            return;

        slopeDriveAcceleration = driveAlongNose * slopeDriveStrength * immersion;
        slopeDriveAcceleration = Mathf.Clamp(slopeDriveAcceleration, 0f, maxSlopeDriveAcceleration);

        rb.AddForce(nose * slopeDriveAcceleration, ForceMode.Acceleration);
    }

    void ApplyFinGrip(float immersion)
    {
        if (finPoint == null || immersion <= 0f)
            return;

        Vector3 localFinVelocity = transform.InverseTransformDirection(rb.GetPointVelocity(finPoint.position));

        float forwardSpeed = Mathf.Abs(localFinVelocity.x);
        float speedFactor = Mathf.Clamp01(forwardSpeed / Mathf.Max(0.001f, speedForFullFin));

        if (speedFactor <= 0.01f)
            return;

        finSlip = localFinVelocity.z;

        if (Mathf.Abs(finSlip) < finSlipDeadzone)
            return;

        float finAcceleration = -finSlip * finGripStrength * speedFactor * immersion;
        finAcceleration = Mathf.Clamp(finAcceleration, -maxFinAcceleration, maxFinAcceleration);

        rb.AddForceAtPosition(FlatSideDirection() * finAcceleration, finPoint.position, ForceMode.Acceleration);
    }

    void ApplyRailBite(float immersion)
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        float forwardSpeed = localVelocity.x;
        float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.001f, speedForFullRailBite));

        railAmount = Mathf.Clamp(transform.forward.y, -1f, 1f);

        if (Mathf.Abs(railAmount) < 0.02f || speedFactor <= 0.01f)
            return;

        float direction = invertRailCarve ? -1f : 1f;
        float speedSign = Mathf.Sign(forwardSpeed);

        float carveAcceleration = railAmount * railBiteStrength * speedFactor * speedSign * immersion * direction;
        carveAcceleration = Mathf.Clamp(carveAcceleration, -maxRailCarveAcceleration, maxRailCarveAcceleration);

        Vector3 forcePoint = rb.worldCenterOfMass - transform.right * railCarvePivotOffset;

        rb.AddForceAtPosition(FlatSideDirection() * carveAcceleration, forcePoint, ForceMode.Acceleration);
    }

    void ApplyLocalDrag(float immersion)
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        localVelocity.x = Damp(localVelocity.x, forwardDrag * immersion);
        localVelocity.y = Damp(localVelocity.y, verticalWaterDrag * immersion);
        localVelocity.z = Damp(localVelocity.z, sidewaysDrag * immersion);

        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }

    void ApplyAngularDrag(float immersion)
    {
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        localAngularVelocity.x = Damp(localAngularVelocity.x, angularDragRoll * immersion);
        localAngularVelocity.y = Damp(localAngularVelocity.y, angularDragYaw * immersion);
        localAngularVelocity.z = Damp(localAngularVelocity.z, angularDragPitch * immersion);

        rb.angularVelocity = transform.TransformDirection(localAngularVelocity);
    }

    float Damp(float value, float drag)
    {
        return value * Mathf.Exp(-drag * Time.fixedDeltaTime);
    }

    Vector3 FlatNoseDirection()
    {
        Vector3 nose = transform.right;
        nose.y = 0f;

        if (nose.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return nose.normalized;
    }

    Vector3 FlatSideDirection()
    {
        Vector3 side = transform.forward;
        side.y = 0f;

        if (side.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return side.normalized;
    }

}
