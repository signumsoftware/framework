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

    [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class DefaultFileTypeAttribute : Attribute
    {
        public string? SymbolContainer { get; set; }
        public string SymbolName { get; set; }

        public FileTypeSymbol FileTypeSymbol { get; set; }

        public DefaultFileTypeAttribute(string symbolName, string? symbolContainer = null)
        {
            this.SymbolName = symbolName;
            this.SymbolContainer = symbolContainer;
        }
    }
}
