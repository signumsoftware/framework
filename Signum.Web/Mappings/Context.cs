#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Signum.Entities;
using System.Web;
using System.Collections;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Web.Properties;
using Signum.Engine;
using Signum.Utilities.DataStructures;
using System.Web.Mvc;
#endregion

namespace Signum.Web
{
    public abstract class MappingContext
    {
        public abstract MappingContext Parent { get; }
        public abstract MappingContext Root { get; }

        internal MappingContext FirstChild { get; private set; }
        internal MappingContext LastChild { get; private set; }
        internal abstract MappingContext Next { get; set; }
        
        internal readonly PropertyPack PropertyPack;

        public abstract void AddOnFinish(Action action);

        internal abstract void AddOnFinish(Action action, long ticks, string controlID);

        internal void AddChild(MappingContext context)
        {
            Debug.Assert(context.Parent == this && context.Next == null);

            if (LastChild == null)
            {
                FirstChild = LastChild = context;
            }
            else
            {
                LastChild.Next = context;
                LastChild = context;
            }
        }

        public abstract Dictionary<string, long> GetTicksDictionary();

        public IEnumerable<MappingContext> Children()
        {
            return FirstChild.FollowC(n => n.Next);
        }

        public long? Ticks
        {
            get
            {
                if (Inputs.ContainsKey(TypeContext.Ticks))
                    return Inputs[TypeContext.Ticks].ToLong();
                else if (Inputs.ContainsKey(EntityBaseKeys.RuntimeInfo))
                    return RuntimeInfo.FromFormValue(Inputs[EntityBaseKeys.RuntimeInfo]).TryCS(ri => ri.Ticks);
                return null;
            }
        }

        public abstract ControllerContext ControllerContext { get; }

        public abstract object UntypedValue { get; }
        public abstract Mapping UntypedMapping { get; }

        public abstract MappingContext UntypedValidateMapping();
        public abstract MappingContext UntypedValidateGlobal();

        public string ControlID { get; private set; }

        public string Input
        {
            get { return Root.Inputs.TryGetC(ControlID); }
        }
        public abstract IDictionary<string, string> Inputs { get; }

        public List<string> Error
        {
            get { return Root.Errors.GetOrCreate(ControlID); }
            set
            {
                if (value == null)
                    Root.Error.Remove(ControlID);
                else
                    Root.Errors[ControlID] = value;
            }
        }
        public abstract IDictionary<string, List<string>> Errors { get; }

        public MappingContext(string controlID, PropertyPack propertyPack)
        {
            this.ControlID = controlID ?? "";
            this.PropertyPack = propertyPack;
        }

        internal abstract void ValidateInternal();

