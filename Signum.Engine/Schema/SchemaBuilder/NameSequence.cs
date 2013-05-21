using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities;

namespace Signum.Engine.Maps
{
    public class NameSequence
    {
        private NameSequence() { }

        public static readonly NameSequence Void = new VoidNameSequence();

        class VoidNameSequence : NameSequence
        {
            public override string ToString()
            {
                return "Value";
            }
        }

        readonly string Value;
        readonly NameSequence PreSequence;

        private NameSequence(string value, NameSequence preSequence)
        {
            this.Value = value;
            this.PreSequence = preSequence;
        }

        public NameSequence Add(string name)
        {
            return new NameSequence(name, this);
        }

        public override string ToString()
        {
            if(PreSequence is VoidNameSequence)
                return Value;

            return "_".Combine(PreSequence.ToString(), Value);
        }
    }
}
