using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast;

public class BlobFetcher : MonoBehaviour
{
    [Header("Visual Targets")]
    [SerializeField] 
    public GameObject scanObjectBoneVisuals;
    [SerializeField] 
    public GameObject scanObjectTissueVisuals;
    [SerializeField] 
    public BoxCollider scanObjectBoxCollider;
    [SerializeField] 
    public GameObject boundingBox;

    [Header("API")]
    [SerializeField] 
    private string apiEndpoint = "mrts/0/sas-urls";

    [Header("Loading Effects")]
    [SerializeField] 
    private LoadingParticleEffect boneParticles;
    [SerializeField] 
    private LoadingParticleEffect tissueParticles;

    [Header("Materials")]
    [SerializeField] 
    private Material boneMaterial;
    [SerializeField] 
    private Material tissueMaterial;

    private float _boneProgress;
    private float _tissueProgress;

    private GltfImport _boneGltf;
    private GltfImport _tissueGltf;

    public float TotalProgress => (_boneProgress + _tissueProgress) * 0.5f;

    public event Action OnFinishedLoading;

    private BlobFetcher() { }

    public static BlobFetcher Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple BlobFetcher instances detected. Destroying duplicate.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    [Serializable]
    private class MeetingApiResponse
    {
        public Meeting meeting;
    }

    [Serializable]
    private class SasUrlResponse
    {
        public string boneSasUrl;
        public string tissueSasUrl;
    }

    void Start()
    {
        var session = SessionManager.Instance;

        if (session == null)
        {
            Debug.LogError("SessionManager instance not found!");
            return;
        }

        if (session.CachedMeeting != null && session.CachedMeeting.id != 0)
        {
            apiEndpoint = $"mrts/{session.CachedMeeting.id}/sas-urls";
            Debug.Log("Using cached meeting data. Skipping network call.");
            StartCoroutine(LoadPipeline());
            SetRenderersVisible(scanObjectBoneVisuals, false);
            SetRenderersVisible(scanObjectTissueVisuals, false);

            session.CachedMeeting = null;
        }
        else
        {
            ApiClient.Instance.Get<MeetingApiResponse>(
                "meetings/current",
                (response) =>
                {
                    var meeting = response?.meeting;
                    if (meeting != null)
                    {
                        apiEndpoint = $"mrts/{meeting.id}/sas-urls";
                        Debug.Log($"Found ID: {meeting.id}");
                        StartCoroutine(LoadPipeline());
                        SetRenderersVisible(scanObjectBoneVisuals, false);
                        SetRenderersVisible(scanObjectTissueVisuals, false);
                    }
                },
                (error) => Debug.LogError(error)
            );
        }
    }

    private void OnDestroy()
    {
        _boneGltf?.Dispose();
        _tissueGltf?.Dispose();
    }

    public void LoadFromJson(string json)
    {
        StartCoroutine(LoadFromJsonCoroutine(json));
    }

