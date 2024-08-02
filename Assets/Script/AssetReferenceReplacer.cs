using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Xeen.AssetReference.Editor
{

    public class AssetReferenceReplacer : EditorWindow
    {

        #region HierarchyParameter
        [System.Serializable]
        public class ReplaceHierarchProperty
        {
            public Component soruceComponent;
            public Component referencedComponent;
            public SerializedObject serializedObject;
            public SerializedProperty serializedProperty;
        }

        [SerializeField]
        private List<ReplaceHierarchProperty> replaceHiereacyProperties = new List<ReplaceHierarchProperty>();

        private bool _isResultFoldout = false;
        #endregion

        #region ProjectParameter

        [System.Serializable]
        public class ObjectSearchResult
        {
            public UnityEngine.Object assetObject = null;
            public bool replace = true;
        }

        [CustomPropertyDrawer(typeof(ObjectSearchResult))]
        public class ObjectSearchResultDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                // ラベルを描画
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                // 矩形を計算
                var objectRect = new Rect(position.x, position.y, 100, position.height);
                var replaceRect = new Rect(position.x + 110, position.y, 10, position.height);
                var replaceFieldRect = new Rect(replaceRect.x + 20, replaceRect.y, 70, replaceRect.height);


                // フィールドを描画 - GUIContent.none をそれぞれに渡すと、ラベルなしに描画されます
                EditorGUI.PropertyField(objectRect, property.FindPropertyRelative("assetObject"), GUIContent.none);
                EditorGUI.PropertyField(replaceRect, property.FindPropertyRelative("replace"), GUIContent.none);
                EditorGUI.LabelField(replaceFieldRect, new GUIContent("Replace ?"));


                EditorGUI.EndProperty();
            }
        }

        [System.Serializable]
        public class ReplaceKeyValue
        {
            public string key;
            public string value;

            public enum EMode { Overwrite, Remove }
            public EMode mode;
        }

        [SerializeField]
        private string[] searchFolders;

        [SerializeField]
        private string searchPrefix;

        [SerializeField]
        private List<ObjectSearchResult> searchResult = new List<ObjectSearchResult>();
        #endregion

        #region SharedParameter
        private UnityEngine.Object sourceObject;
        private UnityEngine.Object replaceObject;

        private SerializedObject serializedObject;
        private Vector2 scrollPosition = Vector2.zero;

        private string[] _tabText = { "Project", "Hierarchy" };
        private int _tabIndex;
        #endregion

        [MenuItem("XEEN/AssetTools/AssetReferenceReplacer")]
        private static void OpenByWindow()
        {
            var window = GetWindow<AssetReferenceReplacer>("AssetReferenceReplacer");
            window.Show();
        }

        [MenuItem("GameObject/AssetTools/AssetReferenceReplacer")]
        private static void OpenByMenu()
        {
            var selected = Selection.activeObject;
            var window = GetWindow<AssetReferenceReplacer>("AssetReferenceReplacer");
            window.sourceObject = selected;
            window._tabIndex = 1;
            window.Show();
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }
        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tabIndex = GUILayout.Toolbar(
                     _tabIndex,
                     _tabText,
                     new GUIStyle(EditorStyles.toolbarButton),
                     GUI.ToolbarButtonSize.FitToContents);
            }

            if (_tabIndex == 0)
                ProjectAssetReplacerGUI();
            else
                HierarchyObjectReplacerGUI();
        }

        private void HierarchyObjectReplacerGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition); // スクロール設定

            sourceObject = EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject));
            replaceObject = EditorGUILayout.ObjectField("Replace Object", replaceObject, typeof(GameObject));

            if (GUILayout.Button("Search", GUILayout.Width(120)))
            {
                replaceHiereacyProperties.Clear();

                List<GameObject> rootObjectsInScene = new List<GameObject>();
                Scene scene = SceneManager.GetActiveScene();
                scene.GetRootGameObjects(rootObjectsInScene);

                GameObject sourceGameObject = (GameObject)sourceObject;
                List<MonoBehaviour> srcComponents = sourceGameObject.GetComponents<MonoBehaviour>().ToList();

                for (int i = 0; i < rootObjectsInScene.Count; i++)
                {
                    MonoBehaviour[] allComponents = rootObjectsInScene[i].GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (MonoBehaviour comp in allComponents)
                    {
                        ObjectIdentifier.TryGetObjectIdentifier(comp, out ObjectIdentifier compIds);
                        SerializedObject serializedObject = new UnityEditor.SerializedObject(comp);
                        SerializedProperty serializedProperty = serializedObject.GetIterator();
                        while (serializedProperty != null)
                        {
                            if (serializedProperty.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                MonoBehaviour srcComponent = srcComponents.SingleOrDefault(s => s == serializedProperty.objectReferenceValue);

                                if (srcComponent != null)
                                {
                                    ReplaceHierarchProperty replaceHierarchProperty = new ReplaceHierarchProperty();
                                    replaceHierarchProperty.soruceComponent = srcComponent;
                                    replaceHierarchProperty.referencedComponent = comp;
                                    replaceHierarchProperty.serializedObject = serializedObject;
                                    replaceHierarchProperty.serializedProperty = serializedProperty.Copy();
                                    replaceHiereacyProperties.Add(replaceHierarchProperty);
                                }
                            }

                            if (!serializedProperty.Next(true))
                                break;
                        }
                    }
                }
            }

            _isResultFoldout = EditorGUILayout.Foldout(_isResultFoldout, "Search Result");
            if (_isResultFoldout)
            {
                for (int i = 0; i < replaceHiereacyProperties.Count; i++)
                {
                    EditorGUILayout.ObjectField(replaceHiereacyProperties[i].referencedComponent, typeof(UnityEngine.Object));
                }
            }


            if (GUILayout.Button("Replace", GUILayout.Width(120)))
            {
                GameObject replaceGameObject = (GameObject)replaceObject;
                List<Component> repComponents = replaceGameObject.GetComponents<Component>().ToList();
                foreach (ReplaceHierarchProperty replaceHiereacyProperty in replaceHiereacyProperties)
                {
                    Component repComp = repComponents.SingleOrDefault(s => s.GetType() == replaceHiereacyProperty.soruceComponent.GetType());
                    replaceHiereacyProperty.serializedProperty.objectReferenceValue = repComp;
                    replaceHiereacyProperty.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.EndScrollView();

            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }

        private void ProjectAssetReplacerGUI()
        {
            serializedObject.Update();

            sourceObject = EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(UnityEngine.Object));
            replaceObject = EditorGUILayout.ObjectField("Replace Object", replaceObject, typeof(UnityEngine.Object));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchFolders"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchPrefix"), true);

            if (GUILayout.Button("Search", GUILayout.Width(120)))
                searchResult = SearchObject(searchPrefix, searchFolders, sourceObject).ToList();


            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchResult"), true);

            if (GUILayout.Button("Replace", GUILayout.Width(120)))
                ReplaceObject(searchResult.ToArray(), sourceObject, replaceObject);

            GUILayout.EndScrollView();


            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        public static ObjectSearchResult[] SearchObject(string prefix, string[] folders, UnityEngine.Object searchObject)
        {
            string assetPath = AssetDatabase.GetAssetPath(searchObject);
            string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

            string[] guidsResult = AssetDatabase.FindAssets(prefix, folders);

            List<ObjectSearchResult> results = new List<ObjectSearchResult>();
            for (int i = 0; i < guidsResult.Length; i++)
            {
                UnityEngine.Object objectAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guidsResult[i]), typeof(UnityEngine.Object));
                UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { objectAsset });

                bool isContain = false;
                for (int j = 0; j < dependencies.Length; j++)
                {
                    string dependentGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dependencies[j]));
                    if (dependentGUID == assetGUID)
                    {
                        isContain = true;
                        break;
                    }
                }

                if (isContain)
                {
                    ObjectSearchResult result = new ObjectSearchResult();
                    result.assetObject = objectAsset;
                    results.Add(result);
                }
            }

            return results.ToArray();
        }


        private void ReplaceObject(ObjectSearchResult[] sourceList, UnityEngine.Object soruceObject, UnityEngine.Object replaceObject)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(soruceObject, out string sourceAssetGUID, out long sourceAssetFileId);


            ReplaceKeyValue[] replaceKeyValues = new ReplaceKeyValue[3];
            if (replaceObject != null)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(replaceObject, out string replaceAssetGUID, out long replaceAssetFileId);

                replaceKeyValues[0] = new ReplaceKeyValue();
                replaceKeyValues[0].key = "fileID";
                replaceKeyValues[0].value = replaceAssetFileId.ToString();
                replaceKeyValues[0].mode = ReplaceKeyValue.EMode.Overwrite;

                replaceKeyValues[1] = new ReplaceKeyValue();
                replaceKeyValues[1].key = "guid";
                replaceKeyValues[1].value = replaceAssetGUID;
                replaceKeyValues[1].mode = ReplaceKeyValue.EMode.Overwrite;

                replaceKeyValues[2] = new ReplaceKeyValue();
                replaceKeyValues[2].key = "type";
                replaceKeyValues[2].value = AssetDatabase.IsNativeAsset(replaceObject) ? "2" : (AssetDatabase.IsForeignAsset(replaceObject) ? "3" : "0");
                replaceKeyValues[2].mode = ReplaceKeyValue.EMode.Overwrite;
            }
            else
            {
                replaceKeyValues[0] = new ReplaceKeyValue();
                replaceKeyValues[0].key = "fileID";
                replaceKeyValues[0].value = "0";
                replaceKeyValues[0].mode = ReplaceKeyValue.EMode.Overwrite;

                replaceKeyValues[1] = new ReplaceKeyValue();
                replaceKeyValues[1].key = "guid";
                replaceKeyValues[1].mode = ReplaceKeyValue.EMode.Remove;

                replaceKeyValues[2] = new ReplaceKeyValue();
                replaceKeyValues[2].key = "type";
                replaceKeyValues[2].mode = ReplaceKeyValue.EMode.Remove;
            }

            string projectDir = Directory.GetParent(Application.dataPath).FullName;
            foreach (ObjectSearchResult source in sourceList)
            {
                if (!source.replace)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(source.assetObject);

                string[] lines = File.ReadAllLines(projectDir + "/" + assetPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Contains(sourceAssetGUID))
                        continue;

                    foreach (ReplaceKeyValue key in replaceKeyValues)
                    {
                        int keyStartIndex = lines[i].IndexOf(key.key);
                        if (keyStartIndex == -1)
                            continue;

                        int valueStartIndex = keyStartIndex + key.key.Length + 2;
                        int valueEndIndex = lines[i].IndexOf(",", valueStartIndex);
                        if (valueEndIndex == -1)
                            valueEndIndex = lines[i].IndexOf("}", valueStartIndex);

                        if (key.mode == ReplaceKeyValue.EMode.Overwrite)
                        {
                            lines[i] = lines[i].Remove(valueStartIndex, valueEndIndex - valueStartIndex);
                            lines[i] = lines[i].Insert(valueStartIndex, key.value);
                        }
                        else
                        {
                            lines[i] = lines[i].Remove(keyStartIndex, valueEndIndex - keyStartIndex);
                        }
                    }
                }

                File.WriteAllLines(projectDir + "/" + assetPath, lines);
            }
        }
    }
}