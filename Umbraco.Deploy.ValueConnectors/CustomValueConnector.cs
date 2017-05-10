using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;

namespace Umbraco.Deploy.ValueConnectors
{
    // though this is a concrete class (not abstract) we do NOT want it to be
    // discovered and automatically registered, instead ppl need to do it explicitely.
    [HideFromTypeFinder]
    internal class CustomValueConnector : IValueConnector
    {
        private readonly Func<Property, ICollection<ArtifactDependency>, string> _getValue;
        private readonly Action<IContentBase, string, string> _setValue;

        public CustomValueConnector(Func<Property, ICollection<ArtifactDependency>, string> getValue, Action<IContentBase, string, string> setValue)
        {
            _getValue = getValue;
            _setValue = setValue;
        }

        /// <inheritdoc/>
        public IEnumerable<string> PropertyEditorAliases => Enumerable.Empty<string>();

        /// <inheritdoc/>
        public string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            return _getValue(property, dependencies);
        }

        /// <inheritdoc/>
        public void SetValue(IContentBase content, string alias, string value)
        {
            _setValue(content, alias, value);
        }
    }
}
