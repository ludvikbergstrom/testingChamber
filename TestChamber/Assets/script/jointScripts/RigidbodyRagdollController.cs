using UnityEngine;

public class RigidbodyRagdollController : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root.TryGetComponent<RagdollController>(out RagdollController ragdollController))
        {
            ragdollController.IsRagdoll = true;
        }
    }
}
