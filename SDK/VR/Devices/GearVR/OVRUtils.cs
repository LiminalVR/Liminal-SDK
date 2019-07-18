using Liminal.SDK.VR.Avatars;

/// <summary>
/// A wrapper utility to interface with OVRInput and OVRPlugin for common OVR Usages.
/// </summary>
public static class OVRUtils
{
    public static bool IsOculusQuest => OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest;
    public static bool IsOculusGo => OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Go;

    public static bool IsQuestControllerConnected 
        => OVRInput.IsControllerConnected(OVRInput.Controller.Touch) || 
           OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) ||
           OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);

    public static bool IsGearVRHeadset()
    {
        OVRPlugin.SystemHeadset headsetType = OVRPlugin.GetSystemHeadsetType();
        switch (headsetType)
        {
            case OVRPlugin.SystemHeadset.GearVR_R320:
            case OVRPlugin.SystemHeadset.GearVR_R321:
            case OVRPlugin.SystemHeadset.GearVR_R322:
            case OVRPlugin.SystemHeadset.GearVR_R323:
            case OVRPlugin.SystemHeadset.GearVR_R324:
            case OVRPlugin.SystemHeadset.GearVR_R325:
                return true;
            default:
                return false;
        }
    }

    public static bool IsLimbConnected(VRAvatarLimbType limbType)
    {
        var type = GetControllerType(limbType);
        return OVRInput.IsControllerConnected(type);
    }

    /// <summary>
    /// OVRInput.Controller will return it as an enum and not a mask.
    /// </summary>
    /// <param name="limbType"></param>
    /// <returns></returns>
    public static OVRInput.Controller GetControllerType(VRAvatarLimbType limbType)
    {
        switch (limbType)
        {
            case VRAvatarLimbType.LeftHand:
                return OVRInput.Controller.LTouch;
            case VRAvatarLimbType.RightHand:
                return OVRInput.Controller.RTouch;
            default:
                return OVRInput.Controller.None;
        }
    }
}