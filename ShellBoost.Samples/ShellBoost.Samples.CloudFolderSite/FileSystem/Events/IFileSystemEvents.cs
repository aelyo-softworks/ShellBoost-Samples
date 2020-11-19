namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Events
{
    public interface IFileSystemEvents
    {
        void Change(IFileSystemEvent @event);
    }
}
