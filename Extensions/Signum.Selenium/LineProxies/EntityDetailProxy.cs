using OpenQA.Selenium;

namespace Signum.Selenium;

public class EntityDetailProxy : EntityBaseProxy
{
    public EntityDetailProxy(IWebElement element, PropertyRoute route)
        : base(element, route)
    {
    }

    public override object? GetValueUntyped() => this.Lite;
    public override void SetValueUntyped(object? value) => this.Lite = value is Entity e ? e.ToLite() : (Lite<Entity>?)value;
    public override bool IsReadonly() => throw new NotImplementedException();

    public Lite<IEntity>? Lite
    {
        get { return EntityInfo()?.ToLite(); }
        set
        {
            if (value == null)
            {
                if (this.EntityInfo() != null)
                    this.Remove();
            }
            else
            {
                this.Find().SelectLite(value);
            }
        }
    }

    public LineContainer<T> Details<T>() where T : ModifiableEntity
    {
        var subRoute = Route.Type == typeof(T) ? Route : PropertyRoute.Root(typeof(T));

        return new LineContainer<T>(this.Element.FindElement(By.CssSelector("div[data-property-path]")), subRoute);
    }

    public EntityInfoProxy? EntityInfo()
    {
        return EntityInfoInternal(null);
    }

    public ILineContainer<T> GetOrCreateDetailControl<T>() where T : ModifiableEntity
    {
        if (this.EntityInfo() !=null)
            return this.Details<T>();

        CreateEmbedded<T>();

        return this.Details<T>();
    }
}
