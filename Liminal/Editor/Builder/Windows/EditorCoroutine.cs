using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Swing.Editor
{
    public class EditorCoroutine
    {
        public static EditorCoroutine Start(IEnumerator routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(routine);
            coroutine.start();
            return coroutine;
        }

        readonly IEnumerator routine;
        EditorCoroutine(IEnumerator _routine)
        {
            routine = _routine;
        }

        void start()
        {
            EditorApplication.update += update;
        }
        public void stop()
        {
            EditorApplication.update -= update;
        }

        void update()
        {
            if (!routine.MoveNext())
            {
                stop();
            }
        }
    }
}