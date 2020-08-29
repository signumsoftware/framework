using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Signum.Entities.Basics
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    /// <summary>
    /// BigString is an extension point so that in Extension to save it in the file system / block storage
    /// 
    /// In order to save the redundan HasValue column, the Embedded should not be nullable, the Text is by default.
    /// </summary>
    [Serializable]
    public class BigStringEmbedded : EmbeddedEntity
    {
        public BigStringEmbedded()
        {
        }

        public BigStringEmbedded(string? text)
        {
            this.Text = text;
        }

        [DbType(Size = int.MaxValue)]
        public string? Text { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
}
