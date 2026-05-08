using UnityEngine;

public class WaterPhysics : MonoBehaviour
{
    public Rigidbody rb;
    public Transform[] floatPoints;

    [Header("Water Physics")]
    public float k = 15f;
    [Range(0f, 1f)]
    public float verticalDamp = 0.2f; // Replaces 'c'. 0.2 means it applies 20% of the stopping force

    [Header("Fin Physics")]
    public float finGrip = 1.5f;

    [Header("Raycast Settings")]
    public LayerMask waterLayer;
    public float floatHeight = 1.65f;

    void FixedUpdate()
    {
        foreach (Transform point in floatPoints)
        {
            if (Physics.Raycast(point.position, Vector3.down, out RaycastHit hit, 100f, waterLayer))
            {
                float d = floatHeight - hit.distance;

                if (d > 0f)
                {
                    Vector3 pointVelocity = rb.GetPointVelocity(point.position);

                    // 1. Buoyancy (Already scales with depth because we multiply by 'd')
                    Vector3 buoyancyForce = Vector3.up * (k * d);

                    // 2. NEW Depth-Scaled Vertical Damping (The Perfect Stop)
                    float verticalVelocity = pointVelocity.y;

                    // Calculate the exact force needed to freeze the board's up/down movement
                    float perfectVerticalStop = (verticalVelocity * rb.mass) / Time.fixedDeltaTime;
                    perfectVerticalStop /= floatPoints.Length;

                    // Multiply by our percentage (verticalDamp) AND by depth (d).
                    // If the board gets pushed deep underwater, 'd' grows larger,
                    // massively multiplying the stopping force to prevent bottoming out!
                    Vector3 verticalDragForce = -Vector3.up * (perfectVerticalStop * verticalDamp * d);



                    // 1. Convert the world velocity of the point into LOCAL velocity
                    // This tells us: x = sideways speed, y = up/down speed, z = forward speed
                    Vector3 localPointVel = transform.InverseTransformDirection(rb.GetPointVelocity(point.position));

                    // 2. Isolate the Sideways Speed (X)
                    float sidewaysSpeed = localPointVel.x;

                    Vector3 lateralDragForce = Vector3.zero;

                    // 3. Only apply force if the slip is big enough to care about (Deadzone)
                    if (Mathf.Abs(sidewaysSpeed) > 0.01f)
                    {
                        // Calculate the force to stop JUST the X movement
                        float perfectSidewaysStop = (sidewaysSpeed * rb.mass) / Time.fixedDeltaTime;
                        perfectSidewaysStop /= floatPoints.Length;

                        // Apply it only on the LOCAL X axis of the board
                        // We use transform.right to turn that local math back into a world force
                        lateralDragForce = transform.right * -perfectSidewaysStop * finGrip;
                    }


                    // 4. Combine and Apply
                    Vector3 totalForce = buoyancyForce + verticalDragForce + lateralDragForce;
                    rb.AddForceAtPosition(totalForce, point.position);
                }
            }
        }
    }
}