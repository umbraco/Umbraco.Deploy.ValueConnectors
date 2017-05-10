using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    public class ObsoleteMediaPickerValueConnector : IValueConnector
    {
        private readonly IEntityService _entityService;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPickerValueConnector"/> class.
        /// </summary>
        /// <param name="entityService">An <see cref="IEntityService"/> implementation.</param>
        public ObsoleteMediaPickerValueConnector(IEntityService entityService, ILogger logger)
        {
            if (entityService == null) throw new ArgumentNullException(nameof(entityService));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _entityService = entityService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.MediaPickerAlias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            if (!(property.Value is int))
                return null;

            int id = (int) property.Value;

            var guidAttempt = _entityService.GetKeyForId(id, UmbracoObjectTypes.Media);
            if (guidAttempt.Success)
            {
                // replace the id by the corresponding Udi
                var udi = new GuidUdi(Constants.UdiEntityType.Media, guidAttempt.Result);
                dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Exist));

                return udi.ToString();
            }
            else
            {
                _logger.Debug<ObsoleteMediaPickerValueConnector>($"Couldn't convert integer value #{id} to UDI");
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            // TODO: Needs logging when things goes wrong
            if (string.IsNullOrWhiteSpace(value) == true)
                return;

            GuidUdi udi;
            if (!GuidUdi.TryParse(value, out udi) || udi.Guid == Guid.Empty)
                return;

            var idAttempt = _entityService.GetIdForKey(udi.Guid, UmbracoObjectTypes.Media);
            if (idAttempt) content.SetValue(alias, idAttempt.Result.ToString()); // picker ids needs to be strings!
            
        }
    }
}
