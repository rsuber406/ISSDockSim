using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour
{
    public Transform targetTransform;
    public Vector3 positionOffset;
    public Vector3 lookAtOffset;

    void Start()
    {
        this.transform.parent = null;
    }
    
    void LateUpdate()
    {
        if (targetTransform != null)
        {
            Vector3 offset = targetTransform.TransformDirection(positionOffset);
            this.transform.position = targetTransform.position + offset;
            this.transform.LookAt(targetTransform.position + targetTransform.TransformDirection(lookAtOffset));
        }
    }
}
