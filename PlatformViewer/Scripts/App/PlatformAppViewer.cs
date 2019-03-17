using System.Collections;
using System.IO;
using Liminal.Platform.Experimental.App.Experiences;
using Liminal.Platform.Experimental.Utils;
using Liminal.Platform.Experimental.VR;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using UnityEngine;

namespace Liminal.Platform.Experimental.App
{
    public class PlatformAppViewer : MonoBehaviour
    {
        public VRAvatar Avatar;
        public ExperienceAppPlayer ExperienceAppPlayer;
        public AppPreviewConfig PreviewConfig;

        private VRDeviceLoader _deviceLoader;
        private byte[] _limappData;

        private void Awake()
        {
            SetupVRDevice();
            BetterStreamingAssets.Initialize();
        }

        private void Start()
        {
            Play();
        }

        private void SetupVRDevice()
        {
            _deviceLoader = new VRDeviceLoader();
            VRDevice.Device.SetupAvatar(Avatar);
        }

        public void Play()
        {
            if(!ExperienceAppPlayer.IsRunning)
                StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            StartCoroutine((StopRoutine()));
        }

        private IEnumerator PlayRoutine()
        {
            ResolvePlatformLimapp(out _limappData, out string fileName);

            var experience = new Experience
            {
                Id = ExperienceAppUtils.AppIdFromName(fileName),
                Bytes = _limappData,
            };

            var loadOp = ExperienceAppPlayer.Load(experience);
            yield return loadOp.LoadScene();
            ExperienceAppPlayer.Begin();
        }

        private IEnumerator StopRoutine()
        {
            yield return ExperienceAppPlayer.Unload();
            Avatar.SetActive(true);
        }

        private void ResolvePlatformLimapp(out byte[] data, out string fileName)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                data = BetterStreamingAssets.ReadAllBytes(PreviewConfig.AndroidAppFullName);
                fileName = PreviewConfig.AndroidAppFullName;
            }
            else
            {
                var limappPath = Application.isEditor ? PreviewConfig.EmulatorPath : PreviewConfig.AndroidPath;
                fileName = Path.GetFileName(limappPath);
                data = File.ReadAllBytes(limappPath);
            }
        }
    }
}