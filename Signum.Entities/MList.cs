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
using System.Runtime.Serialization; 

namespace Signum.Entities
{
    [Serializable, DebuggerTypeProxy(typeof(MListDebugging<>)), DebuggerDisplay("Count = {Count}")]
    public class MList<T> : Modifiable, IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IMListPrivate<T>
    {
        public bool IsNew
        {
            get { return this.innerList.All(a => a.RowId == null); }
        }

        [Serializable]
        public struct RowIdElement : IEquatable<RowIdElement>, IComparable<RowIdElement>, ISerializable
        {
            public readonly PrimaryKey? RowId;
            public readonly T Element;
            public readonly int? OldIndex; 

            public RowIdElement(T value)
            {
                this.Element = value;
                this.RowId = null;
                this.OldIndex = null;
            }

            public RowIdElement(T value, PrimaryKey rowId, int? oldIndex)
            {
                this.Element = value;
                this.RowId = rowId;
                this.OldIndex = oldIndex;
            }

            public bool Equals(RowIdElement other)
            {
                if (other.Element == null && this.Element == null)
                    return true;

                if (other.Element == null || this.Element == null)
                    return false; 

                return other.Element.Equals(this.Element);
            }

            public int CompareTo(RowIdElement other)
            {
                if (other.Element == null && this.Element == null)
                    return 0;

                if (this.Element == null)
                    return -1;

                if (other.Element == null)
                    return 1;

                return ((IComparable<T>)this.Element).CompareTo(other.Element);
            }

            public override int GetHashCode()
            {
                return Element == null ? 0 : Element.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is RowIdElement && base.Equals((RowIdElement)obj);
            }

            public override string ToString()
            {
                var pre = RowId == null ? "New" : RowId.Value.ToString(); 

                if(this.OldIndex != null)
                    pre += " Ix: " + this.OldIndex;

                return "({0}) {1}".FormatWith(pre, Element);
            }

            private RowIdElement(SerializationInfo info, StreamingContext ctxt)
            {
                this.RowId = null;
                this.Element = default(T);
                this.OldIndex = null;
                foreach (SerializationEntry item in info)
                {
                    switch (item.Name)
                    {
                        case "rowid": this.RowId = (PrimaryKey?)item.Value; break;
                        case "oldindex": this.OldIndex = (int?)item.Value; break;
                        case "value": this.Element = (T)item.Value; break;
                        default: throw new InvalidOperationException("Unexpected SerializationEntry");
                    }
                }
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("rowid", this.RowId, typeof(int?));
                info.AddValue("oldindex", this.OldIndex, typeof(int?));
                info.AddValue("value", this.Element, typeof(T));
            }
        }

        List<RowIdElement> innerList = new List<RowIdElement>();

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

            this.collectionChanged?.Invoke(this, e);

        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public MList()
        {
            innerList = new List<RowIdElement>();
        }

        public MList(IEnumerable<T> collection)
        {
            innerList = new List<RowIdElement>(collection.Select(t => new RowIdElement(t)));
        }

        public MList(IEnumerable<RowIdElement> collection)
        {
            innerList = new List<RowIdElement>(collection);
        }

        public MList(int capacity)
        {
            innerList = new List<RowIdElement>(capacity);
        }

        public int Count
        {
            get { return innerList.Count; }
        }

        void AssertNotSealed()
        {
            if (Modified == ModifiedState.Sealed)
                throw new InvalidOperationException("The instance {0} is sealed and can not be modified".FormatWith(this));
        }

