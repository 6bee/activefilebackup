using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Config = ActiveFileBackup.Configuration.Config;

namespace ActiveFileBackup
{
    public sealed class FileBackupManager : IDisposable
    {
        #region Fields

        private static readonly ILog _log = LogManager.GetLogger(typeof(FileBackupManager));
        private static readonly object _singletonLock = new object();
        private static readonly object _eventHandlerSyncLock = new object();
        private static FileBackupManager _singleton;
        private WindowsImpersonationContext _windowsImpersonationContext;
        private readonly string _backupDir;
        private readonly Dictionary<string, FileSystemWatcher> _fileSystemWatcherMap;
        private readonly Regex _invalidCharsRegex;
        private readonly string[] _fileExtension;
        private bool _disposed = false;

        // TODO: include in config
        // ommit 'RECYCLER', 'Recycled', 'System Volume Information'
        private static readonly string[] _ignoreList = new[]{
            @":\System Volume Information",
            @":\$RECYCLE.BIN",
            @":\RECYCLER",
            @":\Recycled",
        };

        #endregion Fields

        #region Singleton

        public static FileBackupManager Instance
        {
            get
            {
                if (_singleton == null)
                {
                    lock (_singletonLock)
                    {
                        if (_singleton == null)
                        {
                            _singleton = new FileBackupManager();
                        }
                    }
                }
                return _singleton;
            }
        }

        #endregion Singleton

        #region ctor

        private FileBackupManager()
        {
            _log.Trace();

            try
            {
                var config = Config.Instance;

                var backupUri = new Uri(config.Backup.Path);
                if (backupUri.IsUnc && !string.IsNullOrEmpty(config.Backup.Username))
                {
                    _log.InfoFormat("impersonate as user '{0}'", config.Backup.Username);
                    _windowsImpersonationContext = Impersonater.LogOn(config.Backup.Username, config.Backup.Password);
                }

                //create resources
                // file system watcher map
                _fileSystemWatcherMap = new Dictionary<string, FileSystemWatcher>();

                _invalidCharsRegex = new Regex(@"[\W-[\.]]", RegexOptions.Compiled);

                _fileExtension = new string[config.NumberOfBackupCopies.Value];
                for (int i = 0; i < _fileExtension.Length; i++)
                {
                    _fileExtension[i] = string.Format(".{0}.bak", (i > 9 ? i.ToString() : "0" + i.ToString()));
                }

                _backupDir = Path.Combine(config.Backup.Path, System.Net.Dns.GetHostName());
                //var writePermission = new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read | FileIOPermissionAccess.Append, _backupDir);
                //writePermission.Demand();
                Directory.CreateDirectory(_backupDir);

                //start jobs
                var activeWatcher = false;
                foreach (Configuration.Folder f in config.FolderList)
                {
                    activeWatcher |= CreateFileSystemWatcher(f.Path, f.Recursive);
                }

                if (activeWatcher)
                {
                    FileBackupJob.StartInitialBackup();
                }
            }
            catch (Exception ex)
            {
                _log.Fatal(string.Format("Failed to create file backup manager - {0}: {1}", ex.GetType().FullName, ex.Message), ex);
                ex.WriteEventLog();
                throw;
            }
        }

        #endregion ctor

        #region File system watcher
        
