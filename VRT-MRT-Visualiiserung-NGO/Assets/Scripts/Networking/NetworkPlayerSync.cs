using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSync : NetworkBehaviour
{
    [Header("References to Prefab Parts")]
    public Transform networkHead;
    public Transform networkLeftHand;
    public Transform networkRightHand;

    private Transform vrHead;
    private Transform vrLeftHand;
    private Transform vrRightHand;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            
            vrHead = rig.centerEyeAnchor;
            vrLeftHand = rig.leftHandAnchor;
            vrRightHand = rig.rightHandAnchor;

            SetLayerRecursively(networkHead.gameObject, 10); 
        }
    }

    void Update()
    {
        if (IsOwner && vrHead != null)
        {
            networkHead.position = vrHead.position;
            networkHead.rotation = vrHead.rotation;

            networkLeftHand.position = vrLeftHand.position;
            networkLeftHand.rotation = vrLeftHand.rotation;

            networkRightHand.position = vrRightHand.position;
            networkRightHand.rotation = vrRightHand.rotation;
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }
}