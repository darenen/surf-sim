using UnityEngine;
using UnityEngine.InputSystem;

public class Surfer : MonoBehaviour
{
    public FeetPhysics feetPhysics;
    public Rigidbody boardRb;

    [Header("Drive Test")]
    public float forwardForce = 80f;
    public bool useSpaceDrive = true;

    [Header("Weight Shift")]
    public float shiftSpeed = 6f;

    [Range(0f, 1f)] public float frontBackRatio = 0.5f;
    [Range(0f, 1f)] public float toeHeelRatio = 0.5f;

    [Header("Input Debug")]
    public float targetFrontBackRatio = 0.5f;
    public float targetToeHeelRatio = 0.5f;

    void Update()
    {
        if (Keyboard.current == null || feetPhysics == null)
            return;

        ReadWeightInput();
    }

    void FixedUpdate()
    {
        if (Keyboard.current == null)
            return;

        ApplySpaceDrive();
    }

    void ReadWeightInput()
    {
        targetFrontBackRatio = 0.5f;
        targetToeHeelRatio = 0.5f;

        // A/D = rail lean only.
        // A = heel side, D = toe side.
        if (Keyboard.current.aKey.isPressed)
            targetToeHeelRatio = 0f;

        if (Keyboard.current.dKey.isPressed)
            targetToeHeelRatio = 1f;

        // Optional W/S = front/back pressure.
        if (Keyboard.current.wKey.isPressed)
            targetFrontBackRatio = 1f;

        if (Keyboard.current.sKey.isPressed)
            targetFrontBackRatio = 0f;

        frontBackRatio = Mathf.MoveTowards(
            frontBackRatio,
            targetFrontBackRatio,
            shiftSpeed * Time.deltaTime
        );

        toeHeelRatio = Mathf.MoveTowards(
            toeHeelRatio,
            targetToeHeelRatio,
            shiftSpeed * Time.deltaTime
        );

        ApplyWeights();
    }

    void ApplyWeights()
    {
        float f = frontBackRatio;
        float b = 1f - f;

        float t = toeHeelRatio;
        float h = 1f - t;

        // Index 0: RightToe
        // Index 1: RightHeel
        // Index 2: LeftToe
        // Index 3: LeftHeel
        feetPhysics.weights[0] = f * t;
        feetPhysics.weights[1] = f * h;
        feetPhysics.weights[2] = b * t;
        feetPhysics.weights[3] = b * h;
    }

    void ApplySpaceDrive()
    {
        if (!useSpaceDrive || boardRb == null)
            return;

        if (!Keyboard.current.spaceKey.isPressed)
            return;

        Vector3 nose = boardRb.transform.right;
        nose.y = 0f;

        if (nose.sqrMagnitude < 0.001f)
            return;

        boardRb.AddForce(nose.normalized * forwardForce, ForceMode.Force);
    }
}
