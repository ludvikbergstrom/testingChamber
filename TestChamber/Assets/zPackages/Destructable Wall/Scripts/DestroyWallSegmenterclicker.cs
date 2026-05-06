using Unity.VisualScripting;
using UnityEngine;

public class DestroyWallSegmenter : clicker
{
    public override void ClickFunction(RaycastHit hit)
    {
        if (hit.collider.gameObject.TryGetComponent<DestructableWallSegment>(out DestructableWallSegment segment))
        {
            segment.health = 0;
            segment.SetPushDirection(hit.transform.position - transform.position);
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
