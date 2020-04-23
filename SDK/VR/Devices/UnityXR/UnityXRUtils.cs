using Liminal.SDK.VR.Avatars;
using UnityEngine.XR;

namespace Liminal.SDK.XR
{
	public static class UnityXRUtils
	{
		public static bool TryLimbToNode(this VRAvatarLimbType limbType, out XRNode xrNode)
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
	}
}
