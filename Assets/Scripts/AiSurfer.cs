using UnityEngine;

public class AiSurfer : MonoBehaviour
{
    public FeetPhysics feetPhysics;
    public Rigidbody boardRb;

    [Header("Episode Start")]
    public bool setInitialSpeedOnStart = true;
    public float initialSpeed = 12f;

    [Header("Weight Shift")]
    public bool smoothWeights = true;
    public float shiftSpeed = 8f;

    [Header("Target Weight Distribution")]
    [Range(0f, 1f)] public float targetRightToe = 0.25f;
    [Range(0f, 1f)] public float targetRightHeel = 0.25f;
    [Range(0f, 1f)] public float targetLeftToe = 0.25f;
    [Range(0f, 1f)] public float targetLeftHeel = 0.25f;

    [Header("Current Weight Distribution")]
    [Range(0f, 1f)] public float rightToe = 0.25f;
    [Range(0f, 1f)] public float rightHeel = 0.25f;
    [Range(0f, 1f)] public float leftToe = 0.25f;
    [Range(0f, 1f)] public float leftHeel = 0.25f;

    [Header("Debug")]
    public float totalWeight;
    public float toeWeight;
    public float heelWeight;
    public float rightFootWeight;
    public float leftFootWeight;

    void Reset()
    {
        feetPhysics = GetComponent<FeetPhysics>();
        boardRb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (setInitialSpeedOnStart)
            SetInitialSpeed();
    }

    void FixedUpdate()
    {
        if (feetPhysics == null || feetPhysics.weights == null || feetPhysics.weights.Length < 4)
            return;

        NormalizeTargets();
        MoveCurrentWeights();
        ApplyWeightsToFeet();
        UpdateDebug();
    }

    // Use this from an RL agent when actions are already in the 0..1 range.
    public void SetWeightDistribution(float newRightToe, float newRightHeel, float newLeftToe, float newLeftHeel)
    {
        targetRightToe = Mathf.Clamp01(newRightToe);
        targetRightHeel = Mathf.Clamp01(newRightHeel);
        targetLeftToe = Mathf.Clamp01(newLeftToe);
        targetLeftHeel = Mathf.Clamp01(newLeftHeel);

        NormalizeTargets();
    }

    // Use this from an RL agent if its continuous actions are in the usual -1..1 range.
    public void SetActionWeights(float actionRightToe, float actionRightHeel, float actionLeftToe, float actionLeftHeel)
    {
        SetWeightDistribution(
            ActionToWeight(actionRightToe),
            ActionToWeight(actionRightHeel),
            ActionToWeight(actionLeftToe),
            ActionToWeight(actionLeftHeel)
        );
    }

    public void SetInitialSpeed()
    {
        if (boardRb == null)
            return;

        Vector3 nose = boardRb.transform.right;
        nose.y = 0f;

        if (nose.sqrMagnitude < 0.001f)
            return;

        boardRb.linearVelocity = nose.normalized * initialSpeed;
        boardRb.angularVelocity = Vector3.zero;
    }

    float ActionToWeight(float action)
    {
        return Mathf.Clamp01((action + 1f) * 0.5f);
    }

    void NormalizeTargets()
    {
        targetRightToe = Mathf.Clamp01(targetRightToe);
        targetRightHeel = Mathf.Clamp01(targetRightHeel);
        targetLeftToe = Mathf.Clamp01(targetLeftToe);
        targetLeftHeel = Mathf.Clamp01(targetLeftHeel);

        float sum = targetRightToe + targetRightHeel + targetLeftToe + targetLeftHeel;

        if (sum <= 0.001f)
        {
            targetRightToe = 0.25f;
            targetRightHeel = 0.25f;
            targetLeftToe = 0.25f;
            targetLeftHeel = 0.25f;
            return;
        }

        targetRightToe /= sum;
        targetRightHeel /= sum;
        targetLeftToe /= sum;
        targetLeftHeel /= sum;
    }

    void MoveCurrentWeights()
    {
        if (!smoothWeights)
        {
            rightToe = targetRightToe;
            rightHeel = targetRightHeel;
            leftToe = targetLeftToe;
            leftHeel = targetLeftHeel;
            return;
        }

        float step = shiftSpeed * Time.fixedDeltaTime;

        rightToe = Mathf.MoveTowards(rightToe, targetRightToe, step);
        rightHeel = Mathf.MoveTowards(rightHeel, targetRightHeel, step);
        leftToe = Mathf.MoveTowards(leftToe, targetLeftToe, step);
        leftHeel = Mathf.MoveTowards(leftHeel, targetLeftHeel, step);

        NormalizeCurrent();
    }

    void NormalizeCurrent()
    {
        float sum = rightToe + rightHeel + leftToe + leftHeel;

        if (sum <= 0.001f)
        {
            rightToe = 0.25f;
            rightHeel = 0.25f;
            leftToe = 0.25f;
            leftHeel = 0.25f;
            return;
        }

        rightToe /= sum;
        rightHeel /= sum;
        leftToe /= sum;
        leftHeel /= sum;
    }

    void ApplyWeightsToFeet()
    {
        // Index 0: RightToe
        // Index 1: RightHeel
        // Index 2: LeftToe
        // Index 3: LeftHeel
        feetPhysics.weights[0] = rightToe;
        feetPhysics.weights[1] = rightHeel;
        feetPhysics.weights[2] = leftToe;
        feetPhysics.weights[3] = leftHeel;
    }

    void UpdateDebug()
    {
        totalWeight = rightToe + rightHeel + leftToe + leftHeel;
        toeWeight = rightToe + leftToe;
        heelWeight = rightHeel + leftHeel;
        rightFootWeight = rightToe + rightHeel;
        leftFootWeight = leftToe + leftHeel;
    }
}
