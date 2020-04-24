using Liminal.SDK.VR.Avatars;
using UnityEngine.XR;

namespace Liminal.SDK.XR
{
	public static class UnityXRUtils
	{
		/// <summary>
		/// There is no version of XRNode.None, so this method returns a boolean based on a successful conversion
		/// with the converted node provided as an 'out' variable
		/// </summary>
		/// <param name="limbType"></param>
		/// <param name="xrNode"></param>
		/// <returns></returns>
		public static bool TryConvertToXRNode(this VRAvatarLimbType limbType, out XRNode xrNode)
		{
			xrNode = XRNode.TrackingReference;

			switch (limbType)
			{
				case VRAvatarLimbType.Head:
					xrNode = XRNode.Head;
					return true;
				case VRAvatarLimbType.LeftHand:
					xrNode = XRNode.LeftHand;
					return true;
				case VRAvatarLimbType.RightHand:
					xrNode = XRNode.RightHand;
					return true;
				case VRAvatarLimbType.Other:
				case VRAvatarLimbType.None:
				default:
					return false;
			}
		}

		/// <summary>
		/// Extension method for XRNode. 
		/// Allows easy conversion to VRAvatarLimbType, and as there is a 'None' option there is no need for a 'Try' type of method
		/// </summary>
		/// <param name="xrNode"></param>
		/// <returns></returns>
		public static VRAvatarLimbType ConvertToVRLimb(this XRNode xrNode)
		{
			switch (xrNode)
			{
				case XRNode.LeftHand:
					return VRAvatarLimbType.LeftHand;
				case XRNode.RightHand: 
					return VRAvatarLimbType.RightHand;
				case XRNode.Head:
					return VRAvatarLimbType.Head;
				case XRNode.LeftEye:
				case XRNode.RightEye:
				case XRNode.CenterEye:
					// TODO: Is there something more appropriate for here?
					return VRAvatarLimbType.Head;
				case XRNode.GameController:
				case XRNode.TrackingReference:
				case XRNode.HardwareTracker: 
					// TODO: Detemine if other or none is more appropriate for each of the above cases
					return VRAvatarLimbType.Other;
				default:
					return VRAvatarLimbType.None;
			}
		}
	}
}
