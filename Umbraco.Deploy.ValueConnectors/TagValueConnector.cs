using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the tags editor.
    /// </summary>
    public class TagValueConnector : IValueConnector
    {
        private readonly IDataTypeService _dataTypeService;
        private readonly PropertyEditorResolver _propertyEditorResolver;

        public TagValueConnector(IDataTypeService dataTypeService, PropertyEditorResolver propertyEditorResolver)
        {
            if (dataTypeService == null) throw new ArgumentNullException(nameof(dataTypeService));
            if (propertyEditorResolver == null) throw new ArgumentNullException(nameof(propertyEditorResolver));
            _dataTypeService = dataTypeService;
            _propertyEditorResolver = propertyEditorResolver;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.TagsAlias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            var value = property.Value;
            return (string) value;
        }

        /// <inheritdoc/>
        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            var property = content.Properties[alias];

            if (property == null)
            {
                throw new IndexOutOfRangeException("No property exists with name " + alias);
            }

            var propertyEditor = _propertyEditorResolver.GetByAlias(property.PropertyType.PropertyEditorAlias);
            if (propertyEditor == null)
            {
                throw new NullReferenceException("No property editor found with alias " + property.PropertyType.PropertyEditorAlias);
            }
            
            var supportTagsAttribute = TagExtractor.GetAttribute(propertyEditor);
            if (supportTagsAttribute != null)
            {
                var preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(property.PropertyType.DataTypeDefinitionId);

                var contentPropertyData = new ContentPropertyData(property.Value, preValues);

                TagExtractor.SetPropertyTags(property, contentPropertyData, value, supportTagsAttribute);
            }
        }
    }
}