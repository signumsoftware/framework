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
                ValueFirst = false,
                ShowFieldDiv = true,
                ShowTicks = true,
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

        bool? showFieldDiv = null;
        public bool ShowFieldDiv
        {
            get { return showFieldDiv ?? parent.ShowFieldDiv; }
            set { showFieldDiv = value; }
        }

        bool? showTicks;
        public bool? ShowTicks
        {
            get { return showTicks ?? parent.showTicks; }
            set { showTicks = value; }
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

        public static StyleContext RegisterCleanStyleContext(bool register)
        {
            return new StyleContext(register)
            {
                BreakLine = true,
                LabelVisible = true,
                ShowValidationMessage = true,
                ReadOnly = false,
                ValueFirst = false,
                ShowFieldDiv = true,
                ShowTicks = true,
            };
        }

        public void Dispose()
        {
            if (this == current)
                current = parent;
            //throw new InvalidOperationException("StyleContext not registered");
        }

        public override string ToString()
        {
            return ReadOnly.ToString() + ((parent != null) ? parent.ToString() : "");
        }
    }
}
