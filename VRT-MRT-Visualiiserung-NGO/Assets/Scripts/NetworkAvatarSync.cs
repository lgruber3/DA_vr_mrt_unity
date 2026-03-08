using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode.Components;

public class NetworkAvatarSync : NetworkBehaviour
{
    [Header("IK Targets (Children of the Prefab)")]
    public Transform ikHeadTarget;
    public Transform ikLeftHandTarget;
    public Transform ikRightHandTarget;

    [Header("Offsets")]
    public Vector3 headPositionOffset;
    public Vector3 headRotationOffset;
    public Vector3 leftHandRotationOffset;
    public Vector3 rightHandRotationOffset; 

    [Header("VR Rig Transforms")]
    private Transform vrHead;
    private Transform vrLeftHand;
    private Transform vrRightHand;

    public List<SkinnedMeshRenderer> avatarMeshRenderers;

    [Header("Movement Detection")]
    public float walkSpeedThreshold = 0.5f;

    private Transform vrRoot;
    private Vector3 lastRootPosition;
    private float smoothedSpeed;

    [Header("Animator")]
    public Animator animator;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            
            vrHead = rig.centerEyeAnchor;
            vrLeftHand = rig.leftHandAnchor;
            vrRightHand = rig.rightHandAnchor;

            var headConstraint = ikHeadTarget.GetComponent<MultiParentConstraint>();
            if (headConstraint != null && headConstraint.data.constrainedObject != null)
            {
                SetLayerRecursively(headConstraint.data.constrainedObject.gameObject, 10); 
            }

            if (avatarMeshRenderers != null)
            {
                foreach (var renderer in avatarMeshRenderers)
                {
                   renderer.enabled = false;
                }
            }

            vrRoot = rig.transform;
            lastRootPosition = vrRoot.position;
        }
    }

    void Update()
    {
        if (!IsOwner || vrHead == null) return;

        Vector3 headPos = vrHead.position;
        transform.position = new Vector3(
            headPos.x,
            transform.position.y, 
            headPos.z
        );

        Vector3 euler = vrHead.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        ikHeadTarget.position = vrHead.TransformPoint(headPositionOffset);
        ikHeadTarget.rotation = vrHead.rotation * Quaternion.Euler(headRotationOffset);

        ikLeftHandTarget.SetPositionAndRotation(vrLeftHand.position, vrLeftHand.rotation);
        ikRightHandTarget.SetPositionAndRotation(vrRightHand.position, vrRightHand.rotation);

        ikLeftHandTarget.rotation =
        vrLeftHand.rotation * Quaternion.Euler(leftHandRotationOffset);

        ikRightHandTarget.rotation =
        vrRightHand.rotation * Quaternion.Euler(rightHandRotationOffset);

        Vector3 delta = vrRoot.position - lastRootPosition;
        delta.y = 0f;
        float speed = delta.magnitude / Time.deltaTime;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 10f * Time.deltaTime);

        bool isWalking = smoothedSpeed > walkSpeedThreshold;
        animator.SetBool("isWalking", isWalking);
        lastRootPosition = vrRoot.position;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }
}