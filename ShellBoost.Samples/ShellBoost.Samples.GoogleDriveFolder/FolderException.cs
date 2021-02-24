using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    [Serializable]
    public class FolderException : Exception
    {
        public const string Prefix = "GDF";

        public FolderException()
            : base(Prefix + "0001: Google Drive Folder exception.")
        {
        }

        public FolderException(string message)
            : base(message)
        {
        }

        public FolderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public FolderException(Exception innerException)
            : base(null, innerException)
        {
        }

        protected FolderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static int GetCode(string message)
        {
            if (message == null)
                return -1;

            if (!message.StartsWith(Prefix, StringComparison.Ordinal))
                return -1;

            var pos = message.IndexOf(':', Prefix.Length);
            if (pos < 0)
                return -1;

            if (int.TryParse(message.Substring(Prefix.Length, pos - Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int i))
                return i;

            return -1;
        }

        public int Code => GetCode(Message);
    }
}
