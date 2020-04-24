using Liminal.SDK.VR.Pointers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Devices;
using Liminal.SDK.VR.Input;

namespace Liminal.SDK.XR
{
    /// <summary>
    /// This is only used by the UnityXRHeadset. At this stage there are no button mappings 
    /// setup for it, so there's no function interaction. 
    /// As more specific support is added (for headsets with DPads, for example) this is likely to change.
    /// </summary>
    public class UnityXRGazePointer : BasePointer
    {
        #region Constants

        #endregion

        #region Statics

        #endregion

        #region Fields
        #region Public

        #endregion

        #region Private
        private IVRInputDevice _inputDevice;
        #endregion
        #endregion

        #region Properties
        #region Public

        #endregion

        #region Private

        #endregion
        #endregion

        #region Constructor
        public UnityXRGazePointer(IVRInputDevice inputDevice) : base(inputDevice)
        {
            _inputDevice = inputDevice;
        }
        #endregion

        #region UnityXRGazePointer
        public override void OnPointerEnter(GameObject target) { }
        public override void OnPointerExit(GameObject target) { }

        /// <summary>
        /// Currently the UnityXRHeadset has no functional inputs. This might change.
        /// </summary>
        /// <returns></returns>
        public override bool GetButtonDown()
        {
            return _inputDevice.GetButtonUp(VRButton.One);
        }

        /// <summary>
        /// Currently the UnityXRHeadset has no functional inputs. This might change.
        /// </summary>
        /// <returns></returns>
        public override bool GetButtonUp()
        {
            return _inputDevice.GetButtonUp(VRButton.One);
        } 
        #endregion
    }
}


