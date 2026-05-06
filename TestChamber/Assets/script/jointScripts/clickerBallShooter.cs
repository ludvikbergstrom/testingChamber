using UnityEngine;

public class clickerBallShooter : clicker 
{
    GameObject ball;

    float radius = 0.2f;
    public float shootForce = 100f;


    void Start()
    {
        ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        
        ball.transform.localScale = Vector3.one * radius * 2f; // diameter = 2 * radius

        //add a rigidbody
        ball.AddComponent<Rigidbody>().mass = 0.2f;
    }

    public override void ClickFunction(RaycastHit hit)
    {
        Vector3 direction = hit.point - Camera.main.transform.position;

        direction = direction.normalized;

        GameObject projectile = Instantiate(ball, Camera.main.transform.position, Quaternion.identity);

        projectile.GetComponent<Rigidbody>().AddForce(direction * shootForce);

        Destroy(projectile, 10f);

    }
    public override void ClickFunctionVoid(Vector3 position)
    {
        Vector3 direction = position - Camera.main.transform.position;

        direction = direction.normalized;

        GameObject projectile = Instantiate(ball, Camera.main.transform.position, Quaternion.identity);

        projectile.GetComponent<Rigidbody>().AddForce(direction * shootForce);

        Destroy(projectile, 10f);

    }

    

}
