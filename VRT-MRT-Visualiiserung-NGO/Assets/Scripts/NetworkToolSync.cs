using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkToolSync : NetworkBehaviour
{
    public static NetworkToolSync Instance { get; private set; }

    [Header("Drawing")]
    [Tooltip("Same prefab used by VRDrawer – must have a LineRenderer component.")]
    public GameObject linePrefab;

    [Header("Remote Pointer Visuals (client-side display of host pointer)")]
    public LineRenderer remotePointerLine;
    public Transform remoteHitDot;
    public Color idleColor = new Color(0.35f, 0.75f, 1f,  1f);
    public Color hitColor = new Color(1f, 0.35f, 0.2f, 1f);

    private readonly Dictionary<int, (LineRenderer lr, List<Vector3> pts)> _remoteStrokes = new();

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (remotePointerLine != null) 
            remotePointerLine.enabled = false;
        if (remoteHitDot != null)
        {
             Renderer hitRenderer = remoteHitDot.GetComponent<Renderer>();
            if (hitRenderer != null)
            {
                Material hitMaterial = new Material(hitRenderer.material) { color = hitColor };
                hitRenderer.material = hitMaterial;
            }

            remoteHitDot.gameObject.SetActive(false);
        }
    }

     public void SendPointerState(bool active, Vector3 start, Vector3 end,
                                 bool hasHit, Vector3 hitPoint, Quaternion hitRot)
    {
        if (!IsHost) return;
        UpdatePointerClientRpc(active, start, end, hasHit, hitPoint, hitRot);
    }

    [ClientRpc]
    private void UpdatePointerClientRpc(bool active, Vector3 start, Vector3 end,
                                        bool hasHit, Vector3 hitPoint, Quaternion hitRot)
    {
        if (IsHost) return; 

        if (remotePointerLine != null)
        {
            remotePointerLine.enabled = active;
            if (active)
            {
                Color c = hasHit ? hitColor : idleColor;
                remotePointerLine.SetPosition(0, start);
                remotePointerLine.SetPosition(1, end);
                remotePointerLine.startColor = c;
                remotePointerLine.endColor   = new Color(c.r, c.g, c.b, 0f);
            }
        }

        if (remoteHitDot != null)
        {
            remoteHitDot.gameObject.SetActive(active && hasHit);
            if (active && hasHit)
            {
                remoteHitDot.position = hitPoint;
                remoteHitDot.rotation = hitRot;
            }
        }
    }

    public int StartStroke()
    {
        if (!IsHost) return -1;
        int id = NetworkManager.Singleton.LocalClientId.GetHashCode() ^ (int)(Time.realtimeSinceStartup * 1000);
        StartStrokeClientRpc(id);
        return id;
    }

    [ClientRpc]
    private void StartStrokeClientRpc(int strokeId)
    {
        if (IsHost) return;
        if (linePrefab == null) return;

        GameObject obj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        var lr = obj.GetComponent<LineRenderer>();
        _remoteStrokes[strokeId] = (lr, new List<Vector3>());
    }

    public void AddStrokePoint(int strokeId, Vector3 point)
    {
        if (!IsHost) return;
        AddStrokePointClientRpc(strokeId, point);
    }

    [ClientRpc]
    private void AddStrokePointClientRpc(int strokeId, Vector3 point)
    {
        if (IsHost) return;
        if (!_remoteStrokes.TryGetValue(strokeId, out var stroke)) return;

        stroke.pts.Add(point);
        stroke.lr.positionCount = stroke.pts.Count;
        stroke.lr.SetPosition(stroke.pts.Count - 1, point);
    }

    public void FinalizeStroke(int strokeId, Vector3[] allPoints)
    {
        if (!IsHost) return;
        FinalizeStrokeClientRpc(strokeId, allPoints);
    }

    [ClientRpc]
    private void FinalizeStrokeClientRpc(int strokeId, Vector3[] allPoints)
    {
        if (IsHost) return;
        if (!_remoteStrokes.TryGetValue(strokeId, out var stroke)) return;

        stroke.lr.positionCount = allPoints.Length;
        stroke.lr.SetPositions(allPoints);
        stroke.lr.gameObject.tag = "Drawing";

        var idComp = stroke.lr.gameObject.AddComponent<DrawingStrokeId>();
        idComp.StrokeId = strokeId;

        Mesh mesh = new Mesh();
        stroke.lr.BakeMesh(mesh, true);
        var mc = stroke.lr.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        _remoteStrokes.Remove(strokeId);
    }

    public void EraseStroke(int strokeId)
    {
        if (!IsHost) return;
        EraseStrokeClientRpc(strokeId);
    }

    [ClientRpc]
    private void EraseStrokeClientRpc(int strokeId)
    {
        if (IsHost) return;

        foreach (var go in GameObject.FindGameObjectsWithTag("Drawing"))
        {
            var idComp = go.GetComponent<DrawingStrokeId>();
            if (idComp != null && idComp.StrokeId == strokeId)
            {
                Destroy(go);
                return;
            }
        }
    }
}
