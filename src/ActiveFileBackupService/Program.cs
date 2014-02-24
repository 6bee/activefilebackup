// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using ActiveFileBackup;
using log4net;
using System;
using System.Collections;
using System.Configuration.Install;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;

namespace ActiveFileBackupService
{
    static class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));
        private static readonly Regex _console = new Regex(@"^[/-]c(onsole)?$", RegexOptions.IgnoreCase);
        private static readonly Regex _install = new Regex(@"^[/-]i(nstall)?$", RegexOptions.IgnoreCase);
        private static readonly Regex _uninstall = new Regex(@"^[/-]u(ninstall)?$", RegexOptions.IgnoreCase);
        private static readonly Regex _start = new Regex(@"^[/-]start$", RegexOptions.IgnoreCase);
        private static readonly Regex _stop = new Regex(@"^[/-]stop$", RegexOptions.IgnoreCase);
        private static readonly Regex _help = new Regex(@"^[/-](\?|h(elp)?)$", RegexOptions.IgnoreCase);
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(params string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    ServiceBase.Run(new ActiveFileBackupService());
                    break;

                case 1:
                    Execute(args[0]);
                    break;

                default:
                    PrintUsage();
                    break;
            }
        }

        private static void PrintUsage()
        {
            _log.Debug("print usage");
            var usage = new StringBuilder();
            usage.AppendLine(string.Format("{0} <args>", Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0])));
            usage.AppendLine();
            usage.AppendLine("  args:");
            usage.AppendLine("     -console      run service in console mode");
            usage.AppendLine("     -install      install windows service");
            usage.AppendLine("     -uninstall    uninstall windows service");
            usage.AppendLine("     -start        start windows service");
            usage.AppendLine("     -stop         stop windows service");
            usage.AppendLine("     -help         prints this help");
            usage.AppendLine();

            Console.WriteLine(usage);
        }

        private static void Execute(string command)
        {
            if (_help.IsMatch(command))
            {
                PrintUsage();
            }
            else if (_console.IsMatch(command))
            {
                _log.Debug("run service in console mode");
                try
                {
                    ActiveFileBackup.Program.Start();
                    Console.ReadLine();
                }
                finally
                {
                    ActiveFileBackup.Program.Stop();
                }
            }
            else if (_install.IsMatch(command))
            {
                _log.Debug("install service");
                var installer = new AssemblyInstaller
                {
                    Assembly = typeof(Program).Assembly,
                    UseNewContext = true,
                };
                var savedStates = new Hashtable();
                try
                {
                    installer.Install(savedStates);
                    installer.Commit(savedStates);
                }
                catch (Exception ex)
                {
                    installer.Rollback(savedStates);
                    _log.Error("Service installation failled", ex);
                }
                EventLogger.WriteEventLog("Installed " + EventLogger.Source);
            }
            else if (_uninstall.IsMatch(command))
            {
                _log.Debug("uninstall service");
                var installer = new AssemblyInstaller
                {
                    Assembly = typeof(Program).Assembly,
                    UseNewContext = true,
                };
                try
                {
                    var savedStates = new Hashtable();
                    installer.Uninstall(savedStates);
                }
                catch (Exception ex)
                {
                    _log.Error("Service installation failled", ex);
                }
                EventLogger.WriteEventLog("Uninstalled " + EventLogger.Source);
            }
            else if (_start.IsMatch(command))
            {
                _log.Debug("start service");
                new ActiveFileBackupServiceController().Start();
            }
            else if (_stop.IsMatch(command))
            {
                _log.Debug("stop service");
                new ActiveFileBackupServiceController().Stop();
            }
            else
            {
                Console.WriteLine("invalid arguments: {0}", command);
                PrintUsage();
            }
        }
    }
}