        // HELPER (WATCHER FACTORY)
        private bool CreateFileSystemWatcher(string path, bool recursive)
        {
            _log.Trace();

            CheckAccessToFolder(path);
            //var readPermission = new FileIOPermission(FileIOPermissionAccess.Read, AccessControlActions.View, path);
            //readPermission.Demand();

            if (!Directory.Exists(path))
            {
                var warnmessage = string.Format("Can't create file system watcher, path doesn't exist: {0}", path);
                _log.Warn(warnmessage);
                warnmessage.WriteEventLog(System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            // Create a new FileSystemWatcher and set its properties.
            var watcher = new FileSystemWatcher();
            _fileSystemWatcherMap.Add(path, watcher);

            watcher.Path = path;
            watcher.IncludeSubdirectories = recursive;

            // Watch for changes in LastWrite times, and 
            // the renaming of files or directories.
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            // log
            var message = string.Format("Watch path{1}: {0}", path, recursive ? " (recursive)" : null);
            _log.Info(message);
            message.WriteEventLog();

            return true;
        }

        // EVENT HANDLER
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            lock (_eventHandlerSyncLock)
            {
                _log.Trace();
                try
                {
                    // do not handle changes for type dirtectory
                    if (Directory.Exists(e.FullPath)) return;

                    // block ms office temp files
                    if (Path.GetFileName(e.FullPath).StartsWith("~$")) return;
                    if (IsIgnored(e.FullPath)) return;

                    _log.DebugFormat("File event {0}: '{1}'", e.ChangeType, e.FullPath);
                    FileBackupJob.Backup(e.FullPath);
                }
                catch (Exception ex)
                {
                    _log.Error(string.Format("File event {0}: '{1}'", e.ChangeType, e.FullPath), ex);
                    ex.WriteEventLog();
                }
            }
        }

        // EVENT HANDLER
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            lock (_eventHandlerSyncLock)
            {
                _log.Trace();
                try
                {
                    // block ms office temp files
                    if (!Path.GetFileName(e.OldFullPath).StartsWith("~") &&
                         Path.GetFileName(e.FullPath).StartsWith("~")) return;

                    _log.DebugFormat("File event {0}: '{1}' renamed to '{2}'", e.ChangeType, e.OldFullPath, e.FullPath);
                    FileBackupJob.Rename(e.OldFullPath, e.FullPath);
                }
                catch (Exception ex)
                {
                    _log.Error(string.Format("File event {0}: '{1}' renamed to '{2}'", e.ChangeType, e.OldFullPath, e.FullPath), ex);
                    ex.WriteEventLog();
                }
            }
        }

        public static bool IsIgnored(string path)
        {
            return _ignoreList.Any(x => path.Contains(x));
        }

        private static void CheckAccessToFolder(string folderPath)
        {
            try
            {
                // Attempt to get a list of security permissions from the folder. 
                // This will raise an exception if the path is read only or do not have access to view the permissions. 
                var directorySecurity = Directory.GetAccessControl(folderPath);
                //var accessRules = directorySecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));

                //var writeAllow = false;
                //var writeDeny = false;
                //foreach (FileSystemAccessRule rule in accessRules)
                //{
                //    if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                //        continue;

                //    if (rule.AccessControlType == AccessControlType.Allow)
                //        writeAllow = true;
                //    else if (rule.AccessControlType == AccessControlType.Deny)
                //        writeDeny = true;
                //}

                //if (!writeAllow || writeDeny)
                //{
                //    throw new UnauthorizedAccessException("missing write permission");
                //}
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException(string.Format("Unauthorized access {0}", folderPath), ex);
            }
            catch { }
        }

        #endregion File system watcher

        #region Backup Execution Job (inner class)

        private sealed class FileBackupJob : IDisposable
        {
            #region Fields

            // initial backup
            //private static Thread _initialBackupThread;
            private static bool _initialBackupStarted = false;

            // backup jops
            private static readonly Dictionary<string, FileBackupJob> _jobQueue = new Dictionary<string, FileBackupJob>();

            private static readonly object _queueLock = new object();
            private static readonly object _exeLock = new object();
            private static readonly long _delay = 5000; //ms

            private string _sourceFile;
            private string _destinationFile;

            private Timer _timer;

            #endregion Fields


            public static bool PendingJobs
            {
                get
                {
                    lock (_queueLock)
                    {
                        return _jobQueue.Count != 0;
                    }
                }
            }

            #region initial backup

            public static void StartInitialBackup()
            {
                if (!_initialBackupStarted)
                {
                    _initialBackupStarted = true;
                    Task.Factory.StartNew(RunInitialBackup, TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent);
                }
            }

            private static void RunInitialBackup()
            {
                var message = "Initial backup started";
                _log.Info(message);
                message.WriteEventLog();
                
                var job = new FileBackupJob();
                foreach (Configuration.Folder f in Config.Instance.FolderList)
                {
                    BackupDirectory(job, f.Path, f.Recursive);
                }

                message = "Initial backup finished";
                _log.Info(message);
                message.WriteEventLog();
            }

