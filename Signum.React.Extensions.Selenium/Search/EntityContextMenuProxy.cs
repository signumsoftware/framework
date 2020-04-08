using OpenQA.Selenium;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Utilities;
using System;
using System.Collections.Generic;

namespace Signum.React.Selenium
{
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

        public void ExecuteClick(IOperationSymbolContainer symbolContainer, bool consumeConfirmation = false, bool shouldDisapear = false)
        {
            var lites = ResultTable.SelectedEntities();

            Operation(symbolContainer).WaitVisible().Click();
            if (consumeConfirmation)
                this.ResultTable.Selenium.ConsumeAlert();

            if (shouldDisapear)
                ResultTable.WaitNoVisible(lites);
            else
                ResultTable.WaitSuccess(lites);
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

        public FrameModalProxy<T> OperationClickPopup<T>(IOperationSymbolContainer symbolContainer)
            where T : Entity
        {
            var popup = Operation(symbolContainer).WaitVisible().CaptureOnClick();
            return new FrameModalProxy<T>(popup);
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
}
