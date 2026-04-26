using UnityEngine;

public class clickerDestruction : clicker
{
    public override void ClickFunction(RaycastHit hit)
    {
        if (hit.transform.CompareTag("Destructable"))
        {
            RootDestruction rootDestructionScript = hit.transform.GetComponent<RootDestruction>();

            Destroy(hit.collider.gameObject);
            Debug.Log(hit.collider.gameObject == null);

            if (rootDestructionScript != null)
            {
                rootDestructionScript.EvaluateStructure();
            }

        }
    }
}
