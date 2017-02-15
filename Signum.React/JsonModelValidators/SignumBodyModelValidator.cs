using Signum.Entities;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Metadata;

namespace Signum.React.Json
{
    public class SignumBodyModelValidator : OriginalDefaultBodyModelValidator
    {
        protected override bool? CustomValidation(ModelMetadata metadata, ValidationContext validationContext, object model)
        {
            return SignumValidate(validationContext, model);
        }

        private bool? SignumValidate(ValidationContext validationContext, object model)
        {
            var lite = model as Lite<Entity>;
            if (lite != null)
                return ValidateLite(validationContext, lite);

            var mod = model as ModifiableEntity;
            if (mod != null)
                return ValidateModifiableEntity(validationContext, mod);

            var mlist = model as IMListPrivate;
            if (mlist != null)
                return ValidateMList(validationContext, mlist);

            return null;
        }

        private bool ValidateModifiableEntity(ValidationContext validationContext, ModifiableEntity mod)
        {
            bool isValid = true;
            PropertyScope propertyScope = new PropertyScope();
            validationContext.KeyBuilders.Push(propertyScope);

            var entity = mod as Entity;
            using (entity == null ? null : entity.Mixins.OfType<CorruptMixin>().Any(c => c.Corrupt) ? Corruption.AllowScope() : Corruption.DenyScope())
            {
                foreach (var kvp in PropertyConverter.GetPropertyConverters(mod.GetType()))
                {
                    if (kvp.Value.AvoidValidate)
                        continue;

                    propertyScope.PropertyName = kvp.Key;
                    if (SignumValidate(validationContext, kvp.Value.GetValue(mod)) ?? true)
                    {
                        isValid = false;
                    }

                    string error = kvp.Value.PropertyValidator.PropertyCheck(mod);

                    if (error != null)
                    {
                        string key = CalculateKey(validationContext);
                        if (validationContext.ModelState.IsValidField(key))
                        {
                            isValid = false;
                            validationContext.ModelState.AddModelError(key, error);
                        }
                    }
                }
            }
            
            if (entity != null && entity.Mixins.Any())
            {
                propertyScope.PropertyName = "mixins";
                PropertyScope mixinScope = new PropertyScope();
                validationContext.KeyBuilders.Push(mixinScope);
                foreach (var mixin in entity.Mixins)
                {
                    mixinScope.PropertyName = mixin.GetType().Name;
                    if (!ValidateModifiableEntity(validationContext, mixin))
                        isValid = false;
                }
                validationContext.KeyBuilders.Pop();
            }

            validationContext.KeyBuilders.Pop();
            return isValid;
        }

        private bool ValidateLite(ValidationContext validationContext, Lite<Entity> lite)
        {
            if (lite.EntityOrNull == null)
                return true;

            PropertyScope propertyScope = new PropertyScope { PropertyName = "entity" };
            validationContext.KeyBuilders.Push(propertyScope);
            var isValid = ValidateModifiableEntity(validationContext, lite.Entity);
            validationContext.KeyBuilders.Pop();
            return isValid;
        }

        private bool ValidateMList(ValidationContext validationContext, IMListPrivate mlist)
        {
            bool isValid = true;
            Type elementType = mlist.GetType().ElementType();
            ModelMetadata elementMetadata = validationContext.MetadataProvider.GetMetadataForType(null, elementType);

            ElementScope elementScope = new ElementScope() { Index = 0 };
            validationContext.KeyBuilders.Push(elementScope);

            PropertyScope property = new PropertyScope { PropertyName = "element" };
            validationContext.KeyBuilders.Push(property);

            foreach (object element in (IEnumerable)mlist)
            {
                elementMetadata.Model = element;
                if (!ValidateNodeAndChildren(elementMetadata, validationContext, mlist))
                {
                    isValid = false;
                }
                elementScope.Index++;
            }
            validationContext.KeyBuilders.Pop();

            validationContext.KeyBuilders.Pop();
            return isValid;
        }
    }
}