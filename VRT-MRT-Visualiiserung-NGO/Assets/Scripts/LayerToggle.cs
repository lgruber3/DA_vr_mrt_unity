using Unity.Netcode;
using UnityEngine;

public enum LayerViewMode { Bone, Tissue, All }

public class LayerToggle : NetworkBehaviour
{
    public GameObject boneLayer;
    public GameObject tissueLayer;
    public LayerViewMode defaultLayer = LayerViewMode.All;

    public override void OnNetworkSpawn()
    {
        ApplyLayer((int)defaultLayer);
    }

    public void SetBoneLayer()   => ChangeLayer(0);
    public void SetTissueLayer() => ChangeLayer(1);
    public void SetAll()         => ChangeLayer(2);

    private void ChangeLayer(int index)
    {
        ApplyLayer(index);
        if (IsHost) ApplyLayerClientRpc(index);
    }

    [ClientRpc]
    private void ApplyLayerClientRpc(int index)
    {
        if (IsHost) return;
        ApplyLayer(index);
    }

    private void ApplyLayer(int index)
    {
        switch (index)
        {
            case 0:
                boneLayer.SetActive(true);
                tissueLayer.SetActive(false);
                break;
            case 1:
                boneLayer.SetActive(false);
                tissueLayer.SetActive(true);
                break;
            case 2:
                boneLayer.SetActive(true);
                tissueLayer.SetActive(true);
                break;
        }
    }
}
