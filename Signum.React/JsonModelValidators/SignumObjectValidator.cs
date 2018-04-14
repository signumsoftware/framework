using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Signum.Entities;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Json
{
    ///// <summary>
    ///// The default implementation of <see cref="IObjectModelValidator"/>.
    ///// </summary>
    //public class DefaultObjectValidator : IObjectModelValidator
    //{
    //    private readonly IModelMetadataProvider _modelMetadataProvider;
    //    private readonly ValidatorCache _validatorCache;
    //    private readonly IModelValidatorProvider _validatorProvider;

    //    /// <summary>
    //    /// Initializes a new instance of <see cref="DefaultObjectValidator"/>.
    //    /// </summary>
    //    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
    //    /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
    //    public DefaultObjectValidator(
    //        IModelMetadataProvider modelMetadataProvider,
    //        IList<IModelValidatorProvider> validatorProviders)
    //    {
    //        if (modelMetadataProvider == null)
    //        {
    //            throw new ArgumentNullException(nameof(modelMetadataProvider));
    //        }

    //        if (validatorProviders == null)
    //        {
    //            throw new ArgumentNullException(nameof(validatorProviders));
    //        }

    //        _modelMetadataProvider = modelMetadataProvider;
    //        _validatorCache = new ValidatorCache();

    //        _validatorProvider = new CompositeModelValidatorProvider(validatorProviders);
    //    }

    //    /// <inheritdoc />
    //    public void Validate(
    //        ActionContext actionContext,
    //        ValidationStateDictionary validationState,
    //        string prefix,
    //        object model)
    //    {
    //        if (actionContext == null)
    //        {
    //            throw new ArgumentNullException(nameof(actionContext));
    //        }

    //        var visitor = new ValidationVisitor(
    //            actionContext,
    //            _validatorProvider,
    //            _validatorCache,
    //            _modelMetadataProvider,
    //            validationState);

    //        var metadata = model == null ? null : _modelMetadataProvider.GetMetadataForType(model.GetType());
    //        visitor.Validate(metadata, prefix, model);
    //    }
    //}

    //public class SignumObjectModelValidator : IObjectModelValidator
    //{
    //    protected override bool? CustomValidation(ModelMetadata metadata, ValidationContext validationContext, object model)
    //    {
    //        validationContext.Visited.Remove(model); //comes already visited when jumping to SignumBodyModelValidator

    //        var result = SignumValidate(validationContext, model);

    //        validationContext.Visited.Add(model);

    //        return result;
    //    }

    //    private bool? SignumValidate(ValidationContext validationContext, object model)
    //    {
    //        if (model is Lite<Entity> lite)
    //            return ValidateLite(validationContext, lite);

    //        if (model is ModifiableEntity mod)
    //            return ValidateModifiableEntity(validationContext, mod);

    //        if (model is IMListPrivate mlist)
    //            return ValidateMList(validationContext, mlist);

    //        return null;
    //    }

    //    private bool ValidateModifiableEntity(ValidationContext validationContext, ModifiableEntity mod)
    //    {
    //        using (Validator.ModelBinderScope())
    //        {
    //            if (mod is Entity && validationContext.Visited.Contains(mod))
    //                return true;

    //            validationContext.Visited.Add(mod);

    //            bool isValid = true;
    //            PropertyScope propertyScope = new PropertyScope();
    //            validationContext.KeyBuilders.Push(propertyScope);

    //            var entity = mod as Entity;
    //            using (entity == null ? null : entity.Mixins.OfType<CorruptMixin>().Any(c => c.Corrupt) ? Corruption.AllowScope() : Corruption.DenyScope())
    //            {
    //                foreach (var kvp in PropertyConverter.GetPropertyConverters(mod.GetType()))
    //                {
    //                    if (kvp.Value.AvoidValidate)
    //                        continue;

    //                    propertyScope.PropertyName = kvp.Key;
    //                    if (SignumValidate(validationContext, kvp.Value.GetValue(mod)) ?? true)
    //                    {
    //                        isValid = false;
    //                    }

    //                    string error = kvp.Value.PropertyValidator.PropertyCheck(mod);

    //                    if (error != null)
    //                    {
    //                        string key = CalculateKey(validationContext);
    //                        if (validationContext.ModelState.IsValidField(key))
    //                        {
    //                            isValid = false;
    //                            validationContext.ModelState.AddModelError(key, error);
    //                        }
    //                    }
    //                }
    //            }

    //            if (entity != null && entity.Mixins.Any())
    //            {
    //                propertyScope.PropertyName = "mixins";
    //                PropertyScope mixinScope = new PropertyScope();
    //                validationContext.KeyBuilders.Push(mixinScope);
    //                foreach (var mixin in entity.Mixins)
    //                {
    //                    mixinScope.PropertyName = mixin.GetType().Name;
    //                    if (!ValidateModifiableEntity(validationContext, mixin))
    //                        isValid = false;
    //                }
    //                validationContext.KeyBuilders.Pop();
    //            }

    //            validationContext.KeyBuilders.Pop();

    //            validationContext.Visited.Remove(mod);
    //            return isValid;
    //        }
    //    }

    //    private bool ValidateLite(ValidationContext validationContext, Lite<Entity> lite)
    //    {
    //        if (lite.EntityOrNull == null)
    //            return true;

    //        PropertyScope propertyScope = new PropertyScope { PropertyName = "entity" };
    //        validationContext.KeyBuilders.Push(propertyScope);
    //        var isValid = ValidateModifiableEntity(validationContext, lite.Entity);
    //        validationContext.KeyBuilders.Pop();
    //        return isValid;
    //    }

    //    private bool ValidateMList(ValidationContext validationContext, IMListPrivate mlist)
    //    {
    //        bool isValid = true;
    //        Type elementType = mlist.GetType().ElementType();
    //        ModelMetadata elementMetadata = validationContext.MetadataProvider.GetMetadataForType(null, elementType);

    //        ElementScope elementScope = new ElementScope() { Index = 0 };
    //        validationContext.KeyBuilders.Push(elementScope);

    //        PropertyScope property = new PropertyScope { PropertyName = "element" };
    //        validationContext.KeyBuilders.Push(property);

    //        foreach (object element in (IEnumerable)mlist)
    //        {
    //            elementMetadata.Model = element;
    //            if (!ValidateNodeAndChildren(elementMetadata, validationContext, mlist))
    //            {
    //                isValid = false;
    //            }
    //            elementScope.Index++;
    //        }
    //        validationContext.KeyBuilders.Pop();

    //        validationContext.KeyBuilders.Pop();
    //        return isValid;
    //    }
    //}

    //public class SignumValidationVisitor : ValidationVisitor
    //{
    //    public SignumValidationVisitor(
    //        ActionContext actionContext, 
    //        IModelValidatorProvider validatorProvider, 
    //        ValidatorCache validatorCache, 
    //        IModelMetadataProvider metadataProvider, 
    //        ValidationStateDictionary validationState)
    //        :base(actionContext, validatorProvider, validatorCache, metadataProvider, validationState)
    //    {
    //    }
    //}
}