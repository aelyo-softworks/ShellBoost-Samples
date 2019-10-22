using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // misc utilities
    internal static class Extensions
    {
        public static void ShowMessage(this IWin32Window owner, string text) => MessageBox.Show(owner, text, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static DialogResult ShowConfirm(this IWin32Window owner, string text) => MessageBox.Show(owner, text, Application.ProductName + " - Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        public static void ShowError(this IWin32Window owner, string text) => MessageBox.Show(owner, text, Application.ProductName + " - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        
        public static bool IsCanceledTask(this Exception error)
        {
            if (error == null)
                return false;

            if (error is TaskCanceledException)
                return true;

            if (error is AggregateException agg)
                return agg.InnerExceptions.Any(e => IsCanceledTask(e));

            return IsCanceledTask(error.InnerException);
        }
    }
}
