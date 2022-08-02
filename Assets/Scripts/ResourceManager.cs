using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR 
using UnityEditor;
#endif
namespace 君莫笑
{
    /// <summary>
    /// 优先级
    /// </summary>
    public enum LoadResPriority
    {
        RES_HIGHT=0,
        RES_MIDDLE=1,
        RES_LOW=2,
        RES_NUM,
    }

    /// <summary>
    /// 实例化对象中间类
    /// </summary>
    public class ResourceObj
    {
        public uint m_Crc = 0;
        //存ResourceItem
        public ResourceItem m_ResItem = null;
        //实例化出来的GameObject
        public GameObject m_CloneObj = null;
        /// <summary>
        /// 是否跳场景清除
        /// </summary>
        public bool m_bClear = true;
        /// <summary>
        /// 唯一资源标识符
        /// </summary>
        public long m_Guid = 0;
        /// <summary>
        /// 是否已被释放
        /// </summary>
        public bool m_Already = false;

        //--------------------------
        public bool m_setSceneParent = false;

        //资源加载完成回调
        public OnAsyncObjFinish m_DealFnish = null;
        //异步参数
        public object m_param1, m_param2, m_param3 = null;

        /// <summary>
        /// 离线数据
        /// </summary>
        public OfflineData m_OfflineData = null;

        public void Reset()
        {
            m_Crc = 0;
            m_CloneObj = null;
            m_bClear = true;
            m_Guid = 0;
            m_setSceneParent = false;
            m_DealFnish = null;
            m_param1=m_param2= m_param3 = null;
            m_OfflineData = null;
        }
    }

    public class AsyncLoadResParam
    {
        public List<AsyncCallBack> m_CallBackList= new List<AsyncCallBack>();
        public uint m_Crc;
        public string m_Path;
        public bool m_Sprite = false;
        public LoadResPriority m_Priority=LoadResPriority.RES_LOW;

        public void Reset()
        {
            m_CallBackList.Clear();
            m_Crc = 0;
            m_Path = "";
            m_Sprite = false;
            m_Priority=LoadResPriority.RES_LOW;
        }
    }

    public class AsyncCallBack
    {
        //加载回传的回调object
        public OnAsyncObjFinish m_DealObjectFinish = null;

        //加载完成回到objectmananger
        public OnAsyncFinish m_DealFinish = null;
        public ResourceObj m_ResObj = null;

        //回调参数
        public object m_Param1 = null;
        public object m_Param2 = null;
        public object m_Param3 = null;

        public void Reset()
        {
            m_DealObjectFinish = null;
            m_DealFinish = null;
            m_Param1 = null;
            m_Param2 = null;
            m_Param3 = null;

            m_ResObj = null;
        }
    }

