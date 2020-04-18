using System;
using System.Linq;
using OpenQA.Selenium;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public abstract class EntityBaseProxy : BaseLineProxy
    {
        public EntityBaseProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }

        public virtual PropertyRoute ItemRoute => this.Route;

        public WebElementLocator CreateButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-create")); }
        }



        protected void CreateEmbedded<T>()
        {
            WaitChanges(() =>
            {
                var imp = this.ItemRoute.TryGetImplementations();
                if (imp != null && imp.Value.Types.Count() != 1)
                {
                    var popup = this.CreateButton.Find().CaptureOnClick();
                    ChooseType(typeof(T), popup);
                }
                else
                {
                    this.CreateButton.Find().Click();
                }
            }, "create clicked");
        }

        public FrameModalProxy<T> CreateModal<T>() where T : ModifiableEntity
        {

            string changes = GetChanges();

            var popup = this.CreateButton.WaitVisible().CaptureOnClick();

            popup = ChooseTypeCapture(typeof(T), popup);

            var itemRoute = this.ItemRoute.Type == typeof(T) ? this.ItemRoute : PropertyRoute.Root(typeof(T));

            return new FrameModalProxy<T>(popup, itemRoute)
            {
                Disposing = okPressed => { WaitNewChanges(changes, "create dialog closed"); }
            };
        }

        public WebElementLocator ViewButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-view")); }
        }

        protected FrameModalProxy<T> ViewInternal<T>() where T : ModifiableEntity
        {
            var newElement = this.ViewButton.Find().CaptureOnClick();
            string changes = GetChanges();

            return new FrameModalProxy<T>(newElement, this.ItemRoute)
            {
                Disposing = okPressed => WaitNewChanges(changes, "create dialog closed")
            };
        }

        public WebElementLocator FindButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-find")); }
        }

        public WebElementLocator RemoveButton
        {
            get { return this.Element.WithLocator(By.CssSelector("a.sf-remove")); }
        }

        public void Remove()
        {
            WaitChanges(() => this.RemoveButton.Find().Click(), "removing");
        }

        public SearchModalProxy Find(Type? selectType = null)
        {
            string changes = GetChanges();
            var popup = FindButton.Find().CaptureOnClick();

            popup = ChooseTypeCapture(selectType, popup);

            return new SearchModalProxy(popup)
            {
                Disposing = okPressed => { WaitNewChanges(changes, "create dialog closed"); }
            };
        }

        private void ChooseType(Type selectType, IWebElement element)
        {
            if (!SelectorModalProxy.IsSelector(element))
                return;

            if (selectType == null)
                throw new InvalidOperationException("No type to choose from selected");

            SelectorModalProxy.Select(this.Element, TypeLogic.GetCleanName(selectType));
        }

        private IWebElement ChooseTypeCapture(Type? selectType, IWebElement element)
        {
            if (!SelectorModalProxy.IsSelector(element))
                return element;

            if (selectType == null)
                throw new InvalidOperationException("No type to choose from selected");

            var newElement = element.GetDriver().CapturePopup(() =>
                SelectorModalProxy.Select(this.Element, TypeLogic.GetCleanName(selectType!)));

            return newElement;
        }

        public void WaitChanges(Action action, string actionDescription)
        {
            var changes = GetChanges();

            action();

            WaitNewChanges(changes, actionDescription);
        }

        public void WaitNewChanges(string changes, string actionDescription)
        {
            Element.GetDriver().Wait(() => GetChanges() != changes, () => "Waiting for changes after {0} in {1}".FormatWith(actionDescription, this.Route.ToString()));
        }

        public string GetChanges()
        {
            return this.Element.GetAttribute("data-changes");
        }

        public void WaitEntityInfoChanges(Action action, string actionDescription, int? index = null)
        {
            var entityInfo = EntityInfoString(index);

            action();

            Element.GetDriver().Wait(() => entityInfo != EntityInfoString(index), 
                () => "Waiting for entity info changes after {0} in {1}".FormatWith(actionDescription, this.Route.ToString()));
        }

        protected string EntityInfoString(int? index)
        {
            var element = index == null ? Element :
                this.Element.FindElements(By.CssSelector("[data-entity]")).ElementAt(index.Value);

            return element.GetAttribute("data-entity");
        }

        protected EntityInfoProxy? EntityInfoInternal(int? index) => EntityInfoProxy.Parse(EntityInfoString(index));

        public void AutoCompleteWaitChanges(IWebElement input, IWebElement container, Lite<IEntity> lite)
        {
            WaitChanges(() =>
            {
                AutoCompleteBasic(input, container, lite);

            }, "autocomplete selection");
        }

        public static void AutoCompleteBasic(IWebElement input, IWebElement container, Lite<IEntity> lite)
        {
            input.SafeSendKeys("id:" + lite.Id.ToString());

            var list = container.WaitElementVisible(By.CssSelector(".typeahead.dropdown-menu"));
            IWebElement itemElement = list.FindElement(By.CssSelector("[data-entity-key='{0}']".FormatWith(lite.Key())));

            itemElement.Click();
        }
    }


    public class EntityInfoProxy
    {
        public bool IsNew { get; set; }
        public string TypeName { get; set; }

        public Type EntityType;
        public PrimaryKey? IdOrNull { get; set; }

        public EntityInfoProxy(string dataEntity)
        {
            var parts = dataEntity.Split(';');

            var typeName = parts[0];
            var id = parts[1];
            var isNew = parts[2];

            var type = TypeLogic.GetType(typeName);

            this.TypeName = typeName;
            this.EntityType = type;
            this.IdOrNull = id.HasText() ? PrimaryKey.Parse(id, type) : (PrimaryKey?)null;
            this.IsNew = isNew.HasText() && bool.Parse(isNew);
        }


        public Lite<Entity> ToLite(string? toString = null)
        {
            return Lite.Create(this.EntityType, this.IdOrNull!.Value, toString);
        }

        public static EntityInfoProxy? Parse(string dataEntity)
        {
            if (dataEntity == "null" || dataEntity == "undefined")
                return null;

            return new EntityInfoProxy(dataEntity);
        }
    }
}
