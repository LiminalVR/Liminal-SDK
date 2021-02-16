using Liminal.SDK.Core;
using Liminal.SDK.VR.Avatars;
using UnityEngine;

public class PicoCheat : MonoBehaviour
{
    private void Update()
    {
        var experienceApp = FindObjectOfType<ExperienceApp>();
        if (experienceApp != null)
        {
            var children = experienceApp.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var child in children)
            {
                if (child.GetComponent<VRAvatar>())
                    continue;

                child.gameObject.SetActive(true);
            }
        }
    }
}