using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web.Mvc;

namespace Signum.Web
{
    public class JsFindOptions : JsRenderer
    {
        public string Prefix { get; set; }
        public FindOptions FindOptions { get; set; }
        public int? Top { get; set; }
        /// <summary>
        /// To be called to open a Find window
        /// </summary>
        public string NavigatorControllerUrl { get; set; }
        /// <summary>
        /// To be called when clicking the search button
        /// </summary>
        public string SearchControllerUrl { get; set; }
        /// <summary>
        /// To be called when closing the popup (if exists) with the Ok button
        /// </summary>
        public string OnOk { get; set; }
        public string OnCancelled { get; set; }
        public bool? Async { get; set; }

        public JsFindOptions()
        {
            renderer = () =>
            {
                JsOptionsBuilder options = new JsOptionsBuilder(true)
                {
                    {"prefix", Prefix.TrySingleQuote()},
                    {"top", Top.TryToString() },
                    {"navigatorControllerUrl", NavigatorControllerUrl.TrySingleQuote()},
                    {"searchControllerUrl",SearchControllerUrl.TrySingleQuote()},
                    {"onOk",OnOk},
                    {"onCancelled", OnCancelled},
                    {"async", Async == true ? "true": null},
                };

                if (FindOptions != null)
                    FindOptions.Fill(options);

                return options.ToJS();
            };
        }
    }

    public class JsFindNavigator : JsRenderer
    {
        public JsFindNavigator(Func<string> renderer) : base(renderer)
        { 
        }

        public static JsFindNavigator JsSelectedItems(JsFindOptions options)
        {
            return new JsFindNavigator(() =>
                "SelectedItems({0})".Formato(options.ToJS()));
        }

        public static JsFindNavigator JsSplitSelectedIds(JsFindOptions options)
        {
            return new JsFindNavigator(() =>
                "SplitSelectedIds({0})".Formato(options.ToJS()));
        }
    }
}
