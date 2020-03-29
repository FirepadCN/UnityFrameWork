using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR 
using UnityEditor;
#endif
namespace 君莫笑
{

    public class ResourceManager : Singleton<ResourceManager>
    {
        protected ResourceManager()
        {
        }

        public bool m_LoadFromEditor = false;
        //缓存使用的资源列表
        public Dictionary<uint,ResourceItem> AssetDic =new Dictionary<uint, ResourceItem>();

        //缓存引用计数为零的列表，达到缓存最大的时候，释放最早缓存的资源
        protected CMapList<ResourceItem> m_NoRefrenceAssetMapList = new CMapList<ResourceItem>();

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
            if(!m_LoadFromEditor)
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
        /// 当前内存使用大于80%的时候清除最早没有使用的资源
        /// </summary>
        protected void WashOut()
        {
//            if (m_NoRefrenceAssetMapList.Size() <= 0)
//                return;
//            ResourceItem item = m_NoRefrenceAssetMapList.Back();
//            DestoryResourceItem(item);
//            m_NoRefrenceAssetMapList.Pop();
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

            //释放Assetbundle引用
            AssetBundleManager.Instance.ReleaseAsset(item);

            if (item.m_Obj != null)
            {
                item.m_Obj = null;
            }
        }

        /// <summary>
        /// 不需要实例化资源的卸载
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
