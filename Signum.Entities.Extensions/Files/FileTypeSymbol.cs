using System;

namespace Signum.Entities.Files
{
    [Serializable]
    public class FileTypeSymbol : Symbol
    {
        private FileTypeSymbol() { }

        public FileTypeSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }
}
