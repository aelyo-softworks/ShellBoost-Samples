using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace ShellBoost.Samples.CloudFolderSite
{
    [Serializable]
    public class WebFolderException : Exception
    {
        public const string Prefix = "CFS";

        public WebFolderException()
            : base(Prefix + "0001: Cloud Folder Site exception.")
        {
        }

        public WebFolderException(string message)
            : base(Prefix + message)
        {
        }

        public WebFolderException(string message, Exception innerException)
            : base(Prefix + message, innerException)
        {
        }

        public WebFolderException(Exception innerException)
            : base(null, innerException)
        {
        }

        protected WebFolderException(SerializationInfo info, StreamingContext context)
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
