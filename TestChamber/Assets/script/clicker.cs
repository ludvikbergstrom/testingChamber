using UnityEngine;
using UnityEngine.InputSystem;

public abstract class clicker : MonoBehaviour
{


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
        }
    }


    public abstract void ClickFunction(RaycastHit hit);


}
