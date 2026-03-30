using Unity.Netcode;
using UnityEngine;

public enum MedicalViewMode { Density, Clinical, SurfaceCurvature }

public class MedicalShaderToggle : NetworkBehaviour
{
    [Header("Bone Materials")]
    public Material boneSurfaceCurvatureMat;
    public Material boneClinicalMat;
    public Material boneDensityMat;

    [Header("Tissue Materials")]
    public Material tissueSurfaceCurvatureMat;
    public Material tissueClinicalMat;
    public Material tissueDensityMat;

    public MedicalViewMode defaultView = MedicalViewMode.Clinical;

    public Renderer _boneRenderer;
    public Renderer _tissueRenderer;

    void Start()
    {
        BlobFetcher.Instance.OnFinishedLoading += () =>
        {
            _boneRenderer = BlobFetcher.Instance.scanObjectBoneVisuals.GetComponent<Renderer>();
            _tissueRenderer = BlobFetcher.Instance.scanObjectTissueVisuals.GetComponent<Renderer>();
             ApplyMaterial((int)defaultView);
        };
    }

    public override void OnNetworkSpawn()
    {
        ApplyMaterial((int)defaultView);
    }

    public void SelectDensity()          
        => ChangeMaterial(0);
    public void SelectClinical()         
        => ChangeMaterial(1);
    public void SelectSurfaceCurvature() 
        => ChangeMaterial(2);

    private void ChangeMaterial(int index)
    {
        ApplyMaterial(index);
        if (IsHost) 
            ApplyMaterialClientRpc(index);
    }

    [ClientRpc]
    private void ApplyMaterialClientRpc(int index)
    {
        if (IsHost) 
            return;
        ApplyMaterial(index);
    }

    private void ApplyMaterial(int index)
    {
        if (_boneRenderer == null)
        {
            var bone = AssetManager.Instance?.GetBoneAsset();
            if (bone != null) 
                _boneRenderer = bone.GetComponent<Renderer>();
        }
        if (_tissueRenderer == null)
        {
            var tissue = AssetManager.Instance?.GetTissueAsset();
            if (tissue != null) 
                _tissueRenderer = tissue.GetComponent<Renderer>();
        }

        switch (index)
        {
            case 0:
                if (_boneRenderer)   
                    _boneRenderer.material   = boneDensityMat;
                if (_tissueRenderer) 
                    _tissueRenderer.material  = tissueDensityMat;
                break;
            case 1:
                if (_boneRenderer)   
                    _boneRenderer.material   = boneClinicalMat;
                if (_tissueRenderer) 
                    _tissueRenderer.material  = tissueClinicalMat;
                break;
            case 2:
                if (_boneRenderer)   
                    _boneRenderer.material   = boneSurfaceCurvatureMat;
                if (_tissueRenderer) 
                    _tissueRenderer.material  = tissueSurfaceCurvatureMat;
                break;
        }
    }
}
