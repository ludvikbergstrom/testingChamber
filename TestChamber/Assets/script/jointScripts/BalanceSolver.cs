using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
public class BalanceSolver : MonoBehaviour
{
    //ragdollController which we check to see if we should balance or not?
    public RagdollController ragdollController;

    //root rigidbody from which we look for all bones containting rigidbodies.
    public Rigidbody rootRigidbody;

    //target right Leg
    public GameObject targetRightLeg;

    [Header("Joints")]
    public ConfigurableJoint ThighJointR;
    public ConfigurableJoint thighJointL;
    public ConfigurableJoint ShinJointR;
    public ConfigurableJoint ShinJointL;
    public Transform FootR;
    public Transform FootL;



    List<Vector3> limbRotationList = new List<Vector3>();
    List<Vector3> ikRotationList;

    List<ConfigurableJoint> rightLegJoints;

    


    private void Update()
    {

    }

    void AssignRotationTarget(ConfigurableJoint joint,Vector3 newDirection)
    {
        Quaternion desiredWorldRot =
    Quaternion.LookRotation(newDirection);

        Quaternion desiredLocalRot =
            Quaternion.Inverse(transform.parent.rotation)
            * desiredWorldRot;

        joint.targetRotation =
            Quaternion.Inverse(desiredLocalRot)
            * joint.transform.localRotation;
    }
}
