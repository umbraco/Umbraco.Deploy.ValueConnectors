using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the new media picker editor (storing Udis).
    /// </summary>
    public class MediaPicker2ValueConnector : IValueConnector
    {
        private readonly IEntityService _entityService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPicker2ValueConnector"/> class.
        /// </summary>
        /// <param name="entityService">An <see cref="IEntityService"/> implementation.</param>
        public MediaPicker2ValueConnector(IEntityService entityService)
        {
            if (entityService == null) throw new ArgumentNullException(nameof(entityService));
            _entityService = entityService;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.MediaPicker2Alias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            if (!(property.Value is string))
                return null;

            var ids = (string) property.Value;
            var vals = new List<Udi>();
            foreach (var s in ids.Split(','))
            {
                GuidUdi udi;
                if (!GuidUdi.TryParse(s, out udi)) continue;

                // try parsing the value as a Udi, make sure the entity exists and add as dependency
                var entity = _entityService.GetByKey(udi.Guid, UmbracoObjectTypes.Media);
                if (entity != null)
                {
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
            var udis = new List<GuidUdi>();
            value = value ?? string.Empty;
            foreach (var s in value.Split(','))
            {
                // try parsing the value as a Udi and skip if it can't be parsed
                GuidUdi udi;
                if (!GuidUdi.TryParse(s, out udi) || udi.Guid == Guid.Empty)
                    continue;
                // make sure the entity exists and add it to the list of udis
                var entity = _entityService.GetByKey(udi.Guid, UmbracoObjectTypes.Media);
                if (entity != null)
                    udis.Add(udi);
            }
            // set the final value
            content.SetValue(alias, string.Join(",", udis));
        }
    }
}
