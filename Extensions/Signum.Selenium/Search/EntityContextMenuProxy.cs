using OpenQA.Selenium;
using Signum.Processes;

namespace Signum.React.Selenium;

public class EntityContextMenuProxy
{
    ResultTableProxy ResultTable;
    public IWebElement Element { get; private set; }
    public EntityContextMenuProxy(ResultTableProxy resultTable, IWebElement element)
    {
        this.ResultTable = resultTable;
        this.Element = element;
    }


    public WebElementLocator QuickLink(string name)
    {
        return this.Element.WithLocator(By.CssSelector("a[data-name='{0}']".FormatWith(name)));
    }

    public SearchModalProxy QuickLinkClickSearch(string name)
    {
        var a = QuickLink(name).WaitPresent();
        var popup = a.CaptureOnClick();
        return new SearchModalProxy(popup);
    }

    public void ExecuteClick<T>(ExecuteSymbol<T> executeSymbol, bool consumeConfirmation = false, bool shouldDisapear = false)
        where T : Entity
    {
        var lites = ResultTable.SelectedEntities();

        Operation(executeSymbol).WaitVisible().Click();
        if (consumeConfirmation)
            this.ResultTable.Selenium.ConsumeAlert();

        if (shouldDisapear)
            ResultTable.WaitNoVisible(lites);
        else
            ResultTable.WaitSuccess(lites);
    }

    public FrameModalProxy<T> ConstructFrom<F, T>(ConstructSymbol<T>.From<F> constructSymbol, bool shouldDisapear = false)
        where F : Entity
        where T : Entity
    {
        var lites = ResultTable.SelectedEntities();

        var modal = Operation(constructSymbol).WaitVisible().CaptureOnClick();

        var result = new FrameModalProxy<T>(modal);
        result.Disposing += okPressed =>
        {
            if (shouldDisapear)
                ResultTable.WaitNoVisible(lites);
            else
                ResultTable.WaitSuccess(lites);
        };

        return result;
    }

    public void DeleteClick(IOperationSymbolContainer symbolContainer, bool consumeConfirmation = true, bool shouldDisapear = true)
    {
        var lites = ResultTable.SelectedEntities();

        Operation(symbolContainer).WaitVisible().Click();
        if (consumeConfirmation)
            ResultTable.Selenium.ConsumeAlert();

        if (shouldDisapear)
            ResultTable.WaitNoVisible(lites);
        else
            ResultTable.WaitSuccess(lites);
    }


    public FrameModalProxy<T> ConstructFromMany<F, T>(ConstructSymbol<T>.FromMany<F> symbolContainer, bool shouldDisapear = true, Action<List<Lite<IEntity>>, ResultTableProxy>? customCheck = null)
        where F : Entity
        where T : Entity
    {
        var lites = ResultTable.SelectedEntities();

        var modal = Operation(symbolContainer).WaitVisible().CaptureOnClick();

        return new FrameModalProxy<T>(modal)
        {
            Disposing = a =>
            {
                if (customCheck != null)
                    customCheck(lites, ResultTable);
                else if (shouldDisapear)
                    ResultTable.WaitNoVisible(lites);
                else
                    ResultTable.WaitSuccess(lites);
            }
        };
    }

    public FrameModalProxy<ProcessEntity> DeleteProcessClick(IOperationSymbolContainer operationSymbol)
    {
        Operation(operationSymbol).WaitVisible();

        var popup = this.Element.GetDriver().CapturePopup(() =>
        ResultTable.Selenium.ConsumeAlert());

        return new FrameModalProxy<ProcessEntity>(popup).WaitLoaded();
    }

    public WebElementLocator Operation(IOperationSymbolContainer symbolContainer)
    {
        return this.Element.WithLocator(By.CssSelector("a[data-operation=\'{0}']".FormatWith(symbolContainer.Symbol.Key)));
    }

    public bool OperationIsDisabled(IOperationSymbolContainer symbolContainer)
    {
        return Operation(symbolContainer).WaitVisible().GetAttribute("disabled").HasText();
    }

    public IWebElement OperationClickCapture(IOperationSymbolContainer symbolContainer)
    {
        var popup = Operation(symbolContainer).WaitVisible().CaptureOnClick();
        return popup;
    }

    private FramePageProxy<T> MenuClickNormalPage<T>(IOperationSymbolContainer contanier) where T : Entity
    {
        OperationIsDisabled(contanier);
        var result = new FramePageProxy<T>(this.ResultTable.Selenium);
        return result;
    }

    public void WaitNotLoading()
    {
        this.Element.WaitElementNotPresent(By.CssSelector("li.sf-tm-selected-loading"));
    }
}
