using UnityEngine;
using UnityEngine.InputSystem;

public abstract class DestructablePole : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    BreakPole(hit.point);
                }
            }
        }
    }

    public abstract void BreakPole(Vector3 breakPoint);
}
