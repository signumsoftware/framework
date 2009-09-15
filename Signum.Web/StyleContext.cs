using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Web
{
    public class StyleContext : IDisposable
    {
        public static StyleContext Default{get;set;}

        static StyleContext()
        {
            Default = new StyleContext(false)
            {
                BreakLine = true,
                LabelVisible = true,
                ShowValidationMessage = true,
                ReadOnly = false,
                ValueFirst = false
            }; 
        }

        [ThreadStatic]
        static StyleContext current;
        public static StyleContext Current { get { return current ?? Default; } }

        StyleContext parent;
        
        bool? labelVisible = null;
        public bool LabelVisible
        {
            get { return labelVisible ?? parent.LabelVisible; }
            set { labelVisible = value; }
        }

        bool? breakLine = null;
        public bool BreakLine
        {
            get { return breakLine ?? parent.BreakLine; }
            set { breakLine = value; }
        }

        bool? readOnly = null;
        public bool ReadOnly
        {
            get { return readOnly ?? parent.ReadOnly; }
            set { readOnly = value; }
        }

        bool? showValidationMessage = null;
        public bool ShowValidationMessage
        {
            get { return showValidationMessage ?? parent.ShowValidationMessage; }
            set { showValidationMessage = value; }
        }

        bool? valueFirst = null;
        public bool ValueFirst
        {
            get { return valueFirst ?? parent.ValueFirst; }
            set { valueFirst = value; }
        }

        public StyleContext(): this(true)
        {
        }

        public StyleContext(bool register)
        {
            if (register)
            {
                Register();
            }
        }

        public StyleContext Register()
        {
            parent = Current;
            current = this;
            return this;
        }

        public void Dispose()
        {
            if (this != current)
                throw new InvalidOperationException("StyleContext not registred");

            current = parent;
        }
    }
}
