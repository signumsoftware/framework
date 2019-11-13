using System;
using OpenQA.Selenium;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.React.Selenium
{
    public class ValueLineProxy : BaseLineProxy
    {
        public ValueLineProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }


        public void SetStringValue(string? value)
        {
            IWebElement checkBox = this.Element.TryFindElement(By.CssSelector("input[type=checkbox]"));
            if (checkBox != null)
            {
                checkBox.SetChecked(bool.Parse(value!));
                return;
            }

            IWebElement dateTimePicker = this.Element.TryFindElement(By.CssSelector("div.rw-datetime-picker input[type=text]"));
            if(dateTimePicker != null)
            {
                dateTimePicker.SafeSendKeys(value);
                dateTimePicker.SendKeys(Keys.Tab);

//                var js = this.Element.GetDriver() as IJavaScriptExecutor;

//                var script =
//$@"arguments[0].value = '{value}';
//arguments[0].dispatchEvent(new Event('input', {{ bubbles: true }}));
//arguments[0].dispatchEvent(new Event('blur'));";

//                js.ExecuteScript(script, dateTimePicker);


                return;
            }

            IWebElement textOrTextArea = this.Element.TryFindElement(By.CssSelector(" input[type=text], textarea"));
            if (textOrTextArea != null)
            {
                textOrTextArea.SafeSendKeys(value);
                return;
            }

            IWebElement select = this.Element.TryFindElement(By.CssSelector("select"));
            if (select != null)
            {
                select.SelectElement().SelectByValue(value);
                return;
            }

            throw new InvalidOperationException("No ValueLine input element for  {0} found".FormatWith(Route));
        }

        private string GetStringValue()
        {
            IWebElement checkBox = this.Element.TryFindElement(By.CssSelector("input[type=checkbox]"));
            if (checkBox != null)
                return checkBox.Selected.ToString();

            IWebElement textOrTextArea = this.Element.TryFindElement(By.CssSelector("input[type=text], textarea"));
            if (textOrTextArea != null)
            {
                return textOrTextArea.GetAttribute("data-value")  ?? textOrTextArea.GetAttribute("value");
            }

            IWebElement select = this.Element.TryFindElement(By.CssSelector("select"));
            if (select != null)
                return select.SelectElement().SelectedOption.GetAttribute("value").ToString();

            IWebElement readonlyField =
                this.Element.TryFindElement(By.CssSelector("input.form-control")) ??
                this.Element.TryFindElement(By.CssSelector("div.form-control")) ??
                this.Element.TryFindElement(By.CssSelector("input.form-control-plaintext")) ??
                this.Element.TryFindElement(By.CssSelector("div.form-control-plaintext"));

            if (readonlyField != null)
                return readonlyField.GetAttribute("data-value") ?? readonlyField.GetAttribute("value") ?? readonlyField.Text;

            throw new InvalidOperationException("Element {0} not found".FormatWith(Route.PropertyString()));
        }

        public bool IsReadonly()
        {
            return Element.TryFindElement(By.CssSelector(".form-control-plaintext")) != null ||
                Element.TryFindElement(By.CssSelector(".form-control.readonly")) != null ||
                Element.TryFindElement(By.CssSelector(".form-control[readonly]")) != null;
        }

        public bool IsDisabled()
        {
            return this.EditableElement.WaitVisible().GetAttribute("disabled") == "true";
        }


        public WebElementLocator EditableElement
        {
            get { return this.Element.WithLocator(By.CssSelector("input, textarea, select")); }
        }


        public object? GetValue()
        {
            return this.GetValue(Route.Type);
        }

        public object? GetValue(Type type)
        {
            return ReflectionTools.Parse(GetStringValue(), type);
        }

        public T GetValue<T>()
        {
            return ReflectionTools.Parse<T>(GetStringValue());
        }

        public void SetValue(object? value)
        {
            var format = Reflector.FormatString(Route);
            this.SetValue(value, format);
        }

        public void SetValue(object? value, string? format)
        {
            var str = value == null ? null :
                    value is IFormattable ? ((IFormattable)value).ToString(format, null) :
                    value.ToString();

            SetStringValue(str);
        }
    }



}
