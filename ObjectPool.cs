using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFrameWork
{
    public interface IObjectPoolUser<out T> where T : class
    {
        public IEnumerable<T> AllPoolObjects();
    }

    public class ObjectPool<T> where T : class
    {
        protected List<T> _freePool;
        protected Func<T> _createFunc;
        protected Func<T, bool> _returnFunc;

        // 只规定还的时候超过多少不还，分配的时候可以临时超过这个上限
        protected int _maxFreePoolSize;

#if MYFRAMEWORK_DEBUG
        protected HashSet<IObjectPoolUser<T>> _users = new HashSet<IObjectPoolUser<T>>();
#endif
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
        public void SetMaxFreePoolSize(int newSize, bool shrinkIfNeeded = true)
        {
            _maxFreePoolSize = Math.Max(1, newSize);
            if (shrinkIfNeeded && _freePool.Count > _maxFreePoolSize)
            {
                _freePool.RemoveRange(_maxFreePoolSize, _freePool.Count - _maxFreePoolSize);
            }
        }

        public T Get(IObjectPoolUser<T> user = null)
        {
            if (_freePool.Count == 0)
            {
                WarmFreePool(1);
            }
            T obj = _freePool[_freePool.Count - 1];
            _freePool.RemoveAt(_freePool.Count - 1);
#if MYFRAMEWORK_DEBUG
            if (user != null)
            {
                _users.Add(user);
            }
#endif
            return obj;
        }

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
#if MYFRAMEWORK_DEBUG
            if (user != null)
            {
                _users.Add(user);
            }
#endif
            return objs;
        }


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

        public void UserReturn(IObjectPoolUser<T> user)
        {
            if (user == null)
            {
                return;
            }
#if MYFRAMEWORK_DEBUG
            _users.Remove(user);
            Return(user.AllPoolObjects());
#else
            Return(user.AllPoolObjects());
#endif
        }

        public void DebugPrintPoolUser()
        {
#if MYFRAMEWORK_DEBUG
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