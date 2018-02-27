using System;
using System.ComponentModel;

namespace ShellBoost.Samples.WebFolderSite.Model
{
    public class Item
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid ParentId { get; set; }
        public long Size { get; set; }
        public ItemType Type { get; set; }
    }
}