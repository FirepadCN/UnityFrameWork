using System;
using System.Collections;
using System.Collections.Generic;

namespace 君莫笑
{
    public class ObjectManager : Singleton<ObjectManager>
    {
        protected Dictionary<Type, object> m_ClassPoolDic=new Dictionary<Type, object>();

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

    }

}

