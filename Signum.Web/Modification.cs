using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Web.Properties;
using Signum.Engine;
using Signum.Entities.Reflection;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Web.Mvc;

namespace Signum.Web
{
    public static class CustomModificationBinders
    {
        public delegate Modification ModificationBinder(SortedList<string, object> formValues, MinMax<int> interval, string controlID);

        public static Dictionary<Type, ModificationBinder> binders = new Dictionary<Type, ModificationBinder>();
        public static Dictionary<Type, ModificationBinder> Binders { get { return binders; } }
    }

    public abstract class Modification
    {
        public string ControlID { get; private set; }
        public Type StaticType { get; private set; }
        public string BindingError { get; set; }
        public bool IsLastChange { get; set; }

        protected static readonly string[] specialProperties = new[] { 
            TypeContext.RuntimeType, 
            TypeContext.Id, 
            TypeContext.StaticType, 
            TypeContext.Changed,
            EntityBaseKeys.ToStr, 
            EntityBaseKeys.ImplementationsDDL,
            EntityBaseKeys.IsNew,
            EntityListKeys.Index,
            EntityComboKeys.Combo,
        }; 

        public Modification(Type staticType, string controlID)
        {
            this.StaticType = staticType;
            this.ControlID = controlID;
        }

        public abstract object ApplyChanges(Controller controller, object obj, ModificationFinish onFinish);

        public abstract void Validate(object entity, Dictionary<string, List<string>> errors);
        
        public static MinMax<int> FindSubInterval(SortedList<string, object> formValues, string prefix)
        {
            return FindSubInterval(formValues, new MinMax<int>(0, formValues.Count), 0, prefix);
        }

        protected static MinMax<int> FindSubInterval(SortedList<string, object> formValues, MinMax<int> interval, int knownPrefixLength, string newPrefix)
        {
            
            int maxLength = knownPrefixLength + newPrefix.Length;

            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (!(subControlID.ContinuesWith(newPrefix, knownPrefixLength) &&
                     (subControlID.Length == maxLength || subControlID.ContinuesWith(TypeContext.Separator, maxLength))))
                    return new MinMax<int>(interval.Min, i);
            }

            return interval;
        }

        public static Modification Create(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
        {
            var binder = CustomModificationBinders.Binders.TryGetC(staticType);
            if(binder!= null)
                return binder(formValues, interval, controlID);
            if (typeof(ModifiableEntity).IsAssignableFrom(staticType)  || typeof(IIdentifiable).IsAssignableFrom(staticType))
                return new EntityModification(staticType, formValues, interval, controlID);
            else if (typeof(Lazy).IsAssignableFrom(staticType))
                return new LazyModification(staticType, formValues, interval, controlID);
            else if (Reflector.IsMList(staticType))
                return new MListModification(staticType, formValues, interval, controlID);
            else
                return new ValueModification(staticType, formValues, controlID);
        }
    }

    public class ValueModification : Modification
    {
        public object Value;

        public ValueModification(Type staticType, string controlID)
            : base(staticType, controlID) 
        { }

        public ValueModification(Type staticType, SortedList<string, object> formValues, string controlID)
            : base(staticType, controlID)
        {
            string valueStr = (string)formValues[controlID];
            try
            {
                if (staticType.UnNullify() == typeof(bool))
                {
                    string[] vals = valueStr.Split(',');
                    Value = (vals[0] == "true" || vals[0] == "True") ? true : false;
                }
                else
                    Value = ReflectionTools.Parse(valueStr, staticType);
            }
            catch (Exception )
            {
                BindingError = BindingError.AddLine("Binding Error");
                //BindingError = BindingError.AddLine(ex.Message);
            }
        }

        public override object ApplyChanges(Controller controller, object obj, ModificationFinish onFinish)
        {
            return Value;
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            if (BindingError != null)
                errors.GetOrCreate(ControlID).AddRange(BindingError.Lines());
        }

        public override string ToString()
        {
            return "Value({0}): {1}".Formato(Value.TryCC(a => CSharpRenderer.Value(a, a.GetType(), null)) ?? "[null]", ControlID);
        }
    }

    public class EntityModification : Modification
    {
        public Type RuntimeType { get; set; }
        public int? EntityId { get; set; } //optional
        public bool IsNew { get; set; }
        public bool AvoidChange = false; //I only have some ValueLines of an Entity (so no Runtime, Id or anything)

        public class PropertyPackModification
        {
            public PropertyPack PropertyPack; 
            public Modification Modification; 
        }

        public Dictionary<string, PropertyPackModification> Properties { get; private set; }

        public EntityModification(Type staticType, string controlID)
            : base(staticType, controlID) 
        { }

