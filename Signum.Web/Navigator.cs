using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Collections.Specialized;
using Signum.Web.Properties;
using Signum.Entities.Properties;
using Signum.Entities.Reflection;

namespace Signum.Web
{
    public static class Navigator
    {
        public static NavigationManager NavigationManager;

        //TODO: 
        //public static EntitySettings FindSettings(Type type)
        //{
        //     //return type.Generate(t => t.BaseType).Select(t => Settings.TryGetC(t)).NotNull().FirstOrDefault();
        //}

        public static Type ResolveType(string typeName)
        {
            return NavigationManager.ResolveType(typeName);
        }

        public static Type ResolveTypeFromUrlName(string typeUrlName)
        {
            return NavigationManager.ResolveTypeFromUrlName(typeUrlName); 
        }

        public static ViewResult View(this Controller controller, object obj)
        {
            return NavigationManager.View(controller, obj); 
        }

        public static PartialViewResult PartialView<T>(this Controller controller, T entity, string prefix)
        {
            return NavigationManager.PartialView(controller, entity, prefix, "", "");
        }

        public static PartialViewResult PartialView<T>(this Controller controller, T entity, string prefix, string onOk, string onCancel)
        {
            return NavigationManager.PartialView(controller, entity, prefix, onOk, onCancel);
        }

        public static SortedList<string, object> ToSortedList(NameValueCollection form, string prefixToIgnore)
        {
            SortedList<string, object> formValues = new SortedList<string, object>(form.Count);
            foreach (string key in form.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key != "prefixToIgnore")
                {
                    if (string.IsNullOrEmpty(prefixToIgnore) || !key.StartsWith(prefixToIgnore))
                        formValues.Add(key, form[key]);
                }
            }
            
            return formValues;
        }

        public static Dictionary<string, List<string>> ApplyChangesAndValidate(NameValueCollection form, string prefixToIgnore, Modifiable obj)
        {
            SortedList<string, object> formValues = ToSortedList(form, prefixToIgnore);

            return NavigationManager.ApplyChangesAndValidate(formValues, obj, null);
        }

        public static Dictionary<string, List<string>> ApplyChangesAndValidate(SortedList<string, object> formValues, Modifiable obj)
        {
            return NavigationManager.ApplyChangesAndValidate(formValues, obj, null);
        }

        public static Dictionary<string, List<string>> ApplyChangesAndValidate(SortedList<string, object> formValues, Modifiable obj, string prefix)
        {
            return NavigationManager.ApplyChangesAndValidate(formValues, obj, prefix);
        }

        public static IdentifiableEntity ExtractEntity(NameValueCollection form)
        {
            return NavigationManager.ExtractEntity(form, null);
        }

        public static IdentifiableEntity ExtractEntity(NameValueCollection form, string prefix)
        {
            return NavigationManager.ExtractEntity(form, prefix);
        }

        public static ModifiableEntity CreateInstance(Type type)
        {
            lock (NavigationManager.Constructors)
            {
                return NavigationManager.Constructors
                    .GetOrCreate(type, () => ReflectionTools.CreateConstructor<ModifiableEntity>(type))
                    .Invoke();
            }
        }

        static Dictionary<string, Type> nameToType;
        public static Dictionary<string, Type> NameToType
        {
            get { return nameToType.ThrowIfNullC("Names to Types dictionary not initialized"); }
            set { nameToType = value; }
        }

        public static string NormalPageUrl
        {
            get { return NavigationManager.NormalPageUrl; }
            set { NavigationManager.NormalPageUrl = value; }
        }


