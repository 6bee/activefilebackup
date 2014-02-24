using ActiveFileBackupManager.Model;
using System.Security;
using System.Threading.Tasks;

namespace ActiveFileBackupManager.ViewModel
{
    public sealed class BackupServerConfigurationViewModel : Observable
    {
        public string Path
        {
            get { return _path; }
            set { _path = value; OnPropertyChanged(() => Path); }
        }
        private string _path;

        public string Username
        {
            get { return _username; }
            set { _username = value; OnPropertyChanged(() => Username); }
        }
        private string _username;

        public SecureString Password
        {
            get { return _password; }
            set { _password = value; OnPropertyChanged(() => Password); }
        }
        private SecureString _password;


        public async Task InitializeAsync(ActiveFileBackup.Configuration.Config config)
        {
            Path = config.Backup.Path;
            Username = config.Backup.Username;
            Password = config.Backup.Password;

            await Task.Yield();
        }

        public void UpdateConfig(ActiveFileBackup.Configuration.Config config)
        {
            config.Backup.Path = Path;
            config.Backup.Username = Username;
            config.Backup.Password = Password;
        }
    }
}
