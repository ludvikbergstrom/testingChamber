using UnityEngine;

namespace BalancingRagdoll
{
    public class SwingLegCP : MonoBehaviour
    {
        [Header("Objects")]
        public Transform com;
        public Transform foot1;
        public Transform foot2;
        public Transform leg1;
        public Transform leg2;
        public Transform capturePointMarker;

        [Header("Stepping")]
        //public float swingSpeed = 5.0f;
        public float velocityThreshold = 0.1f;

        private Vector3 position;
        private Vector3 velocity;
        private float gravity;
        private float height;
        private Transform swingFoot;
        private Transform stanceFoot;
        private Transform swingLeg;
        private Transform stanceLeg;
        private float T = 0.0f;

        Vector3 swingFootStart;

        Rigidbody rb;
        ConfigurableJoint cj1;
        ConfigurableJoint cj2;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            rb = com.gameObject.GetComponent<Rigidbody>();
            cj1 = com.gameObject.GetComponents<ConfigurableJoint>()[0];
            cj2 = com.gameObject.GetComponents<ConfigurableJoint>()[1];

            stanceFoot = foot1;
            stanceLeg = leg1;
            swingFoot = foot2;
            swingLeg = leg2;

            gravity = Physics.gravity.magnitude;

            rb.AddForce(Vector3.one - Vector3.up, ForceMode.Impulse);

        }

        // Update is called once per frame
        void FixedUpdate()
        {
            velocity = rb.linearVelocity;

            //return if com stable
            if (velocity.magnitude < velocityThreshold) return;


            // 1. Calculate the capture point
            Vector3 capturePoint = CalculateCapturePoint(T);

            //2. visually move swing foot
            float t = Mathf.Clamp01(T / 0.3f);
            swingFoot.position = Vector3.Lerp(swingFootStart, capturePoint, t);

            //////3. visually update swing and stance foot
            UpdateLeg(stanceFoot, stanceLeg);
            UpdateLeg(swingFoot, swingLeg);

            capturePointMarker.position = capturePoint;

            //4.when swing foot reaches capture point...
            if (T >= 0.3f)
            {
                T = 0.0f;

                ////activates swing(now stance) foot joint at capture point
                ////and switches feet tasks
                SwitchFeet(capturePoint);

                swingFootStart = swingFoot.position;
            }

            T += Time.deltaTime;

        }

        Vector3 CalculateCapturePoint(float T)
        {
            //1. calculate current capture point
            velocity.y = 0f;

            position = rb.worldCenterOfMass;

            height = Mathf.Max(0.05f, position.y);

            float omega = Mathf.Sqrt(gravity / height);

            Vector3 currentCP = position + velocity / omega;
            currentCP.y = 0f;

            //2. calculate T, time till swing foot lands
            //future implementation...
            
            //3. calculate future capture point
            Vector3 support = stanceFoot.position;

            return support +
                Mathf.Exp(omega * T) * (currentCP - support);
        }


        void UpdateLeg(Transform foot,Transform leg)
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

        void SwitchFeet(Vector3 capturePoint)
        {
            if (swingFoot == foot1)
            {
                cj1.anchor = com.InverseTransformPoint(capturePoint); //convert cp to com's local space
                cj1.connectedAnchor = capturePoint;
                EnableJoint(cj1);
                DisableJoint(cj2);

                stanceFoot = foot1;
                stanceLeg = leg1;

                swingFoot = foot2;
                swingLeg = leg2;
            }
            else
            {
                cj2.anchor = com.InverseTransformPoint(capturePoint); //convert cp to com's local space
                cj2.connectedAnchor = capturePoint;
                EnableJoint(cj2);
                DisableJoint(cj1);

                stanceFoot = foot2;
                stanceLeg = leg2;

                swingFoot = foot1;
                swingLeg = leg1;
            }
        }

        void DisableJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Free;
        }
        void EnableJoint(ConfigurableJoint joint)
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
        }


    }
}
