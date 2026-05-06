using UnityEngine;

public class testScriptFixedJoint : MonoBehaviour
{
    FixedJoint fixedJoint;
    Rigidbody rb;

    public float appliedTorque;
    public float appliedForce;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fixedJoint = GetComponent<FixedJoint>();

        rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.forward * appliedForce);
        rb.AddTorque(Vector3.forward * appliedTorque);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