    //资源加载完成回调
    public delegate void OnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null,object param3 = null);

    //实例化对象加载完成回调
    public delegate void OnAsyncFinish(string path, ResourceObj obj, object param1 = null, object param2 = null,object param3 = null);

    public class ResourceManager : Singleton<ResourceManager>
    {
        //单例需要
        protected ResourceManager()
        {
        }

        protected long m_GUID=0;

        public bool m_LoadFromAssetBundle = false;
        //缓存使用的资源列表
        public Dictionary<uint,ResourceItem> AssetDic =new Dictionary<uint, ResourceItem>();

        //缓存引用计数为零的列表，达到缓存最大的时候，释放最早缓存的资源
        protected CMapList<ResourceItem> m_NoRefrenceAssetMapList = new CMapList<ResourceItem>();


        // 中间类，回调类的类对象池
        protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool=new ClassObjectPool<AsyncLoadResParam>(50);
        protected ClassObjectPool<AsyncCallBack> m_AsyncCallBackPool=new ClassObjectPool<AsyncCallBack>(100);

        protected MonoBehaviour m_Startmono;
        //正在异步加载的资源列表
        protected List<AsyncLoadResParam>[] m_LoadingAssetList=new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
        //宅异步加载的Dic
        protected Dictionary<uint,AsyncLoadResParam> m_LoadingAssetDic=new Dictionary<uint, AsyncLoadResParam>();

        //最长连续卡着加载的时间
        private const long MAXLOADRESTIME = 200000;

        /// <summary>
        /// 最大缓存个数
        /// </summary>
        private const int MAXCACHECOUNT = 500;

        public void Init(MonoBehaviour mono)
        {
            for (int i = 0; i < ((int) LoadResPriority.RES_NUM);i++)
            {
                m_LoadingAssetList[i] = new List<AsyncLoadResParam>();

            }

            m_Startmono = mono;
            m_Startmono.StartCoroutine(AsyncLoadCor());
        }


        /// <summary>
        /// 创建唯一ID
        /// </summary>
        /// <returns></returns>
        public long CreateGUID()
        {
            return m_GUID++;
        }


        /// <summary>
        /// 清空缓存
        /// </summary>
        public void ClearCache()
        {
            List<ResourceItem> tempList=new List<ResourceItem>();
            foreach (var item in AssetDic.Values)
            {
                if(item.m_Clear)
                    tempList.Add(item);
            }

            foreach (ResourceItem resourceItem in tempList)
            {
                DestoryResourceItem(resourceItem,true);
            }

            tempList.Clear();


//            while (m_NoRefrenceAssetMapList.Size()>0)
//            {
//                ResourceItem item = m_NoRefrenceAssetMapList.Back();
//                DestoryResourceItem(item,true);
//                m_NoRefrenceAssetMapList.Pop();
//            }
        }

        /// <summary>
        /// 取消异步加载资源
        /// </summary>
        /// <returns></returns>
        public bool CancleLoad(ResourceObj res)
        {
            AsyncLoadResParam para = null;
            if (m_LoadingAssetDic.TryGetValue(res.m_Crc, out para) &&
                m_LoadingAssetList[(int) para.m_Priority].Contains(para))
            {
                for (int i = para.m_CallBackList.Count; i > 0; i--)
                {
                    AsyncCallBack tempCallBack = para.m_CallBackList[i];
                    if (tempCallBack != null && res == tempCallBack.m_ResObj)
                    {
                        tempCallBack.Reset();
                        m_AsyncCallBackPool.Recycle(tempCallBack);
                        para.m_CallBackList.Remove(tempCallBack);
                    }

                    if (para.m_CallBackList.Count <= 0)
                    {
                        para.Reset();
                        m_LoadingAssetList[(int) para.m_Priority].Remove(para);
                        m_AsyncLoadResParamPool.Recycle(para);
                        m_LoadingAssetDic.Remove(res.m_Crc);
                        return true;
                    }
                }
            }

            return false;
        }


        #region 引用计数增减操作

        /// <summary>
        /// 根据ResObj增加引用计数
        /// </summary>
        /// <param name="resObj"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int IncreaseResourceRef(ResourceObj resObj, int count = 1)
        {
            return resObj != null ? IncreaseResourceRef(resObj.m_Crc, count) : 0;
        }

        /// <summary>
        /// 根据path增加引用计数
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int IncreaseResourceRef(uint crc, int count = 1)
        {
            ResourceItem item = null;
            if (!AssetDic.TryGetValue(crc, out item) || item == null)
                return 0;

            item.RefCount += count;
            item.m_LastUseTime = Time.realtimeSinceStartup;

            return item.RefCount;
        }

        /// <summary>
        /// 根据ResourceObj减少引用计数
        /// </summary>
        /// <param name="resObj"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int DecreaseResourceRef(ResourceObj resObj, int count = 1)
        {
            return resObj != null ? DecreaseResourceRef(resObj.m_Crc, count) : 0;
        }

        /// <summary>
        /// 根据路径减少引用计数
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int DecreaseResourceRef(uint crc, int count = 1)
        {
            ResourceItem item = null;
            if (!AssetDic.TryGetValue(crc, out item) || item == null)
                return 0;

            item.RefCount -= count;
            item.m_LastUseTime = Time.realtimeSinceStartup;

            return item.RefCount;
        }

        #endregion


        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="path"></param>
        public void PreLoadRes(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            
            uint crc = CRC32.GetCRC32(path);


            ResourceItem item = GetCacheResourrceItem(crc,0);

            if (item != null)
            {
                return;
            }

            Object obj = null;

#if UNITY_EDITOR
            if (!m_LoadFromAssetBundle)
            {
                item = AssetBundleManager.Instance.FindResourceItem(crc);
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj;
                }
                else
                {
                    obj = LoadAssetByEditor<Object>(path);

                }
            }
#endif

            if (obj == null)
            {
                item = AssetBundleManager.Instance.LoatResourceAssetBundle(crc);
                if (item != null && item.m_AssetBundle != null)
                {
                    if (item.m_Obj != null)
                    {
                        obj = item.m_Obj;
                    }
                    else
                    {
                        obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                    }
                }
            }

            CacheResource(path, ref item, crc, obj);
            
            //跳场景不清空缓存
            item.m_Clear = false;
            ReleaseResource(obj, false);

        }

        //同步资源加载，外部直接调用，仅加载不需要实例化的资源，如：texure,音频文等
        public T LoadResource<T>(string path) where T : UnityEngine.Object
        {
            if(string.IsNullOrEmpty(path))
                return null;
            uint crc = CRC32.GetCRC32(path);


            ResourceItem item = GetCacheResourrceItem(crc);

            if (item != null)
            {
                return item.m_Obj as T;
            }

            T obj = null;

#if UNITY_EDITOR
            if(!m_LoadFromAssetBundle)
            {
                item = AssetBundleManager.Instance.FindResourceItem(crc);
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as T;
                }
                else
                {
                obj = LoadAssetByEditor<T>(path);

                }
            }
