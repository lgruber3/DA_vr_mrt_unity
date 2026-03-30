using UnityEngine;

public class LoadingParticleEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] 
    private ComputeShader computeShader;
    [SerializeField] 
    private Shader renderShader;

    [Header("Particle Settings")]
    [SerializeField] 
    private int particleCount = 1500;
    [SerializeField] 
    private float scatterRadius = 2.0f;
    [SerializeField] 
    private float particleSize = 0.015f;
    [SerializeField]
    private Vector3 targetSize = new Vector3(0.4f, 0.6f, 0.3f);

    [Header("Motion")]
    [SerializeField] 
    private float orbitSpeed = 0.3f;
    [SerializeField] 
    private float driftIntensity = 0.15f;

    [Header("Colors")]
    [SerializeField] 
    private Color colorStart = new Color(0.3f, 0.7f, 1f, 0.6f);
    [SerializeField] 
    private Color colorEnd = new Color(0.6f, 0.95f, 1f, 0.9f);

    [Header("Completion")]
    [SerializeField] 
    private float collapseDuration = 0.5f;

    public float Progress { get; set; }

    private ComputeBuffer _particleBuffer;
    private ComputeBuffer _argsBuffer;
    private Material _renderMaterial;
    private Mesh _quadMesh;
    private int _kernelID;
    private int _threadGroups;
    private Bounds _drawBounds;

    private float _collapseTimer = -1f;
    private bool _finished;

    struct Particle
    {
        public Vector3 scattered;
        public Vector3 assembled;
        public Vector3 drift;
        public Vector3 position;
        public Vector4 color;
        public float size;
    }

    void OnEnable()
    {
        Initialize();
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    void Update()
    {
        if (_finished || _particleBuffer == null) return;

        if (Progress >= 1f && _collapseTimer < 0f)
            _collapseTimer = 0f;

        if (_collapseTimer >= 0f)
        {
            _collapseTimer += Time.deltaTime;
            if (_collapseTimer >= collapseDuration)
            {
                _finished = true;
                ReleaseBuffers();
                gameObject.SetActive(false);
                return;
            }
        }

        DispatchCompute();
        DrawParticles();
    }

    public void Restart()
    {
        _finished = false;
        _collapseTimer = -1f;
        Progress = 0f;
        gameObject.SetActive(true);
        ReleaseBuffers();
        Initialize();
    }


    private void Initialize()
    {
        if (computeShader == null || renderShader == null)
        {
            Debug.LogError("[GPULoadingParticles] Missing compute or render shader reference!");
            return;
        }

        _kernelID = computeShader.FindKernel("CSUpdate");
        _threadGroups = Mathf.CeilToInt(particleCount / 256f);

        int stride = sizeof(float) * 17;
        _particleBuffer = new ComputeBuffer(particleCount, stride);

        var particles = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            particles[i].scattered = Random.onUnitSphere * scatterRadius
                                   + Random.insideUnitSphere * (scatterRadius * 0.3f);

            Vector3 rnd = Random.insideUnitSphere;
            particles[i].assembled = new Vector3(
                rnd.x * targetSize.x * 0.5f,
                rnd.y * targetSize.y * 0.5f,
                rnd.z * targetSize.z * 0.5f
            );

            particles[i].drift = new Vector3(
                Random.Range(0f, 100f),
                Random.Range(0f, 100f),
                Random.Range(0f, 100f)
            );

            particles[i].position = particles[i].scattered;
            particles[i].color = colorStart;
            particles[i].size = particleSize;
        }

        _particleBuffer.SetData(particles);

        _quadMesh = CreateQuadMesh();
        _argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] {
            (uint)_quadMesh.GetIndexCount(0),
            (uint)particleCount,
            0, 0, 0
        };
        _argsBuffer.SetData(args);

        _renderMaterial = new Material(renderShader);
        _renderMaterial.SetBuffer("_Particles", _particleBuffer);

        _drawBounds = new Bounds(transform.position, Vector3.one * scatterRadius * 3f);
    }

    private void DispatchCompute()
    {
        computeShader.SetBuffer(_kernelID, "_Particles", _particleBuffer);
        computeShader.SetFloat("_Progress", Mathf.Clamp01(Progress));
        computeShader.SetFloat("_Time", Time.time);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetFloat("_CollapseT",
            _collapseTimer >= 0f ? Mathf.Clamp01(_collapseTimer / collapseDuration) : 0f);
        computeShader.SetFloat("_DriftIntensity", driftIntensity);
        computeShader.SetFloat("_OrbitSpeed", orbitSpeed);
        computeShader.SetFloat("_ParticleSize", particleSize);
        computeShader.SetVector("_ColorStart", colorStart);
        computeShader.SetVector("_ColorEnd", colorEnd);
        computeShader.SetInt("_ParticleCount", particleCount);

        computeShader.Dispatch(_kernelID, _threadGroups, 1, 1);
    }

    private void DrawParticles()
    {
        _renderMaterial.SetBuffer("_Particles", _particleBuffer);
        _renderMaterial.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);

        _drawBounds.center = transform.position;

        Graphics.DrawMeshInstancedIndirect(
            _quadMesh,
            0,
            _renderMaterial,
            _drawBounds,
            _argsBuffer
        );
    }

    private void ReleaseBuffers()
    {
        _particleBuffer?.Release();
        _particleBuffer = null;

        _argsBuffer?.Release();
        _argsBuffer = null;

        if (_renderMaterial != null)
            Destroy(_renderMaterial);
    }

    private static Mesh CreateQuadMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3( 0.5f, -0.5f, 0),
            new Vector3( 0.5f,  0.5f, 0),
            new Vector3(-0.5f,  0.5f, 0),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
        return mesh;
    }
}