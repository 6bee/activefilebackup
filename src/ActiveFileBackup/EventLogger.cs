// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace ActiveFileBackup
{
    public static class EventLogger
    {
        public const string Source = "Active File Backup Service";

        static EventLogger()
        {
            if (!EventLog.SourceExists(Source))
                EventLog.CreateEventSource(Source, "Application");
        }

        public static void WriteEventLog(this string message, EventLogEntryType eventLogEntryType = EventLogEntryType.Information)
        {
            EventLog.WriteEntry(Source, message, eventLogEntryType);
        }

        public static void WriteEventLog(this string message, Exception ex, EventLogEntryType eventLogEntryType = EventLogEntryType.Error)
        {
            var text = string.Format("{1}{0}{2}{0}{0}{3}",
                Environment.NewLine,
                message,
                ex.Message,
                ex.ToString());
            EventLog.WriteEntry(Source, text, eventLogEntryType);
        }

        public static void WriteEventLog(this Exception ex, EventLogEntryType eventLogEntryType = EventLogEntryType.Error)
        {
            var message = string.Format("{1}{0}{0}{2}",
                Environment.NewLine,
                ex.Message,
                ex.ToString());
            EventLog.WriteEntry(Source, message, eventLogEntryType);
        }
    }
}
