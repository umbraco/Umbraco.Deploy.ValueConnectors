using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the new content picker editor (storing Udis).
    /// </summary>
    public class ContentPicker2ValueConnector : IValueConnector
    {
        private readonly IEntityService _entityService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentPicker2ValueConnector"/> class.
        /// </summary>
        /// <param name="entityService">An <see cref="IEntityService"/> implementation.</param>
        public ContentPicker2ValueConnector(IEntityService entityService)
        {
            if (entityService == null) throw new ArgumentNullException(nameof(entityService));
            _entityService = entityService;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] {Constants.PropertyEditors.ContentPicker2Alias};

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            if (!(property.Value is string))
                return null;

            var id = (string) property.Value;

            // try parsing the value as a Udi, make sure the entity exists and add as dependency
            GuidUdi udi;
            if (GuidUdi.TryParse(id, out udi))
            {
                var entity = _entityService.GetByKey(udi.Guid, UmbracoObjectTypes.Document);
                if (entity != null)
                {
                    dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Exist));
                    return udi.ToString();
                }
            }
            // else just skip the dependency - and kill the value
            return "";
        }

        /// <inheritdoc/>
        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            // try parsing the value as a Udi and set to null if it can't be parsed
            GuidUdi udi;
            if (!GuidUdi.TryParse(value, out udi))
            {
                content.SetValue(alias, null);
                return;
            }
            // make sure the entity exists and set the value - otherwise set to null
            var entity = _entityService.GetByKey(udi.Guid, UmbracoObjectTypes.Document);
            content.SetValue(alias, entity != null ? udi.ToString() : null);
        }
    }
}