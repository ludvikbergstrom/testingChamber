using UnityEngine;
using UnityEngine.UIElements;

namespace BalancingRagdoll
{
    public class PhysicalCapturePointPendulum : MonoBehaviour
    {
        [Header("Objects")]
        public Transform com;
        public Transform foot;
        public Transform leg;
        public Transform capturePointMarker;

        [Header("Stepping")]
        public float stepThreshold = 0.3f;
        public float stepCooldown = 0.25f;

        private float lastStepTime = -100f;

        private Vector3 position;
        private Vector3 velocity;
        private float gravity;
        private float height;

        Rigidbody rb;
        ConfigurableJoint cj;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            rb = com.gameObject.GetComponent<Rigidbody>();
            cj = com.gameObject.GetComponent<ConfigurableJoint>();

            Disturb();

            gravity = Physics.gravity.magnitude;


        }

        // Update is called once per frame
        void FixedUpdate()
        {
            position = rb.worldCenterOfMass;

            velocity = rb.linearVelocity;
            velocity.y = 0f;

            height = Mathf.Max(0.05f, position.y);

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

                cj.anchor = com.InverseTransformPoint(capturePoint); //convert cp to com's local space
                cj.connectedAnchor = capturePoint;

                lastStepTime = Time.time;
            }

            capturePointMarker.position = capturePoint;

            MoveLeg();

        }

        Vector3 CalculateCapturePoint()
        {
            float omega = Mathf.Sqrt(gravity / height);

            Vector3 cp = position + velocity / omega;
            cp.y = 0f;

            return cp;
        }

        void MoveLeg()
        {
            Vector3 direction = com.position - foot.position;

            leg.position = (com.position + foot.position) / 2f;

            leg.rotation = Quaternion.FromToRotation(
                Vector3.up, direction
            );

            leg.localScale = new Vector3(
                0.2f,
                direction.magnitude / 2f,
                0.2f
                );
        }

        public void Disturb()
        {
            Vector3 pushDir = new Vector3(4f, 0, 4f);

            //if constraints we push in only only one direction
            if (rb.constraints == RigidbodyConstraints.FreezePositionZ) pushDir.z = 0;
            if (rb.constraints == RigidbodyConstraints.FreezePositionX) pushDir.x = 0;

            //introduce disturbance
            rb.AddForce(pushDir, ForceMode.Impulse);
        }


    }
}

