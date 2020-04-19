using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace 君莫笑
{
    public class AssetBundleManager : Singleton<AssetBundleManager>
    {
        protected AssetBundleManager()
        {

        }

        /// <summary>
        /// 加载资源AB配置表，将其中的ab包姐依赖信息转存在m_ResourceItemDic中
        /// </summary>
        protected Dictionary<uint,ResourceItem> m_ResourceItemDic= new Dictionary<uint, ResourceItem>();

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<uint,AssetBundleItem> m_AssetBundleItemDic=new Dictionary<uint, AssetBundleItem>();


        protected ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool; //Fix:=ObjectManager.Instance.GetOrCreateClassPool<AssetBundleItem>(500);不能在这里调用单例，单例中用了unity下的FindObjectByType

        void Awake()
        {
            m_AssetBundleItemPool =ObjectManager.Instance.GetOrCreateClassPool<AssetBundleItem>(500);
        }

        #region 处理AB包加载

        /// <summary>
        /// 加载AB配置表，存储其中AB包及依赖信息到m_ResourceItemDic
        /// </summary>
        /// <returns>加载是否成功</returns>
        public bool LoadAssetBundleConfig()
        {
            string configPath = Application.streamingAssetsPath + "/assetbundleconfig";
            AssetBundle configAB = AssetBundle.LoadFromFile(configPath);
            TextAsset textAsset = configAB.LoadAsset<TextAsset>("assetbundleconfig");
            if (textAsset == null)
            {
                Debug.LogError("AssetBundleConfig is not exit!");
                return false;
            }

            //TODO;SerializerHelper.cs
            MemoryStream stream = new MemoryStream(textAsset.bytes);
            BinaryFormatter bf = new BinaryFormatter();
            AssetBundleConfig abc = bf.Deserialize(stream) as AssetBundleConfig;
            stream.Close();

            for (int i = 0; i < abc.ABList.Count; i++)
            {
                ABBase abBase = abc.ABList[i];
                ResourceItem item = new ResourceItem();
                item.m_Crc = abBase.Crc;
                item.m_ABName = abBase.ABName;
                item.m_AssetName = abBase.AssetName;
                item.m_DependAssetBundle = abBase.ABDenpends;

                if (m_ResourceItemDic.ContainsKey(item.m_Crc))
                {
                    Debug.LogError("重复的Crc:" + $"{item.m_AssetName} --- {item.m_ABName}");
                }
                else
                {

                    m_ResourceItemDic.Add(item.m_Crc, item);
                }
            }

            return true;
        }

        /// <summary>
        /// 依据crc加载AssetBundle资源及其依赖资源
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        public ResourceItem LoatResourceAssetBundle(uint crc)
        {
            ResourceItem item = null;

            if (!m_ResourceItemDic.TryGetValue(crc, out item) || item == null)
            {
                Debug.LogError($"LoatResourceAssetBundle erro:can not find crc{crc} or resourceitem is null ");
                return item;
            }

            if (item.m_AssetBundle != null)
            {
                return item;
            }

            item.m_AssetBundle = LoadAssetBundle(item.m_ABName);

            if (item.m_DependAssetBundle != null)
            {
                for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
                {
                    LoadAssetBundle(item.m_DependAssetBundle[i]);
                }
            }

            return item;
        }

        /// <summary>
        /// 依据name加载Assetbundle文件，并使用对象池存储AssetBundleItem
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private AssetBundle LoadAssetBundle(string name)
        {
            AssetBundleItem item = null;
            uint crc = CRC32.GetCRC32(name);

            if (!m_AssetBundleItemDic.TryGetValue(crc, out item))
            {
                string fullPath = Application.streamingAssetsPath + "/" + name;
                AssetBundle assetBundle = null;

                if (File.Exists(fullPath))
                {
                    assetBundle = AssetBundle.LoadFromFile(fullPath);
                }

                if (assetBundle == null)
                {
                    Debug.Log("Load AssetBundle Error assetbundle is null:" + fullPath);
                }

                item = m_AssetBundleItemPool.Spawn(true);
                item.AssetBundle = assetBundle;
                m_AssetBundleItemDic.Add(crc, item);
            }

            item.RefCount++;

            return item.AssetBundle;
        }

        #endregion


        #region 处理AB包释放

        /// <summary>
        /// 依据resourceitem信息释放ab包及其依赖资源
        /// </summary>
        /// <param name="item">resourceitem</param>
        public void ReleaseAsset(ResourceItem item)
        {
            if (item == null)
            {
                return;
            }

            if (item.m_DependAssetBundle != null && item.m_DependAssetBundle.Count > 0)
            {
                for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
                {
                    UnLoadAssetBundle(item.m_DependAssetBundle[i]);
                }
            }

            UnLoadAssetBundle(item.m_ABName );
        }

        /// <summary>
        /// 通过crc释放ab包,并通过对象池回收assetbundleitem资源
        /// </summary>
        /// <param name="name"></param>
        private void UnLoadAssetBundle(string name)
        {
            AssetBundleItem item = null;
            uint crc = CRC32.GetCRC32(name);
            if (m_AssetBundleItemDic.TryGetValue(crc, out item) && item != null)
            {
                item.RefCount--;

                if (item.RefCount <= 0 && item.AssetBundle != null)
                {
                    item.AssetBundle.Unload(true);
                    item.Reset();
                    m_AssetBundleItemPool.Recycle(item);
                    m_AssetBundleItemDic.Remove(crc);

                }
            }
        }

        /// <summary>
        /// 根据crc找到Resourceitem
        /// </summary>
        /// <param name="crc">crc</param>
        /// <returns>resourceitem</returns>
        public ResourceItem FindResourceItem(uint crc)
        {
            return m_ResourceItemDic[crc];
        }

        #endregion
    }

    /// <summary>
    /// AB项，存储AB包和自身引用次数
    /// </summary>
    public class AssetBundleItem
    {
        public AssetBundle AssetBundle = null;
        public int RefCount;

        public void Reset()
        {
            AssetBundle = null;
            RefCount = 0;
        }
    }


    /// <summary>
    /// AB资源项，存储ab资源信息和依赖项
    /// </summary>
    public class ResourceItem
    {
        public uint m_Crc = 0;
        public string m_AssetName = string.Empty;
        public string m_ABName = string.Empty;
        public List<string> m_DependAssetBundle = null;

        //------------------------------

        public AssetBundle m_AssetBundle = null;
        //资源对象
        public UnityEngine.Object m_Obj = null;
        //资源唯一标识符
        public int m_Guid = 0;
        //资源最后使用的时间
        public float m_LastUseTime = 0.0f;
        //引用计数
        protected int m_RerCount=0;

        //是否切换场景清空
        public bool m_Clear = true;

        public int RefCount
        {
            get { return m_RerCount;}
            set
            {
                m_RerCount = value;
                if(m_RerCount<0)
                    Debug.LogError("refcount < 0"+m_RerCount+","+(m_Obj!=null?m_Obj.name:"name is null"));
            }
        }
    }
}


