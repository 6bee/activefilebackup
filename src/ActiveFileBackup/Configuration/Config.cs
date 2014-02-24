// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using log4net;
using System;
using System.Configuration;

namespace ActiveFileBackup.Configuration
{
    public sealed class Config : ConfigurationSection
    {
        #region Fields

        private const string NAME = "activeFileBackup";
        private static readonly object _lock = new object();
        private static readonly ILog _log = LogManager.GetLogger(typeof(Config));
        private static Config _instance;
        private System.Configuration.Configuration _configuration;

        #endregion Fields

        #region Singleton

        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _log.Debug("create singleton instance");
                            var path = typeof(Config).Assembly.Location;
                            var configuration = ConfigurationManager.OpenExeConfiguration(path);
                            try
                            {
                                _instance = (Config)configuration.Sections[NAME]; // may return null
                                _instance._configuration = configuration;
                            }
                            catch
                            {
                                _instance = new Config();
                                configuration.Sections.Add(NAME, _instance);
                                configuration.Save(ConfigurationSaveMode.Full, false);
                                _instance._configuration = configuration;
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        private Config() { }

        #endregion Singleton

        #region Save config

        public void Save()
        {
            try
            {
                _configuration.Save(ConfigurationSaveMode.Full, false);
            }
            catch (Exception ex)
            {
                _log.Error("failed saving configuration", ex);
                throw;
            }
        }

        #endregion Save config

        #region Config Sections

        [ConfigurationProperty("backup")]
        public Backup Backup
        {
            get { return (Backup)this["backup"]; }
            set { this["backup"] = value; }
        }

        [ConfigurationProperty("numberOfBackupCopies")]
        public BackupSize NumberOfBackupCopies
        {
            get { return (BackupSize)this["numberOfBackupCopies"]; }
            set { this["numberOfBackupCopies"] = value; }
        }

        [ConfigurationProperty("backupPeriod")]
        public BackupInterval BackupPeriod
        {
            get { return (BackupInterval)this["backupPeriod"]; }
            set { this["backupPeriod"] = value; }
        }

        [ConfigurationProperty("folderList")]
        public Folders FolderList
        {
            get { return (Folders)this["folderList"]; }
            set { this["folderList"] = value; }
        }

        #endregion Config Sections
    }
}
