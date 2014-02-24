// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Configuration;

namespace ActiveFileBackup.Configuration
{
    public class BackupSize : ConfigurationElement
    {
        public BackupSize()
        {
        }

        //public Folder(string path, bool recursive)
        public BackupSize(int n)
        {
            Value = n;
        }

        [ConfigurationProperty("value", DefaultValue = 1, IsRequired = true)]
        public int Value
        {
            get
            {
                int n = (int)this["value"];
                if (n < 1) n = 1;
                else if (n > 99) n = 99;
                return n;
            }
            set
            {
                this["value"] = value;
            }
        }
    }
}
