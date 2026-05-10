using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Rigidbody target;

    [Header("Position")]
    public Vector3 offset = new Vector3(0f, 4f, -8f);
    public float followSpeed = 5f;

    [Header("Rotation")]
    public float rotationSmoothness = 2f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position behind the rigidbody
        Vector3 desiredPosition =
            target.position +
            Quaternion.Euler(0f, target.rotation.eulerAngles.y, 0f) * offset;

        // Smooth camera movement
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            1f / followSpeed
        );

        // Only gently follow the target's Y rotation
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