using System;
using System.ServiceProcess;

namespace ActiveFileBackupService
{
    public sealed class ActiveFileBackupServiceController : ServiceController
    {
        public ActiveFileBackupServiceController()
            : base(ActiveFileBackupServiceInstaller.ServiceName)
        {
        }

        public new void Stop()
        {
            if (CanStop) base.Stop();
        }

        public new void Start()
        {
            if (Status != ServiceControllerStatus.Running)
            {
                base.Start();
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }
    }
}
