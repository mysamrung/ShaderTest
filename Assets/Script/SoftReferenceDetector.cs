//using System;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using UnityEditor;
//using UnityEditor.AddressableAssets;
//using UnityEditor.AddressableAssets.Settings;
//using UnityEngine;
//using UnityEngine.AddressableAssets;

//namespace UnityEditor.AddressableAssets.Settings
//{
//    //[InitializeOnLoad]
//    internal class SoftReferenceDetector
//    {
//        //[InitializeOnLoadMethod]
//        internal static void RegisterWithAssetPostProcessor()
//        {
//            AddressablesAssetPostProcessor.OnPostProcess.Register(OnPostprocessAllAssets, 1);
//        }

//        private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//        {
//        }
//    }
//}
