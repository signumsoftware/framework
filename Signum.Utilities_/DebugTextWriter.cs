using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Signum.Utilities
{
    public class DebugTextWriter : TextWriter
    {
        public override void Write(char[] buffer, int index, int count)
        {
            Debug.Write(new String(buffer, index, count));
        }

        public override void Write(string value)
        {
            Debug.Write(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
    }
}
