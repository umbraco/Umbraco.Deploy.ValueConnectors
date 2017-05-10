using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
    /// <summary>
    /// Implements a value connector for the file upload editor.
    /// </summary>
    public class FileUploadValueConnector : IValueConnector
    {
        private readonly IMediaService _mediaService;
        private readonly MediaFileSystem _mediaFileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadValueConnector"/> class.
        /// </summary>
        /// <param name="mediaService">An <see cref="IMediaService"/> implementation.</param>
        /// <param name="mediaFileSystem">The media filesystem.</param>
        public FileUploadValueConnector(IMediaService mediaService, MediaFileSystem mediaFileSystem)
        {
            if (mediaService == null) throw new ArgumentNullException(nameof(mediaService));
            if (mediaFileSystem == null) throw new ArgumentNullException(nameof(mediaFileSystem));
            _mediaService = mediaService;
            _mediaFileSystem = mediaFileSystem;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { Constants.PropertyEditors.UploadFieldAlias };

        /// <inheritdoc/>
        public virtual string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
            var value = property.Value;

            // if we are not referencing a file, return
            var svalue = value as string;
            if (string.IsNullOrWhiteSpace(svalue)) return svalue;

            // ensure we have only one file
            // we *could* support exporting but we don't know how to import at the moment (see SetValue)
            if (svalue.Contains(',') || svalue.Contains(';'))
                throw new NotSupportedException("Deploy does not support FileUpload properties with more than one file.");

            // register the file and add the corresponding dependency
            var filepath = _mediaFileSystem.GetRelativePath(svalue);
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
            var currentPath = content.GetValue<string>(alias);

            // set the value
            content.SetValue(alias, value);

            // if value has changed, delete the previous file
            // this assumes that the 'currentPath' CANNOT be reused by another upload
            // which should be the case if proper media folders are used
            var newPath = value;
            if (currentPath != newPath && !string.IsNullOrWhiteSpace(currentPath))
                _mediaService.DeleteMediaFile(currentPath);

            // we do not need to deal with auto-fill properties as we are setting
            // all properties during deploy... and anyways FileUploadPropertyEditor
            // will populate them again when saving the content
        }
    }
}
