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
using Signum.Engine;
using Signum.Utilities.DataStructures;
using System.Web.Mvc;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;

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
            return FirstChild.Follow(n => n.Next);
        }

        public abstract ControllerBase Controller { get; }

        public abstract object UntypedValue { get; }

        public abstract MappingContext UntypedValidate();

        public string Prefix { get; private set; }

        public abstract SortedList<string, string> GlobalInputs { get; }
        public abstract Dictionary<string, HashSet<string>> GlobalErrors { get; }

        public bool HasInput
        {
            get { return Root.GlobalInputs.ContainsKey(Prefix); }
        }

        public string Input
        {
            get { return Root.GlobalInputs.GetOrThrow(Prefix, "'{0}' is not in the form"); }
        }

        public bool Empty()
        {
            return !HasInput && Inputs.Count == 0;
        }

        public abstract IDictionary<string, string> Inputs { get; }

        public HashSet<string> Error
        {
            get { return Root.GlobalErrors.GetOrCreate(Prefix); }
            set
            {
                if (value == null)
                    Root.GlobalErrors.Remove(Prefix);
                else
                    Root.GlobalErrors[Prefix] = value;
            }
        }

        public abstract IDictionary<string, HashSet<string>> Errors { get; }

        public MappingContext(string prefix, IPropertyValidator propertyValidator, PropertyRoute route)
        {
            this.Prefix = prefix ?? "";
            this.PropertyValidator = propertyValidator;
            this.PropertyRoute = route; 
        }

        public bool Parse<V>(string property, out V value)
        {
            var mapping = Mapping.ForValue<V>();

            if (mapping == null)
                throw new InvalidOperationException("No mapping for value {0}".FormatWith(typeof(V).TypeName()));

            var sc = new SubContext<V>(TypeContextUtilities.Compose(this.Prefix, property), null, null, this);

            value = mapping(sc);

            return !sc.SupressChange;
        }

        public bool Parse<V>(out V value)
        {
            var mapping = Mapping.ForValue<V>();

            if (mapping == null)
                throw new InvalidOperationException("No mapping for value {0}".FormatWith(typeof(V).TypeName()));

            var sc = new SubContext<V>(this.Prefix, null, null, this);

            value = mapping(sc);

            return !sc.SupressChange;
        }

        public bool IsEmpty(string property)
        {
            if (Inputs[property].HasText())
            {
                this.Errors.GetOrCreate(property).Add(ValidationMessage.InvalidFormat.NiceToString());
                return false;
            }
            return true;
        }

        public bool IsEmpty()
        {
            if (Input.HasText())
            {
                this.Error.Add(ValidationMessage.InvalidFormat.NiceToString());
                return false;
            }
            return true;
        }

        public bool HasErrors()
        {
            return GlobalErrors.Any();
        }

        public JsonNetResult ToJsonModelState()
        {
            return ToJsonModelState(null);
        }

        public JsonNetResult ToJsonModelState(string newToString)
        {
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                {"result", JsonResultType.ModelState.ToString()},
                {"ModelState", this.GlobalErrors }
            };

            if (newToString != null)
                result.Add(EntityBaseKeys.ToStr, newToString);
            

            return new JsonNetResult(result);
        }

        public MappingContext<T> TryFindParent<T>()
        {
            MappingContext mapping = this;
            while (mapping != null)
            {
                if (mapping is MappingContext<T>)
                    return (MappingContext<T>)mapping;

                mapping = mapping.Parent;
            }

            return null;
        }

        public MappingContext<T> FindParent<T>()
        {
            var result = TryFindParent<T>();

            if (result == null)
                throw new InvalidOperationException("{0} not found in the chain of parents".FormatWith(typeof(MappingContext<T>).TypeName()));

            return result;
        }


        public void ImportErrors(Dictionary<Guid, IntegrityCheck> errorDictionary)
        {
            this.DistributeErrors(errorDictionary);

            if (errorDictionary.Count > 0)
                this.Errors.GetOrCreate(ViewDataKeys.GlobalErrors)
                    .AddRange(errorDictionary.SelectMany(a => a.Value.Errors.Values));
        }

        protected void DistributeErrors(Dictionary<Guid, IntegrityCheck> errorDictionary)
        {
            var mod = this.UntypedValue as ModifiableEntity;

            var integrityCheck = mod == null ? null : errorDictionary.TryGetC(mod.temporalId);

            foreach (var child in this.Children())
            {
                var pv = child.PropertyValidator;

                if (pv != null && integrityCheck != null)
                {
                    string error = integrityCheck.Errors.TryGetC(pv.PropertyInfo.Name);

                    if (error != null)
                    {
                        integrityCheck.Errors.Remove(pv.PropertyInfo.Name);

                        child.Error.AddRange(error.SplitNoEmpty("\r\n" ));
                    }
                }

                child.DistributeErrors(errorDictionary);
            }

            if (integrityCheck != null && integrityCheck.Errors.IsEmpty())
                errorDictionary.Remove(mod.temporalId);
        }

    }

    public abstract class MappingContext<T> : MappingContext
    {
        public T Value { get; set; }
        public override object UntypedValue
        {
            get { return Value; }
        }

        public MappingContext(string prefix, IPropertyValidator propertyPack, PropertyRoute route)
            : base(prefix, propertyPack, route)
        {
        }

        public bool SupressChange;

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

        public override MappingContext UntypedValidate()
        {
            return Validate();
        }

        public MappingContext<T> Validate()
        {
            var entity = (ModifiableEntity)(object)Value;

            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(entity));

            Dictionary<Guid, IntegrityCheck> errorDictionary = entity.FullIntegrityCheck();

            if (errorDictionary != null)
                ImportErrors(errorDictionary);

            return this;
        }

        public RuntimeInfo GetRuntimeInfo()
        {
            string strRuntimeInfo = Inputs[EntityBaseKeys.RuntimeInfo];
            return RuntimeInfo.FromFormValue(strRuntimeInfo);
        }
    }

    internal class RootContext<T> : MappingContext<T>
    {
        public override MappingContext Parent { get { return null; } }
        public override MappingContext Root { get { return this; } }

        ControllerBase controller;
        public override ControllerBase Controller { get { return controller; } }

        SortedList<string, string> globalInputs;
        public override SortedList<string, string> GlobalInputs
        {
            get { return globalInputs; }
        }

        Dictionary<string, HashSet<string>> globalErrors = new Dictionary<string, HashSet<string>>();
        public override Dictionary<string, HashSet<string>> GlobalErrors
        {
            get { return globalErrors; }
        }

        IDictionary<string, string> inputs;
        public override IDictionary<string, string> Inputs { get { return inputs; } }

        IDictionary<string, HashSet<string>> errors;
        public override IDictionary<string, HashSet<string>> Errors { get { return errors; } }

        public RootContext(string prefix, SortedList<string, string> globalInputs, PropertyRoute route, ControllerBase controller) :
            base(prefix, null, route)
        {
            this.globalInputs = globalInputs;
            if (prefix.HasText())
            {
                this.inputs = new ContextualSortedList<string>(globalInputs, prefix + TypeContext.Separator);
                this.errors = new ContextualDictionary<HashSet<string>>(globalErrors, prefix + TypeContext.Separator);
            }
            else
            {
                this.inputs = globalInputs;
                this.errors = globalErrors;
            }
            this.controller = controller;
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

        public override ControllerBase Controller
        {
            get { return root.Controller; }
        }

        public override SortedList<string, string> GlobalInputs
        {
            get { return root.GlobalInputs; }
        }

        public override Dictionary<string, HashSet<string>> GlobalErrors
        {
            get { return root.GlobalErrors; }
        }

        ContextualSortedList<string> inputs;
        public override IDictionary<string, string> Inputs { get { return inputs; } }

        ContextualDictionary<HashSet<string>> errors;
        public override IDictionary<string, HashSet<string>> Errors { get { return errors; } }

        public SubContext(string prefix, IPropertyValidator propertyValidator, PropertyRoute route, MappingContext parent) :
            base(prefix, propertyValidator, route)
        {
            this.parent = parent;
            this.root = parent.Root;
            this.inputs = new ContextualSortedList<string>(parent.Inputs, prefix + TypeContext.Separator);
            this.errors = new ContextualDictionary<HashSet<string>>(root.GlobalErrors, prefix);
        }
    }

    public class MixinContext<T> : MappingContext<T> where T : MixinEntity
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

        public override ControllerBase Controller {   get { return root.Controller; }}
        public override SortedList<string, string> GlobalInputs{   get { return root.GlobalInputs; }}
        public override Dictionary<string, HashSet<string>> GlobalErrors{ get { return root.GlobalErrors; }}
        public override IDictionary<string, string> Inputs { get { return parent.Inputs; } }
        public override IDictionary<string, HashSet<string>> Errors { get { return parent.Errors; } }

        public MixinContext(PropertyRoute route, MappingContext parent) :
            base(parent.Prefix, null, route)
        {
            this.parent = parent;
            this.root = parent.Root;
        }
    }

    internal class ContextualDictionary<V> : IDictionary<string, V>, ICollection<KeyValuePair<string, V>>
    {
        Dictionary<string, V> global;
        string Prefix;

        public ContextualDictionary(Dictionary<string, V> global, string prefix)
        {
            this.global = global;
            this.Prefix = prefix;
        }

        public void Add(string key, V value)
        {
            global.Add(Prefix + key, value);
        }

        public bool ContainsKey(string key)
        {
            return global.ContainsKey(Prefix + key);
        }

        public ICollection<string> Keys
        {
            get { return global.Keys.Where(s => s.StartsWith(Prefix)).Select(s => s.Substring(Prefix.Length)).ToReadOnly(); }
        }

        public bool Remove(string key)
        {
            return global.Remove(Prefix + key);
        }

        public bool TryGetValue(string key, out V value)
        {
            return global.TryGetValue(Prefix + key, out value);
        }

        public ICollection<V> Values
        {
            get { return global.Where(kvp => kvp.Key.StartsWith(Prefix)).Select(kvp => kvp.Value).ToReadOnly(); }
        }

        public V this[string key]
        {
            get
            {
                return global[Prefix + key];
            }
            set
            {
                global[Prefix + key] = value;
            }
        }

        public int Count
        {
            get { return global.Keys.Count(k => k.StartsWith(Prefix)); }
        }


        void ICollection<KeyValuePair<string, V>>.Add(KeyValuePair<string, V> item)
        {
            global.Add(Prefix + item.Key, item.Value);
        }

        public void Clear()
        {
            global.RemoveRange(Keys);
        }

        bool ICollection<KeyValuePair<string, V>>.Contains(KeyValuePair<string, V> item)
        {
            return ((ICollection<KeyValuePair<string, V>>)global).Contains(new KeyValuePair<string, V>(Prefix + item.Key, item.Value));
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
            return ((ICollection<KeyValuePair<string, V>>)global).Remove(new KeyValuePair<string, V>(Prefix + item.Key, item.Value));
        }

        public IEnumerator<KeyValuePair<string, V>> GetEnumerator()
        {
            return global.Where(a => a.Key.StartsWith(Prefix)).Select(a => new KeyValuePair<string, V>(a.Key.Substring(Prefix.Length), a.Value)).GetEnumerator();
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
        public string Prefix { get; private set; }

        public ContextualSortedList(IDictionary<string, V> sortedList, string prefix)
        {
            ContextualSortedList<V> csl = sortedList as ContextualSortedList<V>;
            
            Debug.Assert(prefix.HasText());
            this.Prefix = prefix ;

            Debug.Assert(csl == null || Prefix.StartsWith(csl.Prefix));

            this.global = csl == null ? (SortedList<string, V>)sortedList : csl.global;

            var parentStart = csl == null ? 0 : csl.startIndex;
            var parentEnd = csl == null ? sortedList.Count : csl.endIndex;

            startIndex = this.BinarySearch(parentStart, parentEnd);

            endIndex = BinarySearchStartsWith(startIndex, parentEnd);
        }

        int BinarySearch(int start, int end)
        {
            while (start <= end && start < global.Keys.Count)
            {
                int mid = (start + end) / 2;

                var num = global.Keys[mid].CompareTo(Prefix);

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
            if (!(start < global.Keys.Count && global.Keys[start].StartsWith(Prefix)))
                return start;

            while (start < end && start < global.Keys.Count)
            {
                int mid = (start + end) / 2;

                if (global.Keys[mid].StartsWith(Prefix))
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
            return global.ContainsKey(Prefix + key);
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
            return global.TryGetValue(Prefix + key, out value);
        }

        public ICollection<V> Values
        {
            get { return this.Select(kvp => kvp.Value).ToReadOnly(); }
        }

        public V this[string key]
        {
            get
            {
                return global[Prefix + key];
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
            return ((ICollection<KeyValuePair<string, V>>)global).Contains(new KeyValuePair<string, V>(Prefix + item.Key, item.Value));
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
                yield return new KeyValuePair<string, V>(global.Keys[i].Substring(Prefix.Length), global.Values[i]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
