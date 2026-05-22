using UnityEngine;
using UnityEngine.Animations.Rigging;

public class BalanceSolver : MonoBehaviour
{
    //ragdollController which we check to see if we should balance or not?
    public RagdollController ragdollController;

    //root rigidbody from which we look for all bones containting rigidbodies.
    public Rigidbody rootRigidbody;

    //target right Leg
    public GameObject targetRightLeg;

    [Header("Joints")]
    public ConfigurableJoint thighJointR;
    public ConfigurableJoint thighJointL;
    public ConfigurableJoint ShinJointR;
    public ConfigurableJoint ShinJointL;







    private void Update()
    {
        //we dont strive for balance when characther isnt ragdolling
        if (ragdollController.IsRagdoll == false) return;



    }


}
