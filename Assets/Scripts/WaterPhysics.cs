using UnityEngine;
using UnityEngine.InputSystem;


public class WaterPhysics : MonoBehaviour
{
    public Rigidbody rb;
    public Transform[] floatPoints; // Nose, Tail, Left Right
    public Transform finPoint;
    public LayerMask waterLayer;

    [Header("Buoyancy")]
    public float buoyancyForce;

    [Header("Drag")]
    public float verticalDamp = 0.5f;
    public float pitchDrag = 10f;
    public float rollDrag = 30f;
    public float tiltDragMultiplier = 2f;

    [Header("Carving")]
    public float carveTurnSpeed = 5f; // How hard the board turns when leaning
    [Header("Fin Dynamics")]
    public float finGrip = 50f; // How hard the fin resists sideways drifting

    // buoyancy is proportional to volume
    // each point represents different volumes of the board
    private float[] buoyancyMultiplier = {0.5f, 0.5f, 1.5f, 1.5f};

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {

        // TODO: gravity and wave normal, fin dynamics and pivot,
        // TODo: water friction plus rails
        // TODO: skimming?

        float averageDepth = 0f;
        for(int i=0;i<4;i++)
        {
            Transform point = floatPoints[i];
            float buoyancy = buoyancyMultiplier[i];

            averageDepth += applyBuoyancy(point, buoyancy);
        }
        averageDepth /= 4;
        applyAngularDrag(averageDepth);
        applyFinDynamics();
        applyCarvingDynamics();
    }

    // Applies bouyant force to a singular point
    // returns true if the point is underwater
    // applies the vertical drag as well
    float applyBuoyancy(Transform point, float multiplier)
    {
        float raycastDistance = 10f;
        float raycastStartHeight = 5f;
        float floatHeight = 5f;

        Vector3 rayOrigin = point.position + Vector3.up * raycastStartHeight;

        // raycasts from the point downward
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, waterLayer, QueryTriggerInteraction.Collide))
            return 0f;
        float heightError = hit.point.y + floatHeight - point.position.y;

        Debug.DrawLine(rayOrigin,rayOrigin + Vector3.down * raycastDistance,Color.yellow);

        if (heightError <= 0f) return 0f;
        float depthMultiplier = Mathf.Clamp(heightError, 0f, 1f);
        // buoyancy force and vertical damping
        Vector3 F_b = buoyancyForce *  hit.normal * (multiplier * depthMultiplier);

        Vector3 pointVelocity = rb.GetPointVelocity(point.position);
        Vector3 F_d = -Vector3.up * (pointVelocity.y * verticalDamp * depthMultiplier);



        rb.AddForceAtPosition(F_b + F_d, point.position);

        return depthMultiplier;
    }

    // calculates the angle and velocity
    // if the angle higher. then it should have more drag bc more of the board is in the water
    // if its faster idk what it should do lowk
    void applyAngularDrag(float averageDepth)
    {
       if (averageDepth <= 0f) return; // assuming this down is positive

        // gets the relative spin
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);

        // get forward speed to increase stablity at high speeds
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float speedStability = 1f + (Mathf.Abs(localVelocity.z) * 0.1f);

        // calculates how heavily the board is angled
        float rollTilt = Mathf.Abs(transform.right.y);  // 0 = flat, 1 = 90 degrees
        float pitchTilt = Mathf.Abs(transform.forward.y);

        float rollAngleMod = 1f + (rollTilt * tiltDragMultiplier);
        float pitchAngleMod = 1f + (pitchTilt * tiltDragMultiplier);

        // calculate the torque
        float torqueX = -localAngVel.x * rollDrag * averageDepth * speedStability * pitchAngleMod;
        float torqueZ = -localAngVel.z * pitchDrag * averageDepth * speedStability * rollAngleMod;

        // apply the force
        Vector3 dampingTorque = new Vector3(torqueX, 0f, torqueZ);
        rb.AddRelativeTorque(dampingTorque, ForceMode.Force);

    }
    void applyCarvingDynamics()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = localVelocity.x;

        // if still
        if (forwardSpeed < 0.5f) return;


        float leanAmount = transform.up.z;
        float turnTorque = -leanAmount * forwardSpeed * carveTurnSpeed;

        rb.AddRelativeTorque(new Vector3(0f,turnTorque, 0f), ForceMode.Force);

    }

    void applyFinDynamics()
    {
        if (finPoint == null) return;

        Vector3 pointVelocity = rb.GetPointVelocity(finPoint.position);
        Vector3 localFinVel = transform.InverseTransformDirection(pointVelocity);

        if (pointVelocity.sqrMagnitude < 1f) return;

        // only care about z axis bc thats normal to the nose
        float sidewaysSlip = localFinVel.z;

        sidewaysSlip = Mathf.Clamp(sidewaysSlip, -5f, 5f);

        // counter force
        Vector3 counterForce = -transform.forward * (sidewaysSlip * finGrip);

        // apply force
        rb.AddForceAtPosition(counterForce, finPoint.position);
    }

    void Update()
    {
        // Safety check to make sure a keyboard is actually connected
        if (Keyboard.current == null) return;

        // TESTING ROLL DRAG (Press '1')
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            rb.AddRelativeTorque(new Vector3(5f, 0f, 0f), ForceMode.Impulse);
        }

        // TESTING PITCH DRAG (Press '2')
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            rb.AddRelativeTorque(new Vector3(0f, 0f, 5f), ForceMode.Impulse);
        }
    }

}