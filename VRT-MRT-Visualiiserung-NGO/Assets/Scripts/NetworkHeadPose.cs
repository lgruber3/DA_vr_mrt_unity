using Unity.Netcode;
using UnityEngine;

public class NetworkHeadPose : NetworkBehaviour
{
    public Transform head; 

    public NetworkVariable<Vector3> headPos =
        new(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<Quaternion> headRot =
        new(writePerm: NetworkVariableWritePermission.Owner);

    void LateUpdate()
    {
        if (!IsOwner) return;

        headPos.Value = head.position;
        headRot.Value = head.rotation;
    }
}
