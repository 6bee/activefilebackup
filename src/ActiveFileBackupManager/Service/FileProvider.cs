// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using ActiveFileBackup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace ActiveFileBackupManager.Service
{
    public static class FileProvider
    {
        public static Task<IEnumerable<DriveInfo>> GetDrivesAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                var fixedDrives =
                    from d in DriveInfo.GetDrives()
                    where d.DriveType == System.IO.DriveType.Fixed
                    select d;
                return fixedDrives.ToList().AsEnumerable();
            });
        }

        public static Task<IEnumerable<DirectoryInfo>> GetDirectoriesAsync(this DirectoryInfo directory)
        {
            directory.GetAccessControl();

            return Task.Factory.StartNew(() =>
            {
                //try
                //{
                var list =
                    from d in directory.GetDirectories()
                    where !FileBackupManager.IsIgnored(d.FullName)
                       && (d.Attributes & FileAttributes.Offline) == 0
                       && (d.Attributes & FileAttributes.System) == 0
                       && (d.Attributes & FileAttributes.ReadOnly) == 0
                       && (d.Attributes & FileAttributes.ReparsePoint) == 0
                    select d;
                return list.ToList().AsEnumerable();
                //}
                //catch (UnauthorizedAccessException)
                //{
                //    return (IEnumerable<DirectoryInfo>)new DirectoryInfo[0];
                //}
            });
        }
    }
}
