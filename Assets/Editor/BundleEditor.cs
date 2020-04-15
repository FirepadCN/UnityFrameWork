using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;
using UnityEditor;
using 君莫笑;

public class BundleEditor : MonoBehaviour
{

    
    /// <summary>
    /// 打包对象目标路径
    /// </summary>
    private static string m_BundleTargetPath = Application.streamingAssetsPath;
    /// <summary>
    /// AB包配置文件所在路径
    /// </summary>
    private static string ABCONFIGPATH = "Assets/Editor/ABConfig.asset";

    //<ab包名,路径>
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();
    //过滤的list 
    private static List<string> m_AllFileAB = new List<string>(); 
    //单个prefab的ab包
    private static Dictionary<string, List<string>> m_AllPrafabDir = new Dictionary<string, List<string>>();

    //储存有效的路径
    private static List<string> m_ConfigFil = new List<string>();

    [MenuItem("Tools/打包(❤ ω ❤)")]
    public static void CreateBundle()
    {
        //simple BuildAsssetBundle
//        BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath,
//            BuildAssetBundleOptions.ChunkBasedCompression,EditorUserBuildSettings.activeBuildTarget);
//        AssetDatabase.Refresh();

        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        m_AllFileDir.Clear();
        m_AllPrafabDir.Clear();
        m_AllFileAB.Clear();
        m_ConfigFil.Clear();

        foreach (var fileDir in abConfig.m_AllFileDirAB)
        {
            Debug.Log(fileDir.ABName + "," + fileDir.Path);
            if (m_AllFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包配置名称重复："+fileDir.ABName);
            }
            else
            {
                m_AllFileDir.Add(fileDir.ABName,fileDir.Path);
                m_AllFileAB.Add(fileDir.Path);
                m_ConfigFil.Add(fileDir.Path);
            }
        }



        string[] allStr = AssetDatabase.FindAssets("t:Prefab", abConfig.m_AllPrefabPath.ToArray());
        for (int i = 0; i < allStr.Length; i++)
        {

            string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
            m_ConfigFil.Add(path);
            EditorUtility.DisplayProgressBar("查找prefab","Prefab:"+path,i*1.0f/allStr.Length);

            if (!ContainAllFileAB(path))
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDepend = AssetDatabase.GetDependencies(path);

                List<string> allDependPath = new List<string>();

                Debug.Log($"alldepend length:{allDepend.Length}");
                for (int j = 0; j < allDepend.Length; j++)
                {
                    if (!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs"))
                    {
                        m_AllFileAB.Add(allDepend[j]);
                        allDependPath.Add(allDepend[j]);
                    }
                }

                if(m_AllPrafabDir.ContainsKey(obj.name))
                {
                    Debug.LogError("存在相同名字的Prefab："+obj.name);
                }
                else
                {
                    m_AllPrafabDir.Add(obj.name,allDependPath);
                }

            }

            
        }

        foreach (var name in m_AllFileDir.Keys)
        {
            SetABName(name,m_AllFileDir[name]);
        }


        foreach (var name in m_AllPrafabDir.Keys)
        {
            SetABName(name,m_AllPrafabDir[name]);
        }

        BuildAssetBundle();

        //清除生产的ab包名，可以防止生成的ab名导致协同工具应.meta文件修改而出问题
        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayCancelableProgressBar("清理AB包名", "名字："+oldABNames[i], i * 0.1f / oldABNames.Length);
        }

