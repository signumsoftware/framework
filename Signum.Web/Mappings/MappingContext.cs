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
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
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
        
        internal readonly IPropertyValidator PropertyValidator;
        internal readonly PropertyRoute PropertyRoute; 

        public void AddChild(MappingContext context)
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

        public IEnumerable<MappingContext> Children()
        {
            return FirstChild.FollowC(n => n.Next);
        }

        public abstract ControllerContext ControllerContext { get; }

        public abstract object UntypedValue { get; }

        public abstract MappingContext UntypedValidateGlobal();

        public string ControlID { get; private set; }

        public abstract SortedList<string, string> GlobalInputs { get; }
        public abstract Dictionary<string, List<string>> GlobalErrors { get; }

        public bool HasInput
        {
            get { return Root.GlobalInputs.ContainsKey(ControlID); }
        }

        public string Input
        {
            get { return Root.GlobalInputs.GetOrThrow(ControlID, "'{0}' is not in the form"); }
        }

        public bool Empty()
        {
            return !HasInput && Inputs.Count == 0;
        }

        public abstract IDictionary<string, string> Inputs { get; }

        public List<string> Error
        {
            get { return Root.GlobalErrors.GetOrCreate(ControlID); }
            set
            {
                if (value == null)
                    Root.GlobalErrors.Remove(ControlID);
                else
                    Root.GlobalErrors[ControlID] = value;
            }
        }

        public abstract IDictionary<string, List<string>> Errors { get; }

        public MappingContext(string controlID, IPropertyValidator propertyValidator, PropertyRoute route)
        {
            this.ControlID = controlID ?? "";
            this.PropertyValidator = propertyValidator;
            this.PropertyRoute = route; 
        }

        public bool Parse<V>(string property, out V value)
        {
            var mapping = Mapping.ForValue<V>();

            if (mapping == null)
                throw new InvalidOperationException("No mapping for value {0}".Formato(typeof(V).TypeName()));

            var sc = new SubContext<V>(TypeContextUtilities.Compose(this.ControlID, property), null, null, this);

            value = mapping(sc);

            return !sc.SupressChange;
        }

        public bool Parse<V>(out V value)
        {
            var mapping = Mapping.ForValue<V>();

            if (mapping == null)
                throw new InvalidOperationException("No mapping for value {0}".Formato(typeof(V).TypeName()));

            var sc = new SubContext<V>(this.ControlID, null, null, this);

            value = mapping(sc);

            return !sc.SupressChange;
        }

        public bool IsEmpty(string property)
        {
            if (Inputs[property].HasText())
            {
                this.Errors.GetOrCreate(property).Add(Resources.InvalidFormat);
                return false;
            }
            return true;
        }

        public bool IsEmpty()
        {
            if (Input.HasText())
            {
                this.Error.Add(Resources.InvalidFormat);
                return false;
            }
            return true;
        }


        public static ModifiableEntity FindSubEntity(ModifiableEntity entity, string prefix)
        {
            if (!prefix.HasText())
                return entity;

            string[] properties = prefix.Split(new string[] { TypeContext.Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (properties == null || properties.Length == 0)
                throw new ArgumentException("Invalid property prefix");

            Modifiable currentEntity = entity;
            try
            {
                foreach (string property in properties)
                {    
                    int index;
                    if (int.TryParse(property, out index))
                    {
                        IList list = (IList)currentEntity;
                        if (list.Count <= index)
                            return null;
                        currentEntity = (Modifiable)list[index];
                    }
                    else
                    {
                        Type cleanType = (currentEntity as Lite<IIdentifiable>).TryCC(t => t.EntityType) ?? currentEntity.GetType();
                        PropertyInfo pi = cleanType.GetProperty(property);
                        currentEntity = (Modifiable)pi.GetValue(currentEntity, null);
                    }

                    if (currentEntity is Lite<IIdentifiable>)
                        currentEntity = Database.Retrieve((Lite<IdentifiableEntity>)currentEntity);

                    if (currentEntity == null)
                        return null;
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Invalid property prefix or wrong entity in Session");
            }

            return (ModifiableEntity)currentEntity;
        }

        public static Type FindSubentityType(IdentifiableEntity entity, string prefix)
        {
            if (!prefix.HasText())
                throw new ApplicationException("Route cannot be null to find subentity");

            string[] properties = prefix.Split(new string[] { TypeContext.Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (properties == null || properties.Length == 0)
                throw new ArgumentException("Invalid property prefix");

            Modifiable currentEntity = entity;
            PropertyInfo pi = null;
            try
            {
                foreach (string property in properties)
                {    
                    int index;
                    if (int.TryParse(property, out index))
                    {
                        IList list = (IList)currentEntity;
                        if (list.Count <= index)
                            currentEntity = (Modifiable)Constructor.Construct(list.GetType().ElementType());
                        else
                            currentEntity = (Modifiable)list[index];
                    }
                    else
                    {
                        Type cleanType = (currentEntity as Lite<IIdentifiable>).TryCC(t => t.EntityType) ?? currentEntity.GetType();
                        pi = cleanType.GetProperty(property);
                        currentEntity = (Modifiable)pi.GetValue(currentEntity, null);
                    }

                    if (currentEntity is Lite<IIdentifiable>)
                        currentEntity = Database.Retrieve((Lite<IdentifiableEntity>)currentEntity);

                    if (currentEntity == null)
                        currentEntity = (Modifiable)Constructor.Construct(pi.PropertyType);
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Invalid property prefix or wrong entity in Session");
            }

            return currentEntity.GetType();
        }
    }

    public abstract class MappingContext<T> : MappingContext
    {
        public T Value { get; set; }
        public override object UntypedValue
        {
            get { return Value; }
        }

        public MappingContext(string controlID, IPropertyValidator propertyPack, PropertyRoute route)
            : base(controlID, propertyPack, route)
        {
        }

        internal bool SupressChange;

        public T None()
        {
            SupressChange = true;
            return default(T);
        }

        public T None(string error)
        {
            this.Error.Add(error);
            SupressChange = true;
            return default(T);
        }

        public T None(string property, string error)
        {
            this.Errors.GetOrCreate(property).Add(error);
            SupressChange = true;
            return default(T);
        }

        public T ParentNone(string property, string error)
        {
            this.Parent.Errors.GetOrCreate(property).Add(error);
            SupressChange = true;
            return default(T);
        }
    
        public override MappingContext Parent
        {
            get { throw new NotImplementedException(); }
        }

        public override MappingContext Root
        {
            get { throw new NotImplementedException(); }
        }

        public override MappingContext UntypedValidateGlobal()
        {
            return ValidateGlobal();
        }

        public MappingContext<T> ValidateGlobal()
        {
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

        public RuntimeInfo GetRuntimeInfo()
        {
            string strRuntimeInfo = Inputs[EntityBaseKeys.RuntimeInfo];
            return RuntimeInfo.FromFormValue(strRuntimeInfo);
        }
    }

    internal class RootContext<T> : MappingContext<T> where T : IRootEntity
    {
        public override MappingContext Parent { get { throw new InvalidOperationException(); } }
        public override MappingContext Root { get { return this; } }

        ControllerContext controllerContext;
        public override ControllerContext ControllerContext { get { return controllerContext; } }

        SortedList<string, string> globalInputs;
        public override SortedList<string, string> GlobalInputs
        {
            get { return globalInputs; }
        }

        Dictionary<string, List<string>> globalErrors = new Dictionary<string,List<string>>();
        public override Dictionary<string, List<string>> GlobalErrors
        {
            get { return globalErrors; }
        }

        IDictionary<string, string> inputs;
        public override IDictionary<string, string> Inputs { get { return inputs; } }

        IDictionary<string, List<string>> errors;
        public override IDictionary<string, List<string>> Errors { get { return errors; } }

        public RootContext(string prefix, SortedList<string, string> globalInputs, ControllerContext controllerContext) :
            base(prefix, null, PropertyRoute.Root(typeof(T)))
        {
            this.globalInputs = globalInputs;
            if (prefix.HasText())
            {
                this.inputs = new ContextualSortedList<string>(globalInputs, prefix);
                this.errors = new ContextualDictionary<List<string>>(globalErrors, prefix);
            }
            else
            {
                this.inputs = globalInputs;
                this.errors = globalErrors;
            }
            this.controllerContext = controllerContext;
        }  

        internal override MappingContext Next
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }
    }

    public class SubContext<T> : MappingContext<T>
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

        public override SortedList<string, string> GlobalInputs
        {
            get { return root.GlobalInputs; }
        }

        public override Dictionary<string, List<string>> GlobalErrors
        {
            get { return root.GlobalErrors; }
        }

        ContextualSortedList<string> inputs;
        public override IDictionary<string, string> Inputs { get { return inputs; } }

        ContextualDictionary<List<string>> errors;
        public override IDictionary<string, List<string>> Errors { get { return errors; } }

        public SubContext(string controlID, IPropertyValidator propertyValidator, PropertyRoute route, MappingContext parent) :
            base(controlID, propertyValidator, route)
        {
            this.parent = parent;
            this.root = parent.Root;
            this.inputs = new ContextualSortedList<string>(parent.Inputs, controlID);
            this.errors = new ContextualDictionary<List<string>>(root.GlobalErrors, controlID);
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
            return global.Remove(ControlID + key);
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

    class ContextualSortedList<V> : IDictionary<string, V>, ICollection<KeyValuePair<string, V>>
    {
        SortedList<string, V> global;
        int startIndex;
        int endIndex;
        public string ControlID { get; private set; }

        public ContextualSortedList(IDictionary<string, V> sortedList, string controlID)
        {
            ContextualSortedList<V> csl = sortedList as ContextualSortedList<V>;
            
            Debug.Assert(controlID.HasText());
            this.ControlID = controlID + TypeContext.Separator;

            Debug.Assert(csl == null || ControlID.StartsWith(csl.ControlID));

            this.global = csl == null ? (SortedList<string, V>)sortedList : csl.global;

            var parentStart = csl == null ? 0 : csl.startIndex;
            var parentEnd = csl == null ? sortedList.Count : csl.endIndex;

            startIndex = this.BinarySearch(parentStart, parentEnd);

            endIndex = BinarySearchStartsWith(startIndex, parentEnd);
        }

        int BinarySearch(int start, int end)
        {
            while (start <= end)
            {
                int mid = (start + end) / 2;

                var num = global.Keys[mid].CompareTo(ControlID);

                if (num > 0)
                    end = mid - 1;
                else if (num < 0)
                    start = mid + 1;
                else
                    return mid;
            }

            return start;
        }

        int BinarySearchStartsWith(int start, int end)
        {
            if (!global.Keys[start].StartsWith(ControlID))
                return start;

            while (start < end)
            {
                int mid = (start + end) / 2;

                if (global.Keys[mid].StartsWith(ControlID))
                    start = mid + 1;
                else
                    end = mid;
            }

            return start;
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
