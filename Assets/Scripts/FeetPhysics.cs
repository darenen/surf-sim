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
    public float weightForce = 100f;

    [Header("Weight Distributions")]
    // Order: RightToe, RightHeel, LeftToe, LeftHeel
    public float[] weights = {0.25f, 0.25f, 0.25f, 0.25f};

    [Header("Effective Contact Width")]
    public bool useEffectiveContactWidth = true;
    public float targetFullRollTorque = 150f;
    public float maxEffectiveHalfWidth = 2f;
    public bool invertToeHeelSide = false;

    [Header("Debug")]
    public float totalWeightRatio;
    public float toeRatio;
    public float heelRatio;
    public float toeHeelInput;
    public float effectiveHalfWidth;
    public float estimatedRollTorque;

    void FixedUpdate()
    {
        if (boardRb == null || weights == null || weights.Length < 4)
            return;

        NormalizeWeights();
        ApplyWeightShifts();
    }

    void ApplyWeightShifts()
    {
        toeRatio = weights[0] + weights[2];
        heelRatio = weights[1] + weights[3];

        toeHeelInput = toeRatio - heelRatio;

        if (invertToeHeelSide)
            toeHeelInput *= -1f;

        effectiveHalfWidth = 0f;

        if (useEffectiveContactWidth)
        {
            effectiveHalfWidth = targetFullRollTorque / Mathf.Max(0.001f, weightForce);
            effectiveHalfWidth = Mathf.Min(effectiveHalfWidth, maxEffectiveHalfWidth);
        }

        estimatedRollTorque = 0f;

        AddFootForce(rightToe, weights[0], true);
        AddFootForce(rightHeel, weights[1], false);
        AddFootForce(leftToe, weights[2], true);
        AddFootForce(leftHeel, weights[3], false);
    }

    void NormalizeWeights()
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Mathf.Clamp01(weights[i]);
        }

        float sum = weights[0] + weights[1] + weights[2] + weights[3];

        if (sum <= 0.001f)
        {
            weights[0] = 0.25f;
            weights[1] = 0.25f;
            weights[2] = 0.25f;
            weights[3] = 0.25f;
            sum = 1f;
        }

        weights[0] /= sum;
        weights[1] /= sum;
        weights[2] /= sum;
        weights[3] /= sum;

        totalWeightRatio = weights[0] + weights[1] + weights[2] + weights[3];
    }

    void AddFootForce(Transform foot, float ratio, bool isToe)
    {
        if (foot == null || ratio <= 0f)
            return;

        Vector3 forcePosition = GetForcePosition(foot, isToe);
        Vector3 force = Vector3.down * weightForce * ratio;

        boardRb.AddForceAtPosition(force, forcePosition, ForceMode.Force);

        Vector3 leverArm = forcePosition - boardRb.worldCenterOfMass;
        Vector3 torque = Vector3.Cross(leverArm, force);
        Vector3 localTorque = boardRb.transform.InverseTransformDirection(torque);

        estimatedRollTorque += localTorque.x;
    }

    Vector3 GetForcePosition(Transform foot, bool isToe)
    {
        if (!useEffectiveContactWidth)
            return foot.position;

        Vector3 localPos = boardRb.transform.InverseTransformPoint(foot.position);
        Vector3 localCenter = boardRb.transform.InverseTransformPoint(boardRb.worldCenterOfMass);

        float toeSign = invertToeHeelSide ? 1f : -1f;
        float heelSign = -toeSign;

        localPos.z = localCenter.z + (isToe ? toeSign : heelSign) * effectiveHalfWidth;

        return boardRb.transform.TransformPoint(localPos);
    }

    void OnDrawGizmosSelected()
    {
        if (boardRb == null)
            return;

        DrawFootPoint(rightToe, true);
        DrawFootPoint(rightHeel, false);
        DrawFootPoint(leftToe, true);
        DrawFootPoint(leftHeel, false);
    }

    void DrawFootPoint(Transform foot, bool isToe)
    {
        if (foot == null)
            return;

        Gizmos.color = isToe ? Color.cyan : Color.magenta;
        Gizmos.DrawSphere(GetForcePosition(foot, isToe), 0.08f);
    }

}