            private static void BackupDirectory(FileBackupJob job, string directory, bool recursive)
            {
                if (!Directory.Exists(directory)) return;
                if (IsIgnored(directory)) return;
                
                // backup files
                foreach (var file in Directory.GetFiles(directory))
                {
                    var dest = Path.Combine(Instance._backupDir, job.GetFileName(file));
                    try
                    {
                        job.BackupFile(file, dest);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(string.Format("Failure: <initial backup> from '{0}' to '{1}'", file, dest), ex);
                        ex.WriteEventLog();
                    }
                }

                // backup recursively
                if (recursive)
                {
                    foreach (var dir in Directory.GetDirectories(directory))
                    {
                        BackupDirectory(job, dir, recursive);
                    }
                }
            }

            #endregion initial backup

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Backup(string file)
            {
                _log.Trace();

                lock (_queueLock)
                {
                    FileBackupJob job;
                    if (!_jobQueue.TryGetValue(file, out job))
                    {
                        job = new FileBackupJob();
                        _jobQueue.Add(file, job);
                    }
                    job.StartBackup(file);
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void Rename(string oldFile, string newFile)
            {
                _log.Trace();

                lock (_queueLock)
                {
                    FileBackupJob job;
                    if (_jobQueue.ContainsKey(oldFile))
                    {
                        job = _jobQueue[oldFile];
                        _jobQueue.Remove(oldFile);
                        _jobQueue.Add(newFile, job);
                    }
                    else
                    {
                        job = new FileBackupJob();
                    }
                    job.StartRename(oldFile, newFile);
                }
            }


            [MethodImpl(MethodImplOptions.Synchronized)]
            private void StartBackup(string file)
            {
                _log.Trace();

                _sourceFile = file;
                _destinationFile = Path.Combine(Instance._backupDir, GetFileName(file));

                // no pending backup job
                if (_timer == null)
                {
                    _timer = new Timer(new TimerCallback(Execute), null, _delay, Timeout.Infinite);
                }
                // pending backup job
                else
                {
                    _timer.Change(_delay, Timeout.Infinite);
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            private void StartRename(string oldFile, string newFile)
            {
                _log.Trace();

                var src = Path.Combine(Instance._backupDir, GetFileName(oldFile));
                var dest = Path.Combine(Instance._backupDir, GetFileName(newFile));

                lock (_exeLock)
                {
                    try
                    {
                        RenameFile(src, dest);

                        // pending backup job
                        if (_timer != null)
                        {
                            _sourceFile = newFile;
                            _destinationFile = dest;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(string.Format("Unknown Failure: <rename> from '{0}' to '{1}'", src, dest), ex);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            private void Execute(object o)
            {
                _log.Trace();

                lock (_queueLock)
                {
                    lock (_exeLock)
                    {
                        try
                        {
                            BackupFile(_sourceFile, _destinationFile);
                        }
                        catch (Exception ex)
                        {
                            _log.Warn(string.Format("Unknown Failure: <backup> from '{0}' to '{1}'", _sourceFile, _destinationFile), ex);
                        }

                        Dispose();
                    }
                }
            }


            // HELPER (CREATE BACKUP FILE NAME)
            private string GetFileName(string filePath)
            {
                _log.Trace();

                var pathRoot = Path.GetPathRoot(filePath);
                if (filePath != null && filePath.Length > pathRoot.Length)
                {
                    filePath = filePath.Remove(0, pathRoot.Length);
                    _log.DebugFormat("path root: {0}", pathRoot);
                    var pathRootType = "";
                    try
                    {
                        pathRootType = new DriveInfo(pathRoot).DriveType.ToString() + "_";
                    }
                    catch { }
                    filePath = Path.Combine(pathRootType + Instance._invalidCharsRegex.Replace(pathRoot, ""), filePath);
                    _log.DebugFormat("File name: {0}", filePath);
                }
                return filePath;
            }

            // RENAME
            private void RenameFile(string srcFile, string destFile)
            {
                _log.Trace();

                if (Directory.Exists(srcFile))
                {
                    _log.InfoFormat("Rename directory from '{0}' to '{1}'", srcFile, destFile);
                    if (Directory.Exists(destFile)) Directory.Delete(destFile, true);
                    Directory.Move(srcFile, destFile);
                }
                else
                {
                    _log.InfoFormat("Rename file from '{0}' to '{1}'", srcFile, destFile);
                    foreach (var ext in Instance._fileExtension)
                    {
                        var src = srcFile + ext;
                        if (File.Exists(src))
                        {
                            var dest = destFile + ext;

                            //if (File.Exists(dest)) File.Delete(dest);
                            //// rename file but keep backup time
                            //DateTime backupTime = File.GetLastWriteTimeUtc(src);
                            //File.Move(src, dest);
                            //File.SetLastWriteTimeUtc(dest, backupTime);

                            MoveFile(src, dest);
                        }
                    }
                    foreach (var ext in new[] { ".y0.bak", ".m0.bak", ".w0.bak", ".d0.bak" })
                    {
                        string src = srcFile + ext;
                        if (File.Exists(src))
                        {
                            MoveFile(src, destFile + ext);
                        }
                    }
                }
            }

            private void MoveFile(string src, string dest)
            {
                if (File.Exists(dest)) File.Delete(dest);
                // rename file but keep backup time
                var backupTime = File.GetLastWriteTimeUtc(src);
                try
                {
                    File.Move(src, dest);
                }
                catch (IOException ex)
                {
                    _log.Error(string.Format("Backup Failure: File Move from '{0}' to '{1}'", src, dest), ex);
                }
                //File.SetLastWriteTimeUtc(dest, backupTime);
            }

            // BACK UP
            private void BackupFile(string srcFile, string destFile)
            {
                _log.Trace();

                if (!File.Exists(srcFile)) return;

                var dest = destFile + Instance._fileExtension[0];
                if (File.Exists(dest) &&
                    File.GetLastWriteTimeUtc(srcFile) <= File.GetLastWriteTimeUtc(dest)) return;

                _log.InfoFormat("Backup file from '{0}' to '{1}'", srcFile, destFile);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                    ShiftBackupFiles(destFile);

                    // backup file
                    File.Copy(srcFile, dest, true);
                    File.SetAttributes(dest, FileAttributes.Normal);
                }
                catch (IOException ex)
                {
                    _log.Error(string.Format("Backup failure: file copy from '{0}' to '{1}'", srcFile, dest), ex);
                }
                // set backup time
                //File.SetLastWriteTimeUtc(dest, DateTime.Now);
            }

            // HELPER (SHIFT BACKUP HISTORY)
            private void ShiftBackupFiles(string file)
            {
                _log.Trace();

                // shift concurrent backup
                if (File.Exists(file + Instance._fileExtension[Instance._fileExtension.Length - 1]))
                {
                    //File.Delete(file + Instance.fileExtension[Instance.fileExtension.Length - 1]);
                    MoveFileBackupPeriod(file, Instance._fileExtension[Instance._fileExtension.Length - 1]);
                }

                for (int i = Instance._fileExtension.Length - 1; i > 0; i--)
                {
                    var src = file + Instance._fileExtension[i - 1];
                    if (File.Exists(src))
                    {
                        var dest = file + Instance._fileExtension[i];

                        // rename file but keep backup time
                        var backupTime = File.GetLastWriteTimeUtc(src);
                        File.Move(src, dest);
                        File.SetLastWriteTimeUtc(dest, backupTime);
                    }
                }
            }


            #region move file in backup period

            private void MoveFileBackupPeriod(string file, string ext)
            {
                var path = file + ext;

                var ageInDays = (DateTime.Now - File.GetLastWriteTimeUtc(path)).Days;

                if (ageInDays > 30)
                {
                    MoveFileToYear(path, file);
                }
                else if (ageInDays > 7)
                {
                    MoveFileToMonth(path, file);
                }
                else if (ageInDays > 1)
                {
                    MoveFileToWeek(path, file);
                }
                else
                {
                    MoveFileToDay(path, file);
                }
            }

            private void MoveFileToYear(string src, string dest)
            {
                var destFile = dest + ".y0.bak";
                if (src == destFile) return;

                if (Config.Instance.BackupPeriod.Yearly)
                {
                    if (File.Exists(destFile))
                    {
                        if ((DateTime.Now - File.GetLastWriteTimeUtc(destFile)).Days <= 365)
                        {
                            return;
                        }
                        else
                        {
                            MoveFileBackupPeriod(dest, ".y0.bak");
                        }
                    }

                    MoveFile(src, destFile);
                    DeleteMonthlyBackup(dest);
                }
                else
                {
                    MoveFileToMonth(src, dest);
                }
            }

            private void MoveFileToMonth(string src, string dest)
            {
                var destFile = dest + ".m0.bak";
                if (src == destFile) return;

                if (Config.Instance.BackupPeriod.Monthly)
                {
                    if (File.Exists(destFile))
                    {
                        if ((DateTime.Now - File.GetLastWriteTimeUtc(destFile)).Days <= 30)
                        {
                            return;
                        }
                        else
                        {
                            MoveFileBackupPeriod(dest, ".m0.bak");
                        }
                    }

                    MoveFile(src, destFile);
                    DeleteWeeklyBackup(dest);
                }
                else
                {
                    MoveFileToWeek(src, dest);
                }
            }

            private void MoveFileToWeek(string src, string dest)
            {
                var destFile = dest + ".w0.bak";
                if (src == destFile) return;

                if (Config.Instance.BackupPeriod.Weekly)
                {
                    if (File.Exists(destFile))
                    {
                        if ((DateTime.Now - File.GetLastWriteTimeUtc(destFile)).Days <= 7)
                        {
                            return;
                        }
                        else
                        {
                            MoveFileBackupPeriod(dest, ".w0.bak");
                        }
                    }

                    MoveFile(src, destFile);
                    DeleteDailyBackup(dest);
                }
                else
                {
                    MoveFileToDay(src, dest);
                }
            }

            private void MoveFileToDay(string src, string dest)
            {
                var destFile = dest + ".d0.bak";
                if (src == destFile) return;

                if (Config.Instance.BackupPeriod.Daily)
                {
                    if (File.Exists(destFile))
                    {
                        if ((DateTime.Now - File.GetLastWriteTimeUtc(destFile)).Days <= 1)
                        {
                            return;
                        }
                        else
                        {
                            MoveFileBackupPeriod(dest, ".d0.bak");
                        }
                    }

                    MoveFile(src, destFile);
                }
                else
                {
                    File.Delete(src);
                }
            }

            private void DeleteMonthlyBackup(string file)
            {
                var monthlyBackupFile = file + ".m0.bak";
                if (File.Exists(monthlyBackupFile)) File.Delete(monthlyBackupFile);
                DeleteWeeklyBackup(file);
            }

            private void DeleteWeeklyBackup(string file)
            {
                var weeklyBackupFilePath = file + ".w0.bak";
                if (File.Exists(weeklyBackupFilePath)) File.Delete(weeklyBackupFilePath);
                DeleteDailyBackup(file);
            }

            private void DeleteDailyBackup(string file)
            {
                var dailyBackupFile = file + ".d0.bak";
                if (File.Exists(dailyBackupFile)) File.Delete(dailyBackupFile);
            }

            #endregion move file in backup period


            #region IDisposable

            [MethodImpl(MethodImplOptions.Synchronized)]
            public void Dispose()
            {
                _log.Trace();

                lock (_queueLock)
                {
                    if (_timer != null)
                    {
                        try
                        {
                            _timer.Dispose();
                        }
                        catch { }
                        _timer = null;
                    }

                    _jobQueue.Remove(_sourceFile);
                }
            }

            #endregion IDisposable
        }

        #endregion Backup Execution Job

        #region IDisposable

        ~FileBackupManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // STOP
                    foreach (FileSystemWatcher fsw in _fileSystemWatcherMap.Values)
                    {
                        //fsw.EnableRaisingEvents = false;
                        fsw.Dispose();
                    }
                    _fileSystemWatcherMap.Clear();

                    // wait on pending jobs... for a maximum of 10 seconds
                    DateTime startTime = DateTime.Now;
                    TimeSpan timeout = new TimeSpan(0, 0, 10);
                    while (FileBackupJob.PendingJobs && (DateTime.Now - startTime) < timeout)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                        catch { }
                    }

                    //if (backupIdentity != null)
                    //{
                    //    backupIdentity.Dispose();
                    //    backupIdentity = null;
                    //}

                    if (_windowsImpersonationContext != null)
                    {
                        Impersonater.LogOff(_windowsImpersonationContext);
                        _windowsImpersonationContext.Dispose();
                        _windowsImpersonationContext = null;
                    }
                }

                FileBackupManager._singleton = null;
                _disposed = true;
            }
        }

        #endregion IDisposable
    }
}
