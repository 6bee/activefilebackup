// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using ActiveFileBackupManager.Commands;
using ActiveFileBackupService;
using log4net;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ActiveFileBackupManager.ViewModel
{
    public sealed class MainViewModel
    {
        private readonly static ILog _log = LogManager.GetLogger(typeof(MainViewModel));

        public MainViewModel()
        {
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            ResetCommand = new AsyncRelayCommand(InitializeAsync);

            DirectorySelectionViewModel = new DirectorySelectionViewModel();
            BackupServerConfigurationViewModel = new BackupServerConfigurationViewModel();

            InitializeAsync();
        }


        public ICommand SaveCommand { get; private set; }
        public ICommand ResetCommand { get; private set; }


        public DirectorySelectionViewModel DirectorySelectionViewModel { get; private set; }
        public BackupServerConfigurationViewModel BackupServerConfigurationViewModel { get; private set; }


        private async Task InitializeAsync()
        {
            var config = ActiveFileBackup.Configuration.Config.Instance;
            var t1 = DirectorySelectionViewModel.InitializeAsync(config);
            var t2 = BackupServerConfigurationViewModel.InitializeAsync(config);
            await Task.WhenAll(t1, t2);
        }

        private async Task SaveAsync()
        {
            await Task.Run(() =>
            {
                // update configuration
                var config = ActiveFileBackup.Configuration.Config.Instance;
                DirectorySelectionViewModel.UpdateConfig(config);
                BackupServerConfigurationViewModel.UpdateConfig(config);

                try
                {
                    config.Save();

                    // restart backup service
                    try
                    {
                        new ActiveFileBackupServiceController().Restart();
                    }
                    catch(Exception ex)
                    {
                        _log.Error("Failed restart service", ex);
                    }
                }
                catch(Exception ex)
                {
                    _log.Error("Failed saving configuration", ex);
                }
            });
        }
    }
}
