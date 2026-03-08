using UnityEngine;

public class HeightCalibrator : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;        
    public Transform headTransform;   

    [Header("Settings")]
    public float targetHeight = 1.7f;

    private float initialOriginY;

    void Start()
    {
        if (xrOrigin != null)
            initialOriginY = xrOrigin.localPosition.y;
    }

    public void CalibrateHeight()
    {
        if (xrOrigin == null || headTransform == null)
        {
            Debug.LogWarning("HeightCalibration: Missing references.");
            return;
        }

        float currentHeadHeight = headTransform.localPosition.y;

        float heightDifference = targetHeight - currentHeadHeight;

        Vector3 newPosition = xrOrigin.localPosition;
        newPosition.y = initialOriginY + heightDifference;

        xrOrigin.localPosition = newPosition;

        Debug.Log($"Calibrated. Head: {currentHeadHeight:F2}m | Offset applied: {heightDifference:F2}m");
    }

    public void ResetHeight()
    {
        Vector3 pos = xrOrigin.localPosition;
        pos.y = initialOriginY;
        xrOrigin.localPosition = pos;
    }
}
