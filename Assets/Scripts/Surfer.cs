using UnityEngine;
using UnityEngine.InputSystem;

public class Surfer : MonoBehaviour
{
    public FeetPhysics feetPhysics;

    // How fast the weight shifts when you press a key (smooths out the input)
    public float shiftSpeed = 5f;

    // Current weight distribution (0 to 1)
    // 0.5 means perfectly centered
    private float frontBackRatio = 0.5f;
    private float toeHeelRatio = 0.5f;

    void Update()
    {
        if (Keyboard.current == null || feetPhysics == null) return;

        // 1. Read Inputs (-1 to 1)
        float targetFrontBack = 0f;
        float targetToeHeel = 0f;

        if (Keyboard.current.wKey.isPressed) targetFrontBack += 1f; // Lean Front (Right Foot)
        if (Keyboard.current.sKey.isPressed) targetFrontBack -= 1f; // Lean Back (Left Foot)

        if (Keyboard.current.dKey.isPressed) targetToeHeel += 1f; // Lean Toe
        if (Keyboard.current.aKey.isPressed) targetToeHeel -= 1f; // Lean Heel

        // 2. Smoothly move the ratios toward the input targets
        // Map the -1 to 1 inputs into a 0 to 1 scale for our math
        float targetFBRatio = (targetFrontBack + 1f) / 2f;
        float targetTHRatio = (targetToeHeel + 1f) / 2f;

        frontBackRatio = Mathf.Lerp(frontBackRatio, targetFBRatio, Time.deltaTime * shiftSpeed);
        toeHeelRatio = Mathf.Lerp(toeHeelRatio, targetTHRatio, Time.deltaTime * shiftSpeed);

        // 3. Calculate the opposing percentages
        float f = frontBackRatio;
        float b = 1f - f;
        float t = toeHeelRatio;
        float h = 1f - t;

        // 4. Apply Bilinear Distribution into the FeetPhysics array
        // Index 0: RightToe, 1: RightHeel, 2: LeftToe, 3: LeftHeel
        feetPhysics.weights[0] = f * t;
        feetPhysics.weights[1] = f * h;
        feetPhysics.weights[2] = b * t;
        feetPhysics.weights[3] = b * h;
    }
}