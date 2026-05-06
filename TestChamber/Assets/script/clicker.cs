using UnityEngine;
using UnityEngine.InputSystem;

public abstract class clicker : MonoBehaviour
{

    float voidDistance = 1000f;

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                ClickFunction(hit);
            }

            else
            {
                // fallback: point in the void
                Vector3 voidPoint = ray.GetPoint(voidDistance);
                ClickFunctionVoid(voidPoint);
            }
        }
    }


    public abstract void ClickFunction(RaycastHit hit);

    public abstract void ClickFunctionVoid(Vector3 position);


}
