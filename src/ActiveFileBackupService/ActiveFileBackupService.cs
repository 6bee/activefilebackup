using System;
using System.ServiceProcess;

namespace ActiveFileBackupService
{
    public partial class ActiveFileBackupService : ServiceBase
    {
        public ActiveFileBackupService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ActiveFileBackup.Program.Start();
        }

        protected override void OnStop()
        {
            ActiveFileBackup.Program.Start();
        }
    }
}
