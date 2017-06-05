using OpenQA.Selenium;
using Signum.Entities;

namespace Signum.React.Selenium
{
    public class EntityTabRepeaterProxy : EntityRepeaterProxy
    {
        public EntityTabRepeaterProxy(IWebElement element, PropertyRoute route)
            : base(element, route)
        {
        }
    }
}
