using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using Signum.Utilities;

namespace Signum.Web
{
    public static class ModelStateExtensions
    {
        public static void FromContext(this ModelStateDictionary modelState, MappingContext context)
        {
            if (context.GlobalErrors.Count > 0)
                foreach (var p in context.GlobalErrors)
                    foreach (var v in p.Value)
                        modelState.AddModelError(p.Key, v, context.GlobalInputs.TryGetC(p.Key));
        }

        public static void FromDictionary(this ModelStateDictionary modelState, Dictionary<string, List<string>> errors, NameValueCollection form)
        {
            if (errors != null)
                foreach (var p in errors)
                    foreach (var v in p.Value)
                        modelState.AddModelError(p.Key, v, form[p.Key]);
        }

        public static string ToJsonData(this ModelStateDictionary modelState)
        {
            return modelState.ToJSonObject(
                key => key.Quote(),
                value => value.Errors.ToJSonArray(me => me.ErrorMessage.Quote()));
        }

        //http://www.crankingoutcode.com/2009/02/01/IssuesWithAddModelErrorSetModelValueWithMVCRC1.aspx
        //Necesary to set model value if you add a model error, otherwise some htmlhelpers throw exception
        public static void AddModelError(this ModelStateDictionary modelState, string key, string errorMessage, string attemptedValue)
        {
            modelState.AddModelError(key, errorMessage);
            modelState.SetModelValue(key, new ValueProviderResult(attemptedValue, attemptedValue, null));
        }

    }
}
