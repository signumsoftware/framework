using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Collections.ObjectModel;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics; 

namespace Signum.Entities
{
    [Serializable, DebuggerTypeProxy(typeof(MListDebugging<>)), DebuggerDisplay("Count = {Count}")]
    public class MList<T> : Modifiable, IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged 
    {
        List<T> innerList = new List<T>();

        #region Events


        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [NonSerialized]
        NotifyCollectionChangedEventHandler collectionChanged;
        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add { collectionChanged += value; }
            remove { collectionChanged -= value; }
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    OnPropertyChanged("Item[]");
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    OnPropertyChanged("Count");
                    OnPropertyChanged("Item[]");
                    break;
                default:
                    break;
            }

            if (this.collectionChanged != null)
            {
                this.collectionChanged(this, e);
            }

        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
           if(PropertyChanged!= null)
               PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); 
        }

        #endregion

        public bool selfModified = true;
        public override bool SelfModified { get { return selfModified; } }

        protected override void CleanSelfModified()
        {
            selfModified = false;
        }

        public MList()
        {
            innerList = new List<T>();
        }

        public MList(IEnumerable<T> collection)
        {
            innerList = new List<T>(collection);
        }

        public MList(int capacity)
        {
            innerList = new List<T>(capacity);
        }

        public int Count
        {
            get { return innerList.Count; }
        }

        public T this[int index]
        {
            get { return innerList[index]; }
            set
            {
                T old = innerList[index];
                innerList[index] = value;
                selfModified = true;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old));
            }
        }

        public void Add(T item)
        {
            innerList.Add(item);
            selfModified = true;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Add(params T[] items)
        {
            AddRange(items);
        }

        public void Add(IEnumerable<T> collection) // util para los inizializadores de objetos
        {
            AddRange(collection);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                Add(item);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return innerList.AsReadOnly();
        }

        public void Sort()
        {
            innerList.Sort();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort<S>(Func<T, S> element)
            where S : IComparable<S>
        {
            innerList.Sort((a, b) => element(a).CompareTo(element(b)));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void SortDescending<S>(Func<T, S> element)
            where S : IComparable<S>
        {
            innerList.Sort((a, b) => element(b).CompareTo(element(a)));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort(Comparison<T> comparison)
        {
            innerList.Sort(comparison);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort(IComparer<T> comparer)
        {
            innerList.Sort(comparer);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void ResetRange(IEnumerable<T> newItems)
        {
            var list = newItems.ToList();

            if (list.Count == innerList.Count)
            {
                foreach (var item in list)
                {
                    if (!innerList.Remove(item))
                    {
                        selfModified = true;
                        break;
                    }
                }
            }
            else
            {
                selfModified = true;
            }

            innerList = list;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Clear()
        {
            if (innerList.Count > 0)
                selfModified = true;
            innerList.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void CopyTo(T[] array)
        {
            innerList.CopyTo(array);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        public void Insert(int index, T item)
        {
            innerList.Insert(index, item);
            selfModified = true;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public bool Remove(T item)
        {
            int index = innerList.IndexOf(item);
            if (index == -1)
                return false;
         
            innerList.RemoveAt(index);
            selfModified = true;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return true;
        }

        public int RemoveAll(Predicate<T> match)
        {
            var toRemove = innerList.Where(a => match(a)).ToList();
            foreach (var item in toRemove)
                Remove(item);
            return toRemove.Count; 
        }

        public void RemoveAt(int index)
        {
            T item = innerList[index];
            innerList.RemoveAt(index);
            selfModified = true;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void RemoveRange(int index, int count)
        {
            for (int i = 0; i < count; i++)
                RemoveAt(index); 
        }

        public int IndexOf(T item)
        {
            return innerList.IndexOf(item);
        }

        public int IndexOf(T item, int index)
        {
            return innerList.IndexOf(item, index);
        }

        public int IndexOf(T item, int index, int count)
        {
            return innerList.IndexOf(item, index, count);
        }

        public int LastIndexOf(T item)
        {
            return innerList.LastIndexOf(item);
        }

        public int LastIndexOf(T item, int index)
        {
            return innerList.LastIndexOf(item, index); 
        }

        public int LastIndexOf(T item, int index, int count)
        {
            return innerList.LastIndexOf(item, index, count);
        }

        public int FindIndex(Predicate<T> match)
        {
            return innerList.FindIndex(match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return innerList.FindIndex(startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return innerList.FindIndex(startIndex, count, match);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return innerList.FindLastIndex(match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return innerList.FindLastIndex(startIndex, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return innerList.FindLastIndex(startIndex, count, match);
        }

        public bool Contains(T item)
        {
            return innerList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            innerList.CopyTo(array, arrayIndex);
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            int count = innerList.Count;
            for (int i = 0; i < count; i++)
            {
                action(this.innerList[i]);
            }
        }

        public void ForEach(Action<T, int> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            int count = innerList.Count;
            for (int i = 0; i < count; i++)
            {
                action(this.innerList[i], i);
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "{0}{{ Count = {1} }}".Formato(GetType().TypeName(), Count);
        }

        #region IList Members

        int IList.Add(object value)
        {
            this.Add((T)value);
            return this.Count; 
        } 

        bool IList.Contains(object value)
        {
            return this.Contains((T)value); 
        }

        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value); 
        }

        void IList.Insert(int index, object value)
        {
            this.Insert(index, (T)value); 
        }

        bool IList.IsFixedSize
        {
            get { return false;  }
        }

        void IList.Remove(object value)
        {
            this.Remove((T)value); 
        }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)innerList).CopyTo(array, index); 
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)innerList).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection)innerList).SyncRoot; }
        }

        #endregion 
    }

    internal sealed class MListDebugging<T>
    {
        private ICollection<T> collection;

        public MListDebugging(ICollection<T> collection)
        {
            this.collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] array = new T[this.collection.Count];
                this.collection.CopyTo(array, 0);
                return array;
            }
        }
    }

    public static class MListExtensions
    {
        public static MList<T> ToMList<T>(this IEnumerable<T> collection)
        {
            return new MList<T>(collection); 
        }

    }
}
