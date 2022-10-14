using UnityEngine;

namespace Liminal.SDK.XR
{
    public class XRInputReferences : MonoBehaviour
    {
        public XRInputControllerReferences LeftControllerReferences;
        public XRInputControllerReferences RightControllerReferences;

        public static XRInputReferences Instance;

        private void Awake()
        {
            Instance = this;
        }

    }
}