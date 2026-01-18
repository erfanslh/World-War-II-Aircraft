using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [Tooltip("Degrees per second around Y axis")]
    public float rotationSpeed = 25f;

    void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }
}

