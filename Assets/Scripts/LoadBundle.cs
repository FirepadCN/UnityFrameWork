using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using 君莫笑;


public class LoadBundle : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
       // AssetBundle bundle= AssetBundle.LoadFromFile(Application.streamingAssetsPath+"/attack");
        //GameObject go = GameObject.Instantiate(bundle.LoadAsset<GameObject>("Attack"));

        TestLoadAB();
        //XmlSerilize(SerilizeTest());

       //Debug.Log(XmlDeSerilize().ToString());

        //BinarySerilize(SerilizeTest());

        //Debug.Log(BinaryDeSerialize().ToString());

        //Debug.Log(ReadAsset().Name);
    }


    void TestLoadAB()
    {
        AssetBundle asset=AssetBundle.LoadFromFile(Application.streamingAssetsPath+"/AssetBundleConfig");
        TextAsset t = asset.LoadAsset<TextAsset>("AssetBundleConfig");

        MemoryStream stream = new MemoryStream(t.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig abc = bf.Deserialize(stream) as AssetBundleConfig;
        stream.Close();
        string path = "Assets/GameData/Prefabs/Attack.prefab";
        uint crc = CRC32.GetCRC32(path);
        ABBase abBase = null;

        for (int i = 0; i < abc.ABList.Count; i++)
        {
            if (abc.ABList[i].Crc == crc)
            {
                abBase = abc.ABList[i];
            }
        }

        for (int i = 0; i < abBase.ABDenpends.Count; i++)
        {
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABDenpends[i]);

        }

        AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABName);
        GameObject go = Instantiate(assetBundle.LoadAsset<GameObject>(abBase.AssetName), Vector3.forward, Quaternion.identity);
    }

    TestSerilize SerilizeTest()
    {
        TestSerilize testSerilize = new TestSerilize();
        testSerilize.Id = 1;
        testSerilize.Name = "name";
        testSerilize.List = new List<int>();
        testSerilize.List.Add(1);
        testSerilize.List.Add(2);
        testSerilize.List.Add(3);
        return testSerilize;
    }

    void XmlSerilize(TestSerilize testSerilize)
    {
        FileStream fileStream = new FileStream(Application.dataPath + "/test.xml", FileMode.Create,
            FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw=new StreamWriter(fileStream,System.Text.Encoding.UTF8);
        XmlSerializer xml = new XmlSerializer(testSerilize.GetType());
        xml.Serialize(sw,testSerilize);
        sw.Close();
        fileStream.Close();
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

    }

    TestSerilize XmlDeSerilize()
    {
        FileStream fileStream = new FileStream(Application.dataPath + "/test.xml", FileMode.Open,
            FileAccess.ReadWrite, FileShare.ReadWrite);
        XmlSerializer xml = new XmlSerializer(typeof(TestSerilize));
        TestSerilize testSerilize= xml.Deserialize(fileStream) as TestSerilize;
        fileStream.Close();
        return testSerilize;
    }

    void BinarySerilize(TestSerilize testSerilize)
    {
        FileStream fileStream = new FileStream(Application.dataPath + "/test.bytes", FileMode.Create,
            FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fileStream,testSerilize);
        fileStream.Close();

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

    }

    TestSerilize BinaryDeSerialize()
    {
        FileStream fileStream = new FileStream(Application.dataPath + "/test.bytes", FileMode.Open,
            FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        return bf.Deserialize(fileStream) as TestSerilize;
    }

#if UNITY_EDITOR
    AssetSerialize ReadAsset()
    {
        AssetSerialize asset = AssetDatabase.LoadAssetAtPath<AssetSerialize>("Assets/AssetSerialize.asset");
        return asset;
    }
#endif

}
