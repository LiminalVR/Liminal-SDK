using Liminal.Core.Fader;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using UnityEngine;

public class FadeExample : MonoBehaviour
{
    public ScreenFader ScreenFader;

    public VRAvatar A;
    public VRAvatar B;

    public void SecondAvatar()
    {
        A.SetActive(false);
        B.SetActive(true);

        VRDevice.Device.SetupAvatar(B);
    }

    public void FadeToBlack()
    {
        ScreenFader.FadeToBlack();
    }

    public void FadeToClear()
    {
        ScreenFader.FadeToClear();
    }
}