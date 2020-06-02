using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace 君莫笑
{
    public class ObjectManager : Singleton<ObjectManager>
    {

        protected ObjectManager()
        {

        }

        //对象池节点
        public Transform ResyclePoolTrs;
        //场景节点
        public Transform SceneTrs;
        /// <summary>
        /// 对象池
        /// </summary>
        protected Dictionary<uint, List<ResourceObj>> m_ObjectPoolDic = new Dictionary<uint, List<ResourceObj>>();

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<int,ResourceObj> m_ResourceObjDic=new Dictionary<int, ResourceObj>();
        /// <summary>
        /// ResourceObj类对象池
        /// </summary>
        protected ClassObjectPool<ResourceObj> m_ResourceObjClassPool; //Fixed:不能在这里调用单例，单例中用了unity下的FindObjectByType,自己都没创建就调自己的方法

        protected Dictionary<Type, object> m_ClassPoolDic;

        //根据异步的GUID储存ResourceObj,来判断是否正在异步加载
        protected Dictionary<long, ResourceObj> m_AsyncResObj = new Dictionary<long, ResourceObj>();

        void Awake()
        {
            m_ClassPoolDic=new Dictionary<Type, object>();
            m_ResourceObjClassPool = ObjectManager.Instance.GetOrCreateClassPool<ResourceObj>(1000);
        }



        /// <summary>
        /// 初始化调用
        /// </summary>
        /// <param name="recycleTrs"></param>
        /// <param name="sceneTrs"></param>
        public void Init(Transform recycleTrs,Transform sceneTrs)
        {
            this.ResyclePoolTrs = recycleTrs;
            this.SceneTrs = sceneTrs;
        }


        /// <summary>
        /// 从对象池中取出对象
        /// </summary>
        /// <param name="crc"></param>
        /// <returns></returns>
        protected ResourceObj GetObjectFromPool(uint crc)
        {
            List<ResourceObj> st = null;
            if (m_ObjectPoolDic.TryGetValue(crc, out st) && st != null && st.Count > 0)
            {
                ResourceManager.Instance.IncreaseResourceRef(crc);

                ResourceObj resObj = st[0];
                st.RemoveAt(0);
                GameObject obj = resObj.m_CloneObj;
                if (!System.Object.ReferenceEquals(obj, null))
                {
                    resObj.m_Already = false;
#if UNITY_EDITOR
                    if (obj.name.EndsWith("(Recycle)"))
                    {
                        obj.name = obj.name.Replace("(Recycle)","");
                    }
#endif
                }
                return resObj;
            }
            return null;
        }

        /// <summary>
        /// 取消异步加载
        /// </summary>
        /// <param name="guid"></param>
        public void CancleLoad(long guid)
        {
            ResourceObj resOjbObj = null;
            if (m_AsyncResObj.TryGetValue(guid, out resOjbObj)&&ResourceManager.Instance.CancleLoad(resOjbObj))
            {
                m_AsyncResObj.Remove(guid);
                resOjbObj.Reset();
                m_ResourceObjClassPool.Recycle(resOjbObj);
            }
        }

        /// <summary>
        /// 预加载
        /// </summary>
        public void PreloadGameObject(string path, int count = 1, bool clear = false)
        {
            List<GameObject> tempGameObjectList = new List<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject obj = InstantiateObject(path, false, clear);
                tempGameObjectList.Add(obj);
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = tempGameObjectList[i];
                ReleaseObject(obj);
                obj = null;
            }

            tempGameObjectList.Clear();
        }

        /// <summary>
        /// 同步加载需要实例化的资源
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bClear"></param>
        /// <returns></returns>
        public GameObject InstantiateObject(string path,bool setSceneObj=false, bool bClear = true)
        {
            uint crc = CRC32.GetCRC32(path);
            ResourceObj resource = GetObjectFromPool(crc);
            if (resource == null)
            {
                resource = m_ResourceObjClassPool.Spawn(true);
                resource.m_Crc = crc;
                resource.m_bClear = bClear;

                resource = ResourceManager.Instance.LoadResource(path, resource);


                if (resource.m_ResItem.m_Obj != null)
                {
                    resource.m_CloneObj = GameObject.Instantiate(resource.m_ResItem.m_Obj) as GameObject;
                }
            }

            if (setSceneObj)
            {
                resource.m_CloneObj.transform.SetParent(SceneTrs);
            }

            int tempID = resource.m_CloneObj.GetInstanceID();
            if (!m_ResourceObjDic.ContainsKey(tempID))
            {
                m_ResourceObjDic.Add(tempID,resource);
            }

            return resource.m_CloneObj;
        }

        /// <summary>
        /// 异步对象加载
        /// </summary>
        public long InstantiateObjectAsync(string path, OnAsyncObjFinish dealFinish, LoadResPriority priority,
            bool setSceneObject = false, object param1 = null, object param2 = null, object param3 = null,
            bool bClear = true)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            uint crc = CRC32.GetCRC32(path);
            ResourceObj resObj = GetObjectFromPool(crc);

            if (resObj != null)
            {
                if(setSceneObject)
                   resObj.m_CloneObj.transform.SetParent(SceneTrs,false);

                if (dealFinish != null)
                    dealFinish(path, resObj.m_CloneObj, param1, param2, param3);
                return resObj.m_Guid;
            }

            long guid = ResourceManager.Instance.CreateGUID();

            resObj = m_ResourceObjClassPool.Spawn(true);
            resObj.m_Crc = crc;
            resObj.m_setSceneParent = setSceneObject;
            resObj.m_bClear = bClear;
            resObj.m_DealFnish = dealFinish;

            resObj.m_param1 = param1;
            resObj.m_param2 = param2;
            resObj.m_param3 = param3;

            //调用ResourceManager异步加载接口
            ResourceManager.Instance.AsyncLoadResource(path,resObj,OnLoadResourceObjFinish,priority);

            return guid;
        }
        

        /// <summary>
        /// 资源加载完成回调
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="resObj">中间类</param>
        void OnLoadResourceObjFinish(string path,ResourceObj resObj,object param1=null, object param2 = null, object param3 = null)
        {
            if (resObj == null)
                return;

            if (resObj.m_ResItem.m_Obj == null)
            {
#if UNITY_EDITOR
                Debug.LogError("异步加载资源为空：" + path);
#endif
            }
            else
            {
                resObj.m_CloneObj = Instantiate(resObj.m_ResItem.m_Obj) as GameObject;
            }

            //加载完成就从正在加载的异步中移除
            if (m_AsyncResObj.ContainsKey(resObj.m_Guid))
                m_AsyncResObj.Remove(resObj.m_Guid);


            if (resObj.m_CloneObj != null && resObj.m_setSceneParent)
            {
                resObj.m_CloneObj.transform.SetParent(SceneTrs,false);
            }

            if (resObj.m_DealFnish != null)
            {
                int tempID = resObj.m_CloneObj.GetInstanceID();
                if(!m_ResourceObjDic.ContainsKey(tempID))
                    m_ResourceObjDic.Add(tempID,resObj);

                resObj.m_DealFnish(path, resObj.m_CloneObj, resObj.m_param1, resObj.m_param2, resObj.m_param3);

            }

        }

        /// <summary>
        /// 释放需要实例化的资源
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="maxCacheCount"></param>
        /// <param name="destoryCache"></param>
        /// <param name="recycleParent"></param>
        public void ReleaseObject(GameObject obj,int maxCacheCount=-1,bool destoryCache=false,bool recycleParent=true)
        {
            if (obj == null)
                return;

            ResourceObj resObj = null;
            int tempID = obj.GetInstanceID();

            if (!m_ResourceObjDic.TryGetValue(tempID, out resObj))
            {
                Debug.Log($"{obj.name}对象非ObjectManager创建");
            }

            if (resObj == null)
            {
                Debug.LogError("缓存的ResourceObj为空!");
                return;
            }

            if (resObj.m_CloneObj == null)
            {
                Debug.LogError("该对象已经放回对象池，检测自己是否清空引用!");
                return;
            }

#if UNITY_EDITOR
            obj.name += "(Recycle)";
#endif
            List<ResourceObj> st = null;

            if (maxCacheCount == 0)
            {
                m_ResourceObjDic.Remove(tempID);
                ResourceManager.Instance.ReleaseResource(resObj, destoryCache);
                resObj.Reset();
                m_ResourceObjClassPool.Recycle(resObj);
            }
            else//回收到对象池
            {
                if (!m_ObjectPoolDic.TryGetValue(resObj.m_Crc, out st) || st == null)
                {
                    st = new List<ResourceObj>();
                    m_ObjectPoolDic.Add(resObj.m_Crc,st);
                }

                if (resObj.m_CloneObj)
                {
                    if (recycleParent)
                    {
                        resObj.m_CloneObj.transform.SetParent(ResyclePoolTrs);
                    }
                    else
                    {
                        resObj.m_CloneObj.SetActive(false);
                    }
                }

                if (maxCacheCount < 0 || st.Count < maxCacheCount)
                {
                    st.Add(resObj);
                    resObj.m_Already = true;

                    ResourceManager.Instance.DecreaseResourceRef(resObj);
                }
                else
                {
                    m_ResourceObjDic.Remove(tempID);
                    ResourceManager.Instance.ReleaseResource(resObj, destoryCache);
                    resObj.Reset();
                    m_ResourceObjClassPool.Recycle(resObj);
                }
            }

        }

        #region 类对象池使用

        /// <summary>
        /// 创建类对象池，创建完成后可外部保存ClassPool,再调用Spawn和Recycle创建和回收对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maxcount"></param>
        /// <returns></returns>
        public ClassObjectPool<T> GetOrCreateClassPool<T>(int maxcount) where T : class, new()
        {
            Type type = typeof(T);
            object outObj = null;
            if (!m_ClassPoolDic.TryGetValue(type, out outObj) || outObj == null)
            {
                ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxcount);
                m_ClassPoolDic.Add(type, newPool);
                return newPool;
            }

            return outObj as ClassObjectPool<T>;
        }


        /// <summary>
        /// 在对象池中取对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maxcount"></param>
        /// <returns></returns>
        public T NewClassObjectFromPool<T>(int maxcount) where T : class, new()
        {
            ClassObjectPool<T> pool = GetOrCreateClassPool<T>(maxcount);
            if (pool == null)
                return null;

            return pool.Spawn(true);
        }

        #endregion

    }

}

