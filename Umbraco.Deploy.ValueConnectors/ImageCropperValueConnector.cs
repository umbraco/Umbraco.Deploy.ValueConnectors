using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the image cropper editor.
    /// </summary>
    public class ImageCropperValueConnector : IValueConnector
    {
        private readonly IMediaService _mediaService;
        private readonly MediaFileSystem _mediaFileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadValueConnector"/> class.
        /// </summary>
        public ImageCropperValueConnector(IMediaService mediaService, MediaFileSystem mediaFileSystem)
        {
            if (mediaService == null) throw new ArgumentNullException(nameof(mediaService));
            if (mediaFileSystem == null) throw new ArgumentNullException(nameof(mediaFileSystem));
            _mediaService = mediaService;
            _mediaFileSystem = mediaFileSystem;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.ImageCropperAlias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            var value = property.Value;

            var svalue = value as string;

            var jvalue = svalue == null ? null : JObject.Parse(svalue);
            var src = jvalue?["src"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(src)) return svalue;

            // register the file and add the corresponding dependency
            var filepath = _mediaFileSystem.GetRelativePath(src);
            if (_mediaFileSystem.FileExists(filepath))
            {
                var file = new StringUdi(Constants.UdiEntityType.MediaFile, filepath);
                dependencies.Add(new ArtifactDependency(file, true, ArtifactDependencyMode.Match));
            }

            return svalue;
        }

        /// <inheritdoc/>
        public virtual void SetValue(IContentBase content, string alias, string value)
        {
            // get the current value
            var currentValue = content.GetValue<string>(alias);
            var currentPath = GetPathFromValue(currentValue);

            // set the value
            content.SetValue(alias, value);

            // if value has changed, delete the previous file
            // this assumes that the 'currentPath' CANNOT be reused by another upload
            // which should be the case if proper media folders are used
            var newPath = GetPathFromValue(value);
            if (currentPath != newPath && !string.IsNullOrWhiteSpace(currentValue))
                _mediaService.DeleteMediaFile(currentPath);
            
            // we do not need to deal with auto-fill properties as we are setting
            // all properties during deploy... and anyways FileUploadPropertyEditor
            // will populate them again when saving the content
        }

        private static string GetPathFromValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var jvalue = JObject.Parse(value);
            return jvalue?["src"]?.Value<string>();
        }
    }
}
