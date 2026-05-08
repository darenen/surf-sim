using UnityEngine;

/// <summary>
/// Does not use full fluid dynamics, instead water physics is
/// modeled with spring equations and linear drag
/// </summary>
public class WaterPhysics : MonoBehaviour
{
    public Rigidbody rb;

    // This creates an array (list) in the Inspector to plug in your 4 float points
    public Transform[] floatPoints;
    public float k = 15f; // Spring Constant (Buoyancy strength)
    public float c = 2f;  // Damping constant (Water drag)

    void FixedUpdate()
    {
        float y_water = 0f; // Standard flat water line

        // Loop through the Nose, Tail, Left, and Right points one by one
        foreach (Transform point in floatPoints)
        {
            // 1. Get the depth of this specific point
            float d = y_water - point.position.y;

            // Only apply physics if this specific point is underwater
            if (d > 0f)
            {
                // 2. Buoyancy Force: Vector3.up * spring constant * depth
                Vector3 buoyancyForce = Vector3.up * k * d;

                // 3. Drag Force
                // THIS is the secret sauce: rb.GetPointVelocity gets the speed of the
                // board at this exact corner, not just the center of the board.
                Vector3 pointVelocity = rb.GetPointVelocity(point.position);
                Vector3 dragForce = -pointVelocity * c;

                // 4. Combine forces and apply them exactly at this point's location
                Vector3 totalForce = buoyancyForce + dragForce;
                rb.AddForceAtPosition(totalForce, point.position);
            }
        }
    }
}