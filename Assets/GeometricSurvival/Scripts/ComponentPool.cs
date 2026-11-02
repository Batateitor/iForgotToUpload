using System.Collections.Generic;
using UnityEngine;

namespace GeometricSurvival
{
    // Pool creates instances from authored prefabs and reuses them between spawns.
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly Stack<T> available = new Stack<T>();
        private readonly T prefab;
        public ComponentPool(T prefab) { this.prefab = prefab; }
        public T Rent()
        {
            T item = available.Count > 0 ? available.Pop() : Object.Instantiate(prefab);
            item.gameObject.SetActive(false);
            return item;
        }
        public void Return(T item) { item.gameObject.SetActive(false); available.Push(item); }
    }
}
