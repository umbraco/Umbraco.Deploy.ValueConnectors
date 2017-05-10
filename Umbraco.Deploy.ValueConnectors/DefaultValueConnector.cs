using System;
using System.Collections.Generic;
using System.Globalization;
using Umbraco.Core.Deploy;
using Umbraco.Core.Models;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a default value connector.
    /// </summary>
    public class DefaultValueConnector : IValueConnector
    {
        /// <inheritdoc/>
        public IEnumerable<string> PropertyEditorAliases => new[] { "*" };

        /// <inheritdoc/>
        public string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            var value = property.Value;
            if (value == null) return null;

            // that value can be
            // int
            // decimal
            // DateTime
            // string

            // we use a prefix so that SetValue (see below) can convert the string back
            // to the proper object type before invoking content.SetValue() - note that
            // what GetValue returns is what goes into SetValue, unchanged, and the value
            // is not parsed outside of this class - so we are entirely free to use
            // whatever mechanism we want to serialize the value.

            if (value is string)
                return "s" + (string) value;
            if (value is int)
                return "i" + ((int) value).ToString();
            if (value is decimal)
                return "d" + ((decimal) value).ToString(NumberFormatInfo.InvariantInfo); // ensure we use a dot, not a comma
            if (value is DateTime)
                return "t" + ((DateTime) value).ToString("o", DateTimeFormatInfo.InvariantInfo); // ISO 8601 etc

            throw new NotSupportedException("Value of type \"" + value.GetType().FullName + "\" is not supported.");
        }

        /// <inheritdoc/>
        public void SetValue(IContentBase content, string alias, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                content.SetValue(alias, value);
                return;
            }

            var t = value[0];
            var v = value.Substring(1);

            object o;
            switch (t)
            {
                case 's':
                    o = v;
                    break;
                case 'i':
                    o = int.Parse(v);
                    break;
                case 'd':
                    o = decimal.Parse(v, NumberFormatInfo.InvariantInfo);
                    break;
                case 't':
                    o = DateTime.ParseExact(v, "o", DateTimeFormatInfo.InvariantInfo);
                    break;
                default:
                    throw new NotSupportedException("Invalid prefix \'" + t + "\'.");
            }

            content.SetValue(alias, o);
        }
    }
}
