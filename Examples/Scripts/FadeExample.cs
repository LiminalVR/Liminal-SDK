using Liminal.Core.Fader;
using UnityEngine;

public class FadeExample : MonoBehaviour
{
    public ScreenFader ScreenFader;

    public void FadeToBlack()
    {
        ScreenFader.FadeToBlack();
    }

    public void FadeToClear()
    {
        ScreenFader.FadeToClear();
    }
}