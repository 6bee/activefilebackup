// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using ActiveFileBackup;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ActiveFileBackupService
{
    [RunInstaller(true)]
    public partial class ActiveFileBackupServiceInstaller : Installer
    {
        public const string DisplayName = EventLogger.Source;
        public const string ServiceName = "ActiveFileBackupService";

        public ActiveFileBackupServiceInstaller()
        {
            InitializeComponent();

            var serviceProcessInstaller = new ServiceProcessInstaller()
            {
                Account = ServiceAccount.LocalSystem,
                //Password = null,
                //Username = null,
            };

            var serviceInstaller = new ServiceInstaller()
            {
                StartType = ServiceStartMode.Automatic,
                DisplayName = DisplayName,
                ServiceName = ServiceName,
                Description = "Monitors file system according configuration and stores file backup upon file modification.",
            };

            Installers.AddRange(new Installer[] { serviceProcessInstaller, serviceInstaller });
        }
    }
}
