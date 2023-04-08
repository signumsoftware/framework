using OpenQA.Selenium;

namespace Signum.React.Selenium;

public class ColumnEditorProxy
{
    public IWebElement Element;

    public ColumnEditorProxy(IWebElement element)
    {
        this.Element = element;
    }


    public void Close()
    {
        this.Element.FindElement(By.ClassName("button.close")).Click();
    }

    public QueryTokenBuilderProxy QueryToken => new QueryTokenBuilderProxy(this.Element.FindElement(By.ClassName("sf-query-token-builder")));
    public IWebElement Name => this.Element.FindElement(By.ClassName("input.form-control"));
}
