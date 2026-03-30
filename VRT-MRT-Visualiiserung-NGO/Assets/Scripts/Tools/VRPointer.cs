using UnityEngine;

public class VRPointer : MonoBehaviour
{
    [Header("Ray")]
    public Transform rayOrigin;
    public float maxDistance = 10f;
    public LayerMask layerMask = ~0;

    [Header("Visuals")]
    public LineRenderer laserLine;
    public Transform hitDot;
    public Color idleColor = new Color(0.35f, 0.75f, 1f, 1f);
    public Color hitColor = new Color(1f, 0.35f, 0.2f, 1f);

    public GameObject HitObject { get; private set; }

    private void OnEnable()
    {
        if (laserLine != null)
            laserLine.enabled = false;

        if (hitDot != null)
        {
            Renderer hitRenderer = hitDot.GetComponent<Renderer>();
            if (hitRenderer != null)
            {
                Material hitMaterial = new Material(hitRenderer.material) { color = hitColor };
                hitRenderer.material = hitMaterial;
            }
            hitDot.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        HitObject = null;

        if (laserLine != null)
            laserLine.enabled = false;

        if (hitDot != null)
            hitDot.gameObject.SetActive(false);

        if (NetworkToolSync.Instance != null)
            NetworkToolSync.Instance.SendPointerState(false, Vector3.zero, Vector3.zero,
                                                       false, Vector3.zero, Quaternion.identity);
    }

    private void Update()
    {
        if (rayOrigin == null || laserLine == null || !laserLine.enabled) 
            return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        bool hasHit;
        Vector3 lineEnd;
        Vector3 hitPoint   = Vector3.zero;
        Quaternion hitRot  = Quaternion.identity;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            HitObject = hit.collider.gameObject;
            hasHit = true;
            lineEnd = hit.point;
            hitPoint = hit.point;
            hitRot = Quaternion.LookRotation(hit.normal);

            SetLine(rayOrigin.position, hit.point, hitColor);

            if (hitDot != null)
            {
                hitDot.gameObject.SetActive(true);
                hitDot.position = hit.point;
                hitDot.rotation = hitRot;
            }
        }
        else
        {
            HitObject = null;
            hasHit = false;
            lineEnd = ray.GetPoint(maxDistance);

            SetLine(rayOrigin.position, lineEnd, idleColor);

            if (hitDot != null)
                hitDot.gameObject.SetActive(false);
        }

        if (NetworkToolSync.Instance != null)
            NetworkToolSync.Instance.SendPointerState(true, rayOrigin.position, lineEnd,
                                                       hasHit, hitPoint, hitRot);
    }

    private void SetLine(Vector3 start, Vector3 end, Color color)
    {
        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);
        laserLine.startColor = color;
        laserLine.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    public void ToggleVisuals(bool show)
    {
        if (laserLine != null)
            laserLine.enabled = show;

        if (hitDot != null && !show)
            hitDot.gameObject.SetActive(false);

        if (!show && NetworkToolSync.Instance != null)
            NetworkToolSync.Instance.SendPointerState(false, Vector3.zero, Vector3.zero,
                                                       false, Vector3.zero, Quaternion.identity);
    }
}
