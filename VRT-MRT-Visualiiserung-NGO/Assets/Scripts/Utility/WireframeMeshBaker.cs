using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
public class WireframeMeshBaker : MonoBehaviour
{
    public bool forceBake = false;

    void Awake()
    {
        Bake();
    }

    public void Bake()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) 
            return;

        Mesh src = mf.sharedMesh;
        Mesh mesh = Instantiate(src);
        mesh.name = src.name + "_wireframeBaked";
        mf.mesh = mesh;

        Vector3[] srcVerts = mesh.vertices;
        Vector3[] srcNorms = mesh.normals;
        Vector2[] srcUV = mesh.uv;
        int[] srcTris = mesh.triangles;
        int triCount = srcTris.Length / 3;

        int vertCount = triCount * 3;
        Vector3[] verts = new Vector3[vertCount];
        Vector3[] norms = new Vector3[vertCount];
        Vector2[] uv = new Vector2[vertCount];
        int[] tris = new int[vertCount];

        Vector3[] baryCoords = new Vector3[vertCount];

        Vector4[] edgeDir0 = new Vector4[vertCount]; 
        Vector4[] edgeDir1 = new Vector4[vertCount]; 
        Vector4[] edgeDir2 = new Vector4[vertCount];

    
        Vector3[] edgeT = new Vector3[vertCount];
        Vector3[] edgeLens = new Vector3[vertCount]; 

        Matrix4x4 localToWorld = transform.localToWorldMatrix;

        for (int t = 0; t < triCount; t++)
        {
            int i0 = srcTris[t * 3 + 0];
            int i1 = srcTris[t * 3 + 1];
            int i2 = srcTris[t * 3 + 2];

            int o0 = t * 3 + 0;
            int o1 = t * 3 + 1;
            int o2 = t * 3 + 2;

            verts[o0] = srcVerts[i0]; verts[o1] = srcVerts[i1]; verts[o2] = srcVerts[i2];

            if (srcNorms != null && srcNorms.Length > 0)
            {
                norms[o0] = srcNorms[i0]; norms[o1] = srcNorms[i1]; norms[o2] = srcNorms[i2];
            }
            if (srcUV != null && srcUV.Length > 0)
            {
                uv[o0] = srcUV[i0]; uv[o1] = srcUV[i1]; uv[o2] = srcUV[i2];
            }

            tris[o0] = o0; tris[o1] = o1; tris[o2] = o2;
 
            baryCoords[o0] = new Vector3(1, 0, 0);
            baryCoords[o1] = new Vector3(0, 1, 0);
            baryCoords[o2] = new Vector3(0, 0, 1);

            Vector3 w0 = localToWorld.MultiplyPoint3x4(srcVerts[i0]);
            Vector3 w1 = localToWorld.MultiplyPoint3x4(srcVerts[i1]);
            Vector3 w2 = localToWorld.MultiplyPoint3x4(srcVerts[i2]);

            Vector3 e0dir = (w2 - w1).normalized; 
            Vector3 e1dir = (w2 - w0).normalized; 
            Vector3 e2dir = (w1 - w0).normalized; 

            float len0 = (w2 - w1).magnitude;
            float len1 = (w2 - w0).magnitude;
            float len2 = (w1 - w0).magnitude;

            Vector4 d0 = new Vector4(e0dir.x, e0dir.y, e0dir.z, 0);
            Vector4 d1 = new Vector4(e1dir.x, e1dir.y, e1dir.z, 0);
            Vector4 d2 = new Vector4(e2dir.x, e2dir.y, e2dir.z, 0);

            for (int v = 0; v < 3; v++)
            {
                int ov = t * 3 + v;
                edgeDir0[ov] = d0;
                edgeDir1[ov] = d1;
                edgeDir2[ov] = d2;
                edgeLens[ov] = new Vector3(len0, len1, len2);
            }

            float t0e0 = len0 > 0.0001f ? Mathf.Clamp01(Vector3.Dot(w0 - w1, e0dir) / len0) : 0.5f;
            edgeT[o0] = new Vector3(t0e0, 0f, 0f);

            float t1e1 = len1 > 0.0001f ? Mathf.Clamp01(Vector3.Dot(w1 - w0, e1dir) / len1) : 0.5f;
            edgeT[o1] = new Vector3(0f, t1e1, 1f);

            float t2e2 = len2 > 0.0001f ? Mathf.Clamp01(Vector3.Dot(w2 - w0, e2dir) / len2) : 0.5f;
            edgeT[o2] = new Vector3(1f, 1f, t2e2);
        }

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uv;
        mesh.triangles = tris;

        mesh.SetUVs(2, baryCoords);       
        mesh.SetUVs(3, edgeDir0);            
        mesh.SetUVs(4, edgeDir1);           
        mesh.SetUVs(5, edgeDir2);          
        mesh.SetUVs(6, edgeT);              
        mesh.SetUVs(7, edgeLens);           

        mesh.RecalculateBounds();

        Debug.Log($"[WireframeMeshBaker] Baked {mesh.name}: {triCount} tris → {vertCount} verts");
    }
}
