using System.Collections.Generic;

namespace 君莫笑
{
    public class ClassObjectPool<T> where T : class, new()
    {
        /// <summary>
        /// Pool
        /// </summary>
        public Stack<T> m_Pool = new Stack<T>();
        /// <summary>
        /// 最大对象个数，小于等于0 不限个数
        /// </summary>
        protected int m_MaxCount = 0;
        /// <summary>
        /// 未回收的对象个数
        /// </summary>
        protected int m_NoRecycleCount = 0;


        public ClassObjectPool(int maxcount)
        {
            m_MaxCount = maxcount;

            for (int i = 0; i < maxcount; i++)
            {
                m_Pool.Push(new T());
            }
        }


        /// <summary>
        /// 从池里面取对象
        /// </summary>
        /// <param name="CreateIfPoolEmpty">如果池为空的话是否创建对象</param>
        public T Spawn(bool CreateIfPoolEmpty)
        {
            if (m_Pool.Count > 0)
            {
                T rtn = m_Pool.Pop();
                if (rtn == null)
                {
                    if (CreateIfPoolEmpty)
                    {
                        rtn = new T();
                    }
                }

                m_NoRecycleCount++;
                return rtn;
            }
            else
            {
                if (CreateIfPoolEmpty)
                {
                    T rtn = new T();
                    m_NoRecycleCount++;
                    return rtn;
                }
            }

            return null;
        }



        /// <summary>
        /// 回收类对象
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>是否回收成功</returns>
        public bool Recycle(T obj)
        {
            if (obj == null)
                return false;

            m_NoRecycleCount--;

            if (m_Pool.Count >= m_MaxCount && m_MaxCount > 0)
            {
                obj = null;
                return false;
            }

            m_Pool.Push(obj);
            return true;
        }
    }


}
