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

namespace Signum.Web
{
    public abstract class Modification
    {
        public string ControlID { get; private set; }
        public Type StaticType { get; private set; }
        public string BindingError { get; internal set; }

        protected static readonly string[] specialProperties = new[] { 
            TypeContext.RuntimeType, 
            TypeContext.Id, 
            TypeContext.TypeName, 
            EntityBaseKeys.ToStr, 
            EntityBaseKeys.ImplementationsDDL }; 

        public Modification(Type staticType, string controlID)
        {
            this.StaticType = staticType;
            this.ControlID = controlID;
        }

        public abstract object ApplyChanges(object obj);

        public abstract void Validate(object entity, Dictionary<string, List<string>> errors);


        public static MinMax<int> FindSubInterval(SortedList<string, object> formValues, string prefix)
        {
            return FindSubInterval(formValues, new MinMax<int>(0, formValues.Count), 0, prefix);
        }

        protected static MinMax<int> FindSubInterval(SortedList<string, object> formValues, MinMax<int> interval, int knownPrefixLength, string newPrefix)
        {
            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (!string.IsNullOrEmpty(newPrefix) && subControlID.IndexOf(newPrefix, knownPrefixLength) != knownPrefixLength)
                    return new MinMax<int>(interval.Min, i);
            }

            return interval;
        }

        public static Modification Create(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
        {
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

    class ValueModification : Modification
    {
        object Value;

        public ValueModification(Type staticType, SortedList<string, object> formValues, string controlID)
            : base(staticType, controlID)
        {
            try
            {
                Value = ReflectionTools.Parse((string)formValues[controlID], staticType);
            }
            catch (Exception ex)
            {
                BindingError = BindingError.AddLine(ex.Message);
            }
        }

        public override object ApplyChanges(object obj)
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
            return "Value({0}): {1}".Formato(Value.TryCC(a => CSharpAuxRenderer.Value(a, a.GetType())) ?? "[null]", ControlID);
        }
    }

    class EntityModification : Modification
    {
        Type RuntimeType; 
        int? EntityId; //optional

        internal class PropertyPackModification
        {
            public PropertyPack PropertyPack; 
            public Modification Modification; 
        }

        internal Dictionary<string, PropertyPackModification> Properties { get; private set; }

        public EntityModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (typeof(EmbeddedEntity).IsAssignableFrom(staticType))
                RuntimeType = staticType;
            else
            {
                string runtimeTypeName = (string)formValues[controlID + TypeContext.Separator + TypeContext.RuntimeType];
                RuntimeType = runtimeTypeName.HasText() ? Navigator.ResolveType(runtimeTypeName) : null; 
                
                string id = (string)formValues[controlID + TypeContext.Separator + TypeContext.Id]; 
                EntityId = id.HasText()? int.Parse(id):(int?)null;
            }

            Fill(formValues, interval);
        }

        private void Fill(SortedList<string, object> formValues, MinMax<int> interval)
        {
            int propertyStart = ControlID.Length + TypeContext.Separator.Length;

            var propertyValidators = ModifiableEntity.GetPropertyValidators(RuntimeType);

            Properties = new Dictionary<string, PropertyPackModification>();

            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (subControlID.IndexOf(TypeContext.Separator, ControlID.Length) != ControlID.Length)
                    throw new FormatException("The control ID {0} has an invalid format".Formato(subControlID));

                int propertyEnd = subControlID.IndexOf(TypeContext.Separator, propertyStart).Map(pe => pe == -1 ? subControlID.Length : pe);

                string propertyName = subControlID.Substring(propertyStart, propertyEnd - propertyStart);

                if (specialProperties.Contains(propertyName))
                    continue;

                string commonSubControlID = subControlID.Substring(0, propertyEnd);
                PropertyPack pp = propertyValidators.GetOrThrow(propertyName, Resource.NoPropertyWithName0FoundInType0.Formato(propertyName, RuntimeType));

                MinMax<int> subInterval = FindSubInterval(formValues, new MinMax<int>(i, interval.Max), ControlID.Length, TypeContext.Separator + propertyName);
                Modification mod = Modification.Create(pp.PropertyInfo.PropertyType, formValues, subInterval, commonSubControlID);
                Properties.Add(propertyName, new PropertyPackModification { Modification = mod, PropertyPack = pp });
                i = subInterval.Max - 1;
            }
        }

        public override object ApplyChanges(object obj)
        {
            if (RuntimeType == null)
                return null; 

            ModifiableEntity entity = Change((ModifiableEntity)obj);

            foreach (var ppm in Properties.Values)
            {
                object oldValue = ppm.PropertyPack.GetValue(entity);
                object newValue =  ppm.Modification.ApplyChanges(oldValue);
                ppm.PropertyPack.SetValue(entity, newValue); 
            }

            return entity;
        }

