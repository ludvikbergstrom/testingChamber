using UnityEngine;
using UnityEditor;

namespace BalancingRagdoll
{
    [CustomEditor(typeof(PhysicalCapturePointPendulum))]
    public class PhysicalCapturePointPendulumEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 1. Draw the default inspector fields (variables)
            DrawDefaultInspector();

            // Get a reference to the actual target script
            PhysicalCapturePointPendulum myScript = (PhysicalCapturePointPendulum)target;

            // 2. Add space and draw the button
            GUILayout.Space(10);
            if (GUILayout.Button("Trigger Disturbance"))
            {
                // 3. Call the function when clicked
                myScript.Disturb();
            }
        }
    }
}
