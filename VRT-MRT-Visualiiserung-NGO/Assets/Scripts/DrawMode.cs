using UnityEngine;
using System.Collections.Generic;

public class VRDrawer : MonoBehaviour
{
    [Header("Settings")]
    public GameObject linePrefab;
    public float minDistance = 0.01f;

    [Header("Input Source")]
    public Transform brushTip;

    private LineRenderer _currentLine;
    private List<Vector3> _points = new List<Vector3>();
    private int _currentStrokeId = -1;

    public void StartDrawing()
    {
        GameObject lineObj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        _currentLine = lineObj.GetComponent<LineRenderer>();
        _points.Clear();

        if (NetworkToolSync.Instance != null)
            _currentStrokeId = NetworkToolSync.Instance.StartStroke();
        else
            _currentStrokeId = -1;

        AddPoint(brushTip.position);
    }

    public void UpdateDrawing()
    {
        if (_currentLine == null) return;

        float distance = Vector3.Distance(_points[_points.Count - 1], brushTip.position);
        if (distance > minDistance)
        {
            AddPoint(brushTip.position);

            if (NetworkToolSync.Instance != null && _currentStrokeId >= 0)
                NetworkToolSync.Instance.AddStrokePoint(_currentStrokeId, brushTip.position);
        }
    }

    public void StopDrawing()
    {
        if (_currentLine == null) return;

        _currentLine.gameObject.tag = "Drawing";

        if (_currentStrokeId >= 0)
        {
            var idComp = _currentLine.gameObject.AddComponent<DrawingStrokeId>();
            idComp.StrokeId = _currentStrokeId;
        }

        Mesh mesh = new Mesh();
        _currentLine.BakeMesh(mesh, true);

        MeshCollider mc = _currentLine.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        if (NetworkToolSync.Instance != null && _currentStrokeId >= 0)
            NetworkToolSync.Instance.FinalizeStroke(_currentStrokeId, _points.ToArray());

        _currentLine = null;
        _currentStrokeId = -1;
    }

    private void AddPoint(Vector3 position)
    {
        _points.Add(position);
        _currentLine.positionCount = _points.Count;
        _currentLine.SetPosition(_points.Count - 1, position);
    }
}
