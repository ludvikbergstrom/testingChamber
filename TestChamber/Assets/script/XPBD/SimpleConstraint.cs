using UnityEngine;
using Unity.Mathematics;


public class SimpleConstraint : MonoBehaviour 
{

    /*
     constraint object for simulating a chain of particles which have a distance constraint with the next and previous particle in the chain
    Result: a chain of particles which act like a chain of beeds.
     */

    public struct Particle
    {
        public Vector3 Position;
        public Vector3 PreviousPosition;
        public Vector3 Velocity;
        public float Mass;
        public float InverseMass;
    }

    public int numParticles = 100;
    public float compliance = 1.0f;
    public int subSteps = 3;

    Particle[] particles;

    public Vector3 g = new Vector3(0,-9.81f,0);

    private void Awake()
    {
        particles = new Particle[numParticles];

        for (int i = 0; i < particles.Length; i++)
        {
            Particle particle = new Particle();

            particle.Position = new Vector3(0f, 0f, i * 0.1f);

            particle.Mass = 1f;
            particle.InverseMass = 1 / particle.Mass;
                
            particles[i] = particle;
        }

        // Start the constrained pair at their rest distance.
        particles[0].Velocity = Vector3.right;
    }

    
    private void FixedUpdate()
    {
        Simulate(Time.fixedDeltaTime);
    }


    //Simulation
    public void Simulate(float dt)
    {
        float dts = dt / subSteps;

        for (int j = 0; j < subSteps; j++)
        {
            // 1. Apply forces and predict positions
            for (int i = 0; i < particles.Length; i++)
            {
                Particle p = particles[i];
                p.Velocity += dts * g;
                p.PreviousPosition = p.Position;
                p.Position += dts * p.Velocity;

                particles[i] = p;
            }
            // 2. Solve constraints
            for (int i = 0; i < particles.Length - 1; i++)
            {
                SolveDistanceConstraint(i, i + 1, 0.2f,compliance,dts);
            }

            for (int i = 0; i < particles.Length; i++)
            {
                Particle p = particles[i];

                SolveGroundCollision(ref p);

                particles[i] = p;
            }

            // 3. Recalculate velocities from corrected positions
            for (int i = 0; i < particles.Length; i++)
            {
                Particle p = particles[i];

                p.Velocity =
                    (p.Position - p.PreviousPosition) / dts;

                particles[i] = p;
            }

        }
    }
    void SolveDistanceConstraint(
    int indexA,
    int indexB,
    float restLength,
    float compliance,
    float dt)
    {
        Particle a = particles[indexA];
        Particle b = particles[indexB];

        Vector3 delta = b.Position - a.Position;
        float distance = delta.magnitude;

        if (distance == 0f)
            return;

        Vector3 direction = delta / distance;

        // Constraint error
        float C = distance - restLength;

        // Inverse masses
        float wA = a.InverseMass;
        float wB = b.InverseMass;

        float w = wA + wB;

        // Both particles are immovable
        if (w == 0f)
            return;

        // Compliance scaled by timestep
        float alpha = compliance / (dt * dt);

        // Constraint correction
        float correction = C / (w + alpha);

        a.Position += direction * correction * wA;
        b.Position -= direction * correction * wB;

        particles[indexA] = a;
        particles[indexB] = b;
    }
    void SolveGroundCollision(ref Particle p)
    {
        float groundY = 0f;
        float radius = 0.02f;

        if (p.Position.y - radius < groundY)
        {
            p.Position.y = groundY + radius;
        }
    }




    private void OnDrawGizmos()
    {
        if (particles == null) return;

        foreach (Particle particle in particles)
        {
            // Set the color with custom alpha.
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red with custom alpha

            // Draw the sphere.
            Gizmos.DrawSphere(particle.Position, 0.02f);
        }
        

    }
}



