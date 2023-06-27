using UnityEngine;

public class CameraRevolve : MonoBehaviour
{
    public Transform targetObject;
    public float distance = 5f;
    public float rotationSpeed = 1f;

    private void Update()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target object not set!");
            return;
        }

        // Rotate the camera around the target object
        transform.position = targetObject.position - transform.forward * distance;
        transform.RotateAround(targetObject.position, Vector3.up, rotationSpeed * Time.deltaTime);
    }
}