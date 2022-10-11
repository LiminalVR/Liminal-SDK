using Liminal.Systems;
using UnityEngine;

public class ControllerVisualPico : MonoBehaviour
{
    public GameObject Neo2Controller;
    public GameObject Neo3Controller;

    public void Start()
    {
        var modelType = XRDeviceUtils.GetDeviceModelType();
        switch (modelType)
        {
            case EDeviceModelType.Pico:
                Neo2Controller.SetActive(true);
                Neo3Controller.SetActive(false);
                break;

            case EDeviceModelType.PicoNeo3:
                Neo2Controller.SetActive(false);
                Neo3Controller.SetActive(true);
                break;
        }
    }
}
