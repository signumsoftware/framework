using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Signum.Entities.Files
{

    [Serializable]
    public class BigStringMixin : MixinEntity
    {
        BigStringMixin(ModifiableEntity mainEntity, MixinEntity? next)
            : base(mainEntity, next)
        {
        }

        public FilePathEmbedded? File { get; set; }

        public static Action<BigStringMixin, PreSavingContext>? PreSavingAction;
        protected override void PreSaving(PreSavingContext ctx)
        {
            PreSavingAction?.Invoke(this, ctx);
        }

        public static Action<BigStringMixin, PostRetrievingContext>? PostRetrievingAction;
        protected override void PostRetrieving(PostRetrievingContext ctx)
        {
            PostRetrievingAction?.Invoke(this, ctx);
        }
    }
}
