using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraContorller : MonoBehaviour
{
    public Transform[] targets;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    private void LateUpdate()
    {
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        Transform activeTarget = FindActiveTarget();

        if (activeTarget == null)
        {
            return;
        }

        Vector3 desiredPosition = activeTarget.position + offset;
        desiredPosition.y = transform.position.y;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
        Transform FindActiveTarget()
        {
            foreach (Transform target in targets)
            {
                if (target.gameObject.activeInHierarchy)
                    return target;
            }
            return null;
        
    }
}
    
