using System.Collections.Generic;
using UnityEngine;

public static class TransformUtils
{

    public static void CreateParentsAndReparent(List<List<Transform>> transformGroups)
    {
        /// <summary>
        /// A method that takes a list of components - cluster of nodes/list of tranforms
        /// And reparents them.
        /// <summary>

        foreach (List<Transform> tranformsToReparent in transformGroups)
        {
            //new parent
            GameObject parent = new GameObject();

            foreach (Transform node in tranformsToReparent)
            {
                node.SetParent(parent.transform);
                node.tag = "Destructable";
            }
            parent.AddComponent<Rigidbody>();
            parent.AddComponent<RootDestruction>();
            parent.tag = "Destructable";
           
        }

    }

}

