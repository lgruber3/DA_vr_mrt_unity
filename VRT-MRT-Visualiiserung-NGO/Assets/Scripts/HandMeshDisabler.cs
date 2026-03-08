using UnityEngine;
using Unity.Netcode;

public class HandMeshDisabler : NetworkBehaviour
{
    [SerializeField] private Renderer[] handRenderers;

    public override void OnNetworkSpawn()
    {
        ApplyVisibility();
    }

    public override void OnGainedOwnership()
    {
        ApplyVisibility();
    }

    public override void OnLostOwnership()
    {
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        bool shouldRender = !IsOwner;

        foreach (var r in handRenderers)
        {
            if (r != null)
                r.enabled = shouldRender;
        }
    }
}