    private void SetRenderersVisible(GameObject target, bool visible)
    {
        if (target == null) return;
        foreach (var r in target.GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }

    private IEnumerator LoadPipeline()
    {
        _boneProgress = 0f;
        _tissueProgress = 0f;

        SasUrlResponse urls = null;
        bool done = false;

        ApiClient.Instance.Get<SasUrlResponse>(apiEndpoint,
            result => { urls = result; done = true; },
            err    => { Debug.LogError($"[BlobFetcher] API request failed: {err}"); done = true; }
        );

        while (!done) yield return null;

        if (urls == null)
        {
            Debug.LogError("[BlobFetcher] Failed to fetch SAS URLs from API.");
            yield break;
        }

        yield return StartCoroutine(ProcessUrls(urls));
    }

    private IEnumerator LoadFromJsonCoroutine(string json)
    {
        _boneProgress = 0f;
        _tissueProgress = 0f;
        SasUrlResponse urls = JsonUtility.FromJson<SasUrlResponse>(json);
        yield return StartCoroutine(ProcessUrls(urls));
    }

    private IEnumerator ProcessUrls(SasUrlResponse urls)
    {
        if (string.IsNullOrEmpty(urls.boneSasUrl) || string.IsNullOrEmpty(urls.tissueSasUrl))
        {
            Debug.LogError("[BlobFetcher] JSON is missing boneSasUrl or tissueSasUrl.");
            yield break;
        }

        Debug.Log("[BlobFetcher] Starting parallel download...");

        byte[] boneBytes   = null;
        byte[] tissueBytes = null;
        bool boneDone   = false;
        bool tissueDone = false;

        StartCoroutine(DownloadWithProgress(
            urls.boneSasUrl,
            p      => _boneProgress = p,
            result => { boneBytes = result; boneDone = true; }
        ));

        StartCoroutine(DownloadWithProgress(
            urls.tissueSasUrl,
            p      => _tissueProgress = p,
            result => { tissueBytes = result; tissueDone = true; }
        ));

        while (!boneDone || !tissueDone)
        {
            if (boneParticles   != null) boneParticles.Progress   = _boneProgress;
            if (tissueParticles != null) tissueParticles.Progress = _tissueProgress;
            yield return null;
        }

        if (boneParticles   != null) boneParticles.Progress   = 1f;
        if (tissueParticles != null) tissueParticles.Progress = 1f;

        Debug.Log("[BlobFetcher] Loading GLB meshes...");

        Mesh boneMesh   = null;
        Mesh tissueMesh = null;
        bool boneParsed   = false;
        bool tissueParsed = false;

        if (boneBytes != null)
        {
            LoadGlbAsync(boneBytes, "BoneMesh", (mesh, gltf) =>
            {
                boneMesh   = mesh;
                _boneGltf  = gltf;
                boneParsed = true;
            });
            boneBytes = null; 
        }
        else
        {
            boneParsed = true;
            Debug.LogError("[BlobFetcher] Bone download failed.");
        }

        if (tissueBytes != null)
        {
            LoadGlbAsync(tissueBytes, "TissueMesh", (mesh, gltf) =>
            {
                tissueMesh   = mesh;
                _tissueGltf  = gltf;
                tissueParsed = true;
            });
            tissueBytes = null;
        }
        else
        {
            tissueParsed = true;
            Debug.LogError("[BlobFetcher] Tissue download failed.");
        }

        while (!boneParsed || !tissueParsed)
            yield return null;


        Debug.Log("[BlobFetcher] Applying meshes...");

        if (boneMesh != null)
        {
            ApplyMesh(scanObjectBoneVisuals, boneMesh, boneMaterial);
            Debug.Log($"[BlobFetcher] Bone mesh applied — {boneMesh.vertexCount:N0} verts.");
        }

        if (tissueMesh != null)
        {
            ApplyMesh(scanObjectTissueVisuals, tissueMesh, tissueMaterial);
            Debug.Log($"[BlobFetcher] Tissue mesh applied — {tissueMesh.vertexCount:N0} verts.");
        }
    }

    private async void LoadGlbAsync(byte[] data, string label, Action<Mesh, GltfImport> onComplete)
{
    GltfImport gltf = null;
    try
    {
        var deferAgent = gameObject.GetComponent<TimeBudgetPerFrameDeferAgent>();
        if (deferAgent == null)
        {
            deferAgent = gameObject.AddComponent<TimeBudgetPerFrameDeferAgent>();
            deferAgent.SetFrameBudget(0.005f); 
        }
        
        gltf = new GltfImport(null, deferAgent);
        
        bool success = await gltf.LoadGltfBinary(data);

        if (!success)
        {
            Debug.LogError($"[BlobFetcher] GLTFast failed to parse {label}.");
            gltf.Dispose();
            onComplete?.Invoke(null, null);
            return;
        }

        var meshes = gltf.GetMeshes();
        Mesh mesh = (meshes != null && meshes.Length > 0) ? meshes[0] : null;

        if (mesh != null)
        {
            mesh.name = label;
        }
        else
        {
            Debug.LogWarning($"[BlobFetcher] No mesh found in {label} GLB.");
        }

        onComplete?.Invoke(mesh, gltf);
    }
    catch (Exception e)
    {
        Debug.LogError($"[BlobFetcher] GLB load error ({label}): {e.Message}\n{e.StackTrace}");
        gltf?.Dispose();
        onComplete?.Invoke(null, null);
    }
}


    private IEnumerator DownloadWithProgress(string url, Action<float> onProgress, Action<byte[]> onComplete)
    {
        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            uwr.timeout = 120;
            var op = uwr.SendWebRequest();

            while (!op.isDone)
            {
                onProgress?.Invoke(uwr.downloadProgress);
                yield return null;
            }

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[BlobFetcher] Download failed: {uwr.error}");
                onProgress?.Invoke(0f);
                onComplete?.Invoke(null);
            }
            else
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke(uwr.downloadHandler.data);
            }
        }
    }


    private void ApplyMesh(GameObject target, Mesh mesh, Material mat)
    {
        if (target == null || mesh == null) return;

        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf == null) mf = target.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = target.GetComponent<MeshRenderer>();
        if (mr == null) mr = target.AddComponent<MeshRenderer>();

        if (mat != null)
            mr.sharedMaterial = mat;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        SetRenderersVisible(target, true);
        scanObjectBoxCollider.center = mesh.bounds.center;
        scanObjectBoxCollider.size = mesh.bounds.size * 0.7f;
        boundingBox.transform.localPosition = mesh.bounds.center;
        boundingBox.transform.localScale = mesh.bounds.size * 0.7f;

        OnFinishedLoading?.Invoke();
    }
}