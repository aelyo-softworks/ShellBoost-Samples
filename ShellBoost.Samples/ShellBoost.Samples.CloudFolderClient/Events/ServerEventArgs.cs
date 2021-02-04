using System;
using System.IO;
using System.Text;

namespace ShellBoost.Samples.CloudFolderClient.Events
{
    public class ServerEventArgs : EventArgs
    {
        public ServerEventArgs(Guid id, Guid itemId, Guid parentId, WatcherChangeTypes type, DateTime creationTimeUtc, string oldName, Guid? oldParentId)
        {
            EventId = id;
            Id = itemId;
            ParentId = parentId;
            Type = type;
            CreationTimeUtc = creationTimeUtc;
            OldName = oldName;
            OldParentId = oldParentId;
        }

        public Guid EventId { get; }
        public Guid Id { get; }
        public Guid ParentId { get; }
        public WatcherChangeTypes Type { get; }
        public DateTime CreationTimeUtc { get; }
        public string OldName { get; }
        public Guid? OldParentId { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Type);
            sb.Append(" Id: ");
            sb.Append(Id);
            sb.Append(" ParentId: ");
            sb.Append(ParentId);
            sb.Append(" CreationTimeUtc: ");
            sb.Append(CreationTimeUtc);

            if (!string.IsNullOrEmpty(OldName))
            {
                sb.Append(" OldName: ");
                sb.Append(OldName);
            }

            if (OldParentId.HasValue)
            {
                sb.Append(" OldParentId: ");
                sb.Append(OldParentId.Value);
            }
            return sb.ToString();
        }
    }
}
