using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    public Camera targetCamera;   // leave null to auto-grab Camera.main

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // Make the object face the camera
        transform.LookAt(targetCamera.transform);
        // Flip 180° so front faces camera
        transform.Rotate(0f, 180f, 0f, Space.Self);
    }
}
