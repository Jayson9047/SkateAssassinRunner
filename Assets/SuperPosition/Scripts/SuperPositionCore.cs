#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace SuperPosition
{
    [ExecuteInEditMode]
    //[CustomEditor(typeof(SuperPositionCore))]
    public class SuperPositionCore : MonoBehaviour
    {
        public bool wideView = false, doubleClickRestore = false;
        public SuperPositionData.State states = new SuperPositionData.State();
    }
    //
}
#endif