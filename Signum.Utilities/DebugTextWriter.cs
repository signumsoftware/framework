using System.IO;
using System.Diagnostics;

namespace Signum.Utilities;

public class DebugTextWriter : TextWriter
{
    public int Lines = 0;

    public override void Write(char[] buffer, int index, int count)
    {
        Lines++;
        Debug.Write(new String(buffer, index, count));
    }

    public override void Write(string? value)
    {
        Lines++;
        Debug.Write(value);
    }

    public override Encoding Encoding
    {
        get { return Encoding.Default; }
    }
}
