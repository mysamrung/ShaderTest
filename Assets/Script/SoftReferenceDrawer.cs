//using System;
//using UnityEditor;
//using UnityEditor.AddressableAssets;
//using UnityEditor.AddressableAssets.Settings;
//using UnityEngine;
//using UnityEngine.UIElements;

//namespace Xeen.AssetReference.Editor
//{

//    [CustomPropertyDrawer(typeof(SoftReferenceAttribute))]
//    public class SoftReferenceDrawer : PropertyDrawer
//    {
//        private UnityEngine.Object _referencedObject;
//        private bool _isAddressable;
//        public override VisualElement CreatePropertyGUI(SerializedProperty property)
//        {
//            SerializedProperty assetAddressProperty = property.FindPropertyRelative("assetAddress");

//            if (assetAddressProperty != null)
//            {
//                SoftReferenceAttribute referenceAttribute = attribute as SoftReferenceAttribute;
//                _referencedObject = AssetDatabase.LoadAssetAtPath(assetAddressProperty.stringValue, referenceAttribute.referencedType);

//                if (assetAddressProperty.stringValue != string.Empty)
//                {
//                    AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
//                    string assetPath = AssetDatabase.GetAssetPath(_referencedObject);
//                    string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
//                    AddressableAssetEntry assetEntry = settings.FindAssetEntry(assetGUID);

//                    _isAddressable = assetEntry != null;
//                }
//                else
//                {
//                    _isAddressable = true;
//                }
//            }

//            return base.CreatePropertyGUI(property);
//        }

//        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//        {
//            SerializedProperty assetAddressProperty = property.FindPropertyRelative("assetAddress");
//            if (assetAddressProperty == null)
//            {
//                EditorGUI.LabelField(position, label.text, "Use SoftReference attribute with SoftReference parameter type.");
//                return;
//            }

//            SoftReferenceAttribute referenceAttribute = attribute as SoftReferenceAttribute;
//            EditorGUILayout.Space(-22);

//            if (!_isAddressable)
//            {
//                GUI.color = Color.red;
//                EditorGUILayout.LabelField("Asset isn't checked as addressable.");
//                GUI.color = Color.white;
//            }

//            UnityEngine.Object referencedObject = EditorGUILayout.ObjectField(label, _referencedObject, referenceAttribute.referencedType, false);
//            if (referencedObject != _referencedObject)
//            {
//                if (referencedObject != null)
//                {
//                    string assetPath = AssetDatabase.GetAssetPath(referencedObject);
//                    string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

//                    AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
//                    AddressableAssetEntry assetEntry = settings.FindAssetEntry(assetGUID);

//                    assetAddressProperty.stringValue = assetPath;
//                    _isAddressable = assetEntry != null;
//                }
//                else
//                {
//                    _isAddressable = true;
//                }

//                _referencedObject = referencedObject;
//            }

//        }
//    }

//}