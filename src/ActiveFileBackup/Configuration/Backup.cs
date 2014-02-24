// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Configuration;
using System.Security;

namespace ActiveFileBackup.Configuration
{
    public class Backup : ConfigurationElement
    {
        public Backup()
        {
        }

        public Backup(string path)
        {
            Path = path;
        }

        public Backup(string path, string userName, SecureString password)
            : this(path)
        {
            Username = userName;
            Password = password;
        }

        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }

        [ConfigurationProperty("username", IsRequired = false)]
        public string Username
        {
            get { return (string)this["username"]; }
            set { this["username"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = false)]
        private string EncryptedPassword
        {
            get { return (string)this["password"]; }
            set { this["password"] = value; }
        }
        public SecureString Password
        {
            get { return EncryptedPassword.DecryptString(); }
            set { EncryptedPassword = value.EncryptString(); }
        }
    }
}
