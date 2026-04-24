using UnityEngine;

public class tentacleMove : MonoBehaviour
{
    public GameObject targetPillar;
    public float speed = 5f;
    public float Length = 20f;

    private float currentLength;
    private Vector3 center;
    private void Start()
    {
        currentLength = Length;

        center = transform.position;
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if (!hasReachedTarget())
        {
            move();
        }
        else
        {
            encircle();
        }


    }

    private bool hasReachedTarget()
    {
        float distance = Vector3.Distance(transform.position, targetPillar.transform.position);

        if (distance > 1) return false;
        return true;
    }
    private void move() 
    {
        Vector3 direction = (targetPillar.transform.position - transform.position).normalized;

        float distanceCenter = Vector3.Distance(transform.position, center);

        if (distanceCenter + direction.magnitude > Length && movingAwayFromTarget(direction)) return;

        transform.position += direction * speed * Time.deltaTime;
    }


    private void encircle() 
    {
        Debug.Log("encircle");
    }

    private bool movingAwayFromTarget(Vector3 direction)
    {
        if (Vector3.Dot(direction, transform.position - center) > 0) return true;
        return false;
    }
}
