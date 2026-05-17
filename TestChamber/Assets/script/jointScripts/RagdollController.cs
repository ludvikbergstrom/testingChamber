using UnityEngine;

public class RagdollController : MonoBehaviour
{
    //root rigidbody from which we look for all bones containting rigidbodies.
    public Rigidbody rootRigidbody;

    //animator component for toggling off when ragdoll is enabled 
    public Animator animator;

    // bool that toogles between ragdoll being enabled/disabled
    private bool _isRagdoll;

    public bool IsRagdoll
    {
        get => _isRagdoll;
        set
        {
            if (_isRagdoll == value) return;

            _isRagdoll = value;

            if (_isRagdoll)
            {
                EnableRagdoll();
            }
            else
            {
                DisableRagdoll();
            }
        }
    }

    void Start()
    {
        DisableRagdoll();
    }

    void EnableRagdoll()
    {
        //disable the animator so it doesnt interfer with the ragdoll
        animator.enabled = false;

        //set every Rigidbody to dynamic so they simulate physics for each bone
        foreach (Transform transform in rootRigidbody.transform)
        {
            if (transform.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false;
            }
        }
    }

    void DisableRagdoll()
    {

        //enable the animator so We can play animations
        animator.enabled = true;

        //set every Rigidbody to kinematic so that they dont interfear with the animation
        foreach (Transform transform in rootRigidbody.transform)
        {
            if (transform.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }
        }
    }

    
}
