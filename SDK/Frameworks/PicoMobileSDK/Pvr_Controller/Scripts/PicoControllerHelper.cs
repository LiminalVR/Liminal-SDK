using System.Collections.Generic;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Pointers;
using UnityEngine;

public class PicoControllerHelper : MonoBehaviour
{
    public Pvr_ControllerInit Controller;

    public Dictionary<IVRAvatarHand, VRAvatarController> Map = new Dictionary<IVRAvatarHand, VRAvatarController>();

    public VRAvatarLimbType Limb;

    private void Update()
    {
        var hand = Limb == VRAvatarLimbType.RightHand ? VRAvatar.Active?.PrimaryHand : VRAvatar.Active?.SecondaryHand;
        if (hand == null)
            return;

        if (Map.TryGetValue(hand, out var controller))
        {
            if (controller == null || !controller.gameObject.activeInHierarchy)
            {
                hand.InputDevice.Pointer.Deactivate();
                SetModelState(false);
            }
            else
            {
                SetModelState(true);
                hand.InputDevice.Pointer.Activate();
            }
        }
        else
        {
            controller = hand.Transform.gameObject.GetComponentInChildren<VRAvatarController>();
            Map.Add(hand, controller);
        }
    }

    public void SetModelState(bool state)
    {
        if(state)
            Controller.Show();
        else
            Controller.Hide();
    }
}