        public static Modifiable GetPropertyValue(Modifiable entity, string prefix)
        {
            if (!prefix.HasText())
                return entity;

            string[] properties = prefix.Split(new string[] { TypeContext.Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (properties == null || properties.Length == 0)
                throw new ArgumentException(Resources.InvalidPropertyPrefix);

            List<PropertyInfo> pis = new List<PropertyInfo>();
            object currentEntity = (entity is Lite) ? Database.Retrieve((Lite)entity) : entity;
            try
            {
                foreach (string property in properties)
                {
                    int index;
                    if (int.TryParse(property, out index))
                    {
                        IList ilist = (IList)currentEntity;
                        if (ilist.Count <= index)
                            return null;
                        currentEntity = ilist[index];
                    }
                    else
                    {
                        Type cleanType = (currentEntity as Lite).TryCC(t => t.RuntimeType) ?? currentEntity.GetType();
                        PropertyInfo pi = cleanType.GetProperty(property);
                        pis.Add(pi);
                        currentEntity = pi.GetValue(currentEntity, null);
                    }

                    if (currentEntity is Lite)
                        currentEntity = Database.Retrieve((Lite)currentEntity);

                    if (currentEntity == null)
                        return null;
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException(Resources.InvalidPropertyPrefixOrWrongEntityInSession);
            }

            return (Modifiable)currentEntity;
        }
    }

    public abstract class MappingContext<T> : MappingContext
    {
        public T Value { get; internal set; }
        public override object UntypedValue
        {
            get { return Value; }
        }

        public Mapping<T> Mapping { get; private set; }
        public override Mapping UntypedMapping { get { return Mapping; } }

        public MappingContext(string controlID, Mapping<T> mapping, PropertyPack propertyPack)
            : base(controlID, propertyPack)
        {
            this.Mapping = mapping;
        }

        internal bool SupressChange;

        public T None(string error)
        {
            this.Error.Add(error);
            SupressChange = true;
            return default(T);
        }

        public T None(string errorKey, string error)
        {
            this.Errors.GetOrCreate(errorKey).Add(error);
            SupressChange = true;
            return default(T);
        }

        public T ParentNone(string errorKey, string error)
        {
            this.Parent.Errors.GetOrCreate(errorKey).Add(error);
            SupressChange = true;
            return default(T);
        }

        public T None()
        {
            SupressChange = true;
            return default(T);
        }

        public bool Empty()
        {
            return Input == null && Inputs.Count == 0;
        }

        public override MappingContext Parent
        {
            get { throw new NotImplementedException(); }
        }

        public override MappingContext Root
        {
            get { throw new NotImplementedException(); }
        }

        internal override void ValidateInternal()
        {
            Mapping.OnValidation(this);
        }

        public override  MappingContext UntypedValidateMapping()
        {
            return ValidateMapping();
        }

        public override MappingContext UntypedValidateGlobal()
        {
            return ValidateGlobal();
        }

        public  MappingContext<T> ValidateMapping()
        {
            Mapping.OnValidation(this);
            return this;
        }

        public MappingContext<T> ValidateGlobal()
        {
            this.ValidateMapping();

            var globalErrors = CalculateGlobalErrors();

            //meter el resto en el diccionario
            if (globalErrors.Count > 0)
                Errors.GetOrCreate(ViewDataKeys.GlobalErrors).AddRange(globalErrors.ToList());

            return this;
        }

        public List<string> CalculateGlobalErrors()
        {
            var entity = (ModifiableEntity)(object)Value;

            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(entity));

            Dictionary<ModifiableEntity, string> dicGlobalErrors = entity.FullIntegrityCheckDictionary();
            //Split each error in one entry in the HashTable:
            return dicGlobalErrors.SelectMany(a => a.Value.Lines()).Except(Errors.SelectMany(e => e.Value)).ToList();
        }

    }

    internal class RootContext<T> : MappingContext<T>
    {
        public override MappingContext Parent { get { throw new InvalidOperationException(); } }
        public override MappingContext Root { get { return this; } }

        ControllerContext controllerContext;
        public override ControllerContext ControllerContext { get { return controllerContext; } }

        SortedList<string, string> inputs;
        public override IDictionary<string, string> Inputs { get { return inputs; } }

        Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
        public override IDictionary<string, List<string>> Errors { get { return errors; } }


        /// <summary>
        /// Ticks => ControlID, Action
        /// </summary>
        public SortedList<long, Tuple<string, Action>> Actions = new SortedList<long, Tuple<string, Action>>();

        public void Finish()
        {
            foreach (var pair in Actions.Values)
                pair.Second();
        }

        public override void AddOnFinish(Action action)
        {
            throw new InvalidOperationException();
        }

        internal override void AddOnFinish(Action action, long ticks, string controlID)
        {
            Actions.GetOrCreate(ticks, () => new Tuple<string, Action>(controlID, action));
        }

        public RootContext(string prefix, Mapping<T> mapping, SortedList<string, string> inputs, ControllerContext controllerContext ) :
            base(prefix, mapping, null)
        {
            this.inputs = inputs;
            this.controllerContext = controllerContext;
        }

        internal override MappingContext Next
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        public override Dictionary<string, long> GetTicksDictionary()
        {
            return Actions.ToDictionary(kvp => kvp.Value.First, kvp => kvp.Key);
        }
    }

    internal class SubContext<T> : MappingContext<T>
    {
        MappingContext parent;
        public override MappingContext Parent { get { return parent; } }

        MappingContext root;
        public override MappingContext Root { get { return root; } }

        MappingContext next;
        internal override MappingContext Next
        {
            get { return next; }
            set { next = value; }
        }

        public override ControllerContext ControllerContext
        {
            get { return root.ControllerContext; }
        }

        ContextualSortedList<string> inputs;
        public override IDictionary<string, string> Inputs { get { return inputs; } }

        ContextualDictionary<List<string>> errors;
        public override IDictionary<string, List<string>> Errors { get { return errors; } }

        public override void AddOnFinish(Action action)
        {
            root.AddOnFinish(action, Ticks.Value, ControlID);
        }

        internal override void AddOnFinish(Action action, long ticks, string controlID)
        {
            throw new InvalidOperationException();
        }

        public SubContext(string controlID, Mapping<T> mapping, PropertyPack propertyPack, MappingContext parent) :
            base(controlID, mapping, propertyPack)
        {
            this.parent = parent;
            this.root = parent.Root;
            this.inputs = parent.Inputs is SortedList<string, string> ?
                new ContextualSortedList<string>((SortedList<string, string>)parent.Inputs, controlID) :
                new ContextualSortedList<string>((ContextualSortedList<string>)parent.Inputs, controlID);

            this.errors = new ContextualDictionary<List<string>>((Dictionary<string, List<string>>)root.Errors, controlID);
        }

        public override Dictionary<string, long> GetTicksDictionary()
        {
            throw new InvalidOperationException();
        }
    }

    internal class ContextualDictionary<V> : IDictionary<string, V>, ICollection<KeyValuePair<string, V>>
    {
        Dictionary<string, V> global;
        string ControlID;

        public ContextualDictionary(Dictionary<string, V> global, string controlID)
        {
            this.global = global;
            this.ControlID = controlID + TypeContext.Separator;
        }

        public void Add(string key, V value)
        {
            global.Add(ControlID + key, value);
        }

        public bool ContainsKey(string key)
        {
            return global.ContainsKey(ControlID + key);
        }

        public ICollection<string> Keys
        {
            get { return global.Keys.Where(s => s.StartsWith(ControlID)).Select(s => s.Substring(ControlID.Length)).ToReadOnly(); }
        }

        public bool Remove(string key)
        {
            return global.Remove(ControlID);
        }

        public bool TryGetValue(string key, out V value)
        {
            return global.TryGetValue(ControlID + key, out value);
        }

        public ICollection<V> Values
        {
            get { return global.Where(kvp => kvp.Key.StartsWith(ControlID)).Select(kvp => kvp.Value).ToReadOnly(); }
        }

        public V this[string key]
        {
            get
            {
                return global[ControlID + key];
            }
            set
            {
                global[ControlID + key] = value;
            }
        }

        public int Count
        {
            get { return global.Keys.Count(k => k.StartsWith(ControlID)); }
        }


        void ICollection<KeyValuePair<string, V>>.Add(KeyValuePair<string, V> item)
        {
            global.Add(ControlID + item.Key, item.Value);
        }

        public void Clear()
        {
            global.RemoveRange(Keys);
        }

        bool ICollection<KeyValuePair<string, V>>.Contains(KeyValuePair<string, V> item)
        {
            return ((ICollection<KeyValuePair<string, V>>)global).Contains(new KeyValuePair<string, V>(ControlID + item.Key, item.Value));
        }

        void ICollection<KeyValuePair<string, V>>.CopyTo(KeyValuePair<string, V>[] array, int arrayIndex)
        {
            this.ToList().CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, V>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<string, V>>.Remove(KeyValuePair<string, V> item)
        {
            return ((ICollection<KeyValuePair<string, V>>)global).Remove(new KeyValuePair<string, V>(ControlID + item.Key, item.Value));
        }

        public IEnumerator<KeyValuePair<string, V>> GetEnumerator()
        {
            return global.Where(a => a.Key.StartsWith(ControlID)).Select(a => new KeyValuePair<string, V>(a.Key.Substring(ControlID.Length), a.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class ContextualSortedList<V> : IDictionary<string, V>, ICollection<KeyValuePair<string, V>>
    {
        SortedList<string, V> global;
        int startIndex;
        int endIndex;
        string ControlID;

        public ContextualSortedList(SortedList<string, V> global, string controlID) :
            this(global, controlID, 0, global.Count)
        {
        }

        public ContextualSortedList(ContextualSortedList<V> csl, string controlID) :
            this(csl.global, controlID, csl.startIndex, csl.endIndex)
        {
        }


        public ContextualSortedList(SortedList<string, V> global, string controlID, int startParent, int endParent)
        {
            this.global = global;
            this.ControlID = controlID + TypeContext.Separator;

            for (int i = startParent; i < endParent; i++)
            {
                if (global.Keys[i].StartsWith(ControlID))
                {
                    this.startIndex = i;
                    break;
                }
            }

            for (int i = endParent - 1; i >= startParent; i--)
            {
                if (global.Keys[i].StartsWith(ControlID))
                {
                    this.endIndex = i + 1;
                    break;
                }
            }
        }

        public void Add(string key, V value)
        {
            throw new InvalidOperationException();
        }

        public bool ContainsKey(string key)
        {
            return global.ContainsKey(ControlID + key);
        }

        public ICollection<string> Keys
        {
            get { return this.Select(kvp => kvp.Key).ToReadOnly(); }
        }

        public bool Remove(string key)
        {
            throw new InvalidOperationException();
        }

        public bool TryGetValue(string key, out V value)
        {
            return global.TryGetValue(ControlID + key, out value);
        }

        public ICollection<V> Values
        {
            get { return this.Select(kvp => kvp.Value).ToReadOnly(); }
        }

        public V this[string key]
        {
            get
            {
                return global[ControlID + key];
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public int Count
        {
            get { return endIndex - startIndex; }
        }


        void ICollection<KeyValuePair<string, V>>.Add(KeyValuePair<string, V> item)
        {
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        bool ICollection<KeyValuePair<string, V>>.Contains(KeyValuePair<string, V> item)
        {
            return ((ICollection<KeyValuePair<string, V>>)global).Contains(new KeyValuePair<string, V>(ControlID + item.Key, item.Value));
        }

        void ICollection<KeyValuePair<string, V>>.CopyTo(KeyValuePair<string, V>[] array, int arrayIndex)
        {
            this.ToList().CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, V>>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<KeyValuePair<string, V>>.Remove(KeyValuePair<string, V> item)
        {
            throw new InvalidOperationException();
        }

        public IEnumerator<KeyValuePair<string, V>> GetEnumerator()
        {
            for (int i = startIndex; i < endIndex; i++)
                yield return new KeyValuePair<string, V>(global.Keys[i].Substring(ControlID.Length), global.Values[i]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
