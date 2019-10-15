using System.Collections;
using UnityEngine;

public class Feeder : MonoBehaviour
{
    public Shuttle ShuttlePrefab;
    public Transform SpawnPoint;

    IEnumerator Start()
    {
        while (true)
        {
            var shuttle = Instantiate(ShuttlePrefab, SpawnPoint.position, SpawnPoint.rotation);
            shuttle.Rigidbody.velocity = SpawnPoint.forward * 11;
            yield return new WaitForSeconds(3);
        }
    }
}