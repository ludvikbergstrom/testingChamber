using UnityEngine;

public class destructableClicker : clicker
{
    public override void ClickFunction(RaycastHit hit)
    {
        if (hit.collider.gameObject.TryGetComponent<destructable>(out destructable segment))
        {
            segment.Damage(1f);
        }
        else
        {
            if (hit.transform != null) Debug.Log(hit.transform.name);
        }

    }

    public override void ClickFunctionVoid(Vector3 position)
    {
        throw new System.NotImplementedException();
    }
}