#endif

            if (obj == null)
            {
                item = AssetBundleManager.Instance.LoatResourceAssetBundle(crc);
                if (item != null && item.m_AssetBundle != null)
                {
                    if (item.m_Obj != null)
                    {
                        obj = item.m_Obj as T;
                    }
                    else
                    {
                        obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                    }
                }
            }

            CacheResource(path,ref item,crc,obj);

            return obj;
        }

        /// <summary>
        /// 同步加载资源，针对给ObjectManager接口
        /// </summary>
        /// <param name="path"></param>
        /// <param name="resObj"></param>
        /// <returns></returns>
        public ResourceObj LoadResource(string path, ResourceObj resObj)
        {
            if (resObj == null)
                return null;

            uint crc = resObj.m_Crc == 0 ? CRC32.GetCRC32(path) : resObj.m_Crc;

            ResourceItem item = GetCacheResourrceItem(crc);

            if (item != null)
            {
                resObj.m_ResItem = item;
                return resObj;
            }

            Object obj = null;

#if UNITY_EDITOR
            if (!m_LoadFromAssetBundle)
            {
                item = AssetBundleManager.Instance.FindResourceItem(crc);

                if (item!=null&&item.m_Obj != null)
                    obj = item.m_Obj as Object;
                else
                {
                    if (item == null)
                        item = new ResourceItem {m_Crc = crc};
                    obj = LoadAssetByEditor<Object>(path);
                }
            }
#endif

            if (obj == null)
            {
                item = AssetBundleManager.Instance.LoatResourceAssetBundle(crc);
                if (item.m_Obj != null)
                    obj = item.m_Obj as Object;
                else
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_ABName);
            }

            CacheResource(path,ref item,crc,obj);
            resObj.m_ResItem = item;
            item.m_Clear = resObj.m_bClear;
            return resObj;
        }

        /// <summary>
        /// 当前内存使用大于80%的时候清除最早没有使用的资源
        /// </summary>
        protected void WashOut()
        {
            while (m_NoRefrenceAssetMapList.Size()>=MAXCACHECOUNT)
            {
                for (int i = 0; i < MAXCACHECOUNT; i++)
                {
                    ResourceItem item = m_NoRefrenceAssetMapList.Back();
                    DestoryResourceItem(item,true);                    
                }
            }
        }


        /// <summary>
        /// 回收一个资源
        /// </summary>
        /// <param name="item"></param>
        /// <param name="destorycache"></param>
        protected void DestoryResourceItem(ResourceItem item, bool destorycache = false)
        {
            if (item == null || item.RefCount > 0) return;

            if (!destorycache)
            {
                m_NoRefrenceAssetMapList.InsertToHead(item);
                return;
            }

            if (!AssetDic.Remove(item.m_Crc))
            {
                return;
            }

            m_NoRefrenceAssetMapList.Remove(item);
            //释放Assetbundle引用
            AssetBundleManager.Instance.ReleaseAsset(item);
            
            //清空资源对应的对象池
            ObjectManager.Instance.ClearPoolObject(item.m_Crc);

            if (item.m_Obj != null)
            {
                item.m_Obj = null;
#if UNITY_EDITOR
                Resources.UnloadUnusedAssets();
#endif
            }

        }

        /// <summary>
        /// 不需要实例化资源的卸载，根据对象
        /// </summary>
        /// <returns></returns>
        public bool ReleaseResource(Object obj, bool destoryObj = false)
        {
            if (obj == null) return false;

            ResourceItem item = null;
            foreach (var res in AssetDic.Values)
            {
                if (res.m_Guid == obj.GetInstanceID())
                {
                    item = res;
                }
            }
            if (item == null)
            {
                Debug.LogError($"AssetDic里不存在资源： {obj.name},可能多次释放");
                return false;
            }

            item.RefCount--;
            DestoryResourceItem(item, destoryObj);
            return true;

        }

        /// <summary>
        /// 释放需要实例化的资源,根据
        /// </summary>
        /// <param name="resobj"></param>
        /// <param name="destoryObj"></param>
        /// <returns></returns>
        public bool ReleaseResource(ResourceObj resobj, bool destoryObj = false)
        {
            if (resobj == null) return false;

            ResourceItem item = null;

            if (!AssetDic.TryGetValue(resobj.m_Crc,out item)||null==item)
            {
                Debug.LogError($"AssetDic里不存在资源： {resobj.m_CloneObj.name},可能多次释放");
            }

            GameObject.Destroy(resobj.m_CloneObj);

            item.RefCount--;
            DestoryResourceItem(item,destoryObj);
            return true;
        }

        /// <summary>
        /// 不需要实例化的资源协助,根据路径
        /// </summary>
        /// <param name="path"></param>
        /// <param name="destoryObj"></param>
        /// <returns></returns>
        public bool ReleaseResource(string path, bool destoryObj = false)
        {
            if (string.IsNullOrEmpty(path)) return false;

            uint crc = CRC32.GetCRC32(path);


            ResourceItem item = null;

            if (!AssetDic.TryGetValue(crc, out item) || item == null)
            {
                Debug.LogError($"AssetDic里不存在资源： {path},可能多次释放");
                return false;
            }

            item.RefCount--;
            DestoryResourceItem(item, destoryObj);
            return true;

        }



        /// <summary>
        /// 缓存加载的资源
        /// </summary>
        void CacheResource(string path, ref ResourceItem item, uint crc, Object obj,int addrefcount=1)
        {
            //缓存过多时，清除最早没有使用的资源
            if(item==null)
                Debug.LogError($"item is null,item path：{path}");

            if(obj==null)
                Debug.LogError($"Resource load failed! path:{path}");

            item.m_Crc = crc;
            item.m_Obj = obj;
            item.m_Guid = obj.GetInstanceID();
            item.m_LastUseTime = Time.realtimeSinceStartup;
            item.RefCount += addrefcount;

            ResourceItem oldItem = null;
            if (AssetDic.TryGetValue(item.m_Crc, out oldItem))
            {
                AssetDic[item.m_Crc] = item;
            }
            else
            {
                AssetDic.Add(item.m_Crc,item);
            }

        }


