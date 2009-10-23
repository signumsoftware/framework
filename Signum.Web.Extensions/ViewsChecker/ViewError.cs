using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.ViewsChecker
{
    public class ViewError
    {
        public static string DescriptionLbl = Resources.Description;
        public static string CompilerErrorMsgLbl = Resources.CompilerErrorMessage;
        public static string SourceCodeErrorLbl = Resources.SourceCodeError;
        public static string SourceFileLbl = Resources.SourceFile;
        public static string LineLbl = Resources.Line;

        public string Description { get; set; }

        public string CompilerErrorMsg { get; set; }

        public string SourceCodeError { get; set; }

        public string SourceFile { get; set; }

        public string Line { get; set; }
    }
}
