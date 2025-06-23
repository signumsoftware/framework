using OpenQA.Selenium;

namespace Signum.Selenium;


public abstract class FilterProxy { }
public class FilterGroupProxy : FilterProxy
{
    public IWebElement Element;
    readonly object queryName;

    public FilterGroupProxy(IWebElement element, object queryName)
    {
        this.Element = element;
        this.queryName = queryName;
    }
}

public class FilterConditionProxy : FilterProxy
{
    public IWebElement Element;
    readonly object QueryName;

    public FilterConditionProxy(IWebElement element, object queryName)
    {
        this.Element = element;
        this.QueryName = queryName;
    }

    public WebElementLocator DeleteButton
    {
        get { return this.Element.WithLocator(By.CssSelector(".sf-line-button.sf-remove")); }
    }

    public QueryTokenBuilderProxy QueryToken
    {
        get { return new QueryTokenBuilderProxy(this.Element.FindElement(By.ClassName("sf-query-token-builder"))); }
    }

    public WebElementLocator OperationElement
    {
        get { return this.Element.WithLocator(By.CssSelector("td.sf-filter-operation select")); }
    }

    public WebElementLocator ValueElement
    {
        get { return this.Element.WithLocator(By.CssSelector("td.sf-filter-value *")); }
    }

    public FilterOperation Operation
    {
        get { return OperationElement.Find().SelectElement().SelectedOption.GetDomProperty("value")!.ToEnum<FilterOperation>(); }
        set { OperationElement.Find().SelectElement().SelectByValue(value.ToString()); }
    }

    public void Delete()
    {
        DeleteButton.Find().Click();
    }

    public EntityLineProxy EntityLine()
    {
        return new EntityLineProxy(this.ValueElement.Find(), null!);
    }

    internal void SetValue(object? value)
    {
        var qt = QueryUtils.Parse(this.QueryToken.FullKey!, QueryLogic.Queries.QueryDescription(this.QueryName), SubTokensOptions.CanElement | SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll);

        var al = BaseLineProxy.AutoLine(this.ValueElement.Find(), qt.GetPropertyRoute()!);

        if (value is PrimaryKey pk)
            al.SetValueUntyped(pk.Object);
        else
            al.SetValueUntyped(value);
    }
}
