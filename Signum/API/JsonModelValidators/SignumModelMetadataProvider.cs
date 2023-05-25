using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Signum.API.JsonModelValidators;

internal class SignumModelMetadataProvider : DefaultModelMetadataProvider
{
    public SignumModelMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider) : 
        base(detailsProvider)
    {
    }

    protected override ModelMetadata CreateModelMetadata(DefaultMetadataDetails entry)
    {
        if (typeof(Lite<Entity>).IsAssignableFrom(entry.Key.ModelType) ||
            typeof(IModifiableEntity).IsAssignableFrom(entry.Key.ModelType))
            return new SignumModelMetadata(this, this.DetailsProvider, entry);

        return base.CreateModelMetadata(entry);
    }
}

internal class SignumModelMetadata : DefaultModelMetadata
{
    public SignumModelMetadata(IModelMetadataProvider provider, ICompositeMetadataDetailsProvider detailsProvider, DefaultMetadataDetails details) :
        base(provider, detailsProvider, details)
    {
    }

    public override bool? HasValidators => true;
}
