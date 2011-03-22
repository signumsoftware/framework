using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Extensions.Properties;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.SMS
{

    public enum SMSTemplateState
    {
        Disabled,
        Enabled
    }

    public enum SMSTemplateOperations
    { 
        Create,
        Modify,
        Disable,
        Enable
    }

    [Serializable]
    public class SMSTemplateDN : Entity
    {
        string name;
        public string Name
        {
            get { return name; }
            set { Set(ref name, value, () => Name); }
        }

        string message;
        [StringLengthValidator(AllowNulls = false, Max = SMSCharacters.SMSMaxTextLength)]
        public string Message
        {
            get { return message; }
            set { Set(ref message, value, () => Message); }
        }

        string from;
        [StringLengthValidator(AllowNulls = false)]
        public string From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        SMSTemplateState state = SMSTemplateState.Disabled;
        public SMSTemplateState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        DateTime startDate = DateTime.Now;
        [MinutesPrecissionValidator]
        public DateTime StartDate
        {
            get { return startDate; }
            set { Set(ref startDate, value, () => StartDate); }
        }

        DateTime? endDate;
        [MinutesPrecissionValidator]
        public DateTime? EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value, () => EndDate); }
        }

        static Expression<Func<SMSTemplateDN, bool>> ActiveExpression =
            (mt) => mt.State == SMSTemplateState.Enabled && DateTime.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool Active()
        { 
            return ActiveExpression.Invoke(this);
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => StartDate) || pi.Is(() => EndDate))
            {
                if (EndDate != null && EndDate >= StartDate)
                    return Resources.EndDateMustBeHigherThanStartDate;
            }

            return base.PropertyValidation(pi);
        }
    }


    public static class SMSCharacters
    {
        public static Dictionary<char, int> NormalCharacters = new Dictionary<char, int>();
        public static Dictionary<char, int> DoubleCharacters = new Dictionary<char, int>();

        static SMSCharacters()
        {
            FillNormalCharacaters();
            FillDoubleCharacters();
        }

        private static void FillDoubleCharacters()
        {
            LoadDoublePeriod(91, 94);
            LoadDoublePeriod(123, 126);
            DoubleCharacters.Add(Convert.ToChar(128), 128);
        }

        private static void LoadDoublePeriod(int a, int b)
        {
            for (int i = a; i <= b; i++)
            {
                DoubleCharacters.Add(Convert.ToChar(i), i);
            }
        }

        private static void FillNormalCharacaters()
        {
            LoadNormalPeriod(32, 90);
            LoadNormalPeriod(97, 122);

            LoadNormalRange(10, 13, 95);
            LoadNormalRange(161, 163, 165, 167, 191, 201, 209, 214, 216, 220,
            228, 230, 233, 246, 252);

            LoadNormalPeriod(196, 199);

            LoadNormalPeriod(223, 224);
            LoadNormalPeriod(235, 236);
            LoadNormalPeriod(241, 242);
            LoadNormalPeriod(248, 249);
        }

        private static void LoadNormalRange(params int[] caracter)
        {
            foreach (int c in caracter)
            {
                NormalCharacters.Add(Convert.ToChar(c), c);
            }
        }


        private static void LoadNormalPeriod(int a, int b)
        {
            for (int i = a; i <= b; i++)
            {
                NormalCharacters.Add(Convert.ToChar(i), i);
            }
        }

        public const int SMSMaxTextLength = 160; //default length for SMS messages

        public static int CharactersToEnd(string text, int maxLength)
        {
            if (maxLength == 0)
                maxLength = SMSMaxTextLength;
            int count = text.Length;
            foreach (var l in text.ToCharArray())
            {
                if (!SMSCharacters.NormalCharacters.ContainsKey(l))
                {
                    if (SMSCharacters.DoubleCharacters.ContainsKey(l))
                        count += 1;
                    else
                    {
                        maxLength = 60;
                        count = text.Length;
                        break;
                    }
                }
            }
            return maxLength - count;
        }

        public static int CharactersToEnd(string text)
        {
            return CharactersToEnd(text, 0);
        }
    }
}
