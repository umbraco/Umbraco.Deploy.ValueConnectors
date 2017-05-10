using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Deploy.GridCellValueConnectors;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the grid editor.
    /// This value connector uses GridCellValueConnectors to manipulate data in the grid cells.
    /// </summary>
    public class GridValueConnector : IValueConnector
    {
        private readonly Lazy<IGridCellValueConnectorFactory> _gridCellValueConnectorFactory;
        private readonly IMediaService _mediaService;
        private readonly ILogger _logger;
        private static readonly Regex MediaUrlRegex = new Regex(@"url\((?:.+)?(?<url>/media/\d+/.+?)\)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public GridValueConnector(Lazy<IGridCellValueConnectorFactory> gridCellValueConnectorFactory, IMediaService mediaService, ILogger logger)
        {
            if (gridCellValueConnectorFactory == null) throw new ArgumentNullException(nameof(gridCellValueConnectorFactory));
            _gridCellValueConnectorFactory = gridCellValueConnectorFactory;
            _mediaService = mediaService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.GridAlias };

        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            var value = property.Value as string;

            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value.DetectIsJson() == false)
                return null;

            var grid = JsonConvert.DeserializeObject<GridValue>(value);

            if (grid == null)
                return null;

            foreach (var section in grid.Sections)
            {
                foreach (var row in section.Rows)
                {
                    // Rows can have styles and we need to resolve dependencies if any
                    if (row.Styles != null)
                    {
                        AddStylesDependencies(dependencies, row.Styles);
                    }
                    foreach (var area in row.Areas)
                    {
                        // Areas can also have and we also need to resolve dependencies if any
                        if (area.Styles != null)
                        {
                            AddStylesDependencies(dependencies, area.Styles);
                        }

                        foreach (var control in area.Controls)
                        {
                            if (control?.Editor?.Alias != null)
                            {
                                var connector = _gridCellValueConnectorFactory.Value.GetGridCellConnector(control.Editor.Alias);
                                control.Value = connector.GetValue(control, property, dependencies);
                            }
                        }
                    }
                }
            }
            value = JsonConvert.SerializeObject(grid);
            return value;
        }

        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                content.SetValue(alias, value);
                return;
            }

            if (value.DetectIsJson() == false)
                return;

            var grid = JsonConvert.DeserializeObject<GridValue>(value);

            if (grid == null)
                return;

            var property = content.Properties.FirstOrDefault(x => x.Alias == alias);

            foreach (var section in grid.Sections)
            {
                foreach (var row in section.Rows)
                {
                    foreach (var area in row.Areas)
                    {
                        foreach (var control in area.Controls)
                        {
                            var connector = _gridCellValueConnectorFactory.Value.GetGridCellConnector(control.Editor.Alias);
                            connector.SetValue(control, property);
                        }
                    }
                }
            }
            value = JObject.FromObject(grid).ToString(Formatting.Indented);
            content.SetValue(alias, value);
        }

        /// <summary>
        /// Adds media library dependencies from the styles object.
        /// Since this is just a URL stored with no id, we try to look it up in the media library and add it as a depencency if it exists.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="styles"></param>
        private void AddStylesDependencies(ICollection<ArtifactDependency> dependencies, JToken styles)
        {
            if (styles == null)
                throw new ArgumentNullException(nameof(styles), "Styles should not be null");

            foreach (var style in styles)
            {
                foreach (var styleItems in style.Children())
                {
                    var styleValue = styleItems.ToString();
                    var match = MediaUrlRegex.Match(styleValue);
                    if (match.Success)
                    {
                        var media = _mediaService.GetMediaByPath(match.Groups["url"].ToString());
                        if (media != null)
                        {
                            dependencies.Add(new ArtifactDependency(media.GetUdi(), false, ArtifactDependencyMode.Exist));
                        }
                        else
                        {
                            _logger.Info<GridValueConnector>($"Could not locate a media item in the media library with the URL: '{match.Groups["url"]}'");
                        }
                    }
                }
            }
        }
    }
}
