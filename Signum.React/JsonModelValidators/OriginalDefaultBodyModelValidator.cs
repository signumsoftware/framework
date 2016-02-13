using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Web.Http.ModelBinding;
using System.Web.Http.Validation;


namespace Signum.React.Json
{
    /// <summary>
    /// Recursively validate an object. 
    /// </summary>
    public class OriginalDefaultBodyModelValidator : IBodyModelValidator
    {
 

        /// <summary>
        /// Determines whether the <paramref name="model"/> is valid and adds any validation errors to the <paramref name="actionContext"/>'s <see cref="ModelStateDictionary"/>
        /// </summary>
        /// <param name="model">The model to be validated.</param>
        /// <param name="type">The <see cref="Type"/> to use for validation.</param>
        /// <param name="metadataProvider">The <see cref="ModelMetadataProvider"/> used to provide the model metadata.</param>
        /// <param name="actionContext">The <see cref="HttpActionContext"/> within which the model is being validated.</param>
        /// <param name="keyPrefix">The <see cref="string"/> to append to the key for any validation errors.</param>
        /// <returns><c>true</c>if <paramref name="model"/> is valid, <c>false</c> otherwise.</returns>
        public bool Validate(object model, Type type, ModelMetadataProvider metadataProvider, HttpActionContext actionContext, string keyPrefix)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException("metadataProvider");
            }

            if (actionContext == null)
            {
                throw new ArgumentNullException("actionContext");
            }

            if (model != null && MediaTypeFormatterCollection.IsTypeExcludedFromValidation(model.GetType()))
            {
                // no validation for some DOM like types
                return true;
            }

            ModelValidatorProvider[] validatorProviders = actionContext.GetValidatorProviders().ToArray();
            // Optimization : avoid validating the object graph if there are no validator providers
            if (validatorProviders == null || validatorProviders.Length == 0)
            {
                return true;
            }

