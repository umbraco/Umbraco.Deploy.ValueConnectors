using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the new multi node tree picker editor (storing Udis).
    /// </summary>
    public class MultiNodeTreePicker2ValueConnector : IValueConnector
    {
        private readonly IEntityService _entityService;
        private readonly IDataTypeService _dataTypeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiNodeTreePicker2ValueConnector"/> class.
        /// </summary>
        /// <param name="entityService">An <see cref="IEntityService"/> implementation.</param>
        public MultiNodeTreePicker2ValueConnector(IEntityService entityService, IDataTypeService dataTypeService)
        {
            if (entityService == null) throw new ArgumentNullException(nameof(entityService));
            _entityService = entityService;
            _dataTypeService = dataTypeService; 
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.MultiNodeTreePicker2Alias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            if (!(property.Value is string))
                return null;

            // determine the type of the mntp value (only content and media is supported)
            var type = "";
            var preValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(property.PropertyType.DataTypeDefinitionId);
            var startNodeJson = preValues.PreValuesAsDictionary["startNode"].Value;
            if (string.IsNullOrWhiteSpace(startNodeJson) == false)
            {
                var startNode = JsonConvert.DeserializeObject<StartNode>(startNodeJson);
                if (startNode != null && startNode.type.IsNullOrWhiteSpace() == false)
                {
                    type = startNode.type;
                }
            }

            var udis = (string)property.Value;
            var vals = new List<Udi>();
            if (type == "content" || type == "media")
            {
                foreach (var s in udis.Split(','))
                {
                    GuidUdi udi;
                    if (!GuidUdi.TryParse(s, out udi)) continue;
                    // verify the entity exists and add it as a dependency
                    var entity = _entityService.GetByKey(udi.Guid, Constants.UdiEntityType.ToUmbracoObjectType(udi.EntityType));
                    if (entity != null)
                    {
                        dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Exist));
                        vals.Add(udi);
                    }
                    // else just skip the dependency and kill the value
                }
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
                GuidUdi udi;
                if (!GuidUdi.TryParse(s, out udi) || udi.Guid == Guid.Empty)
                    continue;

                if (udi.EntityType == Constants.UdiEntityType.Document || udi.EntityType == Constants.UdiEntityType.Media)
                {
                    var entity = _entityService.GetByKey(udi.Guid, Constants.UdiEntityType.ToUmbracoObjectType(udi.EntityType));
                    if (entity != null)
                        udis.Add(udi);
                    
                }
            }
            content.SetValue(alias, string.Join(",", udis));
        }

        [Serializable]
        public class StartNode
        {
            public string type { get; set; }
        }
    }
}
