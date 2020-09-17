using System.Collections;
using UnityEngine;

public class ReflectionProbeUsage : MonoBehaviour
{
    public Transform Head;
    public ReflectionProbe Probe;
    public int UpdateRate = 3;
    public Material Mat;

    private int _currentFrame = 0;

    private void LateUpdate()
    {
        _currentFrame++;

        if (_currentFrame == UpdateRate)
        {
            var position = Head.transform.position;
            position.y = transform.position.y;

            transform.position = position;

            Probe.RenderProbe();

            Mat.SetTexture("_Cube", Probe.realtimeTexture);

            _currentFrame = 0;
        }
    }

    IEnumerator WaitForFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
            yield return new WaitForEndOfFrame();
    }
}
