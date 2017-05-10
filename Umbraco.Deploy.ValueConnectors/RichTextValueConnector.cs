using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the rich text editor.
    /// </summary>
    public class RichtextValueConnector : IValueConnector
    {
        private readonly ILocalLinkParser _localLinkParser;
        private readonly IImageSourceParser _imageSourceParser;
        private readonly IMacroParser _macroParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="RichtextValueConnector"/> class.
        /// </summary>
        /// <param name="localLinkParser">A <see cref="ILocalLinkParser"/>.</param>
        /// <param name="imageSourceParser">An <see cref="IImageSourceParser"/>.</param>
        /// <param name="macroParser">A <see cref="IMacroParser"/>.</param>
        public RichtextValueConnector(ILocalLinkParser localLinkParser, IImageSourceParser imageSourceParser, IMacroParser macroParser)
        {
            if (localLinkParser == null) throw new ArgumentNullException(nameof(localLinkParser));
            if (imageSourceParser == null) throw new ArgumentNullException(nameof(imageSourceParser));
            if (macroParser == null) throw new ArgumentNullException(nameof(macroParser));
            _localLinkParser = localLinkParser;
            _imageSourceParser = imageSourceParser;
            _macroParser = macroParser;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.TinyMCEAlias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            var value = property.Value as string;
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var dependencyUdis = new List<Udi>();
            value = _localLinkParser.ToArtifact(value, dependencyUdis);
            value = _imageSourceParser.ToArtifact(value, dependencyUdis);
            value = _macroParser.ToArtifact(value, dependencyUdis);

            foreach (var udi in dependencyUdis)
            {
                if (udi.EntityType == Constants.UdiEntityType.Macro)
                    dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Match));
                else
                    dependencies.Add(new ArtifactDependency(udi, false, ArtifactDependencyMode.Exist));
            }

            return value;
        }

        /// <inheritdoc/>
        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                content.SetValue(alias, value);
                return;
            }
            value = _localLinkParser.FromArtifact(value);
            value = _imageSourceParser.FromArtifact(value);
            value = _macroParser.FromArtifact(value);

            content.SetValue(alias, value);
        }
    }
}
