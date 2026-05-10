using UnityEngine;
using UnityEngine.InputSystem;

public class FeetPhysics : MonoBehaviour
{
    public Rigidbody boardRb;

    [Header("FeetPoints")]
    public Transform rightToe;
    public Transform rightHeel;
    public Transform leftToe;
    public Transform leftHeel;

    [Header("Surfer Weight")]
    public float weightForce = 150f;

    [Header("Weight Distributions")]
    public float[] weights = {0f,0f,0f,0f};

    void FixedUpdate()
    {
        ApplyWeightShifts();
    }

    void ApplyWeightShifts()
    {
        return;
    }

}