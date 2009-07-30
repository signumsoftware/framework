using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Web
{
    public static class Utils
    {
        public static string Specify(string functionText)
        {
            if (string.IsNullOrEmpty(functionText))
                return functionText;

            string body = functionText.Trim();
            if (string.IsNullOrEmpty(body))
                return functionText;

            if (!body.StartsWith("function"))
                return functionText;
            body = body.Substring(8, body.Length - 8).Trim();
            if (string.IsNullOrEmpty(body))
                return functionText;

            if (!body.StartsWith("("))
                return functionText;
            body = body.Substring(1, body.Length - 1).Trim();
            if (string.IsNullOrEmpty(body))
                return functionText;

            if (!body.StartsWith(")"))
                return functionText;
            body = body.Substring(1, body.Length - 1).Trim();
            if (string.IsNullOrEmpty(body))
                return functionText;

            if (!body.StartsWith("{"))
                return functionText;
            body = body.Substring(1, body.Length - 1).Trim();
            if (string.IsNullOrEmpty(body))
                return functionText;

            if (!body.EndsWith("}"))
                return functionText;
            body = body.Substring(0, body.Length - 1).Trim();
            if (string.IsNullOrEmpty(body))
                return functionText;

            return body;
        }
    }
}
