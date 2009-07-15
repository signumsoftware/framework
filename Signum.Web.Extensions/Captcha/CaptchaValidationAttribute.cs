/** 
 * Copyright (C) 2007-2008 Nicholas Berardi, Managed Fusion, LLC (nick@managedfusion.com)
 * 
 * <author>Nicholas Berardi</author>
 * <author_email>nick@managedfusion.com</author_email>
 * <company>Managed Fusion, LLC</company>
 * <product>Url Rewriter and Reverse Proxy</product>
 * <license>Microsoft Public License (Ms-PL)</license>
 * <agreement>
 * This software, as defined above in <product />, is copyrighted by the <author /> and the <company /> 
 * and is licensed for use under <license />, all defined above.
 * 
 * This copyright notice may not be removed and if this <product /> or any parts of it are used any other
 * packaged software, attribution needs to be given to the author, <author />.  This can be in the form of a textual
 * message at program startup or in documentation (online or textual) provided with the packaged software.
 * </agreement>
 * <product_url>http://www.managedfusion.com/products/url-rewriter/</product_url>
 * <license_url>http://www.managedfusion.com/products/url-rewriter/license.aspx</license_url>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web.Captcha
{
	/// <summary>
	/// 
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class CaptchaValidationAttribute : ActionFilterAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CaptchaCheckAttribute"/> class.
		/// </summary>
		public CaptchaValidationAttribute() 
			: this("captcha") { }

		/// <summary>
		/// Initializes a new instance of the <see cref="CaptchaCheckAttribute"/> class.
		/// </summary>
		/// <param name="field">The field.</param>
		public CaptchaValidationAttribute(string field)
		{
			Field = field;
		}

		/// <summary>
		/// Gets or sets the field.
		/// </summary>
		/// <value>The field.</value>
		public string Field { get; private set; }

        /// <summary>
		/// Called when [action executed].
		/// </summary>
		/// <param name="filterContext">The filter filterContext.</param>
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
            //skip if captcha is disabled in App Settings
            if (!AppSettings.ReadBoolean(AppSettingsKeys.UseCaptcha, true))
            {
                filterContext.ActionParameters["captchaValid"] = true;
                return;
            }

			// make sure no values are getting sent in from the outside
			if (filterContext.ActionParameters.ContainsKey("captchaValid"))
				filterContext.ActionParameters["captchaValid"] = null;

			// get the guid from the post back
			string guid = filterContext.HttpContext.Request.Form["captcha-guid"];

			// check for the guid because it is required from the rest of the opperation
			if (String.IsNullOrEmpty(guid))
			{
				filterContext.RouteData.Values.Add("captchaValid", false);
				return;
			}

			// get values
			CaptchaImage image = CaptchaImage.GetCachedCaptcha(guid);
			string actualValue = filterContext.HttpContext.Request.Form[Field];
			string expectedValue = image == null ? String.Empty : image.Text;

			// removes the captch from cache so it cannot be used again
			filterContext.HttpContext.Cache.Remove(guid);

			// validate the captch
			filterContext.ActionParameters["captchaValid"] =
				!String.IsNullOrEmpty(actualValue)
				&& !String.IsNullOrEmpty(expectedValue)
				&& String.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase);
		}
	}
}
