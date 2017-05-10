using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the content picker editor.
    /// </summary>
    public class ContentPickerValueConnector : IValueConnector
    {
        private readonly IEntityService _entityService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentPickerValueConnector"/> class.
        /// </summary>
        /// <param name="entityService">An <see cref="IEntityService"/> implementation.</param>
        public ContentPickerValueConnector(IEntityService entityService)
        {
            if (entityService == null) throw new ArgumentNullException(nameof(entityService));
            _entityService = entityService;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.ContentPickerAlias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            if (!(property.Value is int))
                return null;

            var id = (int) property.Value;

            // get the guid corresponding to the id
            // it *can* fail if eg the id points to a deleted content
            var guidAttempt = _entityService.GetKeyForId(id, UmbracoObjectTypes.Document);
            if (guidAttempt.Success)
            {
                // replace the id by the corresponding Udi
                var udi = new GuidUdi(Constants.UdiEntityType.Document, guidAttempt.Result);
                dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Exist));
                return udi.ToString();
            }
            // else just skip the dependency - and kill the value
            return "";
        }

        /// <inheritdoc/>
        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            GuidUdi udi;
            if (!GuidUdi.TryParse(value, out udi))
            {
                content.SetValue(alias, null);
                return;
            }

            // get the id corresponding to the guid
            // it *should* succeed when deploying, due to dependencies management
            // nevertheless, assume it can fail, and then create an invalid localLink
            var idAttempt = _entityService.GetIdForKey(udi.Guid, UmbracoObjectTypes.Document);
            var id = idAttempt.Success ? idAttempt.Result : 0;
            content.SetValue(alias, id > 0 ? (object) id : null);
        }
    }
}
