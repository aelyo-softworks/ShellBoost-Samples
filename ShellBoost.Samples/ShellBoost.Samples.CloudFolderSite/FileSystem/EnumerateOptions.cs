namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class EnumerateOptions
    {
        public EnumerateOptions()
        {
            IncludeFolders = true;
            IncludeFiles = true;
        }

        public bool IncludeFolders { get; set; }
        public bool IncludeFiles { get; set; }
        public bool IncludeHidden { get; set; }
    }
}
