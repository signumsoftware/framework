using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Selenium;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Signum.Web.Selenium
{
    public static class LinesTestExtensions
    {
        public static string EntityLineToStrSelector(string prefix)
        {
            return "jq=#{0}sfToStr".Formato(prefix);
        }

        public static string EntityLineLinkSelector(string prefix)
        {
            return "jq=#{0}sfLink".Formato(prefix);
        }

        public static void EntityLineHasValue(this ISelenium selenium, string prefix, bool hasValue)
        {
            if (hasValue)
            {
                Assert.IsFalse(selenium.IsElementPresent("{0}:visible".Formato(EntityLineToStrSelector(prefix))));
                Assert.IsTrue(selenium.IsElementPresent("{0}:visible".Formato(EntityLineLinkSelector(prefix))));
            }
            else
            {
                Assert.IsTrue(selenium.IsElementPresent("{0}:visible".Formato(EntityLineToStrSelector(prefix))));
                Assert.IsFalse(selenium.IsElementPresent("{0}:visible".Formato(EntityLineLinkSelector(prefix))));
            }
        }

        public static string EntityLineDetailDivSelector(string prefix)
        {
            return "jq=#{0}sfDetail".Formato(prefix);
        }

        public static void EntityLineDetailHasValue(this ISelenium selenium, string prefix, bool hasValue)
        {
            Assert.IsTrue(CheckEntityLineDetailHasValue(selenium, prefix, hasValue));
        }

        public static bool CheckEntityLineDetailHasValue(this ISelenium selenium, string prefix, bool hasValue)
        {
            return selenium.IsElementPresent("{0}:{1}".Formato(
                EntityLineDetailDivSelector(prefix),
                hasValue ? "parent" : "empty"));
        }

        public static void EntityListDetailHasValue(this ISelenium selenium, string prefix, bool hasValue)
        {
            EntityLineDetailHasValue(selenium, prefix, hasValue);
        }

        public static bool CheckEntityListDetailHasValue(this ISelenium selenium, string prefix, bool hasValue)
        {
            return CheckEntityLineDetailHasValue(selenium, prefix, hasValue);
        }

        public static void ListLineElementExists(this ISelenium selenium, string prefix, int elementIndexBase0, bool exists)
        {
            bool optionPresent = selenium.IsElementPresent("jq=#{0}".Formato(ListLineOptionId(prefix, elementIndexBase0)));
            bool runtimeInfoPresent = selenium.IsElementPresent(SeleniumExtensions.RuntimeInfoSelector(prefix, elementIndexBase0));

            if (exists)
                Assert.IsTrue(optionPresent && runtimeInfoPresent);
            else
            {
                Assert.IsFalse(optionPresent);
                Assert.IsFalse(runtimeInfoPresent);
            }
        }

        public static string RepeaterItemSelector(string prefix, int elementIndexBase0)
        {
            return "jq=#{0}sfItemsContainer > #{0}{1}_sfRepeaterItem".Formato(prefix, elementIndexBase0);
        }

        public static void RepeaterItemExists(this ISelenium selenium, string prefix, int elementIndexBase0, bool exists)
        {
            bool divPresent = selenium.IsElementPresent(RepeaterItemSelector(prefix, elementIndexBase0));
            bool runtimeInfoPresent = selenium.IsElementPresent(SeleniumExtensions.RuntimeInfoSelector(prefix, elementIndexBase0));

            if (exists)
                Assert.IsTrue(divPresent && runtimeInfoPresent);
            else
            {
                Assert.IsFalse(divPresent);
                Assert.IsFalse(runtimeInfoPresent);
            }
        }

        public static void RepeaterWaitUntilItemLoaded(this ISelenium selenium, string repeaterPrefix, int itemIndexBase0)
        {
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(RepeaterItemSelector(repeaterPrefix, itemIndexBase0)));
        }

        public static string LineCreateSelector(string prefix)
        {
            return "jq=#{0}btnCreate".Formato(prefix);
        }

        public static void LineCreate(this ISelenium selenium, string prefix, bool opensEntity)
        {
            LineCreate(selenium, prefix, opensEntity, -1);
        }

        /// <summary>
        /// To be used for EntityList
        /// </summary>
        /// <param name="selenium"></param>
        /// <param name="elementIndexBase0"></param>
        /// <param name="prefix"></param>
        public static void LineCreate(this ISelenium selenium, string prefix, bool opensEntity, int elementIndexBase0)
        {
            selenium.Click(LineCreateSelector(prefix));

            if (opensEntity)
            {
                if (elementIndexBase0 != -1)
                    prefix += elementIndexBase0 + "_";
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(prefix)));
            }
        }

        public static void LineCreateWithImpl(this ISelenium selenium, string prefix, bool opensEntity, string typeToChoose)
        {
            LineCreateWithImpl(selenium, prefix, opensEntity, typeToChoose, -1);
        }

        /// <summary>
        /// To be used for EntityList
        /// </summary>
        /// <param name="selenium"></param>
        /// <param name="elementIndexBase0"></param>
        /// <param name="prefix"></param>
        public static void LineCreateWithImpl(this ISelenium selenium, string prefix, bool opensEntity, string typeToChoose, int elementIndexBase0)
        {
            selenium.Click(LineCreateSelector(prefix));

            //implementation popup opens
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(prefix)));
            selenium.Click(typeToChoose);
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0} .sf-chooser-button".Formato(SeleniumExtensions.PopupSelector(prefix))));

            if (opensEntity)
            {
                //entity popup opens
                if (elementIndexBase0 != -1)
                    prefix += elementIndexBase0 + "_";
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(prefix)));
            }
        }

        public static string LineViewSelector(string prefix)
        {
            return "jq=#{0}btnView".Formato(prefix);
        }

        public static void LineView(this ISelenium selenium, string prefix)
        {
            selenium.Click(LineViewSelector(prefix));
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(SeleniumExtensions.PopupSelector(prefix))));
        }

        public static string ListLineSelector(string prefix)
        {
            return "jq=#" + prefix;
        }

        public static string ListLineOptionId(string prefix, int itemIndexBase0)
        {
            return "{0}{1}_sfToStr".Formato(prefix, itemIndexBase0);
        }

        public static void ListLineSelectElement(this ISelenium selenium, string prefix, int itemIndexBase0, bool multiSelect)
        {
            if (multiSelect)
                selenium.AddSelection(ListLineSelector(prefix).RemoveRight(1), "id={0}".Formato(ListLineOptionId(prefix, itemIndexBase0)));
            else
                selenium.Select(ListLineSelector(prefix).RemoveRight(1), "id={0}".Formato(ListLineOptionId(prefix, itemIndexBase0)));
        }

        public static void ListLineViewElement(this ISelenium selenium, string prefix, int itemIndexBase0, bool opensEntity)
        {
            ListLineSelectElement(selenium, prefix, itemIndexBase0, false);
            if (opensEntity)
            {
                selenium.Click(ListLineOptionId(prefix, itemIndexBase0));
                selenium.Click(LineViewSelector(prefix));
                selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:visible".Formato(SeleniumExtensions.PopupSelector(prefix + itemIndexBase0 + "_"))));
            }
            else
            {
                selenium.DoubleClick(ListLineOptionId(prefix, itemIndexBase0));
            }
        }

        public static string LineFindSelector(string prefix)
        {
            return "jq=#{0}btnFind".Formato(prefix);
        }

        public static void LineFind(this ISelenium selenium, string prefix)
        {
            LineFind(selenium, prefix, -1);
        }

        /// <summary>
        /// To be used for list lines
        /// </summary>
        /// <param name="selenium"></param>
        /// <param name="elementIndexBase0"></param>
        /// <param name="prefix"></param>
        public static void LineFind(this ISelenium selenium, string prefix, int elementIndexBase0)
        {
            selenium.Click(LineFindSelector(prefix));

            if (elementIndexBase0 != -1)
                prefix += elementIndexBase0 + "_";
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(prefix)));
        }

        public static void LineFindWithImpl(this ISelenium selenium, string prefix, string typeToChoose)
        {
            LineFindWithImpl(selenium, prefix, typeToChoose, -1);
        }

        /// <summary>
        /// To be used for EntityList
        /// </summary>
        /// <param name="selenium"></param>
        /// <param name="elementIndexBase0"></param>
        /// <param name="prefix"></param>
        public static void LineFindWithImpl(this ISelenium selenium, string prefix, string typeToChoose, int elementIndexBase0)
        {
            selenium.Click(LineFindSelector(prefix));

            //implementation popup opens
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(prefix)));
            selenium.Click(typeToChoose);
            selenium.WaitAjaxFinished(() => !selenium.IsElementPresent("{0} .sf-chooser-button".Formato(SeleniumExtensions.PopupSelector(prefix))));

            //search popup opens
            if (elementIndexBase0 != -1)
                prefix += elementIndexBase0 + "_";
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent(SeleniumExtensions.PopupSelector(prefix)));
        }

        public static void LineFindAndSelectElements(this ISelenium selenium, string prefix, bool allowMultiple, int[] rowIndexesBase0)
        {
            LineFindAndSelectElements(selenium, prefix, allowMultiple, rowIndexesBase0, -1);
        }

        public static void LineFindAndSelectElements(this ISelenium selenium, string prefix, bool allowMultiple, int[] rowIndexesBase0, int elementIndexBase0)
        {
            selenium.LineFind(prefix, elementIndexBase0);

            if (elementIndexBase0 != -1)
                prefix += elementIndexBase0 + "_";

            selenium.Search(prefix);

            if (allowMultiple)
            {
                foreach (int index in rowIndexesBase0)
                    SearchTestExtensions.SelectRowCheckbox(selenium, index, prefix);
            }
            else
                SearchTestExtensions.SelectRowRadioButton(selenium, rowIndexesBase0[0], prefix);

            selenium.PopupOk(prefix);
        }

        public static void LineFindWithImplAndSelectElements(this ISelenium selenium, string prefix, string typeToChoose, bool allowMultiple, int[] rowIndexesBase0)
        {
            LineFindWithImplAndSelectElements(selenium, prefix, typeToChoose, allowMultiple, rowIndexesBase0, -1);
        }

        public static void LineFindWithImplAndSelectElements(this ISelenium selenium, string prefix, string typeToChoose, bool allowMultiple, int[] rowIndexesBase0, int elementIndexBase0)
        {
            selenium.LineFindWithImpl(prefix, typeToChoose, elementIndexBase0);

            if (elementIndexBase0 != -1)
                prefix += elementIndexBase0 + "_";

            selenium.Search(prefix);

            if (allowMultiple)
            {
                foreach (int index in rowIndexesBase0)
                    SearchTestExtensions.SelectRowCheckbox(selenium, index, prefix);
            }
            else
                SearchTestExtensions.SelectRowRadioButton(selenium, rowIndexesBase0[0], prefix);

            selenium.PopupOk(prefix);
        }



        public static string LineRemoveSelector(string prefix)
        {
            return "jq=#{0}btnRemove".Formato(prefix);
        }

        public static void LineRemove(this ISelenium selenium, string prefix)
        {
            selenium.Click(LineRemoveSelector(prefix));
        }
    }
}
