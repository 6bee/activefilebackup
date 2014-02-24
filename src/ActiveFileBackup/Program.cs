// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace ActiveFileBackup
{
    public static class Program
    {
        private static readonly object _lock = new object();
        private static FileBackupManager _fileBackup;

        public static void Start()
        {
            lock (_lock)
            {
                if (_fileBackup == null)
                {
                    _fileBackup = FileBackupManager.Instance;
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                }
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                if (_fileBackup != null)
                {
                    try
                    {
                        _fileBackup.Dispose();
                    }
                    catch { }

                    _fileBackup = null;
                }
            }
        }
    }
}
