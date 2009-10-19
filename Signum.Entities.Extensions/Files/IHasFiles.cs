using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Files
{
    public interface IHasFiles
    {
        Enum GetFileType(string propertyName);
    }
}
