using System.Collections;
using Liminal.Core.Fader;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using UnityEngine;

public class FadeExample : MonoBehaviour
{
    public ScreenFader ScreenFader;

    public VRAvatar A;
    public VRAvatar B;

    public bool _useAvatarB;

    private IEnumerator Start()
    {
        yield break;
        while (true)
        {
            yield return new WaitForSeconds(5);
            ToggleAvatar();
        }
    }

    private void Update()
    {
        if (VRDevice.Device == null)
            return;

        // What probably happened is UnityXRDevice is still listening!
        if (VRDevice.Device.GetButtonUp(VRButton.Back))
        {
            Debug.Log("Trigger Pressed");
            ToggleAvatar();
        }
    }

    [ContextMenu("Toggle Avatar")]
    public void ToggleAvatar()
    {
        _useAvatarB = !_useAvatarB;

        A.SetActive(false);
        B.SetActive(false);

        var activeAvatar = _useAvatarB ? B : A;
        VRDevice.Device.SetupAvatar(activeAvatar);
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