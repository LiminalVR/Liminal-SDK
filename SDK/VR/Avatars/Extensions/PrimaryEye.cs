using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrimaryEye : MonoBehaviour
{
    public static PrimaryEye Instance;

    public Camera Camera;

    private void Awake()
    {
        Instance = this;
    }
}
