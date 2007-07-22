using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Framework.Colecciones
{
    [Serializable]
    public class PriorityQueue<T> : ICloneable where T : IComparable<T>//, ICollection<T>,IList<T>ICollection<T>,
    {
        protected List<T> InnerList = new List<T>();

        #region contructors
        public PriorityQueue()
        { }

        public PriorityQueue(int Capacity)
        {

            InnerList.Capacity = Capacity;
        }

        protected PriorityQueue(List<T> Core, bool Copy)
        {
            if (Copy)
                InnerList = new List<T>(Core);
            else
                InnerList = Core;
        }

        #endregion
        protected void SwitchElements(int i, int j)
        {
            T h = InnerList[i];
            InnerList[i] = InnerList[j];
            InnerList[j] = h;
        }

        protected virtual int OnCompare(int i, int j)
        {

            return InnerList[i].CompareTo(InnerList[j]);
        }

        #region public methods
        /// <summary>
        /// Push an object onto the PQ
        /// </summary>
        /// <param name="O">The new object</param>
        /// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
        public int Push(T elemento)
        {
            int p = InnerList.Count, p2;
            InnerList.Add(elemento); // E[p] = O
            do
            {
                if (p == 0)
                    break;
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                    break;
            } while (true);
            return p;
        }

        /// <summary>
        /// Get the smallest object and remove it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Pop()
        {
            T result = InnerList[0];
            int p = 0, p1, p2, pn;
            InnerList[0] = InnerList[InnerList.Count - 1];
            InnerList.RemoveAt(InnerList.Count - 1);
            do
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) // links kleiner
                    p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) // rechts noch kleiner
                    p = p2;

                if (p == pn)
                    break;
                SwitchElements(p, pn);
            } while (true);
            return result;
        }

        /// <summary>
        /// Notify the PQ that the object at position i has changed
        /// and the PQ needs to restore order.
        /// Since you dont have access to any indexes (except by using the
        /// explicit IList.this) you should not call this function without knowing exactly
        /// what you do.
        /// </summary>
        /// <param name="i">The index of the changed object.</param>
        private void Update(int i)
        {
            int p = i, pn;
            int p1, p2;
            do	// aufsteigen
            {
                if (p == 0)
                    break;
                p2 = (p - 1) / 2;
                if (OnCompare(p, p2) < 0)
                {
                    SwitchElements(p, p2);
                    p = p2;
                }
                else
                    break;
            } while (true);
            if (p < i)
                return;
            do	   // absteigen
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (InnerList.Count > p1 && OnCompare(p, p1) > 0) // links kleiner
                    p = p1;
                if (InnerList.Count > p2 && OnCompare(p, p2) > 0) // rechts noch kleiner
                    p = p2;

                if (p == pn)
                    break;
                SwitchElements(p, pn);
            } while (true);
        }

        /// <summary>
        /// Notifica al PQ que el elemento ha cambiado y que debe reordenarlo
        /// </summary>
        /// <param name="elemento">El objeto que ha cambiado</param>
        public void Update(T elemento)
        {
            int r = InnerList.IndexOf(elemento);
            if (r != -1) Update(r);
        }

        /// <summary>
        /// Get the smallest object without removing it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Peek()
        {
            if (InnerList.Count > 0)
                return InnerList[0];
            throw new Exception("The priority Queue is empty");
        }

        public bool Contains(T value)
        {
            return InnerList.Contains(value);
        }

        public void Clear()
        {
            InnerList.Clear();
        }

        public int Count
        {
            get
            {
                return InnerList.Count;
            }
        }


        public void CopyTo(T[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        public object Clone()
        {
            return new PriorityQueue<T>(InnerList, true);
        }
        #endregion



    }
}