        public EntityModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (formValues.ContainsKey(controlID + TypeContext.Separator + TypeContext.RuntimeType))
            {
                string runtimeTypeName = (string)formValues[controlID + TypeContext.Separator + TypeContext.RuntimeType];
                RuntimeType = (runtimeTypeName.HasText()) ? Navigator.ResolveType(runtimeTypeName) : null;
            }
            else
            {
                AvoidChange = true;
            }
            
            IsNew = formValues.ContainsKey(controlID + TypeContext.Separator + EntityBaseKeys.IsNew);

            if (!typeof(EmbeddedEntity).IsAssignableFrom(staticType) && formValues.ContainsKey(controlID + TypeContext.Separator + TypeContext.Id))
            {
                string id = (string)formValues[controlID + TypeContext.Separator + TypeContext.Id];
                EntityId = id.HasText() ? int.Parse(id) : (int?)null;
            }

            Fill(formValues, interval);
        }

        private void Fill(SortedList<string, object> formValues, MinMax<int> interval)
        {
            int propertyStart = ControlID.Length + TypeContext.Separator.Length;

            Dictionary<string, PropertyPack> propertyValidators = null;
            Type myType = RuntimeType ?? StaticType;
            if (!myType.IsInterface && !myType.IsAbstract)
                propertyValidators = Reflector.GetPropertyValidators(myType);

            Properties = new Dictionary<string, PropertyPackModification>();

            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (!subControlID.ContinuesWith(TypeContext.Separator, ControlID.Length))
                    throw new FormatException("The control ID {0} has an invalid format".Formato(subControlID));

                int propertyEnd = subControlID.IndexOf(TypeContext.Separator, propertyStart).Map(pe => pe == -1 ? subControlID.Length : pe);

                string propertyName = subControlID.Substring(propertyStart, propertyEnd - propertyStart);

                if (specialProperties.Contains(propertyName))
                    continue;

                string commonSubControlID = subControlID.Substring(0, propertyEnd);

                i = GeneratePropertyModification(formValues, interval, subControlID, commonSubControlID, propertyName, i, propertyValidators);
            }
        }

        protected virtual int GeneratePropertyModification(SortedList<string, object> formValues, MinMax<int> interval, string subControlID, string commonSubControlID, string propertyName, int index, Dictionary<string, PropertyPack> propertyValidators)
        { 
            PropertyPack pp = propertyValidators.GetOrThrow(propertyName, Resource.NoPropertyWithName0FoundInType0.Formato(propertyName, RuntimeType));

            MinMax<int> subInterval = FindSubInterval(formValues, new MinMax<int>(index, interval.Max), ControlID.Length, TypeContext.Separator + propertyName);
            bool propertyIsLastChange = false;
            if (formValues.ContainsKey(subControlID + TypeContext.Separator + TypeContext.Changed))
            {
                string changed = (string)formValues.TryGetC(subControlID + TypeContext.Separator + TypeContext.Changed);
                if (!changed.HasText())
                    return subInterval.Max-1; //Don't apply changes, it will affect other properties and it has not been changed in the IU
                else
                    propertyIsLastChange = true; 
            }
            Modification mod = Modification.Create(pp.PropertyInfo.PropertyType, formValues, subInterval, commonSubControlID);
            mod.IsLastChange = propertyIsLastChange;
            Properties.Add(propertyName, new PropertyPackModification { Modification = mod, PropertyPack = pp });
            return subInterval.Max - 1;
        }

        public override object ApplyChanges(Controller controller, object obj, ModificationFinish onFinish)
        {
            ModifiableEntity entity;
            if (AvoidChange)
                entity = (ModifiableEntity)obj;
            else
                entity = Change(controller, (ModifiableEntity)obj);

            ApplyChangesOfProperties(controller, entity, onFinish);

            return entity;
        }

        protected virtual bool ApplyChangesOfProperties(Controller controller, ModifiableEntity entity, ModificationFinish onFinish)
        {
            if (Properties != null)
            {
                foreach (var ppm in Properties.Values)
                {
                    object oldValue = (entity != null) ? ppm.PropertyPack.GetValue(entity) : null;
                    object newValue = ppm.Modification.ApplyChanges(controller, oldValue, onFinish);
                    try
                    {
                        if (ppm.Modification.IsLastChange)
                            onFinish.OnFinish += ()=>ppm.PropertyPack.SetValue(entity, newValue);
                        ppm.PropertyPack.SetValue(entity, newValue);
                    }
                    catch (NullReferenceException nullEx)
                    {
                        if (ppm.Modification.BindingError != null && ppm.Modification.BindingError.Contains("Binding Error"))
                        {
                            ppm.Modification.BindingError = ppm.Modification.BindingError.Replace("Binding Error", "");
                            if (ppm.Modification.BindingError != null && ppm.Modification.BindingError.Contains("\r\n\r\n"))
                                ppm.Modification.BindingError = ppm.Modification.BindingError.Replace("\r\n\r\n", "");
                            ppm.Modification.BindingError = ppm.Modification.BindingError.AddLine(Resource.NotPossibleToAssign0To1.Formato(newValue, ppm.PropertyPack.PropertyInfo.NiceName()));
                        }
                        else if (entity != null && newValue == null && ppm.PropertyPack.PropertyInfo.PropertyType.IsValueType && !ppm.Modification.BindingError.HasText())
                            ppm.Modification.BindingError = ppm.Modification.BindingError.AddLine(Resource.ValueMustBeSpecifiedFor0.Formato(ppm.PropertyPack.PropertyInfo.NiceName()));
                        else
                            ppm.Modification.BindingError = nullEx.Message;
                    }
                    catch (Exception)
                    {
                        if (ppm.Modification.BindingError != null && ppm.Modification.BindingError.Contains("Binding Error"))
                            ppm.Modification.BindingError = ppm.Modification.BindingError.Replace("Binding Error", "");
                        if (ppm.Modification.BindingError != null && ppm.Modification.BindingError.Contains("\r\n\r\n"))
                            ppm.Modification.BindingError = ppm.Modification.BindingError.Replace("\r\n\r\n", "");
                        ppm.Modification.BindingError = ppm.Modification.BindingError.AddLine(Resource.NotPossibleToAssign0To1.Formato(newValue, ppm.PropertyPack.PropertyInfo.NiceName()));
                    }
                }
            }
            return false;
        }

        protected virtual ModifiableEntity Change(Controller controller, ModifiableEntity entity)
        {
            if (RuntimeType == null)
                return null;

            if (IsNew)
                return (ModifiableEntity)Constructor.Construct(RuntimeType, controller);

            if (typeof(EmbeddedEntity).IsAssignableFrom(RuntimeType))
                return entity;

            IdentifiableEntity ident = (IdentifiableEntity)entity;

            if (ident == null)
                return Database.Retrieve(RuntimeType, EntityId.Value);

            if (EntityId == ident.IdOrNull && RuntimeType == ident.GetType())
                return ident;
            else
                return Database.Retrieve(RuntimeType, EntityId.Value);
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            if (entity!=null && Properties != null)
            {
                foreach (var ppm in Properties.Values)
                {
                    ppm.Modification.Validate(ppm.PropertyPack.GetValue(entity), errors);

                    string error = ((ModifiableEntity)entity)[ppm.PropertyPack.PropertyInfo.Name];
                    if (error != null)
                        errors.GetOrCreate(ppm.Modification.ControlID).AddRange(error.Lines());
                }
            }

            if (!string.IsNullOrEmpty(BindingError))
                errors.GetOrCreate(ControlID).Add(BindingError);
        }

        public override string ToString()
        {
            string identity =
                RuntimeType == null ? "[null]" :
                EntityId == null ? RuntimeType.TypeName() :
                "{0}({1})".Formato(RuntimeType.TypeName(), EntityId);

            return "Entity({0}): {1}\r\n{{\r\n{2}\r\n}}".Formato(
                identity,
                ControlID,
                Properties.ToString(kvp => "{0} = {1}".Formato(
                    kvp.Key,
                    kvp.Value.Modification), "\r\n").Indent(4));
        }
    }

    public class LazyModification : Modification
    {
        public Type RuntimeType;
        public int? EntityId; //optional
        public Type CleanType;
        public EntityModification EntityModification;
        public bool IsNew;

        public LazyModification(Type staticType, string controlID)
            : base(staticType, controlID) 
        { }

        public LazyModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (formValues.ContainsKey(controlID + TypeContext.Separator + TypeContext.RuntimeType))
            {
                string runtimeTypeName = (string)formValues[controlID + TypeContext.Separator + TypeContext.RuntimeType];
                RuntimeType = (runtimeTypeName.HasText()) ? Navigator.ResolveType(runtimeTypeName) : null;
            }

            if (!typeof(EmbeddedEntity).IsAssignableFrom(staticType) && formValues.ContainsKey(controlID + TypeContext.Separator + TypeContext.Id))
            {
                string id = (string)formValues[controlID + TypeContext.Separator + TypeContext.Id];
                EntityId = id.HasText() ? int.Parse(id) : (int?)null;
            }

            IsNew = formValues.ContainsKey(controlID + TypeContext.Separator + EntityBaseKeys.IsNew);

            CleanType = Reflector.ExtractLazy(staticType);

            if (CustomModificationBinders.Binders.ContainsKey(CleanType))
                EntityModification = (EntityModification)CustomModificationBinders.Binders[CleanType](formValues, interval, controlID);
            else
                EntityModification = new EntityModification(CleanType, formValues, interval, controlID);
            
            if (EntityModification.Properties.Count == 0)
                EntityModification = null;
        }

        public override object ApplyChanges(Controller controller, object obj, ModificationFinish onFinish)
        {
            if (RuntimeType == null)
                return null;

            if (IsNew)
                return Lazy.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(controller, null, onFinish));

            Lazy lazy = (Lazy)obj;

            if (lazy == null)
            {
                if (EntityModification == null)
                    return Lazy.Create(CleanType, EntityId.Value, RuntimeType);
                else
                    return Lazy.Create(CleanType,
                        (IdentifiableEntity)EntityModification.ApplyChanges(
                           controller, Database.Retrieve(RuntimeType, EntityId.Value), onFinish));
            }

            if (EntityId == null)
            {
                Debug.Assert(lazy.IdOrNull == null && RuntimeType == lazy.GetType() && EntityModification != null);
                return Lazy.Create(CleanType,
                    (IdentifiableEntity)EntityModification.ApplyChanges(controller, lazy.UntypedEntityOrNull, onFinish));
            }
            else
            {
                if (EntityId.Value == lazy.IdOrNull && RuntimeType == lazy.RuntimeType)
                {
                    if (EntityModification == null)
                        return lazy;
                    else
                        return Lazy.Create(CleanType,
                            (IdentifiableEntity)EntityModification.ApplyChanges(controller, Database.Retrieve(lazy), onFinish));
                }
                else
                {
                    if (EntityModification == null)
                        return Lazy.Create(CleanType, EntityId.Value, RuntimeType);
                    else
                        return Lazy.Create(CleanType,
                            (IdentifiableEntity)EntityModification.ApplyChanges(controller, Database.Retrieve(RuntimeType, EntityId.Value), onFinish));
                }
            }
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            if (EntityModification != null)
                EntityModification.Validate(((Lazy)entity).TryCC(l => l.UntypedEntityOrNull), errors); 
        }
    }

    class MListModification : Modification
    {
        List<Tuple<Modification, int?>> modifications;
        Type staticElementType;

        public MListModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (!Reflector.IsMList(staticType))
                throw new InvalidOperationException("MListModification with staticType {0}".Formato(staticType.TypeName()));

            staticElementType = ReflectionTools.CollectionType(staticType);

            Fill(formValues, interval);
        }

        private void Fill(SortedList<string, object> formValues, MinMax<int> interval)
        {
            SortedList<int, Tuple<Modification, int?>> list = new SortedList<int, Tuple<Modification, int?>>();

            int propertyStart = ControlID.Length + TypeContext.Separator.Length;

            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (!subControlID.ContinuesWith(TypeContext.Separator, ControlID.Length))
                    continue;

                int propertyEnd = subControlID.IndexOf(TypeContext.Separator, propertyStart);

                if (propertyEnd == -1)
                {
                    string propertyName = subControlID.Substring(propertyStart);
                    if (specialProperties.Contains(propertyName))
                        continue; 

                    throw new InvalidOperationException("Malformed controlID {0}".Formato(subControlID));
                }

                string index = subControlID.Substring(propertyStart, propertyEnd - propertyStart);
                string commonSubControlID = subControlID.Substring(0, propertyEnd);

                MinMax<int> subInterval = FindSubInterval(formValues, new MinMax<int>(i, interval.Max), ControlID.Length, TypeContext.Separator + index);
                Modification mod = Modification.Create(staticElementType, formValues, subInterval, commonSubControlID);
                
                string oldIndex = "";
                string indexFormEntry = commonSubControlID + TypeContext.Separator + EntityListKeys.Index;
                if (formValues.ContainsKey(indexFormEntry))
                    oldIndex = formValues[indexFormEntry].ToString();

                list.Add(int.Parse(index), Tuple.New(mod, string.IsNullOrEmpty(oldIndex) ? (int?)null : (int?)int.Parse(oldIndex)));

                i = subInterval.Max - 1;
            }

            modifications = list.Values.ToList();
        }


        public override object ApplyChanges(Controller controller, object obj, ModificationFinish onFinish)
        {
            IList old = (IList)obj;
            IList list = (IList)Activator.CreateInstance(StaticType, modifications.Count);
            foreach (var item in modifications)
            {
                if (item.Second.HasValue)
                    list.Add(item.First.ApplyChanges(controller, old[item.Second.Value], onFinish));
                else
                    list.Add(item.First.ApplyChanges(controller, null, onFinish));
            }
            return list;
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            IList list = (IList)entity;
            list.Cast<object>().ZipForeachStrict(modifications, (obj, mod) => mod.First.Validate(obj, errors));
        }
    }

    public class ReloadModificationException : Exception
    { 
        
    }

    public class ModificationFinish
    {
        public event Action OnFinish;

        public void Finish()
        {
            if (OnFinish != null)
                OnFinish();
        }
    }
}
