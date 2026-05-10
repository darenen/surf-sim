using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Rigidbody target;

    [Header("Position")]
    public float distanceBehind = 8f;
    public float heightAbove = 4f;
    public float followSpeed = 5f;

    [Header("Rotation")]
    public float rotationSmoothness = 2f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Find the board's true "Nose" direction (X-axis for your setup)
        Vector3 flatNoseDirection = target.transform.right;

        // Flatten the Y value so the camera doesn't plunge underwater when you carve!
        flatNoseDirection.y = 0f;

        // Normalize ensures the distance stays exact even if the board is tilted
        if (flatNoseDirection != Vector3.zero)
        {
            flatNoseDirection.Normalize();
        }
        else
        {
            flatNoseDirection = Vector3.right; // Fallback
        }

        // 2. Calculate the spot behind the board
        Vector3 desiredPosition = target.position
                                  - (flatNoseDirection * distanceBehind)
                                  + (Vector3.up * heightAbove);

        // 3. Smooth camera movement
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            1f / followSpeed
        );

        // 4. Smoothly rotate to look at the board
        Quaternion desiredRotation = Quaternion.LookRotation(
            target.position - transform.position,
            Vector3.up
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSmoothness * Time.deltaTime
        );
    }
}