        private ModifiableEntity Change(ModifiableEntity entity)
        {
            if (typeof(EmbeddedEntity).IsAssignableFrom(RuntimeType))
            {
                if (entity == null)
                    return (EmbeddedEntity)Constructor.Construct(RuntimeType);
                else
                    return entity;
            }
            else
            {
                IdentifiableEntity ident = (IdentifiableEntity)entity;

                if (ident == null)
                {
                    if (EntityId == null)
                        return (IdentifiableEntity)Constructor.Construct(RuntimeType);
                    else
                        return Database.Retrieve(RuntimeType, EntityId.Value);
                }
                else
                {
                    if (EntityId == ident.IdOrNull && RuntimeType == ident.GetType())
                        return ident;
                    else
                    {
                        if (EntityId == null)
                            return (IdentifiableEntity)Constructor.Construct(RuntimeType);
                        else
                            return Database.Retrieve(RuntimeType, EntityId.Value);
                    }
                }
            }
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            foreach (var ppm in Properties.Values)
            {
                ppm.Modification.Validate(ppm.PropertyPack.GetValue(entity), errors);

                string error = ((ModifiableEntity)entity)[ppm.PropertyPack.PropertyInfo.Name];
                if (error != null)
                    errors.GetOrCreate(ppm.Modification.ControlID).AddRange(error.Lines());
            }
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

    class LazyModification : Modification
    {
        Type RuntimeType; 
        int? EntityId; //optional
        Type CleanType; 
        EntityModification EntityModification;

        public LazyModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, controlID)
        {
            string runtimeTypeName = (string)formValues[controlID + TypeContext.Separator + TypeContext.RuntimeType];
            RuntimeType = runtimeTypeName.HasText() ? Navigator.ResolveType(runtimeTypeName) : null;

            string id = (string)formValues[controlID + TypeContext.Separator + TypeContext.Id];
            EntityId = id.HasText() ? int.Parse(id) : (int?)null;

            CleanType = Reflector.ExtractLazy(staticType); 

            EntityModification = new EntityModification(CleanType, formValues, interval, controlID);
            if (EntityModification.Properties.Count == 0)
                EntityModification = null;
        }

        public override object ApplyChanges(object obj)
        {
            if (RuntimeType == null)
                return null;

            Lazy lazy = (Lazy)obj;

            if (lazy == null)
            {
                if (EntityId == null)
                    return Lazy.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(null));
                else
                {
                    if (EntityModification == null)
                        return Lazy.Create(CleanType, RuntimeType, EntityId.Value);
                    else
                        return Lazy.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(Database.Retrieve(RuntimeType, EntityId.Value)));
                }
            }
            else
            {
                if (EntityId == null)
                {
                    if (lazy.IdOrNull != null)
                        return Lazy.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(null));
                    else
                        return Lazy.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(lazy.UntypedEntityOrNull));
                }
                else
                {
                    if (EntityId == lazy.IdOrNull && RuntimeType == lazy.RuntimeType)
                    {
                        if (EntityModification == null)
                            return lazy;
                        else
                            return Lazy.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(Database.Retrieve(lazy)));
                    }
                    else
                        return Lazy.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(Database.Retrieve(RuntimeType, EntityId.Value)));
                }
            }
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            if (EntityModification != null)
                EntityModification.Validate(((Lazy)entity).UntypedEntityOrNull, errors); 
        }
    }

    class MListModification : Modification
    {
        List<Modification> modifications;
        Type staticElementType;

        public MListModification(Type staticType, SortedList<string, object> formValues, MinMax<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (!Reflector.IsMList(staticType))
                throw new InvalidOperationException("MListModification with staticType {0}".Formato(staticType.TypeName()));

            staticElementType = Reflector.CollectionType(staticType);

            Fill(formValues, interval);
        }

        private void Fill(SortedList<string, object> formValues, MinMax<int> interval)
        {
            SortedList<int, Modification> list = new SortedList<int, Modification>();

            int propertyStart = ControlID.Length + TypeContext.Separator.Length;

            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (subControlID.IndexOf(TypeContext.Separator, ControlID.Length) != ControlID.Length)
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

                list.Add(int.Parse(index), mod);

                i = subInterval.Max - 1;
            }

            modifications = list.Values.ToList();
        }


        public override object ApplyChanges(object obj)
        {
            IList list = (IList)Activator.CreateInstance(StaticType, modifications.Count);
            foreach (var item in modifications)
            {
                list.Add(item.ApplyChanges(null));
            }
            return list;
        }

        public override void Validate(object entity, Dictionary<string, List<string>> errors)
        {
            IList list = (IList)entity;
            list.Cast<object>().ZipForeachStrict(modifications, (obj, mod) => mod.Validate(obj, errors));
        }
    }
}
