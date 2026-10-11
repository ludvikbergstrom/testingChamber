using UnityEngine;
using System.Collections.Generic;

public class MeshTest : MonoBehaviour
{
    public HashSet<int>[] neighbours;

    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        //neighbours = BuildNeighbours(mesh);

    }

    //HashSet<int>[] BuildNeighbours(Mesh mesh)
    //{
        
    //}
}