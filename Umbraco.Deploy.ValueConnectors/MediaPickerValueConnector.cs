using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the media picker editor.
    /// </summary>
    public class MediaPickerValueConnector : IValueConnector
    {
        private readonly IEntityService _entityService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPickerValueConnector"/> class.
        /// </summary>
        /// <param name="entityService">An <see cref="IEntityService"/> implementation.</param>
        public MediaPickerValueConnector(IEntityService entityService)
        {
            if (entityService == null) throw new ArgumentNullException(nameof(entityService));
            _entityService = entityService;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.MultipleMediaPickerAlias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            if (!(property.Value is string))
                return null;

            var ids = (string) property.Value;
            var vals = new List<Udi>();
            foreach (var s in ids.Split(','))
            {
                int id;
                if (!int.TryParse(s, out id)) continue;

                // get the guid corresponding to the id
                // it *can* fail if eg the id points to a deleted content,
                // and then we use an empty guid
                var guidAttempt = _entityService.GetKeyForId(id, UmbracoObjectTypes.Media);
                if (guidAttempt.Success)
                {
                    // replace the id by the corresponding Udi
                    var udi = new GuidUdi(Constants.UdiEntityType.Media, guidAttempt.Result);
                    dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Exist));

                    vals.Add(udi);
                }
                // else just skip the dependency and kill the value
            }

            return string.Join(",", vals);
        }

        /// <inheritdoc/>
        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            var ids = new List<int>();
            value = value ?? string.Empty;
            foreach (var s in value.Split(','))
            {
                GuidUdi udi;
                if (!GuidUdi.TryParse(s, out udi) || udi.Guid == Guid.Empty)
                    continue;

                // get the id corresponding to the guid
                // it *should* succeed when deploying, due to dependencies management
                // nevertheless, assume it can fail, and then create an invalid localLink
                var idAttempt = _entityService.GetIdForKey(udi.Guid, UmbracoObjectTypes.Media);
                if (idAttempt) ids.Add(idAttempt.Result);
            }

            content.SetValue(alias, string.Join(",", ids));
        }
    }
}
