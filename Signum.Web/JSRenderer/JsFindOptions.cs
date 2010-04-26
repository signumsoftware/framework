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
        public JsValue<string> Prefix { get; set; }
        public FindOptions FindOptions { get; set; }
        public JsValue<int?> Top { get; set; }
        /// <summary>
        /// To be called to open a Find window
        /// </summary>
        public JsValue<string> NavigatorControllerUrl { get; set; }
        /// <summary>
        /// To be called when clicking the search button
        /// </summary>
        public JsValue<string> SearchControllerUrl { get; set; }
        /// <summary>
        /// To be called when closing the popup (if exists) with the Ok button
        /// </summary>
        public JsFunction OnOk { get; set; }
        public JsFunction OnCancelled { get; set; }
        public JsValue<bool?> Async { get; set; }

        public JsFindOptions()
        {
            Renderer = () =>
            {
                JsOptionsBuilder options = new JsOptionsBuilder(true)
                {
                    {"prefix", Prefix.TryCC(t=>t.ToJS())},
                    {"top", Top.TryCC(t=>t.ToJS()) },
                    {"navigatorControllerUrl", NavigatorControllerUrl.TryCC(t=>t.ToJS())},
                    {"searchControllerUrl",SearchControllerUrl.TryCC(t=>t.ToJS())},
                    {"onOk",OnOk.TryCC(t=>t.ToJS())},
                    {"onCancelled", OnCancelled.TryCC(t=>t.ToJS())},
                    {"async", Async.TryCC(t=>t.ToJS())},
                };

                if (FindOptions != null)
                    FindOptions.Fill(options);

                return options.ToJS();
            };
        }
    }

    public class JsFindNavigator : JsInstruction
    {
        public JsFindNavigator(Func<string> renderer) : base(renderer)
        { 
        }

        public static JsFindNavigator JsOpenFinder(JsFindOptions options)
        {
            return new JsFindNavigator(() =>
                "OpenFinder({0})".Formato(options.ToJS()));
        }

        public static JsFindNavigator JsSelectedItems(JsFindOptions options)
        {
            return new JsFindNavigator(() =>
                "SelectedItems({0})".Formato(options.ToJS()));
        }

        public static JsInstruction HasSelectedItems(JsValue<string> prefix, JsFunction onSuccess)
        {
            return HasSelectedItems(new JsFindOptions { Prefix = prefix }, onSuccess);
        }

        public static JsInstruction HasSelectedItems(JsFindOptions options, JsFunction onSuccess)
        {
            return new JsInstruction(() => "HasSelectedItems({0},{1})".Formato(options.ToJS(), onSuccess.ToJS()));
        }

        public static JsFindNavigator JsSplitSelectedIds(JsFindOptions options)
        {
            return new JsFindNavigator(() =>
                "SplitSelectedIds({0})".Formato(options.ToJS()));
        }
    }
}
