namespace Liminal.Systems
{
    using UnityEngine;
    using UnityEngine.XR;

    public class ReflectionSetup : MonoBehaviour
    {
        public GameObject DefaultSetup;
        public GameObject ReflectionProbeSetup;

        private void Awake()
        {
            Debug.Log($"Known Device Name: {XRDeviceUtils.GetDeviceModelType()} - Actual Device Model: {SystemInfo.deviceModel}");

            if (XRDeviceUtils.SupportsPlanarReflection())
            {
                DefaultSetup.SetActive(true);
                ReflectionProbeSetup.SetActive(false);
            }
            else
            {
                DefaultSetup.SetActive(false);
                ReflectionProbeSetup.SetActive(true);
            }
        }
    }
}