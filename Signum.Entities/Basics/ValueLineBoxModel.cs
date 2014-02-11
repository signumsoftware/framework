using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Web
{
    [Serializable]
    public class ValueLineBoxModel : ModelEntity
    {
        public ValueLineBoxModel()
        {
        }

        public ValueLineBoxModel(ValueLineBoxType boxType, string fieldName, string topText)
        {
            this.boxType = boxType;
            this.fieldName = fieldName;
            this.topText = topText;
        }

        string topText;
        public string TopText
        {
            get { return topText; }
            set { Set(ref topText, value, () => TopText); }
        }

        string fieldName;
        public string FieldName
        {
            get { return fieldName; }
            set { Set(ref fieldName, value, () => FieldName); }
        }

        ValueLineBoxType boxType;
        public ValueLineBoxType BoxType
        {
            get { return boxType; }
            set { Set(ref boxType, value, () => BoxType); }
        }

        bool isNullable;
        public bool IsNullable
        {
            get { return isNullable; }
            set { Set(ref isNullable, value, () => IsNullable); }
        }

        int? intValue;
        public int? IntValue
        {
            get { return intValue; }
            set { Set(ref intValue, value, () => IntValue); }
        }

        decimal? decimalValue;
        public decimal? DecimalValue
        {
            get { return decimalValue; }
            set { Set(ref decimalValue, value, () => DecimalValue); }
        }

        bool? boolValue;
        public bool? BoolValue
        {
            get { return boolValue; }
            set { Set(ref boolValue, value, () => BoolValue); }
        }

        string stringValue;
        public string StringValue
        {
            get { return stringValue; }
            set { Set(ref stringValue, value, () => StringValue); }
        }

        DateTime? dateValue;
        public DateTime? DateValue
        {
            get { return dateValue; }
            set { Set(ref dateValue, value, () => DateValue); }
        }


        protected internal override string PropertyValidation(PropertyInfo pi)
        {
            string error = SelectorMessage.ValueMustBeSpecifiedFor0.NiceToString().Formato(fieldName);
            switch (boxType)
            {
                case ValueLineBoxType.Boolean:
                    if (boolValue == null)
                        return error;
                    break;
                case ValueLineBoxType.Integer:
                    if (intValue == null)
                        return error;
                    break;
                case ValueLineBoxType.Decimal:
                    if (decimalValue == null)
                        return error;
                    break;
                case ValueLineBoxType.DateTime:
                    if (dateValue == null && !IsNullable)
                        return error;
                    break;
                case ValueLineBoxType.String:
                    if (string.IsNullOrEmpty(stringValue))
                        return error;
                    break;
                default:
                    throw new ArgumentException("ValueLineBoxType {0} does not exist".Formato(boxType));
            }
            return null;
        }
    }

    public enum ValueLineBoxType
    {
        String,
        Boolean,
        Integer,
        Decimal,
        DateTime,
    }

    public class ValueLineOptions
    {
        public string prefix;
        public ValueLineBoxType type;
        public string title = SelectorMessage.ChooseAValue.NiceToString();
        public string message = SelectorMessage.PleaseChooseAValueToContinue.NiceToString();
        public string fieldName = null;

        public ValueLineOptions(ValueLineBoxType type, string parentPrefix, string newPart)
        {
            this.type = type;
            this.prefix = "_".CombineIfNotEmpty(parentPrefix, newPart);
        }
    }
}
