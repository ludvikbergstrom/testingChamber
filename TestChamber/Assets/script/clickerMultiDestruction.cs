using System;
using System.Collections.Generic;
using UnityEngine;

public class clickerMultiDestruction : clicker
{
    Dictionary<Transform, List<Transform>> parentsAndNodes;

    List<Transform> nodesToDestroy;

    public float destructiveSphereRadius = 1f;

    public override void ClickFunction(RaycastHit hit)
    {
        //does a spherecast which will hit all sorrounding gameobject
        Vector3 center = hit.point;
        float radius = destructiveSphereRadius;

        Collider[] hits = Physics.OverlapSphere(
            center,
            radius
        );

        //a dictionary with root parent and all its destructable children as values
        parentsAndNodes = new Dictionary<Transform, List<Transform>>();

        // foreach that assigns each node to its respective parent
        foreach (Collider node in hits)
        {
            if (!node.transform.CompareTag("Destructable")) continue;

            node.transform.GetComponent<Collider>().enabled = false;

            if (!parentsAndNodes.ContainsKey(node.transform.root))
            {
                parentsAndNodes[node.transform.root] = new List<Transform>() { node.transform };
            }
            else
            {
                parentsAndNodes[node.transform.root].Add(node.transform);
            }
        }

        //if we have hit no destructables we return
        if (parentsAndNodes.Count == 0) return;

        //goes through each parent and calls eveluate structure with all nodes that are to be destroyed
        foreach (KeyValuePair<Transform, List<Transform>> parentNode in parentsAndNodes)
        {
            RootDestruction rootDestructionScript = parentNode.Key.GetComponent<RootDestruction>();

            nodesToDestroy = parentNode.Value;

            if (rootDestructionScript != null)
            {
                rootDestructionScript.EvaluateStructure(nodesToDestroy);
            }
        }
        
    }

    public override void ClickFunctionVoid(Vector3 position)
    {
        throw new NotImplementedException();
    }
}
