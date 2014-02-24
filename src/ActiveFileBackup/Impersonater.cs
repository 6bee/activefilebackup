using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace ActiveFileBackup
{
    public static class Impersonater
    {
        // constants from winbase.h
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_LOGON_NETWORK = 3;
        private const int LOGON32_LOGON_BATCH = 4;
        private const int LOGON32_LOGON_SERVICE = 5;
        private const int LOGON32_LOGON_UNLOCK = 7;
        private const int LOGON32_LOGON_NETWORK_CLEARTEXT = 8;
        private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

        private const int LOGON32_PROVIDER_DEFAULT = 0;
        private const int LOGON32_PROVIDER_WINNT35 = 1;
        private const int LOGON32_PROVIDER_WINNT40 = 2;
        private const int LOGON32_PROVIDER_WINNT50 = 3;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int LogonUserA(string lpszUserName,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int DuplicateToken(IntPtr hToken,
            int impersonationLevel,
            ref IntPtr hNewToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool RevertToSelf();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool CloseHandle(IntPtr handle);

        public static WindowsImpersonationContext LogOn(string userName, SecureString password, string domain = "")
        {
            WindowsIdentity tempWindowsIdentity;
            WindowsImpersonationContext impersonationContext;
            IntPtr token = IntPtr.Zero;
            IntPtr tokenDuplicate = IntPtr.Zero;

            if (RevertToSelf())
            {
                if (LogonUserA(userName, domain, password.ToInsecureString(), LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_WINNT50, ref token) != 0)
                {
                    if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                    {
                        tempWindowsIdentity = new WindowsIdentity(tokenDuplicate);
                        impersonationContext = tempWindowsIdentity.Impersonate();
                        if (impersonationContext != null)
                        {
                            CloseHandle(token);
                            CloseHandle(tokenDuplicate);
                            return impersonationContext;
                        }
                    }
                }
                else
                {
                    var win32 = new Win32Exception(Marshal.GetLastWin32Error());
                    throw new Exception(win32.Message);
                }
            }
            if (token != IntPtr.Zero)
                CloseHandle(token);
            if (tokenDuplicate != IntPtr.Zero)
                CloseHandle(tokenDuplicate);
            return null; // Failed to impersonate
        }

        public static bool LogOff(WindowsImpersonationContext context)
        {
            var result = false;
            try
            {
                if (context != null)
                {
                    context.Undo();
                    result = true;
                }
            }
            catch
            {
            }
            return result;
        }
    }
}
