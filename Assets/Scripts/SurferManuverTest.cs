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

        rb.AddForceAtPosition(Vector3.down * force, foot.position, ForceMode.Force);
    }
}
