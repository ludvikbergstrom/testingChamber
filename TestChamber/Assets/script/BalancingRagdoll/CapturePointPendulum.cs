using UnityEngine;

namespace BalancingRagdoll
{
    public class CapturePointPendulum : MonoBehaviour
    {
        [Header("Objects")]
        public Transform com;
        public Transform foot;
        public Transform leg;
        public Transform capturePointMarker;

        [Header("Physics")]
        public float height = 2f;
        public float gravity = 9.81f;
        public Vector3 initialVelocity = new Vector3(1.5f, 0, 0);

        [Header("Stepping")]
        public float stepThreshold = 0.3f;
        public float stepCooldown = 0.25f;

        private Vector3 position;
        private Vector3 velocity;
        private float lastStepTime = -100f;

        void Start()
        {
            position = com.position;
            position.y = height;

            velocity = initialVelocity;
            velocity.y = 0f;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // 1. Calculate the capture point
            Vector3 capturePoint = CalculateCapturePoint();

            // 2. Step if the capture point is too far away
            float distance = Vector3.Distance(
                foot.position, capturePoint
            );

            if (distance > stepThreshold &&
                Time.time - lastStepTime >= stepCooldown)
            {
                foot.position = capturePoint;
                lastStepTime = Time.time;
            }

            // 3. Simulate the inverted pendulum
            Vector3 displacement = position - foot.position;
            displacement.y = 0f;

            Vector3 acceleration =
                (gravity / height) * displacement;

            velocity += acceleration * dt;
            position += velocity * dt;

            // Keep the COM at a constant height
            position.y = height;

            // 4. Update visuals
            com.position = position;

            capturePointMarker.position = CalculateCapturePoint();

            UpdateLeg();
        }

        Vector3 CalculateCapturePoint()
        {
            float omega = Mathf.Sqrt(gravity / height);

            Vector3 cp = position + velocity / omega;
            cp.y = 0f;

            return cp;
        }

        void UpdateLeg()
        {
            Vector3 direction = com.position - foot.position;

            leg.position = (com.position + foot.position) / 2f;

            leg.rotation = Quaternion.FromToRotation(
                Vector3.up, direction
            );

            // A Unity cylinder has a default height of 2
            leg.localScale = new Vector3(
                0.05f,
                direction.magnitude / 2f,
                0.05f
            );
        }
    }
}
