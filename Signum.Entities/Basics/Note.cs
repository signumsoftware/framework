using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    public interface INoteDN : IIdentifiable
    {
    }

    [Serializable]
    public class NoteDN : IdentifiableEntity, INoteDN
    {
        [ImplementedByAll]
        Lazy<IdentifiableEntity> entity;
        public Lazy<IdentifiableEntity> Entity
        {
            get { return entity; }
            set { Set(ref entity, value, "Entity"); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(Min = 1)]
        public string Text
        {
            get { return text; }
            set { Set(ref text, value, "Text"); }
        }

        public override string ToString()
        {
            return text.Etc(200).TryCC(t => t.Split('\r', '\n').FirstOrDefault());
        }
    }
}