        EditorUtility.ClearProgressBar();

    }




    #region 设置AssetBundle命名

    static void SetABName(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null)
            Debug.LogError("不存在此路径文件：" + path);
        else
        {
            assetImporter.assetBundleName = name;
        }
    }


    static void SetABName(string name, List<string> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"name: {name}");
            SetABName(name, path[i]);
        }
    }

    #endregion


    static bool ContainAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAB.Count; i++)
        {
            if (path == m_AllFileAB[i] || path.Contains(m_AllFileAB[i])&&path.Replace(m_AllFileAB[i],"")[0]=='/')//Asset/Attack   Asset/Attackss/ss.png
                return true;
        }

        return false;
    }

    static void BuildAssetBundle()
    {
        string[] allbundles = AssetDatabase.GetAllAssetBundleNames();

        //<path,name>
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for (int i = 0; i < allbundles.Length; i++)
        {
            string[] allBundlesPath = AssetDatabase.GetAssetPathsFromAssetBundle(allbundles[i]);
            for (int j = 0; j < allBundlesPath.Length; j++)
            {
                if(allBundlesPath[j].EndsWith(".cs"))
                    continue;

                Debug.Log($"此AB包：{allbundles[i]}下面包含的资源文件路径：{allBundlesPath[j]}");
                if (ValidPath(allBundlesPath[j]))
                {
                    resPathDic.Add(allBundlesPath[j],allbundles[i]);
                }
            }
        }

        //删除已存在并不需要再次打包的AB包资源
        DeletAB();

        //生成自己配置表
        WriteData(resPathDic);

        BuildPipeline.BuildAssetBundles(m_BundleTargetPath,
            BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }


    /// <summary>
    /// 删除此次打包不需要的AB包，打AB包为增量打包，保留此次打包中仍需要打包的ab包文件可以提高打包速度
    /// </summary>
    static void DeletAB()
    {
        string[] allbundlesName = AssetDatabase.GetAllAssetBundleNames();

        DirectoryInfo directory=new DirectoryInfo(m_BundleTargetPath);
        FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            if (ContainABName(files[i].Name, allbundlesName) || files[i].Name.EndsWith(".meta"))//删除.meta对应自以后，unity会自动清理.meta文件
                continue;
            else
            {
                Debug.Log("此AB包已被删除或改名:"+files[i].Name);
                if(File.Exists(files[i].FullName))
                    File.Delete(files[i].FullName);
            }

        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="resPathDir"></param>
    static void WriteData(Dictionary<string, string> resPathDir)
    {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();

        foreach (var path in resPathDir.Keys)
        {
            ABBase abBase = new ABBase();
            abBase.Path = path;
            abBase.Crc = CRC32.GetCRC32(path);
            abBase.ABName = resPathDir[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);

            abBase.ABDenpends = new List<string>();

            string[] resDependance = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDependance.Length; i++)
            {
                string temPath = resDependance[i];
                if(temPath==path||path.EndsWith(".cs"))
                    continue;
                string abName = "";
                if (resPathDir.TryGetValue(temPath, out abName))
                {
                    if(abName==resPathDir[path])continue;
                    ;
                    if(!abBase.ABDenpends.Contains(abName))
                        abBase.ABDenpends.Add(abName);
                }
            }

            config.ABList.Add(abBase);

            
        }

        //Fixed
        //xml
        string xmlPath = Application.dataPath + "/AssetBundleConfig.xml";
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xs = new XmlSerializer(config.GetType());
        xs.Serialize(sw, config);
        sw.Close();
        fileStream.Close();

        //二进制

        //可通过crc校验，去掉path信息，减少二进制文件冗余
        foreach (var abbase in config.ABList)
        {
            abbase.Path = "";
        }

        string bytePath = "Assets/GameData/Data/ABdata/AssetBundleConfig.bytes";
        FileStream fs = new FileStream(bytePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, config);
        fs.Close();
    }


    /// <summary>
    /// strs中是否存在name，打AB包为增量打包，保留此次打包中仍需要打包的ab包可以提高打包速度
    /// </summary>
    /// <returns></returns>
    static bool ContainABName(string name, string[] strs)
    {
        for (int i = 0; i < strs.Length; i++)
        {
            if (name==strs[i])
                return true;
        }

        return false;
    }

    /// <summary>
    /// 是否为有效路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ValidPath(string path)
    {
        for (int i = 0; i < m_ConfigFil.Count; i++)
        {
            if (path.Contains(m_ConfigFil[i]))
            {
                return true;
            }
        }

        return false;
    }

}
