using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;
using Newtonsoft.Json;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Signum.Web
{
    public class MessageBoxOptions
    {
        public readonly string result = JsonResultType.messageBox.ToString();

        public string prefix;
        public string title = NormalWindowMessage.Message.NiceToString();
        public string message;
    }
}
