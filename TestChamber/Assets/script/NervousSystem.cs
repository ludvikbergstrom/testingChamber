using System.Collections.Generic;
using UnityEngine;

public class NervousSystem : MonoBehaviour
{
    public Dictionary<GameObject, int> MuscleHealth;

    private void Start()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<ConfigurableJoint>() != null)
            {
                MuscleHealth[child.gameObject] = 100;
            }
        }
    }
}
