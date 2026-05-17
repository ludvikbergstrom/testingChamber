using UnityEngine;

public class clickerBallShooter : clicker 
{
    public GameObject ball;

    float radius = 0.2f;
    public float shootForce = 100f;


    void Start()
    {
        //if a ball prefab is assigned in the inspector we skip the ball making process
        if (ball != null) return;

        //if no prefab is given we create a simple ball

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
