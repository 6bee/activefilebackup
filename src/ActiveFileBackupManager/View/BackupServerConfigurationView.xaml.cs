using ActiveFileBackupManager.Extensions;
using ActiveFileBackupManager.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ActiveFileBackupManager.View
{
    /// <summary>
    /// Interaction logic for BackupServerConfigurationView.xaml
    /// </summary>
    public partial class BackupServerConfigurationView : UserControl
    {
        public BackupServerConfigurationView()
        {
            InitializeComponent();
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog(this.GetIWin32Window());
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ((BackupServerConfigurationViewModel)DataContext).Path = dialog.SelectedPath;
            }
        }
    }
}