            ModelMetadata metadata = metadataProvider.GetMetadataForType(() => model, type);
            ValidationContext validationContext = new ValidationContext()
            {
                MetadataProvider = metadataProvider,
                ActionContext = actionContext,
                //ValidatorCache = actionContext.GetValidatorCache(),
                ModelState = actionContext.ModelState,
                Visited = new HashSet<object>(),
                KeyBuilders = new Stack<IKeyBuilder>(),
                RootPrefix = keyPrefix
            };
            return ValidateNodeAndChildren(metadata, validationContext, container: null);
        }

        protected bool ValidateNodeAndChildren(ModelMetadata metadata, ValidationContext validationContext, object container)
        {
            object model = metadata.Model;
            bool isValid = true;

            // Optimization: we don't need to recursively traverse the graph for null and primitive types
            if (model == null || TypeHelper.IsSimpleType(model.GetType()))
            {
                return ShallowValidate(metadata, validationContext, container);
            }

            // Check to avoid infinite recursion. This can happen with cycles in an object graph.
            if (validationContext.Visited.Contains(model))
            {
                return true;
            }
            validationContext.Visited.Add(model);

            var custom = CustomValidation(metadata, validationContext, model);
            if (custom != null)
            {
                isValid = custom.Value;
            }
            else
            {
                // Validate the children first - depth-first traversal
                IEnumerable enumerableModel = model as IEnumerable;
                if (enumerableModel == null)
                {
                    isValid = ValidateProperties(metadata, validationContext);
                }
                else
                {
                    isValid = ValidateElements(enumerableModel, validationContext);
                }
                if (isValid)
                {
                    // Don't bother to validate this node if children failed.
                    isValid = ShallowValidate(metadata, validationContext, container);
                }
            }

            // Pop the object so that it can be validated again in a different path
            validationContext.Visited.Remove(model);

            return isValid;
        }

        protected virtual bool? CustomValidation(ModelMetadata metadata, ValidationContext validationContext, object model)
        {
            return null;
        }

        private bool ValidateProperties(ModelMetadata metadata, ValidationContext validationContext)
        {
            bool isValid = true;
            PropertyScope propertyScope = new PropertyScope();
            validationContext.KeyBuilders.Push(propertyScope);
            foreach (ModelMetadata childMetadata in validationContext.MetadataProvider.GetMetadataForProperties(metadata.Model, metadata.ModelType))
            {
                propertyScope.PropertyName = childMetadata.PropertyName;
                if (!ValidateNodeAndChildren(childMetadata, validationContext, metadata.Model))
                {
                    isValid = false;
                }
            }
            validationContext.KeyBuilders.Pop();
            return isValid;
        }

        private bool ValidateElements(IEnumerable model, ValidationContext validationContext)
        {
            bool isValid = true;
            
            Type elementType = model.GetType().ElementType();
            ModelMetadata elementMetadata = validationContext.MetadataProvider.GetMetadataForType(null, elementType);

            ElementScope elementScope = new ElementScope() { Index = 0 };
            validationContext.KeyBuilders.Push(elementScope);
            foreach (object element in model)
            {
                elementMetadata.Model = element;
                if (!ValidateNodeAndChildren(elementMetadata, validationContext, model))
                {
                    isValid = false;
                }
                elementScope.Index++;
            }
            validationContext.KeyBuilders.Pop();
            return isValid;
        }

        // Validates a single node (not including children)
        // Returns true if validation passes successfully
        private static bool ShallowValidate(ModelMetadata metadata, ValidationContext validationContext, object container)
        {
            bool isValid = true;
          
            foreach (ModelValidator validator in validationContext.ActionContext.GetValidators(metadata/*, validationContext.ValidatorCache*/))
            {
                foreach (ModelValidationResult error in validator.Validate(metadata, container))
                {
                    string key = CalculateKey(validationContext);

                    // Avoid adding model errors if the model state already contains model errors for that key
                    // We can't perform this check earlier because we compute the key string only when we detect an error
                    if (!validationContext.ModelState.IsValidField(key))
                    {
                        return false;
                    }

                    validationContext.ModelState.AddModelError(key, error.Message);
                    isValid = false;
                }
            }
            return isValid;
        }

        protected static string CalculateKey(ValidationContext validationContext)
        {
            string key = validationContext.RootPrefix;
            foreach (IKeyBuilder keyBuilder in validationContext.KeyBuilders.Reverse())
            {
                key = keyBuilder.AppendTo(key);
            }
            return key;
        }

        protected interface IKeyBuilder
        {
            string AppendTo(string prefix);
        }

        protected class PropertyScope : IKeyBuilder
        {
            public string PropertyName { get; set; }

            public string AppendTo(string prefix)
            {
                return ModelBindingHelper.CreatePropertyModelName(prefix, PropertyName);
            }
        }

        protected class ElementScope : IKeyBuilder
        {
            public int Index { get; set; }

            public string AppendTo(string prefix)
            {
                return ModelBindingHelper.CreateIndexModelName(prefix, Index);
            }
        }

        protected class ValidationContext
        {
            public ModelMetadataProvider MetadataProvider { get; set; }
            public HttpActionContext ActionContext { get; set; }
            //public IModelValidatorCache ValidatorCache { get; set; }
            public ModelStateDictionary ModelState { get; set; }
            public HashSet<object> Visited { get; set; }
            public Stack<IKeyBuilder> KeyBuilders { get; set; }
            public string RootPrefix { get; set; }
        }


        static class ModelBindingHelper
        {
            internal static string CreatePropertyModelName(string prefix, string propertyName)
            {
                if (String.IsNullOrEmpty(prefix))
                {
                    return propertyName ?? String.Empty;
                }
                else if (String.IsNullOrEmpty(propertyName))
                {
                    return prefix ?? String.Empty;
                }
                else {
                    return prefix + "." + propertyName;
                }
            }

            internal static string CreateIndexModelName(string parentName, int index)
            {
                return CreateIndexModelName(parentName, index.ToString(CultureInfo.InvariantCulture));
            }

            internal static string CreateIndexModelName(string parentName, string index)
            {
                return (parentName.Length == 0) ? "[" + index + "]" : parentName + "[" + index + "]";
            }
        }

        static class TypeHelper
        {
            internal static bool IsSimpleType(Type type)
            {
                return type.IsPrimitive ||
                       type.Equals(typeof(string)) ||
                       type.Equals(typeof(DateTime)) ||
                       type.Equals(typeof(Decimal)) ||
                       type.Equals(typeof(Guid)) ||
                       type.Equals(typeof(DateTimeOffset)) ||
                       type.Equals(typeof(TimeSpan));
            }
        }
    }

}