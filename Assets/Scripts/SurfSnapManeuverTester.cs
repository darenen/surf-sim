using UnityEngine;

public class SurfSnapManeuverTester : MonoBehaviour
{
    public Rigidbody rb;

    [Header("Forces")]
    public float forwardForce = 50f;
    public float pitchTorque = 4f;
    public float rollTorque = 3f;

    [Header("Test Timing")]
    public float buildSpeedTime = 2f;
    public float bottomTurnTime = 1f;
    public float snapTime = 0.45f;
    public float recoverTime = 1f;

    [Header("Inputs")]
    [Range(-1f, 1f)] public float bottomTurnRoll = 1f;
    [Range(-1f, 1f)] public float snapRoll = -1f;
    [Range(-1f, 1f)] public float snapPitch = 0.4f;

    [Header("Results")]
    public float startYaw;
    public float finalYaw;
    public float headingChange;
    public float peakYawRate;
    public float peakSideSlip;
    public float snapScore;

    float timer;
    bool running;

    void Start()
    {
        StartTest();
    }

    [ContextMenu("Start Snap Test")]
    public void StartTest()
    {
        timer = 0f;
        running = true;

        startYaw = transform.eulerAngles.y;
        finalYaw = startYaw;
        headingChange = 0f;
        peakYawRate = 0f;
        peakSideSlip = 0f;
        snapScore = 0f;
    }

    void FixedUpdate()
    {
        if (!running || rb == null) return;

        timer += Time.fixedDeltaTime;

        float drive = 0f;
        float pitch = 0f;
        float roll = 0f;

        float t1 = buildSpeedTime;
        float t2 = t1 + bottomTurnTime;
        float t3 = t2 + snapTime;
        float t4 = t3 + recoverTime;

        if (timer < t1)
        {
            drive = 1f;
        }
        else if (timer < t2)
        {
            drive = 1f;
            roll = bottomTurnRoll;
            pitch = -0.2f;
        }
        else if (timer < t3)
        {
            drive = 0.5f;
            roll = snapRoll;
            pitch = snapPitch;
        }
        else if (timer < t4)
        {
            drive = 0.25f;
            roll = 0f;
            pitch = 0f;
        }
        else
        {
            FinishTest();
            return;
        }

        ApplyControl(drive, pitch, roll);
        Measure();
    }

    void ApplyControl(float drive, float pitch, float roll)
    {
        Vector3 flatNose = transform.right;
        flatNose.y = 0f;

        if (flatNose.sqrMagnitude > 0.001f)
            rb.AddForce(flatNose.normalized * drive * forwardForce);

        rb.AddRelativeTorque(Vector3.forward * pitch * -pitchTorque);
        rb.AddRelativeTorque(Vector3.right * roll * -rollTorque);
    }

    void Measure()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 localAngVel = transform.InverseTransformDirection(rb.angularVelocity);

        float yawRateDeg = Mathf.Abs(localAngVel.y * Mathf.Rad2Deg);
        float sideSlip = Mathf.Abs(localVel.z);

        peakYawRate = Mathf.Max(peakYawRate, yawRateDeg);
        peakSideSlip = Mathf.Max(peakSideSlip, sideSlip);

        finalYaw = transform.eulerAngles.y;
        headingChange = Mathf.Abs(Mathf.DeltaAngle(startYaw, finalYaw));

        snapScore = headingChange + peakYawRate * 0.25f - peakSideSlip * 2f;
    }

    void FinishTest()
    {
        running = false;
        Measure();

        Debug.Log(
            $"SNAP TEST | Heading Change: {headingChange:F1} deg | " +
            $"Peak Yaw Rate: {peakYawRate:F1} deg/s | " +
            $"Peak Side Slip: {peakSideSlip:F2} m/s | " +
            $"Score: {snapScore:F1}"
        );
    }
}
