using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RootDestruction : MonoBehaviour
{

    //Stores nodes(blockSegment) and their connected nodes. 
    Dictionary<Transform, List<Transform>> nodeTree = new Dictionary<Transform, List<Transform>>();


    HashSet<Transform> visited;
    List<List<Transform>> components;

    List<List<Transform>> newComponents;


    Rigidbody rb;

    void Start()
    {
        //get the rigidbody component
        rb = GetComponent<Rigidbody>();

        //set the mass of the rigidbody component using number of child nodes
        rb.mass = transform.childCount;

        //!!! THIS METHOD HAS PROBLEMS CREATING CONNECTIONS BETWEENS NODES WHERE IT SHOULDNT
        // THIS IS DUE TO THE OVERLAPBOX BEING TO BIG !!!

        //runs through every node and looks for connections
        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf) continue;

            //temporary list for current elements connected nodes
            List<Transform> connectedNodes = new List<Transform>();

            //attain the boxcollider of the current node/element and store its dimensions
            BoxCollider childBoxCollider = child.GetComponent<BoxCollider>();
            Vector3 center = child.position;
            Vector3 halfExtents = childBoxCollider.bounds.extents * 1.1f;

            //Use the dimensions of the hitbox to boxcast finding immediate nodes
            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                child.rotation
            );

            //nodes are added to connected nodes unless theyre the current node/element
            foreach (Collider col in hits)
            {
                if (col.transform == child || col.transform == null) continue;

                //only connect destructable transforms
                if (col.transform.root == transform)
                    connectedNodes.Add(col.transform);
            }

            //add the current node and its connected nodes
            nodeTree[child] = connectedNodes;
        }
        


        //Finds components in the node tree 
        newComponents = FindComponents(nodeTree);



        //doesnt reparent if there are no components
        if (newComponents.Count == 1) return;

        //method that reparents components
        TransformUtils.CreateParentsAndReparent(newComponents);

        Destroy(gameObject);



        ////posts nodes and connected nodes in the terminal 
        //Debug.Log("| BLOCK | Connected Brothers |");
        //foreach (KeyValuePair<Transform, List<Transform>> pair in nodeTree)
        //{
        //    string block = pair.Key.name;
        //    string brothas = "";

        //    foreach (Transform brotha in pair.Value)
        //    {
        //        brothas = brothas + " " + brotha.name;
        //    }

        //    Debug.Log("|" + block + "|" + brothas + "|");

        //}





        //print the nodes of each component.
        //foreach (List<Transform> list in newComponents)
        //{

        //    string elements = "";
        //    foreach (Transform element in list)
        //    {
        //        elements = elements + " " + element;
        //    }

        //    Debug.Log(elements);
        //}

    }

    //Finds the connected components(clusters of connected nodes)
    private List<List<Transform>> FindComponents(Dictionary<Transform, List<Transform>> nodeTree)
    {
        //Keeps track of nodes we've already explored
        visited = new HashSet<Transform>();
        //Stores all connected components
        components = new List<List<Transform>>();
        
        foreach (Transform node in nodeTree.Keys)
        {
            if (!visited.Contains(node))
            {
                List<Transform> component = new List<Transform>();
                DFS(node, component);
                components.Add(component);
            }
        }

        return components;
    }

    private void DFS(Transform node, List<Transform> currentComponent)
    {
        // Mark the current node as visited
        visited.Add(node);

        //Add it to the current component we are building
        currentComponent.Add(node);

        foreach (Transform neighbor in nodeTree[node])
        {
            if (!visited.Contains(neighbor))
            {
                DFS(neighbor, currentComponent);
            }
        }
    }

    public void EvaluateStructure(List<Transform> nodesToDestroy)
    {
        RemoveNulls(nodeTree, nodesToDestroy);

        //Finds components in the node tree 
        newComponents = FindComponents(nodeTree);

        //doesnt reparent if there are no components
        if (newComponents.Count == 1) return;

        //method that reparents components
        TransformUtils.CreateParentsAndReparent(newComponents);

        Destroy(gameObject);
    }

    private void RemoveNulls(Dictionary<Transform, List<Transform>> dict, List<Transform> nodesToDestroy)
    {
        // First pass: clean list values
        foreach (var kvp in dict)
        {
            List<Transform> list = kvp.Value;

            list.RemoveAll(item => nodesToDestroy.Contains(item));


        }

        // Second pass: remove null keys
        // (must do separately to avoid modifying dictionary during iteration)
        List<Transform> nullKeys = new List<Transform>();

        foreach (var kvp in dict)
        {
            if (nodesToDestroy.Contains(kvp.Key))
            {
                nullKeys.Add(kvp.Key);
            }
        }

        foreach (var key in nullKeys)
        {
            dict.Remove(key);
        }


        foreach(Transform node in nodesToDestroy)
        {
            Destroy(node.gameObject);
        }
    }


}

