namespace Signum.Excel.Generator
{
    using CarlosAg.Utils.Colorizers;
    using Microsoft.CSharp;
    using Microsoft.VisualBasic;
    using System;
    using System.CodeDom.Compiler;
    using System.Reflection;
    using System.Windows.Forms;

    internal sealed class Language
    {
        private string _name;

        public Language(string language)
        {
            this._name = language.ToUpper();
        }

        public CodeDomProvider GetCodeProvider()
        {
            try
            {
                switch (this._name)
                {
                    case "CS":
                    case "C#":
                    case "CSHARP":
                        return new CSharpCodeProvider();

                    case "VB":
                    case "VB.NET":
                        return new VBCodeProvider();

                    case "JS":
                    case "JSCRIPT.NET":
                        return (CodeDomProvider) Assembly.Load("Microsoft.JScript").CreateInstance("Microsoft.JScript.JScriptCodeProvider");

                    case "J#":
                    case "JSHARP":
                        return (CodeDomProvider) Assembly.Load("VJSharpCodeProvider").CreateInstance("Microsoft.VJSharp.VJSharpCodeProvider");
                }
                throw new ArgumentException("Invalid language. Specify C#, VB, or JScript", "language");
            }
            catch (Exception exception)
            {
                MessageBox.Show("Unable to create the Code Provider for the selected language, please verify you have it installed.\n" + exception.ToString(), "Error:", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return null;
        }

        public Colorizer GetColorizer()
        {
            switch (this._name)
            {
                case "CS":
                case "C#":
                case "CSHARP":
                    return new CSharpColorizer();

                case "VB":
                case "VB.NET":
                    return new VBColorizer();

                case "JS":
                case "JSCRIPT.NET":
                    return new JavascriptColorizer();

                case "J#":
                case "JSHARP":
                    return new JSharpColorizer();
            }
            throw new ArgumentException("Invalid language. Specify C#, VB, or JScript", "language");
        }

        public string GetExtension()
        {
            switch (this._name)
            {
                case "CS":
                case "C#":
                case "CSHARP":
                    return "C#";

                case "VB":
                case "VB.NET":
                    return "VB";

                case "JS":
                case "JSCRIPT.NET":
                    return "JS";

                case "J#":
                case "JSHARP":
                    return "J#";
            }
            throw new ArgumentException("Invalid language. Specify C#, VB, or JScript", "language");
        }

        public override string ToString()
        {
            switch (this._name)
            {
                case "CS":
                case "C#":
                case "CSHARP":
                    return "C#";

                case "VB":
                case "VB.NET":
                    return "Visual Basic.net";

                case "JS":
                case "JSCRIPT.NET":
                    return "JScript.net";

                case "J#":
                case "JSHARP":
                    return "J#";
            }
            throw new ArgumentException("Invalid language. Specify C#, VB, or JScript", "language");
        }

        public string Name
        {
            get
            {
                return this._name;
            }
        }
    }
}

