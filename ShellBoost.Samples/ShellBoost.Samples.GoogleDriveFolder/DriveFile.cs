using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Drive.v3;
using ShellBoost.Core.Utilities;
using SqlNado;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // a database table that represent the a a Google Drive file.
    public class DriveFile
    {
        // https://developers.google.com/drive/api/v3/mime-types
        public const string FolderMimeType = "application/vnd.google-apps.folder";

        // https://support.google.com/drive/answer/6374270?visit_id=637072450166330725-2466926012&rd=1&hl=en
        // these mime type don't take size on Google drive, so they report a size of zero
        public static string[] GoogleDocMimeTypes = new string[] {
            "application/vnd.google-apps.document",
            "application/vnd.google-apps.drawing",
            "application/vnd.google-apps.file",
            "application/vnd.google-apps.form",
            "application/vnd.google-apps.fusiontable",
            "application/vnd.google-apps.map",
            "application/vnd.google-apps.presentation",
            "application/vnd.google-apps.script",
            "application/vnd.google-apps.site",
            "application/vnd.google-apps.spreadsheet"
        };

        [SQLiteColumn(IsPrimaryKey = true)]
        public string Id { get; set; }

        // note we consider only the first parent but Google Drive has many
        [SQLiteIndex("IDX_Parent")]
        public string ParentId { get; set; }

        [SQLiteIndex("IDX_Name")]
        public string Name { get; set; }

        [SQLiteIndex("IDX_Type")]
        public string MimeType { get; set; }

        public long Size { get; set; }
        public long Version { get; set; }
        public DateTime ModifiedTime { get; set; }
        public DateTime CreatedTime { get; set; }
        public string Owners { get; set; }
        public string LastModifyingUser { get; set; }
        public string WebViewLink { get; set; }
        public string WebContentLink { get; set; }
        public string FolderColorRgb { get; set; }

        [SQLiteColumn(Ignore = true)]
        public string FileName => IOUtilities.PathToValidFileName(Name);

        [SQLiteColumn(Ignore = true)]
        public bool IsDirectory => MimeType == FolderMimeType;

        [SQLiteColumn(Ignore = true)]
        public bool IsGoogleDoc => GoogleDocMimeTypes.Contains(MimeType);

        public override string ToString() => Name;

        // build an instance from Google Drive API
        public static DriveFile From(Google.Apis.Drive.v3.Data.File file)
        {
            if (file == null)
                return null;

            // since we use .url for handling google docs, we can't support it as is either.
            if (file.Name.EndsWith(FileSystem.UrlExt, StringComparison.OrdinalIgnoreCase))
                return null;

            // https://developers.google.com/drive/api/v3/reference/about
            // note this code presume the API has been called with all required fields
            var df = new DriveFile();
            df.Id = file.Id;
            df.Name = file.Name;
            df.ParentId = file.Parents?.FirstOrDefault();
            if (df.ParentId == null) // we only get files in a hierarchy
                return null;

            df.MimeType = file.MimeType;
            df.Version = file.Version.GetValueOrDefault();
            df.Size = file.Size.GetValueOrDefault();
            df.ModifiedTime = file.ModifiedTime.GetValueOrDefault();
            df.CreatedTime = file.CreatedTime.GetValueOrDefault();
            if (file.Owners != null && file.Owners.Count > 0)
            {
                df.Owners = string.Join("\0", file.Owners);
            }

            df.LastModifyingUser = file.LastModifyingUser?.DisplayName;
            df.WebViewLink = file.WebViewLink;
            df.WebContentLink = file.WebContentLink;
            df.FolderColorRgb = file.FolderColorRgb;
            return df;
        }
    }
}
