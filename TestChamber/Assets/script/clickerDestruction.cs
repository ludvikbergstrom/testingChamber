using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;

public class clickerDestruction : clicker
{
    List<Transform> nodesToDestroy;

    public override void ClickFunction(RaycastHit hit)
    {
        nodesToDestroy = new List<Transform>();

        if (hit.transform.CompareTag("Destructable"))
        {
            RootDestruction rootDestructionScript = hit.transform.GetComponent<RootDestruction>();

            hit.collider.transform.GetComponent<Collider>().enabled = false;

            nodesToDestroy.Add(hit.collider.transform);

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
