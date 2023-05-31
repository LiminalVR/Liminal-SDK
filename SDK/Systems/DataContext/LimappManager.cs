using System;
using System.Collections;
using Liminal.Core.Fader;
using Liminal.SDK.Core;
using UnityEngine;

namespace Liminal.Shared
{
    public class LimappManager : MonoBehaviour
    {
        public int ExperienceId = 234;

        public float TransitionOutDelay = 0.0f;
        public float TransitionOutDuration = 2.0f;

        protected virtual IEnumerator Start()
        {
            yield return StartExperience();
        }

        protected virtual IEnumerator StartExperience()
        {
            LimappDataContext.ExperienceId = ExperienceId;
            var settings = LimappDataContext.Load();
            var runtimeDuration = settings.GetRuntimeDuration();

            if (runtimeDuration.Unlimited)
            {
                Debug.Log($"[Experience] - Duration: Unlimited");
                yield break;
            }

            var totalSeconds = (float)runtimeDuration.TimeSpan.TotalSeconds;

            Debug.Log($"[Experience] - Experience is running for {totalSeconds}");
            yield return new WaitForSeconds(totalSeconds);
            yield return new WaitForSeconds(TransitionOutDelay);
            var currentListenerVolume = AudioListener.volume;
            StartCoroutine(RunProcess(TransitionOutDuration, (progress) =>
            {
                AudioListener.volume = Mathf.Lerp(currentListenerVolume, 0, progress);
            }));

            ScreenFader.Instance.FadeToBlack(TransitionOutDuration);
            yield return ScreenFader.Instance.WaitUntilFadeComplete();

            ExperienceApp.End();
        }

        protected virtual IEnumerator RunProcess(float duration, Action<float> onUpdate)
        {
            var time = 0.0f;
            while (time < duration)
            {
                time += Time.deltaTime;

                var progress = time / duration;
                onUpdate?.Invoke(progress);

                yield return null;
            }
        }
    }
}