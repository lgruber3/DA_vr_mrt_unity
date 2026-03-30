using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LoadingShaderController : MonoBehaviour
{
    [Header("References")]
    public Shader loadingShader;
    public Renderer[] targetRenderers;

    [Header("Transition")]
    [Range(0.2f, 3f)]
    public float fadeDuration = 1.0f;

    [Header("Tweaks")]
    public Color baseColor = new Color(0.2f, 0.6f, 1f, 0.3f);
    public Color rimColor  = new Color(0.3f, 0.8f, 1f, 1f);

    private Dictionary<Renderer, Material[]> _origMaterials = new Dictionary<Renderer, Material[]>();
    private bool _isLoading;

    void Awake()
    {
        if (loadingShader == null)
            loadingShader = Shader.Find("Custom/LoadingHologram");

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();
    }

    public void ShowLoading()
    {
        if (_isLoading) 
            return;
        _isLoading = true;
        _origMaterials.Clear();

        foreach (var r in targetRenderers)
        {
            if (r == null) continue;

            _origMaterials[r] = r.sharedMaterials;

            var holoMats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < holoMats.Length; i++)
            {
                holoMats[i] = new Material(loadingShader);
                holoMats[i].SetColor("_Color", baseColor);
                holoMats[i].SetColor("_RimColor", rimColor);
                holoMats[i].renderQueue = 3000;
            }
            r.materials = holoMats;
        }
    }

    public void HideLoading()
    {
        if (!_isLoading) 
            return;
        RestoreOriginals();
        _isLoading = false;
    }

    public void FadeToReal()
    {
        if (!_isLoading) 
            return;
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float t = 0f;
        var holoSnap = new Dictionary<Renderer, Material[]>();
        foreach (var r in targetRenderers)
        {
            if (r == null) 
                continue;
            holoSnap[r] = r.materials;
        }

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(t / fadeDuration);

            foreach (var kvp in holoSnap)
            {
                foreach (var m in kvp.Value)
                {
                    Color c = m.GetColor("_Color");
                    c.a = baseColor.a * alpha;
                    m.SetColor("_Color", c);
                    m.SetFloat("_PulseMax", 0.45f * alpha);
                }
            }
            yield return null;
        }

        RestoreOriginals();
        _isLoading = false;

        foreach (var kvp in holoSnap)
            foreach (var m in kvp.Value)
                Destroy(m);
    }

    private void RestoreOriginals()
    {
        foreach (var kvp in _origMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.sharedMaterials = kvp.Value;
        }
    }
}