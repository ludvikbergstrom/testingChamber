using Unity.Mathematics;

public class ShapeMatchingParticle
{
    public float3 position;
    public float3 predictedPosition;
    public float3 velocity;

    public float mass;
    public float inverseMass;

    public ShapeMatchingParticle(float3 position, float mass)
    {
        this.position = position;
        predictedPosition = position;
        velocity = float3.zero;

        this.mass = mass;
        inverseMass = (mass > 0f) ? (1f / mass) : 0f;
    }
}