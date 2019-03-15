using System.Collections;
using Liminal.SDK.Core;
using UnityEngine;

public class SpinningCubeExample : MonoBehaviour
{
    public float Speed = 5;

    private void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * Speed);
    }

    private void OnEnable()
    {
        StartCoroutine(ShutDown());
    }

    private IEnumerator ShutDown()
    {
        yield return new WaitForSeconds(100);
        End();
    }

    private void End()
    {
        ExperienceApp.End();
    }
}