        public T this[int index]
        {
            get { return innerList[index].Element; }
            set
            {
                AssertNotSealed();
                T old = innerList[index].Element;
                innerList[index] = new RowIdElement(value);
                SetSelfModified();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old));
            }
        }

        public void Add(T item)
        {
            AssertNotSealed();
            innerList.Add(new RowIdElement(item)); 
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Add(params T[] items)
        {
            AddRange(items);
        }

        public void Add(IEnumerable<T> collection) //for object initializers
        {
            AddRange(collection);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                this.Add(item);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            throw new NotImplementedException("Use ToReadOnly instead");
        }

        public void Sort()
        {
            var orderMatters = OrderMatters();
            if (orderMatters)
                AssertNotSealed();
            innerList.Sort();
            if (orderMatters)
                this.SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Sort<S>(Func<T, S> element)
            where S : IComparable<S>
        {
            var orderMatters = OrderMatters();
            if (orderMatters)
                AssertNotSealed();
            innerList.Sort((a, b) => element(a.Element).CompareTo(element(b.Element)));
            if (orderMatters)
                this.SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void SortDescending<S>(Func<T, S> element)
            where S : IComparable<S>
        {
            var orderMatters = OrderMatters();
            if (orderMatters)
                AssertNotSealed();
            innerList.Sort((a, b) => element(b.Element).CompareTo(element(a.Element)));
            if (orderMatters) 
                this.SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort(Comparison<T> comparison)
        {
            var orderMatters = OrderMatters();
            if (orderMatters)
                AssertNotSealed();
            innerList.Sort((a, b) => comparison(a.Element, b.Element));
            if (orderMatters) 
                this.SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        public void Sort(IComparer<T> comparer)
        {
            var orderMatters = OrderMatters();
            if (orderMatters)
                AssertNotSealed();
            innerList.Sort((a, b) => comparer.Compare(a.Element, b.Element));
            if (orderMatters) 
                this.SetSelfModified(); 
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); 
        }

        private bool OrderMatters()
        {
            for (int i = 0; i < innerList.Count; i++)
            {
                if (innerList[i].OldIndex.HasValue)
                    return true;
            }
            return false;
        }

        public bool ResetRange(IEnumerable<T> newItems)
        {
            var list = newItems.Select(a => new RowIdElement(a)).ToList();

            bool modified = list.Count != this.Count;

            for (int i = 0; i < list.Count; i++)
			{
                if (this.innerList.Contains(list[i]))
                {
                    var index = this.innerList.IndexOf(list[i]);

                    list[i] = this.innerList[index];

                    this.innerList.RemoveAt(index);
                }
                else
                {
                    modified = true;
                }
			}

            var oldList = innerList;

            innerList = list;

            if (modified || oldList.Any() || WrongPosition())
                SetSelfModified();

            foreach (var item in innerList.Except(oldList).ToList())
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));

            foreach (var item in oldList.Except(innerList).ToList())
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));

            return modified;
        }

        private bool WrongPosition()
        {
            for (int i = 0; i < innerList.Count; i++)
            {
                if (innerList[i].OldIndex.HasValue && innerList[i].OldIndex != i)
                    return true;
            }
            return false;
        }

        public void Clear()
        {
            AssertNotSealed();

            if (innerList.Count > 0)
                SetSelfModified();

            var oldItems = innerList.ToList();

            innerList.Clear();

            foreach (var item in oldItems)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item.Element)); 
        }

        public void CopyTo(T[] array)
        {   
            for (int i = 0; i < this.Count; i++)
            {
                array[i] = this.innerList[i].Element;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in innerList)
            {
                yield return item.Element;
            }
        }

        public void Insert(int index, T item)
        {
            AssertNotSealed();
            innerList.Insert(index, new RowIdElement(item));
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public bool Remove(T item)
        {
            AssertNotSealed();
            int index = innerList.IndexOf(new RowIdElement(item));
            if (index == -1)
                return false;
         
            innerList.RemoveAt(index);
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return true;
        }

        public int RemoveAll(Predicate<T> predicate)
        {
            List<T> removed = new List<T>(); 
            for (int i = 0; i < this.innerList.Count; )
            {
                var val = innerList[i].Element;
                if (predicate(val))
                {
                    innerList.RemoveAt(i);
                    removed.Add(val);
                }
                else
                {
                    i++;
                }
            }

            if (removed.Any())
            {
                SetSelfModified();
                foreach (var item in removed)
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));             
            }

            return removed.Count; 
        }


        public int RemoveAllElements(Predicate<RowIdElement> predicate)
        {
            List<T> removed = new List<T>();
            for (int i = 0; i < this.innerList.Count;)
            {
                var rowElem = innerList[i];
                if (predicate(rowElem))
                {
                    innerList.RemoveAt(i);
                    removed.Add(rowElem.Element);
                }
                else
                {
                    i++;
                }
            }

            if (removed.Any())
            {
                SetSelfModified();
                foreach (var item in removed)
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            }

            return removed.Count;
        }

        public void RemoveAt(int index)
        {
            AssertNotSealed();
            T item = innerList[index].Element;
            innerList.RemoveAt(index);
            SetSelfModified();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void RemoveRange(int index, int count)
        {
            for (int i = 0; i < count; i++)
                RemoveAt(index); 
        }

        public int IndexOf(T item)
        {
            return innerList.IndexOf(new RowIdElement(item));
        }

        public int IndexOf(T item, int index)
        {
            return innerList.IndexOf(new RowIdElement(item), index);
        }

        public int IndexOf(T item, int index, int count)
        {
            return innerList.IndexOf(new RowIdElement(item), index, count);
        }

        public int LastIndexOf(T item)
        {
            return innerList.LastIndexOf(new RowIdElement(item));
        }

        public int LastIndexOf(T item, int index)
        {
            return innerList.LastIndexOf(new RowIdElement(item), index); 
        }

        public int LastIndexOf(T item, int index, int count)
        {
            return innerList.LastIndexOf(new RowIdElement(item), index, count);
        }

        public int FindIndex(Predicate<T> match)
        {
            return innerList.FindIndex(a => match(a.Element));
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return innerList.FindIndex(startIndex, a => match(a.Element));
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return innerList.FindIndex(startIndex, count, a => match(a.Element));
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return innerList.FindLastIndex(a => match(a.Element));
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return innerList.FindLastIndex(startIndex, a => match(a.Element));
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return innerList.FindLastIndex(startIndex, count, a => match(a.Element));
        }

        public bool Contains(T item)
        {
            return innerList.Contains(new RowIdElement(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < this.Count; i++)
            {
                array[i + arrayIndex] = this.innerList[i].Element;
            }
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            int count = innerList.Count;
            for (int i = 0; i < count; i++)
            {
                action(this.innerList[i].Element);
            }
        }

        public void ForEach(Action<T, int> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            int count = innerList.Count;
            for (int i = 0; i < count; i++)
            {
                action(this.innerList[i].Element, i);
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
            return "{0}{{ Count = {1} }}".FormatWith(GetType().TypeName(), Count);
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

        List<RowIdElement> IMListPrivate<T>.InnerList
        {
            get { return this.innerList; }
        }

        void IMListPrivate.InnerListModified(IList newItems, IList oldItems)
        {
            this.SetSelfModified();

            if (newItems != null)
                foreach (var item in newItems)
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));

            if (oldItems != null)
                foreach (var item in oldItems)
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        void IMListPrivate.SetRowId(int index, PrimaryKey rowId)
        {
            var prev = this.innerList[index]; 

            if(prev.RowId.HasValue)
                throw new InvalidOperationException("Index {0} already as RowId".FormatWith(index));

            this.innerList[index] = new RowIdElement(prev.Element, rowId, null);
        }

        PrimaryKey? IMListPrivate.GetRowId(int index)
        {
            var prev = this.innerList[index];

            return prev.RowId;
        }

        void IMListPrivate.ForceRowId(int index, PrimaryKey rowId)
        {
            var prev = this.innerList[index];

            this.innerList[index] = new RowIdElement(prev.Element, rowId, null);
        }

        void IMListPrivate.SetOldIndex(int index)
        {
            var prev = this.innerList[index];

            this.innerList[index] = new RowIdElement(prev.Element, prev.RowId.Value, index);
        }

        void IMListPrivate.ExecutePostRetrieving()
        {
            this.PostRetrieving(); 
        }

        protected internal override void PostRetrieving()
        {
            if (this.innerList.Any(a => a.RowId == null))
                return; //The MList was changed in the entity PostRetriever, like UserChart Columns

            if (this.innerList.Select(a => a.RowId.Value).Duplicates().Any())
                throw new InvalidOperationException("Duplicated RowId found, possible problem in LINQ provider"); 

            if (this.innerList.Any(a => a.OldIndex.HasValue))
                this.innerList.Sort(a => a.OldIndex.Value);
        }

        public bool AssignMList(MList<T> list)
        {
            return ((IMListPrivate<T>)this).AssignMList(((IMListPrivate<T>)list).InnerList);
        }

        bool IMListPrivate<T>.AssignMList(List<RowIdElement> newList)
        {
            if (((IMListPrivate<T>)this).IsEqualTo(newList, orderMatters: true))
                return false;
            
            //Even if the entities are Equals, we need to call SetParentEntity
            var added = newList.Select(a => a.Element).ToList();
            var removed = innerList.Select(a => a.Element).ToList(); 

            innerList.Clear();
            innerList.AddRange(newList);
            ((IMListPrivate<T>)this).InnerListModified(added, removed);

            return true;
        }

        public bool IsEqualTo(MList<T> list, bool orderMatters)
        {
            return ((IMListPrivate<T>)this).IsEqualTo(((IMListPrivate<T>)list).InnerList, orderMatters);
        }

        bool IMListPrivate<T>.IsEqualTo(List<MList<T>.RowIdElement> newList, bool orderMatters)
        {
            if (newList.IsNullOrEmpty() && innerList.IsNullOrEmpty())
                return true;

            if (newList == null || innerList == null)
                return false;

            if (newList.Count != innerList.Count)
                return false;

            if (innerList.Any(a => a.RowId == null) ||
                newList.Any(a => a.RowId == null))
                return false;

            if (orderMatters)
            {
                for (int i = 0; i < newList.Count; i++)
                {
                    if (newList[i].RowId != innerList[i].RowId)
                        return false;

                    if (!object.Equals(newList[i].Element, innerList[i].Element))
                        return false;
                }

                return true;
            }
            else
            {
                var current = innerList.ToDictionary(a => a.RowId.Value, a => a.Element);

                foreach (var item in newList)
                {
                    if (item.RowId == null)
                        return false;

                    if (!current.Remove(item.RowId.Value))
                        return false;
                }

                return current.Count == 0;
            }

        }

        public void AssignAndPostRetrieving(IMListPrivate newList)
        {
            this.AssignMList((MList<T>)newList);
            this.PostRetrieving();
        }
    }


    public class MListElement<E, V> where E : Entity
    {
        public PrimaryKey RowId { get; set; }
        public int Order { get; set; }
        public E Parent { get; set; }
        public V Element { get; set; }

        public override string ToString()
        {
            return $"MListEntity: ({nameof(RowId)}:{RowId}, {nameof(Order)}:{Order}, {nameof(Parent)}:{Parent}, {nameof(Element)}:{Element})";
        }
    }

    public interface IMListPrivate
    {
        bool IsNew { get; }
        
        int Count { get; }

        void ExecutePostRetrieving();
        void SetOldIndex(int index);
        PrimaryKey? GetRowId(int index);
        void SetRowId(int index, PrimaryKey rowId);
        void ForceRowId(int index, PrimaryKey rowId);

        void InnerListModified(IList newItems, IList oldItems);

        void AssignAndPostRetrieving(IMListPrivate newList);
    }

    public interface IMListPrivate<T>  : IMListPrivate
    {
        List<MList<T>.RowIdElement> InnerList { get; }

        bool AssignMList(List<MList<T>.RowIdElement> newList);
        bool IsEqualTo(List<MList<T>.RowIdElement> newList, bool orderMatters);
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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class PreserveOrderAttribute : SqlDbTypeAttribute
    {
        public string Name { get; set; }

        public PreserveOrderAttribute()
        {
        }

        public PreserveOrderAttribute(string name)
        {
            this.Name = name;
        }
    }
    //only used for Virtual Mlist
    public interface ICanBeOrdered
    {
        int Order { get; set; }
    }

}
