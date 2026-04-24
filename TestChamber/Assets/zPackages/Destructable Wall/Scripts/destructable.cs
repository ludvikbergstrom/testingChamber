using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class destructable : MonoBehaviour
{
    public float health = 1f;

    public List<destructable> neighbourList = new List<destructable>();

    //[SerializeField] private bool isGrounded = false;

    private destructable thisDestructable;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisDestructable = GetComponent<destructable>();

        GetNeighbours();
    }

    // Update is called once per frame



    private void GetNeighbours()
    {
        Collider col = GetComponent<Collider>();

        Vector3 halfExtents = col.bounds.extents + Vector3.one * 0.1f;

        RaycastHit[] hits = Physics.BoxCastAll(transform.position, halfExtents, transform.forward);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform == transform) continue;

            //if (hit.transform.CompareTag("floor")) isGrounded = true;

            if (hit.collider.transform.TryGetComponent<destructable>(out destructable segment))
            {
                neighbourList.Add(segment);
            }

        }
    }

    public void Damage(float damage)
    {
        health -= damage;

        if (health <= 0)
        {
            DestroySegment();
        }
    }

    private void DestroySegment(float force = 1000) 
    {
        TellYourNeighbour();

        transform.AddComponent<Rigidbody>();
        transform.SetParent(null);
        transform.GetComponent<Rigidbody>().AddForce(-transform.forward * force);
    }

    private void TellYourNeighbour()
    {
        foreach (destructable neighbour in neighbourList)
        {
            neighbour.neighbourList.Remove(thisDestructable);
        }
        
        foreach (destructable neighbour in neighbourList)
        {
            neighbour.CheckIfGrounded();
        }

    }

    private void CheckIfGrounded()
    {
        if (neighbourList.Count == 0) DestroySegment(0);
    }
}
