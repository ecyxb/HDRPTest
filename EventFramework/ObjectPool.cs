using System;
using System.Collections;
using System.Collections.Generic;

namespace EventFramework
{
    /// <summary>
    /// 对象池用户接口，用于追踪和返回从对象池中获取的对象
    /// </summary>
    /// <typeparam name="T">对象池管理的对象类型</typeparam>
    public interface IObjectPoolUser<out T> where T : class
    {
        /// <summary>
        /// 返回该用户持有的所有池化对象
        /// </summary>
        /// <returns>所有池化对象的集合</returns>
        public IEnumerable<T> AllPoolObjects();
    }

    /// <summary>
    /// 泛型对象池实现，用于管理可重用对象以减少垃圾回收压力
    /// </summary>
    /// <typeparam name="T">要池化的对象类型，必须是引用类型</typeparam>
    public class ObjectPool<T> where T : class
    {
        /// <summary>
        /// 空闲对象池列表
        /// </summary>
        protected List<T> _freePool;
        
        /// <summary>
        /// 创建新对象的函数
        /// </summary>
        protected Func<T> _createFunc;
        
        /// <summary>
        /// 对象返回时的验证函数，决定对象是否应该返回到池中
        /// </summary>
        protected Func<T, bool> _returnFunc;

        // 只规定还的时候超过多少不还，分配的时候可以临时超过这个上限
        /// <summary>
        /// 空闲池的最大大小
        /// </summary>
        protected int _maxFreePoolSize;

#if EventFrameWork_DEBUG
        /// <summary>
        /// 当前持有池对象的用户集合（仅调试模式）
        /// </summary>
        protected HashSet<IObjectPoolUser<T>> _users = new HashSet<IObjectPoolUser<T>>();
#endif

        /// <summary>
        /// 初始化对象池实例
        /// </summary>
        /// <param name="createFunc">创建新对象的函数，不能为null</param>
        /// <param name="returnFunc">对象返回时的验证函数，可选</param>
        /// <param name="maxFreePoolSize">空闲池的最大大小</param>
        /// <exception cref="Exception">当createFunc为null时抛出异常</exception>
        public ObjectPool(
            Func<T> createFunc,
            Func<T, bool> returnFunc,
            int maxFreePoolSize
        )
        {
            _createFunc = createFunc;
            _returnFunc = returnFunc;
            _maxFreePoolSize = Math.Max(1, maxFreePoolSize);
            _freePool = new List<T>(_maxFreePoolSize);
            if (_createFunc == null)
            {
                throw new Exception("ObjectPool requires a createFunc to create new objects.");
            }
        }

        /// <summary>
        /// 预热空闲池，创建指定数量的对象并添加到池中
        /// </summary>
        /// <param name="count">要创建的对象数量</param>
        public void WarmFreePool(int count)
        {
            if (_freePool.Count < count)
            {
                int toCreate = Math.Max(0, count - _freePool.Count);
                for (int i = 0; i < toCreate; i++)
                {
                    _freePool.Add(_createFunc());
                }
            }
        }

        /// <summary>
        /// 设置空闲池的新最大大小
        /// </summary>
        /// <param name="newSize">新的最大大小（最小值为1）</param>
        /// <param name="shrinkIfNeeded">如果为true，当当前池大小超过新限制时会移除多余对象</param>
        public void SetMaxFreePoolSize(int newSize, bool shrinkIfNeeded = true)
        {
            _maxFreePoolSize = Math.Max(1, newSize);
            if (shrinkIfNeeded && _freePool.Count > _maxFreePoolSize)
            {
                _freePool.RemoveRange(_maxFreePoolSize, _freePool.Count - _maxFreePoolSize);
            }
        }

        /// <summary>
        /// 从对象池获取单个对象。如果池为空则创建新对象
        /// </summary>
        /// <param name="user">可选的用户跟踪参数（用于调试）</param>
        /// <returns>从池中获取的对象</returns>
        public T Get(IObjectPoolUser<T> user = null)
        {
            if (_freePool.Count == 0)
            {
                WarmFreePool(1);
            }
            T obj = _freePool[_freePool.Count - 1];
            _freePool.RemoveAt(_freePool.Count - 1);
#if EventFrameWork_DEBUG
            if (user != null)
            {
                _users.Add(user);
            }
#endif
            return obj;
        }

        /// <summary>
        /// 从对象池获取指定数量的对象数组
        /// </summary>
        /// <param name="count">要获取的对象数量</param>
        /// <param name="user">可选的用户跟踪参数（用于调试）</param>
        /// <returns>包含指定数量对象的数组，如果count <= 0则返回空数组</returns>
        public T[] Get(int count, IObjectPoolUser<T> user = null)
        {
            if (count <= 0)
            {
                return Array.Empty<T>();
            }
            if (_freePool.Count < count)
            {
                WarmFreePool(count - _freePool.Count);
            }
            // 借的时候一起取出
            T[] objs = new T[count];
            _freePool.CopyTo(_freePool.Count - count, objs, 0, count);
            _freePool.RemoveRange(_freePool.Count - count, count);
#if EventFrameWork_DEBUG
            if (user != null)
            {
                _users.Add(user);
            }
#endif
            return objs;
        }

        /// <summary>
        /// 将单个对象返回到对象池
        /// </summary>
        /// <param name="obj">要返回的对象，如果为null则忽略</param>
        public void Return(T obj)
        {
            if (obj == null) return;
            if (_returnFunc != null && !_returnFunc(obj))
            {
                return;
            }
            if (_freePool.Count < _maxFreePoolSize)
            {
                _freePool.Add(obj);
            }
        }

        /// <summary>
        /// 将多个对象返回到对象池
        /// </summary>
        /// <param name="objs">要返回的对象集合</param>
        public void Return(IEnumerable<T> objs)
        {
            if (objs == null)
            {
                return;
            }
            //还的时候一个个还，因为returnFunc可能会触发操作
            foreach (var obj in objs)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// 让用户返回其持有的所有池化对象
        /// </summary>
        /// <param name="user">要返回对象的用户，如果为null则忽略</param>
        public void UserReturn(IObjectPoolUser<T> user)
        {
            if (user == null)
            {
                return;
            }
#if EventFrameWork_DEBUG
            _users.Remove(user);
            Return(user.AllPoolObjects());
#else
            Return(user.AllPoolObjects());
#endif
        }

        /// <summary>
        /// 调试方法：打印当前持有池对象的所有用户类型（仅在调试模式下有效）
        /// </summary>
        public void DebugPrintPoolUser()
        {
#if EventFrameWork_DEBUG
            HashSet<Type> allTypes = new HashSet<Type>();
            foreach (var user in _users)
            {
                allTypes.Add(user.GetType());
            }
            Debug.Log(string.Join(", ", allTypes));
#endif
        }
    }
}