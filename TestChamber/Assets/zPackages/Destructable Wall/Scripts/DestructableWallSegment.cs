using System.Drawing;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

public class DestructableWallSegment : MonoBehaviour
{
    public float health = 1f;

    public DestructableWallSegment top;
    public DestructableWallSegment bottom;
    public DestructableWallSegment left;
    public DestructableWallSegment right;

    DestructableWallSegment ThisDestructableSegment;

    private bool connected = true;

    private Vector3 pushDirection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ThisDestructableSegment = GetComponent<DestructableWallSegment>();


        GetNeighbours();

    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            if (!connected) return;

            if (left != null)
            {
                left.right = null;
                left.CheckNeighbours();
            }
            if (right != null)
            {
                right.left = null;
                right.CheckNeighbours();
            }
            if (top != null)
            {
                top.bottom = null;
                top.CheckNeighbours();
            }
            if (bottom != null)
            {
                bottom.top = null;
                bottom.CheckNeighbours();
            }

            
            transform.AddComponent<Rigidbody>();
            transform.SetParent(null);
            transform.GetComponent<Rigidbody>().AddForce(pushDirection * 1000);
            connected = false;
            
        }
    }



    private void GetNeighbours()
    {
        Collider col = GetComponent<Collider>();

        Vector3 halfExtents = col.bounds.extents + Vector3.one * 0.1f;

        RaycastHit[] hits = Physics.BoxCastAll(transform.position, halfExtents, transform.forward);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform == transform) continue;

            if (hit.collider.transform.TryGetComponent<DestructableWallSegment>(out DestructableWallSegment segment))
            {
                SetNeigbour(segment, hit.collider.transform);
            }

        }
    }
    private void SetNeigbour(DestructableWallSegment neighbour, Transform neighbourPos)
    {

        Vector3 directionToNeighbour = (neighbourPos.position - transform.position).normalized;

        if (right == null && Vector3.Dot(directionToNeighbour, transform.right) > 0.9) 
        {
            right = neighbour;
            neighbour.left = ThisDestructableSegment;
        }

        else if (left == null && Vector3.Dot(directionToNeighbour, -1 * transform.right) > 0.9) 
        {
            left = neighbour;
            neighbour.right = ThisDestructableSegment;
        }
        
        else if (top == null && Vector3.Dot(directionToNeighbour, transform.up) > 0.9) 
        {
            top = neighbour;
            neighbour.bottom = ThisDestructableSegment;
        }
        
        else if (bottom == null && Vector3.Dot(directionToNeighbour, -1 * transform.up) > 0.9) 
        {
            bottom = neighbour;
            neighbour.top = ThisDestructableSegment;
        }
    }

    public void CheckNeighbours()
    {
        if (left != null) return;
        if (right != null) return;
        if (top != null) return;
        if (bottom != null) return;

        health = 0;
    }

    public void SetPushDirection(Vector3 direction)
    {
        pushDirection = direction;
    }


    //void OnDrawGizmos()
    //{
    //    Gizmos.DrawWireCube(transform.position, (transform.lossyScale / 2f) + Vector3.one * 0.1f);

    //}
}
