using System;
using System.Linq;
using System.Windows.Forms;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public partial class ChooseAccount : Form
    {
        public ChooseAccount()
        {
            InitializeComponent();
            Icon = Program.AppIcon;

            for (int i = 0; i < Settings.Current.Accounts.Count; i++)
            {
                var account = Settings.Current.Accounts[i];
                var item = listViewAccounts.Items.Add(account.UserEmailAddress);
                item.Tag = account;
                if (account.About != null)
                {
                    item.SubItems.Add(account.About.User.DisplayName);
                }
                else
                {
                    item.SubItems.Add(string.Empty);
                }

                item.SubItems.Add(account.DataDirectoryPath);
            }
        }

        public Account SelectedAccount => (Account)listViewAccounts.SelectedItems.Cast<ListViewItem>().FirstOrDefault()?.Tag;

        private void ListViewAccounts_SelectedIndexChanged(object sender, EventArgs e) => buttonOk.Enabled = listViewAccounts.SelectedItems.Count > 0;
    }
}
