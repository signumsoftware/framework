using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Web
{
    internal static class EntityBaseKeys
    {
        public const string Implementations = "sfImplementations"; //READONLY
        public const string ImplementationsDDL = "sfImplementationsDDL";
        public const string Entity = "sfEntity";
        public const string ToStr = "sfToStr";
        public const string ToStrLink = "sfLink";
    }

    public class EntityBase
    {
        private string labelText = "";
        public string LabelText
        {
            get { return labelText; }
            set { labelText = value; }
        }

        private Type[] implementations;
        public Type[] Implementations
        {
            get { return implementations; }
            set { implementations = value; }
        }

        private Type runtimeType;
        public Type RuntimeType
        {
            get { return runtimeType; }
            set { runtimeType = value; }
        }

        bool create = true;
        public bool Create
        {
            get { return create; }
            set { create = value; }
        }

        bool find = true;
        public bool Find
        {
            get { return find; }
            set { find = value; }
        }

        bool view = true;
        public bool View
        {
            get { return view; }
            set { view = value; }
        }
 
        bool remove = true;
        public bool Remove 
        {
            get { return remove; }
            set { remove = value; }
        }

        string creating = "";
        public string Creating
        {
            get { return creating; }
            set { creating = value; }
        }

        string finding = "";
        public string Finding
        {
            get { return finding; }
            set { finding = value; }
        }

        string viewing = "";
        public string Viewing
        {
            get { return viewing; }
            set { viewing = value; }
        }

        string removing = "";
        public string Removing
        {
            get { return removing; }
            set { removing = value; }
        }


    }
}
