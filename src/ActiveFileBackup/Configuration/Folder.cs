// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Configuration;

namespace ActiveFileBackup.Configuration
{
    public class Folder : ConfigurationElement
    {
        public Folder()
        {
        }

        public Folder(string path)
        {
            Path = path;
            Recursive = false;
        }

        public Folder(string path, bool recursive)
        {
            Path = path;
            Recursive = recursive;
        }

        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }

        [ConfigurationProperty("recursive", DefaultValue = false, IsRequired = true)]
        public bool Recursive
        {
            get { return (bool)this["recursive"]; }
            set { this["recursive"] = value; }
        }
    }
}
