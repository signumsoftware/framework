using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Signum.Entities;
using Signum.React.Facades;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Signum.React.JsonModelValidators
{
    //don't make it public! use services.AddSignumValidation(); instead
    internal class SignumObjectModelValidator : IObjectModelValidator
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ValidatorCache _validatorCache;
        private readonly CompositeModelValidatorProvider _validatorProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectModelValidator"/>.
        /// </summary>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="validatorProviders">The list of <see cref="IModelValidatorProvider"/>.</param>
        public SignumObjectModelValidator(
            IModelMetadataProvider modelMetadataProvider,
            IList<IModelValidatorProvider> validatorProviders)
        {
            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (validatorProviders == null)
            {
                throw new ArgumentNullException(nameof(validatorProviders));
            }

            _modelMetadataProvider = modelMetadataProvider;
            _validatorCache = new ValidatorCache();

            _validatorProvider = new CompositeModelValidatorProvider(validatorProviders);
        }

        /// <inheritdoc />
        public virtual void Validate(
            ActionContext actionContext,
            ValidationStateDictionary validationState,
            string prefix,
            object model)
        {
            var visitor = GetValidationVisitor(
                actionContext,
                _validatorProvider,
                _validatorCache,
                _modelMetadataProvider,
                validationState);

            var metadata = model == null ? null : _modelMetadataProvider.GetMetadataForType(model.GetType());
            visitor.Validate(metadata, prefix, model, alwaysValidateAtTopLevel: false);
        }

        /// <summary>
        /// Validates the provided object model.
        /// If <paramref name="model"/> is <see langword="null"/> and the <paramref name="metadata"/>'s
        /// <see cref="ModelMetadata.IsRequired"/> is <see langword="true"/>, will add one or more
        /// model state errors that <see cref="Validate(ActionContext, ValidationStateDictionary, string, object)"/>
        /// would not.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="validationState">The <see cref="ValidationStateDictionary"/>.</param>
        /// <param name="prefix">The model prefix key.</param>
        /// <param name="model">The model object.</param>
        /// <param name="metadata">The <see cref="ModelMetadata"/>.</param>
        public virtual void Validate(
            ActionContext actionContext,
            ValidationStateDictionary validationState,
            string prefix,
            object model,
            ModelMetadata metadata)
        {
            var visitor = GetValidationVisitor(
                actionContext,
                _validatorProvider,
                _validatorCache,
                _modelMetadataProvider,
                validationState);

            visitor.Validate(metadata, prefix, model, alwaysValidateAtTopLevel: metadata.IsRequired);
        }

        /// <summary>
        /// Gets a <see cref="ValidationVisitor"/> that traverses the object model graph and performs validation.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="validatorProvider">The <see cref="IModelValidatorProvider"/>.</param>
        /// <param name="validatorCache">The <see cref="ValidatorCache"/>.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="validationState">The <see cref="ValidationStateDictionary"/>.</param>
        /// <returns>A <see cref="ValidationVisitor"/> which traverses the object model graph.</returns>
        public ValidationVisitor GetValidationVisitor(
             ActionContext actionContext,
             IModelValidatorProvider validatorProvider,
             ValidatorCache validatorCache,
             IModelMetadataProvider metadataProvider,
             ValidationStateDictionary validationState)
        {
            return new SignumValidationVisitor(
                actionContext,
                validatorProvider,
                validatorCache,
                metadataProvider,
                validationState);
        }
    }

    internal class SignumValidationVisitor : ValidationVisitor
    {
        public SignumValidationVisitor(
            ActionContext actionContext,
            IModelValidatorProvider validatorProvider,
            ValidatorCache validatorCache,
            IModelMetadataProvider metadataProvider,
            ValidationStateDictionary validationState) :
            base(
                actionContext,
                validatorProvider,
                validatorCache,
                metadataProvider,
                validationState)
        {
        }

        protected override bool VisitComplexType(IValidationStrategy defaultStrategy)
        {
            bool? customValidate = SignumValidate();

            if (customValidate.HasValue)
                return customValidate.Value;

            return base.VisitComplexType(defaultStrategy);
        }

        private bool? SignumValidate()
        {
            if (this.Model is Lite<Entity> lite)
                return ValidateLite(lite);

            if (this.Model is ModifiableEntity mod)
                return ValidateModifiableEntity(mod);

            if (this.Model is IMListPrivate mlist)
                return ValidateMList(mlist);

            return null;
        }

        private bool ValidateLite(Lite<Entity> lite)
        {
            if (lite.EntityOrNull == null)
                return true;

            if (this.CurrentPath.Push(lite.EntityOrNull))
            {
                using (StateManager.Recurse(this, this.Key + ".entity", null, lite.EntityOrNull, null))
                {
                    return this.ValidateModifiableEntity(lite.EntityOrNull);
                }
            }

            return true;
        }

        private bool ValidateMList(IMListPrivate mlist)
        {
            bool isValid = true;
            Type elementType = mlist.GetType().ElementType()!;

            int i = 0;
            foreach (object? element in (IEnumerable)mlist)
            {
                if (element != null && this.CurrentPath.Push(element))
                {
                    using (StateManager.Recurse(this, this.Key + "[" + (i++) + "].element", null, element, null))
                    {
                        if (element is ModifiableEntity me)
                            isValid &= ValidateModifiableEntity(me);
                        else if (element is Lite<Entity> lite)
                            isValid &= ValidateLite(lite);
                        else
                            isValid &= true;
                    }
                }
            }

            return isValid;
        }


        private bool ValidateModifiableEntity(ModifiableEntity mod)
        {
            using (Validator.ModelBinderScope())
            {
                bool isValid = true;

                var entity = mod as Entity;
                using (entity == null ? null : entity.Mixins.OfType<CorruptMixin>().Any(c => c.Corrupt) ? Corruption.AllowScope() : Corruption.DenyScope())
                {
                    foreach (var kvp in SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(mod.GetType()))
                    {
                        if (kvp.Value.AvoidValidate)
                            continue;

                        string? error = kvp.Value.PropertyValidator!.PropertyCheck(mod);
                        if (error != null)
                        {
                            isValid = false;
                            ModelState.AddModelError(this.Key + "." + kvp.Key, error);
                        }

                        var val = kvp.Value.GetValue!(mod);
                        if (val != null && this.CurrentPath.Push(val))
                        {
                            using (StateManager.Recurse(this, this.Key + "." + kvp.Key, null, val, null))
                            {
                                if (this.SignumValidate() == false)
                                {
                                    isValid = false;
                                }
                            }
                        }
                    }

                    if (entity != null && entity.Mixins.Any())
                    {
                        foreach (var mixin in entity.Mixins)
                        {
                            if (this.CurrentPath.Push(mixin))
                            {
                                using (StateManager.Recurse(this, this.Key + ".mixins[" + mixin.GetType().Name + "]", null, mixin, null))
                                {
                                    isValid &= ValidateModifiableEntity(mixin);
                                }
                            }
                        }
                    }
                }

                return isValid;
            }
        }
    }
}
