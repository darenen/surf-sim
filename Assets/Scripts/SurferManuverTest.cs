using UnityEngine;
using UnityEngine.InputSystem;

public class SurferManuverTest : MonoBehaviour
{
    public enum TestCase
    {
        Manual,
        Neutral,
        LeftFoot,
        RightFoot,
        ToeSide,
        HeelSide,
        LeftToe,
        LeftHeel,
        RightToe,
        RightHeel,
        AlternatingToeHeel
    }

    public Rigidbody rb;

    [Header("Foot Points")]
    [Tooltip("Order: Left Toe, Left Heel, Right Toe, Right Heel")]
    public Transform[] feetPoints;

    public float massSurfer = 5f;

    [Header("Virtual Pressure Shape")]
    public bool useVirtualToeHeelWidth = true;
    public float virtualToeHeelHalfWidth = 0.45f;
    public bool invertToeHeelSide = true;

    [Header("Test")]
    public TestCase testCase = TestCase.Manual;
    public float alternatingPeriod = 1f;

    [Header("Manual Input")]
    [Range(-1f, 1f)] public float leftRightFootShift;
    [Range(-1f, 1f)] public float toeHeelShift;
    public float inputSharpness = 8f;

    [Header("Debug")]
    public float totalAppliedForce;
    public float leftToeWeight;
    public float leftHeelWeight;
    public float rightToeWeight;
    public float rightHeelWeight;
    public float estimatedRollTorque;

    float targetLeftRightFootShift;
    float targetToeHeelShift;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb == null || feetPoints == null || feetPoints.Length < 4)
            return;

        ReadTestInput();
        SmoothInput();
        ApplySurferWeight();
    }

    void ReadTestInput()
    {
        targetLeftRightFootShift = 0f;
        targetToeHeelShift = 0f;

        switch (testCase)
        {
            case TestCase.Manual:
                ReadKeyboardInput();
                break;

            case TestCase.Neutral:
                break;

            case TestCase.LeftFoot:
                targetLeftRightFootShift = -1f;
                break;

            case TestCase.RightFoot:
                targetLeftRightFootShift = 1f;
                break;

            case TestCase.ToeSide:
                targetToeHeelShift = 1f;
                break;

            case TestCase.HeelSide:
                targetToeHeelShift = -1f;
                break;

            case TestCase.LeftToe:
                targetLeftRightFootShift = -1f;
                targetToeHeelShift = 1f;
                break;

            case TestCase.LeftHeel:
                targetLeftRightFootShift = -1f;
                targetToeHeelShift = -1f;
                break;

            case TestCase.RightToe:
                targetLeftRightFootShift = 1f;
                targetToeHeelShift = 1f;
                break;

            case TestCase.RightHeel:
                targetLeftRightFootShift = 1f;
                targetToeHeelShift = -1f;
                break;

            case TestCase.AlternatingToeHeel:
                targetToeHeelShift = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.01f, alternatingPeriod));
                break;
        }
    }

    void ReadKeyboardInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.aKey.isPressed) targetLeftRightFootShift -= 1f;
        if (kb.dKey.isPressed) targetLeftRightFootShift += 1f;

        if (kb.wKey.isPressed) targetToeHeelShift += 1f;
        if (kb.sKey.isPressed) targetToeHeelShift -= 1f;

        targetLeftRightFootShift = Mathf.Clamp(targetLeftRightFootShift, -1f, 1f);
        targetToeHeelShift = Mathf.Clamp(targetToeHeelShift, -1f, 1f);
    }

    void SmoothInput()
    {
        float blend = 1f - Mathf.Exp(-inputSharpness * Time.fixedDeltaTime);

        leftRightFootShift = Mathf.Lerp(leftRightFootShift, targetLeftRightFootShift, blend);
        toeHeelShift = Mathf.Lerp(toeHeelShift, targetToeHeelShift, blend);
    }

    void ApplySurferWeight()
    {
        float totalWeight = massSurfer * Physics.gravity.magnitude;

        float rightFootRatio = Mathf.Clamp01(0.5f + leftRightFootShift * 0.5f);
        float leftFootRatio = 1f - rightFootRatio;

        float toeRatio = Mathf.Clamp01(0.5f + toeHeelShift * 0.5f);
        float heelRatio = 1f - toeRatio;

        leftToeWeight = totalWeight * leftFootRatio * toeRatio;
        leftHeelWeight = totalWeight * leftFootRatio * heelRatio;
        rightToeWeight = totalWeight * rightFootRatio * toeRatio;
        rightHeelWeight = totalWeight * rightFootRatio * heelRatio;

        totalAppliedForce = leftToeWeight + leftHeelWeight + rightToeWeight + rightHeelWeight;
        estimatedRollTorque = 0f;

        AddFootForce(0, leftToeWeight);
        AddFootForce(1, leftHeelWeight);
        AddFootForce(2, rightToeWeight);
        AddFootForce(3, rightHeelWeight);
    }

    void AddFootForce(int index, float force)
    {
        Transform foot = feetPoints[index];
        if (foot == null)
            return;

        Vector3 forcePosition = GetForcePosition(index, foot);
        Vector3 forceVector = Vector3.down * force;

        rb.AddForceAtPosition(forceVector, forcePosition, ForceMode.Force);

        Vector3 r = forcePosition - rb.worldCenterOfMass;
        Vector3 torque = Vector3.Cross(r, forceVector);
        Vector3 localTorque = transform.InverseTransformDirection(torque);

        estimatedRollTorque += localTorque.x;
    }

    Vector3 GetForcePosition(int index, Transform foot)
    {
        if (!useVirtualToeHeelWidth)
            return foot.position;

        Vector3 localPos = transform.InverseTransformPoint(foot.position);
        Vector3 localCenter = transform.InverseTransformPoint(rb.worldCenterOfMass);

        bool isToe = index == 0 || index == 2;

        float toeSign = invertToeHeelSide ? 1f : -1f;
        float heelSign = -toeSign;

        localPos.z = localCenter.z + (isToe ? toeSign : heelSign) * virtualToeHeelHalfWidth;

        return transform.TransformPoint(localPos);
    }

    void OnDrawGizmosSelected()
    {
        if (feetPoints == null || feetPoints.Length < 4)
            return;

        for (int i = 0; i < 4; i++)
        {
            if (feetPoints[i] == null)
                continue;

            Gizmos.color = (i == 0 || i == 2) ? Color.cyan : Color.magenta;
            Gizmos.DrawSphere(GetEditorForcePosition(i, feetPoints[i]), 0.08f);
        }
    }

    Vector3 GetEditorForcePosition(int index, Transform foot)
    {
        if (!useVirtualToeHeelWidth || rb == null)
            return foot.position;

        Vector3 localPos = transform.InverseTransformPoint(foot.position);
        Vector3 localCenter = transform.InverseTransformPoint(rb.worldCenterOfMass);

        bool isToe = index == 0 || index == 2;

        float toeSign = invertToeHeelSide ? 1f : -1f;
        float heelSign = -toeSign;

        localPos.z = localCenter.z + (isToe ? toeSign : heelSign) * virtualToeHeelHalfWidth;

        return transform.TransformPoint(localPos);
    }
}
