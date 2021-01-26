using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolderSite
{
    public class WebFolderOptions
    {
        public WebFolderOptions()
        {
            // allow user to change these from command line (mostly for tests, kestrel, etc.)
            MaxChangesDays = CommandLine.GetArgument(nameof(MaxChangesDays), 7);
            MaxTempFilesDays = CommandLine.GetArgument(nameof(MaxTempFilesDays), 2);
        }

        public virtual int MaxChangesDays { get; set; }
        public virtual int MaxTempFilesDays { get; set; }
    }
}
