using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IssuesUtility
{
    public static Dictionary<string, string> IncompatiblePackagesTable = new Dictionary<string, string>()
    {
        {"Unity.Postprocessing.Runtime","Post Processing"},
        {"FluffyUnderware.Curvy","Curvy"},
        {"DOTween","DOTween"}
    };
}
