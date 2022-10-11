using UnityEngine;

public class CameraCheck : MonoBehaviour
{
    public Camera Camera;

    private void Update()
    {
        if(Camera.enabled)
            Debug.Log($"{transform.name} is enabled");
    }
}