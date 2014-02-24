using System;
using System.Configuration;

namespace ActiveFileBackup.Configuration
{
    public class BackupInterval : ConfigurationElement
    {
        public BackupInterval()
        {
        }

        [ConfigurationProperty("daily", DefaultValue = true)]
        public bool Daily
        {
            get { return (bool)this["daily"]; }
            set { this["daily"] = value; }
        }

        [ConfigurationProperty("weekly", DefaultValue = true)]
        public bool Weekly
        {
            get { return (bool)this["weekly"]; }
            set { this["weekly"] = value; }
        }

        [ConfigurationProperty("monthly", DefaultValue = true)]
        public bool Monthly
        {
            get { return (bool)this["monthly"]; }
            set { this["monthly"] = value; }
        }

        [ConfigurationProperty("yearly", DefaultValue = true)]
        public bool Yearly
        {
            get { return (bool)this["yearly"]; }
            set { this["yearly"] = value; }
        }
    }
}
