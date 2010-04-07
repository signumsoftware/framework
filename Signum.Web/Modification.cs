#region usings
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
using System.Collections.Specialized;
using System.Linq.Expressions;
#endregion

namespace Signum.Web
{
    public static class CustomModificationBinders
    {
        public delegate Modification ModificationBinder(SortedList<string, object> formValues, Interval<int> interval, string controlID);

        public static Dictionary<Type, ModificationBinder> binders = new Dictionary<Type, ModificationBinder>();
        public static Dictionary<Type, ModificationBinder> Binders { get { return binders; } }
    }

    public abstract class Modification
    {
        public string ControlID { get; private set; }
        public Type StaticType { get; private set; }
        public string BindingError { get; set; }
        public long? TicksLastChange { get; set; }

        protected static readonly string[] specialProperties = new[] 
        { 
            TypeContext.Id, 
            TypeContext.Ticks,
            EntityBaseKeys.RuntimeInfo,
            EntityBaseKeys.ToStr, 
            EntityBaseKeys.Implementations,
            EntityListBaseKeys.Index,
            EntityComboKeys.Combo,
        }; 

        public Modification(Type staticType, string controlID)
        {
            this.StaticType = staticType;
            this.ControlID = controlID;
        }

        public abstract object ApplyChanges(Controller controller, object obj, ModificationState onFinish);

        public abstract void Validate(Controller controller, object entity, Dictionary<string, List<string>> errors, string prefix);
        
        public static Interval<int> FindSubInterval(SortedList<string, object> formValues, string prefix)
        {
            return FindSubInterval(formValues, new Interval<int>(0, formValues.Count), 0, prefix);
        }

        protected static Interval<int> FindSubInterval(SortedList<string, object> formValues, Interval<int> interval, int knownPrefixLength, string newPrefix)
        {
            if (newPrefix == null)
                newPrefix = "";

            int maxLength = knownPrefixLength + newPrefix.Length;

            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (!(subControlID.ContinuesWith(newPrefix, knownPrefixLength) &&
                     (subControlID.Length == maxLength || subControlID.ContinuesWith(TypeContext.Separator, maxLength))))
                    return new Interval<int>(interval.Min, i);
            }

            return interval;
        }

