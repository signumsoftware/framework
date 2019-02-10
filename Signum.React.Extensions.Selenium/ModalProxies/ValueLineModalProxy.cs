using OpenQA.Selenium;

namespace Signum.React.Selenium
{
    public class ValueLineModalProxy : ModalProxy
    {
        public ValueLineModalProxy(IWebElement element) : base(element)
        {
        }

        public ValueLineProxy ValueLine
        {
            get
            {
                var formGroup = this.Element.FindElement(By.CssSelector("div.modal-body div.form-group"));
                return new ValueLineProxy(formGroup, null!);
            }
        }
    }
}
