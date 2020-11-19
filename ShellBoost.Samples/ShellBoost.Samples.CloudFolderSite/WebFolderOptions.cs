namespace ShellBoost.Samples.CloudFolderSite
{
    public class WebFolderOptions
    {
        public WebFolderOptions()
        {
            MaxChangesDays = 7;
        }

        public virtual int MaxChangesDays { get; set; }
    }
}