#if UNITY_EDITOR
        protected T LoadAssetByEditor<T>(string path) where T:UnityEngine.Object
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif

        ResourceItem GetCacheResourrceItem(uint crc, int addrefcount = 1)
        {
            ResourceItem item = null;
            if (AssetDic.TryGetValue(crc, out item))
            {
                item.RefCount += addrefcount;
                item.m_LastUseTime = Time.realtimeSinceStartup;
            }

            return item ;
        }


        /// <summary>
        /// 异步加载资源（仅仅是不需要实例化的资源，如音频，图片等等）
        /// </summary>
        public void AsyncLoadResource(string path,OnAsyncObjFinish dealFinish,LoadResPriority priority,object p1=null, object p2 = null, object p3 = null,uint crc=0)
        {
            if (crc == 0)
            {
                crc = CRC32.GetCRC32(path);
            }

            ResourceItem item = GetCacheResourrceItem(crc);


            if (item != null)
            {
                if (dealFinish != null)
                {
                    dealFinish(path, item.m_Obj, p1, p2, p3);
                }
                return;
            }

            //判断是否在加载中
            AsyncLoadResParam para = null;
            if (!m_LoadingAssetDic.TryGetValue(crc, out para) || para == null)
            {
                para = m_AsyncLoadResParamPool.Spawn(true);
                para.m_Crc = crc;
                para.m_Path = path;
                para.m_Priority = priority;

                m_LoadingAssetDic.Add(crc, para);
                m_LoadingAssetList[(int)priority].Add(para);

            }

            //往回调列表里面添加回调
            AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
            callBack.m_DealObjectFinish = dealFinish;
            callBack.m_Param1 = p1;
            callBack.m_Param2 = p2;
            callBack.m_Param3 = p3;
            para.m_CallBackList.Add(callBack);
        }


        /// <summary>
        /// 针对ObjectManager的异步加载接口
        /// </summary>
        public void AsyncLoadResource(string path, ResourceObj resObj, OnAsyncFinish dealFinish,
            LoadResPriority priority)
        {
            ResourceItem item = GetCacheResourrceItem(resObj.m_Crc);

            if (item != null)
            {
                resObj.m_ResItem = item;
                if (dealFinish != null)
                    dealFinish(path,resObj);
            }

            //判断是否在加载中
            AsyncLoadResParam para = null;
            if (!m_LoadingAssetDic.TryGetValue(resObj.m_Crc, out para) || para == null)
            {
                para = m_AsyncLoadResParamPool.Spawn(true);
                para.m_Crc = resObj.m_Crc;
                para.m_Path = path;
                para.m_Priority = priority;

                m_LoadingAssetDic.Add(resObj.m_Crc, para);
                m_LoadingAssetList[(int)priority].Add(para);

            }


            //往回调列表里面添加回调
            AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
            callBack.m_DealFinish = dealFinish;
            callBack.m_ResObj = resObj;

            para.m_CallBackList.Add(callBack);

        }

        /// <summary>
        /// 异步加载
        /// </summary>
        /// <returns></returns>
        IEnumerator AsyncLoadCor()
        {
            List<AsyncCallBack> callBackList = null;
            //上一次yield的时间
            long lastYieldTime = System.DateTime.Now.Ticks;

            while (true)
            {
                bool haveYield = false;

                for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
                {
                    List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                    if (loadingList.Count <= 0)
                        continue;

                    AsyncLoadResParam loadingItem = loadingList[0];
                    loadingList.RemoveAt(0);
                    callBackList = loadingItem.m_CallBackList;

                    Object obj = null;
                    ResourceItem item = null;
#if UNITY_EDITOR
                    if (!m_LoadFromAssetBundle)
                    {
                        obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                        //模拟异步加载
                        yield return new WaitForSeconds(0.5f);

                        item = AssetBundleManager.Instance.FindResourceItem(loadingItem.m_Crc);

                    }
#endif
                    if (obj == null)
                    {
                        item = AssetBundleManager.Instance.LoatResourceAssetBundle(loadingItem.m_Crc);
                        if (item != null && item.m_AssetBundle != null)
                        {
                            AssetBundleRequest abRequest=null;

                            if (loadingItem.m_Sprite)
                            {
                                abRequest = item.m_AssetBundle.LoadAssetAsync<Sprite>(item.m_AssetName);
                            }
                            else
                            {
                                abRequest = item.m_AssetBundle.LoadAssetAsync(item.m_AssetName);

                            }
                            yield return abRequest;

                            if (abRequest.isDone)
                            {
                                obj = abRequest.asset;
                            }

                            lastYieldTime = System.DateTime.Now.Ticks;
                        }
                    }

                    //缓存资源
                    CacheResource(loadingItem.m_Path,ref item,loadingItem.m_Crc,obj,callBackList.Count);

                    for (int j = 0; j < callBackList.Count; j++)
                    {
                        AsyncCallBack callBack = callBackList[j];

                        if (callBack != null && callBack.m_DealFinish != null && callBack.m_ResObj != null)
                        {
                            ResourceObj tempResourceObj = callBack.m_ResObj;
                            tempResourceObj.m_ResItem = item;
                            callBack.m_DealFinish(loadingItem.m_Path, tempResourceObj, tempResourceObj.m_param1,
                                tempResourceObj.m_param2, tempResourceObj.m_param3);
                            callBack.m_DealFinish = null;
                            tempResourceObj = null;
                        }

                        if (callBack != null && callBack.m_DealObjectFinish != null)
                        {
                            callBack.m_DealObjectFinish(loadingItem.m_Path, obj, callBack.m_Param1, callBack.m_Param2,
                                callBack.m_Param3);
                            callBack.m_DealObjectFinish = null;
                        }

                        callBack.Reset();
                        m_AsyncCallBackPool.Recycle(callBack);

                    }

                    obj = null;
                    callBackList.Clear();
                    m_LoadingAssetDic.Remove(loadingItem.m_Crc);

                    loadingItem.Reset();
                    m_AsyncLoadResParamPool.Recycle(loadingItem);

                    if (System.DateTime.Now.Ticks - lastYieldTime > MAXLOADRESTIME)
                    {
                        haveYield = true;
                        lastYieldTime = System.DateTime.Now.Ticks;
                        yield return null;
                    }
                }

                if (!haveYield||System.DateTime.Now.Ticks - lastYieldTime >MAXLOADRESTIME)
                {
                    lastYieldTime = System.DateTime.Now.Ticks;
                    yield return null;
                }
            }
        }

    }

    /// <summary>
    /// 双向链表节点结构
    /// </summary>
    public class DoubleLinkListNode<T> where T : class, new()
    {
        public DoubleLinkListNode<T> prev = null;
        public DoubleLinkListNode<T> next = null;
        public T t = null;
    }

    /// <summary>
    /// 双向链表结构
    /// </summary>
    public class DoubleLinkedList<T> where T : class, new()
    {
        public DoubleLinkListNode<T> Head = null;
        public DoubleLinkListNode<T> Tail = null;

        /// <summary>
        /// 双向链表结构类对象池
        /// </summary>
        protected ClassObjectPool<DoubleLinkListNode<T>> m_DoubleLinkNodePool =
            ObjectManager.Instance.GetOrCreateClassPool<DoubleLinkListNode<T>>(500);

        protected int m_Count = 0;

        public int Count
        {
            get { return m_Count; }
        }

        /// <summary>
        /// 添加一个头部节点
        /// </summary>
        public DoubleLinkListNode<T> AddToHeader(T t)
        {
            DoubleLinkListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
            pList.next = null;
            pList.prev = null;
            pList.t = t;
            return AddToHeader(pList);
        }
        
        /// <summary>
        /// 添加一个头部节点
        /// </summary>
        public DoubleLinkListNode<T> AddToHeader(DoubleLinkListNode<T> pNode)
        {
            if (pNode == null)
                return null;

            pNode.prev = null;
            if (Head == null)
            {
                Head = Tail = pNode;
            }
            else
            {
                pNode.next = Head;
                Head.prev = pNode;
                Head = pNode;
            }

            m_Count++;
            return Head;
        }

        /// <summary>
        /// 添加一个节点到尾部
        /// </summary>
        public DoubleLinkListNode<T> AddToTail(T t)
        {
            DoubleLinkListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
            pList.next = null;
            pList.prev = null;
            pList.t = t;
            return AddToTail(pList);
        }

        /// <summary>
        /// 添加节点到尾部
        /// </summary>
        public DoubleLinkListNode<T> AddToTail(DoubleLinkListNode<T> pNode)
        {
            if (pNode == null)
                return null;

            pNode.next = null;
            if (Tail == null)
            {
                Head = Tail = pNode;
            }
            else
            {
                pNode.prev = Tail;
                Tail.next = pNode;
                Tail = pNode;
            }

            m_Count++;
            return Tail;
        }

        /// <summary>
        /// 移除节点
        /// </summary>
        public void RemoveNode(DoubleLinkListNode<T> pNode)
        {
            if (pNode == null)
                return;
            if (pNode == Head)
                Head = pNode.next;

            if (pNode == Tail)
                Tail = pNode.prev;

            if (pNode.prev != null)
                pNode.prev.next = pNode.next;

            if (pNode.next != null)
                pNode.next.prev = pNode.prev;

            pNode.next = pNode.prev = null;
            pNode.t = null;
            m_DoubleLinkNodePool.Recycle(pNode);
            m_Count--;
        }


        /// <summary>
        /// 将某个节点移动到头部
        /// </summary>
        /// <param name="pNode"></param>
        public void MoveToHead(DoubleLinkListNode<T> pNode)
        {
            if (pNode == null || pNode == Head)
                return;

            if (pNode.prev == null && pNode.next == null)
                return;

            if (pNode == Tail)
                Tail = pNode.prev;

            if (pNode.prev != null)
                pNode.prev.next = pNode.next;

            if (pNode.next != null)
                pNode.next.prev = pNode.prev;
            pNode.prev = null;
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;

            if (Tail == null)
                Tail = Head;
        }
    }


    public class CMapList<T> where T : class, new()
    {
        DoubleLinkedList<T> m_Dlink = new DoubleLinkedList<T>();
        Dictionary<T, DoubleLinkListNode<T>> m_FindMap = new Dictionary<T, DoubleLinkListNode<T>>();

        ~CMapList() { Clear();}

        /// <summary>
        /// 清空链表
        /// </summary>
        public void Clear()
        {
            while (m_Dlink.Tail!=null)
            {
                Remove(m_Dlink.Tail.t);
            }
        }

        /// <summary>
        /// 插入一个节点到表头
        /// </summary>
        /// <param name="t"></param>
        public void InsertToHead(T t)
        {
            DoubleLinkListNode<T> node = null;
            if (m_FindMap.TryGetValue(t, out node) && node != null)
            {
                m_Dlink.AddToHeader(node);
                return;
            }

            m_Dlink.AddToHeader(node);
            m_FindMap.Add(t,m_Dlink.Head);

        }

        /// <summary>
        /// 从表尾弹出节点
        /// </summary>
        public void Pop()
        {
            if (m_Dlink.Tail != null)
            {
                Remove(m_Dlink.Tail.t);
            }
        }

        /// <summary>
        /// 删除某个节点
        /// </summary>
        public void Remove(T t)
        {
            DoubleLinkListNode<T> node = null;
            if (!m_FindMap.TryGetValue(t, out node) || node == null)
            {
                return;
            }

            m_Dlink.RemoveNode(node);
            m_FindMap.Remove(t);
        }

        /// <summary>
        /// 获取尾部节点
        /// </summary>
        /// <returns></returns>
        public T Back()
        {
            return (m_Dlink.Tail == null) ? null : m_Dlink.Tail.t;
        }


        /// <summary>
        /// 返回节点个数
        /// </summary>
        /// <returns></returns>
        public int Size()
        {
            return m_FindMap.Count;
        }


        public bool Find(T t)
        {
            DoubleLinkListNode<T> node = null;
            if (!m_FindMap.TryGetValue(t, out node) || node == null)
                return false;

            return true;
        }

        /// <summary>
        /// 刷新某个节点，把节点移动到头部
        /// </summary>
        public bool Refresh(T t)
        {
            DoubleLinkListNode<T> node = null;
            if (!m_FindMap.TryGetValue(t, out node) || node == null)
                return false;

            m_Dlink.MoveToHead(node);
            return true;
        }

    }
}
