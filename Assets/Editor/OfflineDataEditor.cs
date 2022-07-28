using UnityEngine;
using UnityEditor;
namespace 君莫笑
{
    public class OfflineDataEditor
    {
        #region Editor

        [MenuItem("Assets/生成离线数据")]
        public static void AssetCreateOfflineData()
        {
            GameObject[] objects = Selection.gameObjects;
            for (int i = 0; i < objects.Length; i++)
            {
                EditorUtility.DisplayProgressBar("添加离线数据",$"正在修改...{objects[i]}...",1.0f/objects.Length*i);
                CreateOfflineData(objects[i]);
            }
            EditorUtility.ClearProgressBar();
        }
        
        [MenuItem("Assets/生成UI离线数据")]
        public static void AssetCreateUIData()
        {
            GameObject[] objects = Selection.gameObjects;
            for (int i = 0; i < objects.Length; i++)
            {
                EditorUtility.DisplayProgressBar("添加UI离线数据",$"正在修改...{objects[i]}...",1.0f/objects.Length*i);
                CreateUIData(objects[i]);
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Assets/生成全部UI离线数据")]
        public static void AllCreateUIData()
        {
            string path = "Assets/GameData/Prefabs/UGUI/";
            string[] allStr = AssetDatabase.FindAssets("t:Prefab", new string[] {path});
            for (int i = 0; i < allStr.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(allStr[i]);
                EditorUtility.DisplayProgressBar("添加UI离线数据", $"正在扫描路径：{prefabPath}......", 1.0f / allStr.Length * i);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if(obj==null) continue;
                CreateUIData(obj);
            }

            Debug.Log("UI离线数据全部生成完毕！");
            EditorUtility.ClearProgressBar();
        }
        
        [MenuItem("Assets/生成特效离线数据")]
        public static void AssetCreateEffectData()
        {
            GameObject[] objects = Selection.gameObjects;
            for (int i = 0; i < objects.Length; i++)
            {
                EditorUtility.DisplayProgressBar("添加特效离线数据",$"正在修改...{objects[i]}...",1.0f/objects.Length*i);
                CreateEffectData(objects[i]);
            }
            EditorUtility.ClearProgressBar();
        }
        
        [MenuItem("Assets/生成全部特效离线数据")]
        public static void AllCreateEffectData()
        {
            string path = "Assets/GameData/Prefabs/Effect/";
            string[] allStr = AssetDatabase.FindAssets("t:Prefab", new string[] {path});
            for (int i = 0; i < allStr.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(allStr[i]);
                EditorUtility.DisplayProgressBar("添加特效离线数据", $"正在扫描路径：{prefabPath}......", 1.0f / allStr.Length * i);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if(obj==null) continue;
                CreateEffectData(obj);
            }

            Debug.Log("UI离线数据全部生成完毕！");
            EditorUtility.ClearProgressBar();
        }
        #endregion
        


        #region Static Method
        private static void CreateOfflineData(GameObject obj)
        {
            OfflineData offlineData = obj.GetComponent<OfflineData>();
            if (offlineData == null)
            {
                offlineData = obj.AddComponent<OfflineData>();
            }
            offlineData.BindData();
            EditorUtility.SetDirty(obj);
            Debug.Log($"修改了:{obj.name} prefab!");
            Resources.UnloadUnusedAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateUIData(GameObject obj)
        {
            obj.layer = LayerMask.NameToLayer("UI");
            UIOfflineData uiOfflineData = obj.GetComponent<UIOfflineData>();
            if (uiOfflineData == null)
                uiOfflineData = obj.AddComponent<UIOfflineData>();
            
            uiOfflineData.BindData();
            EditorUtility.SetDirty(obj);
            Debug.Log($"修改了：{obj.name} UIOffLineData");
        }        
        
        private static void CreateEffectData(GameObject obj)
        {
            EffectOfflineData effectOfflineData = obj.GetComponent<EffectOfflineData>();
            if (effectOfflineData == null)
                effectOfflineData = obj.AddComponent<EffectOfflineData>();
            
            effectOfflineData.BindData();
            EditorUtility.SetDirty(obj);
            Debug.Log($"修改了：{obj.name} EffectOfflineData");
        }

        #endregion
        
    }
}