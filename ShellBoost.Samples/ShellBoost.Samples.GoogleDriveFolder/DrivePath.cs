using SqlNado;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // a database table that represent the mapping between a relative file path and a Google Drive file id.
    public class DrivePath
    {
        [SQLiteColumn(IsPrimaryKey = true)]
        public string RelativePath { get; set; }

        [SQLiteIndex("IDX_Id")]
        public string Id { get; set; }
    }
}