        public static Modification Create(Type staticType, SortedList<string, object> formValues, Interval<int> interval, string controlID)
        {
            if (controlID == null)
                controlID = "";
            var binder = CustomModificationBinders.Binders.TryGetC(staticType);
            if(binder!= null)
                return binder(formValues, interval, controlID);
            if (typeof(ModifiableEntity).IsAssignableFrom(staticType)  || typeof(IIdentifiable).IsAssignableFrom(staticType))
                return new EntityModification(staticType, formValues, interval, controlID);
            else if (typeof(Lite).IsAssignableFrom(staticType))
                return new LiteModification(staticType, formValues, interval, controlID);
            else if (Reflector.IsMList(staticType))
                return new MListModification(staticType, formValues, interval, controlID);
            else
                return new ValueModification(staticType, formValues, controlID);
        }

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
            }
        }

        public override object ApplyChanges(Controller controller, object obj, ModificationState onFinish)
        {
            return Value;
        }

        public override void Validate(Controller controller, object entity, Dictionary<string, List<string>> errors, string prefix)
        {
            if ((!prefix.HasText() || ControlID.StartsWith(prefix)) && BindingError != null)
                errors.GetOrCreate(ControlID).AddRange(BindingError.Lines());
        }

        public override string ToString()
        {
            return "Value({0}-Ticks:{1}): {2}".Formato(
                Value.TryCC(a => CSharpRenderer.Value(a, a.GetType(), null)) ?? "[null]",
                TicksLastChange != null ? TicksLastChange.ToString() : "",
                ControlID);
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

            public void SafeSet(ModifiableEntity entity, object value)
            {
                try
                {
                    if (PropertyPack.SetValue == null)
                    {
                        if (PropertyPack.PropertyInfo.PropertyType != typeof(bool))
                            throw new InvalidOperationException("Binding Error");
                        return; //MVC Helper will send the hidden field even if the checkbox was disabled
                    }
                    PropertyPack.SetValue(entity, value);
                }
                catch (NullReferenceException nullEx)
                {
                    if (Modification.BindingError != null && Modification.BindingError.Contains("Binding Error"))
                    {
                        Modification.BindingError = Modification.BindingError.Replace("Binding Error", "");
                        if (Modification.BindingError != null && Modification.BindingError.Contains("\r\n\r\n"))
                            Modification.BindingError = Modification.BindingError.Replace("\r\n\r\n", "");
                        Modification.BindingError = Modification.BindingError.AddLine(Resources.NotPossibleToAssign0.Formato(PropertyPack.PropertyInfo.NiceName()));
                    }
                    else if (entity != null && value == null && PropertyPack.PropertyInfo.PropertyType.IsValueType && !Modification.BindingError.HasText())
                        Modification.BindingError = Modification.BindingError.AddLine(Resources.ValueMustBeSpecifiedFor0.Formato(PropertyPack.PropertyInfo.NiceName()));
                    else
                        Modification.BindingError = nullEx.Message;
                }
                catch (Exception)
                {
                    if (Modification.BindingError != null && Modification.BindingError.Contains("Binding Error"))
                        Modification.BindingError = Modification.BindingError.Replace("Binding Error", "");
                    if (Modification.BindingError != null && Modification.BindingError.Contains("\r\n\r\n"))
                        Modification.BindingError = Modification.BindingError.Replace("\r\n\r\n", "");
                    Modification.BindingError = Modification.BindingError.AddLine(Resources.NotPossibleToAssign0.Formato(PropertyPack.PropertyInfo.NiceName()));
                }
            }
        }

        public Dictionary<string, PropertyPackModification> Properties { get; private set; }

        public EntityModification(Type staticType, string controlID)
            : base(staticType, controlID) 
        { }

        public EntityModification(Type staticType, SortedList<string, object> formValues, Interval<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (!formValues.ContainsKey(TypeContext.Compose(controlID, EntityBaseKeys.RuntimeInfo)))
                AvoidChange = true;
            else
            {
                string sfInfo = (string)formValues[TypeContext.Compose(controlID, EntityBaseKeys.RuntimeInfo)];
                RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(sfInfo);

                RuntimeType = runtimeInfo.RuntimeType;
                IsNew = runtimeInfo.IsNew;
                EntityId = runtimeInfo.IdOrNull;
                TicksLastChange = runtimeInfo.Ticks;
            }
            
            Fill(formValues, interval);
        }

        protected void Fill(SortedList<string, object> formValues, Interval<int> interval)
        {
            int propertyStart = ControlID.Length + TypeContext.Separator.Length;

            Dictionary<string, PropertyPack> propertyValidators = null;
            Type myType = RuntimeType ?? StaticType;
            if (!myType.IsInterface && !myType.IsAbstract)
                propertyValidators = Validator.GetPropertyPacks(myType);

            Properties = new Dictionary<string, PropertyPackModification>();

            if (TicksLastChange != null && TicksLastChange == 0)
                return; //Don't apply changes, it will affect other properties and it has not been changed in the IU
    
            for (int i = interval.Min; i < interval.Max; i++)
            {
                string subControlID = formValues.Keys[i];

                if (!subControlID.ContinuesWith(TypeContext.Separator, ControlID.Length))
                    throw new FormatException(Resources.ControlID0HasAnInvalidFormat.Formato(subControlID));

                int propertyEnd = subControlID.IndexOf(TypeContext.Separator, propertyStart).Map(pe => pe == -1 ? subControlID.Length : pe);

                string propertyName = subControlID.Substring(propertyStart, propertyEnd - propertyStart);

                if (specialProperties.Contains(propertyName))
                    continue;

                string commonSubControlID = subControlID.Substring(0, propertyEnd);

                i = GeneratePropertyModification(formValues, interval, subControlID, commonSubControlID, propertyName, i, propertyValidators);
            }
            
        }

        protected virtual int GeneratePropertyModification(SortedList<string, object> formValues, Interval<int> interval, string subControlID, string commonSubControlID, string propertyName, int index, Dictionary<string, PropertyPack> propertyValidators)
        {
            PropertyPack pp = propertyValidators.GetOrThrow(propertyName, Resources.NoPropertyWithName0FoundInType1.Formato(propertyName, RuntimeType));

            Interval<int> subInterval = FindSubInterval(formValues, new Interval<int>(index, interval.Max), ControlID.Length, TypeContext.Separator + propertyName);

            string propertySFInfo = (string)formValues.TryGetC(TypeContext.Compose(commonSubControlID, EntityBaseKeys.RuntimeInfo));
            RuntimeInfo propertyInfo = propertySFInfo.HasText() ? RuntimeInfo.FromFormValue(propertySFInfo) : null;
            
            long? propertyIsLastChange = null;
            if (propertyInfo.TryCS(nfo => nfo.Ticks) != null)
            {
                if (TicksLastChange == 0)
                    return subInterval.Max - 1; //Don't apply changes, it will affect other properties and it has not been changed in the IU
                else
                    propertyIsLastChange = propertyInfo.Ticks;
            }

            Modification mod = Modification.Create(pp.PropertyInfo.PropertyType, formValues, subInterval, commonSubControlID);
            if (mod.TicksLastChange == null)
                mod.TicksLastChange = propertyIsLastChange;
            Properties.Add(propertyName, new PropertyPackModification { Modification = mod, PropertyPack = pp });
            return subInterval.Max - 1;
        }

        public override object ApplyChanges(Controller controller, object obj, ModificationState onFinish)
        {
            ModifiableEntity entity;
            if (AvoidChange)
                entity = (ModifiableEntity)obj;
            else
                entity = Change(controller, (ModifiableEntity)obj);

            if (Properties != null)
            {
                foreach (var ppm in Properties.Values)
                {
                    ApplyChangesProperty(ppm, controller, entity, onFinish);
                }
            }            

            return entity;
        }

        protected virtual void ApplyChangesProperty(PropertyPackModification ppm, Controller controller, ModifiableEntity entity, ModificationState onFinish)
        {
            object oldValue = (entity != null) ? ppm.PropertyPack.GetValue(entity) : null;
            object newValue = ppm.Modification.ApplyChanges(controller, oldValue, onFinish);

            if (ppm.Modification.TicksLastChange != null && ppm.Modification.TicksLastChange != 0)
            {
                PropertyPack pp = ppm.PropertyPack;
                onFinish.Actions.Add(ppm.Modification.TicksLastChange.Value,
                    new Tuple<string, Action>(ppm.Modification.ControlID, () => ppm.SafeSet(entity, newValue)));
            }
            else
                ppm.SafeSet(entity, newValue);
        }       

        protected virtual ModifiableEntity Change(Controller controller, ModifiableEntity entity)
        {
            if (RuntimeType == null)
                return null;

            if (IsNew)
            {
                if (entity != null)
                {
                    if (typeof(EmbeddedEntity).IsAssignableFrom(entity.GetType()) ||
                        (typeof(IIdentifiable).IsAssignableFrom(entity.GetType()) && ((IIdentifiable)entity).IsNew))
                        return entity;
                }
                return Constructor.Construct(RuntimeType);
            }

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

        public override void Validate(Controller controller, object entity, Dictionary<string, List<string>> errors, string prefix)
        {
            if (entity!=null && Properties != null)
            {
                foreach (var ppm in Properties.Values)
                {
                    ppm.Modification.Validate(controller, ppm.PropertyPack.GetValue(entity), errors, prefix);

                    if (!prefix.HasText() || ControlID.StartsWith(prefix))
                    {
                        string error = ((ModifiableEntity)entity).PropertyCheck(ppm.PropertyPack);
                        if (error != null)
                            errors.GetOrCreate(ppm.Modification.ControlID).AddRange(error.Lines());
                    }
                }
            }

            if ((!prefix.HasText() || ControlID.StartsWith(prefix)) && BindingError.HasText())
                errors.GetOrCreate(ControlID).Add(BindingError);
        }

        public override string ToString()
        {
            string identity =
                RuntimeType == null ? "[null]" :
                EntityId == null ? RuntimeType.TypeName() :
                "{0}({1})".Formato(RuntimeType.TypeName(), EntityId);

            return "Entity({0}-Ticks:{1}): {2}\r\n{{\r\n{3}\r\n}}".Formato(
                identity,
                TicksLastChange != null ? TicksLastChange.ToString() : "",
                ControlID,
                Properties.ToString(kvp => "{0} = {1}".Formato(
                    kvp.Key,
                    kvp.Value.Modification), "\r\n").Indent(4));
        }
    }

    public class LiteModification : Modification
    {
        public Type RuntimeType;
        public int? EntityId; //optional
        public Type CleanType;
        public EntityModification EntityModification;
        public bool IsNew;
        public bool AvoidChange = false; //I only have some ValueLines of an Entity (so no Runtime, Id or anything)

        public LiteModification(Type staticType, string controlID)
            : base(staticType, controlID) 
        { }

        public LiteModification(Type staticType, SortedList<string, object> formValues, Interval<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (!formValues.ContainsKey(TypeContext.Compose(controlID, EntityBaseKeys.RuntimeInfo)))
                AvoidChange = true;
            else
            {
                string sfInfo = (string)formValues[TypeContext.Compose(controlID, EntityBaseKeys.RuntimeInfo)];
                RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(sfInfo);

                RuntimeType = runtimeInfo.RuntimeType;
                IsNew = runtimeInfo.IsNew;
                EntityId = runtimeInfo.IdOrNull;
                TicksLastChange = runtimeInfo.Ticks;
            }
            
            CleanType = Reflector.ExtractLite(staticType);

            if (CustomModificationBinders.Binders.ContainsKey(CleanType))
                EntityModification = (EntityModification)CustomModificationBinders.Binders[CleanType](formValues, interval, controlID);
            else
                EntityModification = new EntityModification(CleanType, formValues, interval, controlID);
            
            if (EntityModification.Properties.Count == 0)
                EntityModification = null;
        }

        public override object ApplyChanges(Controller controller, object obj, ModificationState onFinish)
        {
            Lite lite = (Lite)obj;

            if (AvoidChange)
                return Lite.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(controller, Database.Retrieve(lite), onFinish));

            if (RuntimeType == null)
                return null;

            if (IsNew)
            {
                if (lite != null && lite.UntypedEntityOrNull != null && lite.UntypedEntityOrNull.IsNew)
                    return Lite.Create(CleanType,
                            (IdentifiableEntity)EntityModification.ApplyChanges(controller, lite.UntypedEntityOrNull, onFinish));
                return Lite.Create(CleanType, (IdentifiableEntity)EntityModification.ApplyChanges(controller, null, onFinish));
            }

            if (lite == null)
            {
                if (EntityModification == null)
                    return Database.RetrieveLite(CleanType, RuntimeType, EntityId.Value);
                else
                    return Lite.Create(CleanType,
                        (IdentifiableEntity)EntityModification.ApplyChanges(
                           controller, Database.Retrieve(RuntimeType, EntityId.Value), onFinish));
            }

            if (EntityId == null)
            {
                Debug.Assert(lite.IdOrNull == null && RuntimeType == lite.GetType() && EntityModification != null);
                return Lite.Create(CleanType,
                    (IdentifiableEntity)EntityModification.ApplyChanges(controller, lite.UntypedEntityOrNull, onFinish));
            }
            else
            {
                if (EntityId.Value == lite.IdOrNull && RuntimeType == lite.RuntimeType)
                {
                    if (EntityModification == null)
                        return lite;
                    else
                        return Lite.Create(CleanType,
                            (IdentifiableEntity)EntityModification.ApplyChanges(controller, Database.Retrieve(lite), onFinish));
                }
                else
                {
                    if (EntityModification == null)
                        return Database.RetrieveLite(CleanType, RuntimeType, EntityId.Value);
                    else
                        return Lite.Create(CleanType,
                            (IdentifiableEntity)EntityModification.ApplyChanges(controller, Database.Retrieve(RuntimeType, EntityId.Value), onFinish));
                }
            }
        }

        public override void Validate(Controller controller, object entity, Dictionary<string, List<string>> errors, string prefix)
        {
            if (EntityModification != null)
                EntityModification.Validate(controller, ((Lite)entity).TryCC(l => l.UntypedEntityOrNull), errors, prefix); 
        }

        public override string ToString()
        {
            string identity =
                RuntimeType == null ? "[null]" :
                EntityId == null ? RuntimeType.TypeName() :
                "{0}({1})".Formato(RuntimeType.TypeName(), EntityId);

            return "Lite({0}-Ticks:{1}): {2}\r\n{{\r\n{3}\r\n}}".Formato(
                identity,
                TicksLastChange != null ? TicksLastChange.ToString() : "",
                ControlID,
                EntityModification.ToString());
        }
    }

    public class MListModification : Modification
    {
        public List<Tuple<Modification, int?>> modifications;
        public Type staticElementType;

        public MListModification(Type staticType, string controlID)
            : base(staticType, controlID) 
        { }

        public MListModification(Type staticType, SortedList<string, object> formValues, Interval<int> interval, string controlID)
            : base(staticType, controlID)
        {
            if (!Reflector.IsMList(staticType))
                throw new InvalidOperationException(Resources.MListModificationWithStaticType0.Formato(staticType.TypeName()));

            staticElementType = ReflectionTools.CollectionType(staticType);
            
            Fill(formValues, interval);
        }

        private void Fill(SortedList<string, object> formValues, Interval<int> interval)
        {
            SortedList<int, Tuple<Modification, int?>> list = new SortedList<int, Tuple<Modification, int?>>();

            int propertyStart = ControlID.Length + TypeContext.Separator.Length;

            if (TicksLastChange == null && TicksLastChange == 0)
                return; //Don't apply changes, it will affect other properties and it has not been changed in the IU

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

                    throw new InvalidOperationException(Resources.ControlID0HasAnInvalidFormat.Formato(subControlID));
                }

                string index = subControlID.Substring(propertyStart, propertyEnd - propertyStart);
                string commonSubControlID = subControlID.Substring(0, propertyEnd);

                Interval<int> subInterval = FindSubInterval(formValues, new Interval<int>(i, interval.Max), ControlID.Length, TypeContext.Separator + index);
                Modification mod = Modification.Create(staticElementType, formValues, subInterval, commonSubControlID);
                
                string oldIndex = "";
                string indexFormEntry = TypeContext.Compose(commonSubControlID, EntityListBaseKeys.Index);
                if (formValues.ContainsKey(indexFormEntry))
                    oldIndex = formValues[indexFormEntry].ToString();

                list.Add(int.Parse(index), Tuple.New(mod, string.IsNullOrEmpty(oldIndex) ? (int?)null : (int?)int.Parse(oldIndex)));

                i = subInterval.Max - 1;
            }

            modifications = list.Values.ToList();
        }

        public override object ApplyChanges(Controller controller, object obj, ModificationState onFinish)
        {
            IList old = (IList)obj;
            IList list = (IList)Activator.CreateInstance(StaticType, modifications.Count);
            foreach (var item in modifications)
            {
                if (item.Second.HasValue && old != null && old.Count > item.Second.Value)
                    list.Add(item.First.ApplyChanges(controller, old[item.Second.Value], onFinish));
                else
                    list.Add(item.First.ApplyChanges(controller, null, onFinish));
            }
            return list;
        }

        public override void Validate(Controller controller, object entity, Dictionary<string, List<string>> errors, string prefix)
        {
            IList list = (IList)entity;
            for (int i = 0; i < modifications.Count; i++)
            {
                Tuple<Modification, int?> item = modifications[i];
                if (item.Second.HasValue && list.Count > item.Second.Value)
                    item.First.Validate(controller, list[item.Second.Value], errors, prefix);
                else
                {
                    //TODO Anto: If an MList of Lite of an abstract type, the following construct will fail
                    object newValue = typeof(Lite).IsAssignableFrom(item.First.StaticType) ?
                        ((IdentifiableEntity)Constructor.Construct(Reflector.ExtractLite(item.First.StaticType))).ToLiteFat() :
                        (object)Constructor.Construct(item.First.StaticType);
                    ModificationState modState = new ModificationState();
                    Modifiable mod = (Modifiable)item.First.ApplyChanges(controller, newValue, modState);
                    modState.Finish();
                    item.First.Validate(controller, mod, errors, prefix);
                }
            }
        }

        public override string ToString()
        {
            return "List<{0}>(Count:{1}-Ticks:{2}): {3}\r\n{{\r\n{4}\r\n}}".Formato(
                staticElementType.Name,
                modifications.Count,
                TicksLastChange != null ? TicksLastChange.ToString() : "",
                ControlID,
                modifications.ToString("\r\n").Indent(4));
        }
    }

    public class ModificationState
    {
        /// <summary>
        /// Ticks => ControlID, Action
        /// </summary>
        public SortedList<long, Tuple<string, Action>> Actions = new SortedList<long, Tuple<string, Action>>();

        public static Dictionary<string, long> ToDictionary(SortedList<long, Tuple<string, Action>> actions)
        {
            return actions.ToDictionary(kvp => kvp.Value.First, kvp => kvp.Key);
        }

        public void Finish()
        {
            if (Actions.Count > 0)
                foreach (var key in Actions.Keys)
                    Actions[key].Second();
        }
    }

    public class ChangesLog
    {
        public Dictionary<string, List<string>> Errors { get; set; }
        /// <summary>
        /// ControlID => Ticks
        /// </summary>
        public Dictionary<string, long> ChangeTicks;
    }
}