        internal static void ConfigureEntityBase(EntityBase el, Type entityType, bool admin)
        {
            EntitySettings es = NavigationManager.Settings[entityType];
            if (es.IsCreable != null)
                el.Create = es.IsCreable(admin);

            if (es.IsViewable != null)
                el.View = es.IsViewable(admin);

            el.Find = false; //depends on having a query
        }
    }

    public class EntitySettings
    {
        public string PartialViewName;
        public Func<bool, bool> IsCreable;
        public Func<bool, bool> IsViewable;

        public EntitySettings(bool isSimpleType)
        {
            if (isSimpleType)
            {
                IsCreable = admin => admin;
                IsViewable = admin => admin;
            }
            else
            {
                IsCreable = admin => true;
                IsViewable = admin => true;
            }
        }
    }

    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> Settings = new Dictionary<Type, EntitySettings>();

        internal string NormalPageUrl = "~/Plugin/Signum.Web.dll/Signum.Web.Views.NormalPage.aspx";
        
        internal Dictionary<string, Type> URLNamesToTypes { get; private set; }
        internal Dictionary<Type, string> TypesToURLNames { get; private set; }
        internal Dictionary<Type, TypeDN> TypesToTypesDN { get; private set; }
        internal Dictionary<string, Type> ModifiablesNamesToTypes { get; private set; }
        internal Dictionary<Type, Func<ModifiableEntity>> Constructors = new Dictionary<Type, Func<ModifiableEntity>>();

        public NavigationManager()
        {
            URLNamesToTypes = Schema.Current.Tables.Keys.ToDictionary(t =>
                    t.Name.EndsWith("DN") ? t.Name.Substring(0, t.Name.Length - 2) : t.Name);
            TypesToURLNames = Schema.Current.Tables.Keys.ToDictionary(
                t => t,
                e => e.Name.EndsWith("DN") ? e.Name.Substring(0, e.Name.Length - 2) : e.Name);

            InitializeTypesDN();
        }

        public NavigationManager(Dictionary<string, Type> customTypeURLNames)
        {
            URLNamesToTypes = customTypeURLNames;

            TypesToURLNames = new Dictionary<Type, string>();
            customTypeURLNames.ForEach(t => TypesToURLNames.Add(t.Value,t.Key));

            InitializeTypesDN();
        }

        public NavigationManager(Dictionary<string, Type> customTypeURLNames, Dictionary<Type, TypeDN> customTypesToTypeDN)
        {
            URLNamesToTypes = customTypeURLNames;

            TypesToURLNames = new Dictionary<Type, string>();
            customTypeURLNames.ForEach(t => TypesToURLNames.Add(t.Value, t.Key));

            TypesToTypesDN = customTypesToTypeDN;

            Navigator.NameToType = TypesToTypesDN.SelectDictionary(k => k.Name, (t, tdn) => t);
        }

        public virtual void InitializeModifiablesNamesToTypes()
        {
            ModifiablesNamesToTypes = new Dictionary<string, Type>();
            ModifiablesNamesToTypes
                .AddRange(Settings.Keys.Where(t => typeof(ModifiableEntity).IsAssignableFrom(t)),
                          k => k.Name,
                          v => v);
        }

        private void InitializeTypesDN()
        { 
            List<TypeDN> typesDN = Database.RetrieveAll<TypeDN>();

            TypesToTypesDN = TypeLogic.TypeToDN;
                //Schema.Current.Tables.Keys.ToDictionary(t => t, t => (Reflector.ExtractEnumProxy(t) ?? t).Name)
                //.JumpDictionary(typesDN.ToDictionary(td=>td.ClassName));

            Navigator.NameToType = Schema.Current.Tables.Keys.ToDictionary(t => t.Name, t => t); 
        }

        protected internal virtual ViewResult View(Controller controller, object obj)
        {
            EntitySettings es = Navigator.NavigationManager.Settings.TryGetC(obj.GetType()).ThrowIfNullC("No hay una vista asociada al tipo: " + obj.GetType());
            string urlName = Navigator.NavigationManager.TypesToURLNames.TryGetC(obj.GetType()).ThrowIfNullC("No hay un nombre asociado al tipo: " + obj.GetType());
            
            controller.ViewData[ViewDataKeys.MainControlUrl] = es.PartialViewName;
            IdentifiableEntity entity = (IdentifiableEntity)obj;
            controller.ViewData.Model = entity;
            controller.ViewData[ViewDataKeys.PageTitle] = entity.ToStr;

            return new ViewResult()
            {
                ViewName = NormalPageUrl,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult PartialView<T>(Controller controller, T entity, string prefix, string onOk, string onCancel)
        {
            EntitySettings es = Navigator.NavigationManager.Settings.TryGetC(entity.GetType()).ThrowIfNullC("No hay una vista asociada al tipo: " + entity.GetType());
            
            controller.ViewData[ViewDataKeys.MainControlUrl] = es.PartialViewName;
            controller.ViewData[ViewDataKeys.PopupPrefix] = prefix;
            controller.ViewData[ViewDataKeys.OnOk] = onOk;
            controller.ViewData[ViewDataKeys.OnCancel] = onCancel;
            controller.ViewData.Model = entity;
            
            return new PartialViewResult
            {
                ViewName = "~/Plugin/Signum.Web.dll/Signum.Web.Views.PopupControl.ascx",
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual Type ResolveTypeFromUrlName(string typeUrlName)
        {
            return Navigator.NavigationManager.URLNamesToTypes
                .TryGetC(typeUrlName)
                .ThrowIfNullC("No hay un tipo asociado al nombre: " + typeUrlName);
        }

        protected internal virtual Type ResolveType(string typeName)
        {
            Type type = null;
            if (Navigator.NameToType.ContainsKey(typeName))
                type = Navigator.NameToType[typeName];
            else
                type = Navigator.NavigationManager.ModifiablesNamesToTypes[typeName];
            
            if (type == null)
                throw new ArgumentException(Resource.Type0NotFoundInTheSchema);
            return type;
        }

        protected internal virtual Dictionary<string, List<string>> ApplyChangesAndValidate(SortedList<string, object> formValues, Modifiable obj, string prefix)
        {
            Modification modification = GenerateModification(formValues, obj, prefix ?? "");

            modification.ApplyChanges(obj);

            return GenerateErrors(obj, modification);
        }

        protected internal virtual Dictionary<string, List<string>> GenerateErrors(Modifiable obj, Modification modification)
        {
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
            modification.Validate(obj, errors);

            Dictionary<Modifiable, string> dicGlobalErrors = obj.FullIntegrityCheckDictionary();
            //Split each error in one entry in the HashTable:
            var globalErrors = dicGlobalErrors.SelectMany(a => a.Value.Lines()).ToList();
            //eliminar de globalErrors los que ya hemos metido en el diccionario
            errors.SelectMany(a => a.Value)
                  .ForEach(e => globalErrors.Remove(e));
            //meter el resto en el diccionario
            if (globalErrors.Count > 0)
                errors.Add(ViewDataKeys.GlobalErrors, globalErrors.ToList());

            return errors;
        }

        protected internal virtual Modification GenerateModification(SortedList<string, object> formValues, Modifiable obj, string prefix)
        {
            MinMax<int> interval = Modification.FindSubInterval(formValues, prefix);

            return Modification.Create(obj.GetType(), formValues, interval, prefix);
        }

        protected internal virtual IdentifiableEntity ExtractEntity(NameValueCollection form, string prefix)
        {
            string typeName = form[(prefix ?? "") + TypeContext.Separator + TypeContext.RuntimeType];
            string id = form[(prefix ?? "") + TypeContext.Separator + TypeContext.Id];

            Type type = Navigator.NameToType.GetOrThrow(typeName, Resource.Type0NotFoundInTheSchema);

            if (!string.IsNullOrEmpty(id))
                return Database.Retrieve(type, int.Parse(id));
            else
                return (IdentifiableEntity)Constructor.Construct(type);
        }
    }

    public static class ModelStateExtensions
    { 
        public static void FromDictionary(this ModelStateDictionary modelState, Dictionary<string, List<string>> errors, NameValueCollection form)
        {
            if (errors != null)
                errors.ForEach(p => p.Value.ForEach(v => modelState.AddModelError(p.Key, v, form[p.Key])));
        }

        public static string ToJsonData(this ModelStateDictionary modelState)
        {
            return modelState.ToJSonObjectBig(
                key => key.Quote(),
                value => value.Errors.ToJSonArray(me => me.ErrorMessage.Quote()));
        }

        //http://www.crankingoutcode.com/2009/02/01/IssuesWithAddModelErrorSetModelValueWithMVCRC1.aspx
        //Necesary to set model value if you add a model error, otherwise some htmlhelpers throw exception
        public static void AddModelError(this ModelStateDictionary modelState, string key, string errorMessage, string attemptedValue)
        {
            modelState.AddModelError(key, errorMessage);
            modelState.SetModelValue(key, new ValueProviderResult(attemptedValue, attemptedValue, null));
        }

    }
}
