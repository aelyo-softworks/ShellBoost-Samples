namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class MoveOptions
    {
        public bool Copy { get; set; } // default is move only
        public bool Overwrite { get; set; }
        public bool EnsureUniqueName { get; set; }
    }
}
