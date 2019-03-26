using System;
using OpenQA.Selenium;
using Signum.Engine.Basics;
using Signum.Utilities;

namespace Signum.React.Selenium
{
    public class SelectorModalProxy : ModalProxy
    {
        public SelectorModalProxy(IWebElement element) : base(element) { }

        public void Select(string value)
        {
            Select(this.Element, value);
            this.WaitNotVisible();
        }

        public void Select(Enum enumValue)
        {
            Select(this.Element, enumValue.ToString());
            this.WaitNotVisible();
        }

        public void Select<T>()
        {
            Select(this.Element, TypeLogic.GetCleanName(typeof(T)));
            this.WaitNotVisible();
        }

        public static bool IsSelector(IWebElement element)
        {
            return element.IsElementPresent(By.CssSelector(".sf-selector-modal"));
        }

        public static void Select(IWebElement element, Type type)
        {
            Select(element, TypeLogic.GetCleanName(type));
        }

        public static void Select(IWebElement element, string name)
        {
            element.FindElement(By.CssSelector("button[name={0}]".FormatWith(name))).Click();
        }
    }
}
