using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

public static class SecureIt
{
    private static readonly byte[] entropy = Encoding.Unicode.GetBytes("Salt Is Not A Password");

    public static string EncryptString(this SecureString input)
    {
        if (input == null) return null;
        if (input.Length == 0) return string.Empty;

        var encryptedData = ProtectedData.Protect(
            Encoding.Unicode.GetBytes(input.ToInsecureString()),
            entropy,
            DataProtectionScope.LocalMachine);

        return Convert.ToBase64String(encryptedData);
    }

    public static SecureString DecryptString(this string encryptedData)
    {
        if (encryptedData == null) return null;
        if (encryptedData.Length == 0)
        {
            var secureString = new SecureString();
            secureString.MakeReadOnly();
            return secureString;
        }

        var decryptedData = ProtectedData.Unprotect(
            Convert.FromBase64String(encryptedData),
            entropy,
            DataProtectionScope.LocalMachine);

        return Encoding.Unicode.GetString(decryptedData).ToSecureString();
    }

    public static SecureString ToSecureString(this IEnumerable<char> input)
    {
        if (input == null) return null;

        var secure = new SecureString();
        foreach (var c in input)
        {
            secure.AppendChar(c);
        }
        secure.MakeReadOnly();
        return secure;
    }

    public static string ToInsecureString(this SecureString input)
    {
        if (input == null) return null;
        if (input.Length == 0) return string.Empty;

        var ptr = Marshal.SecureStringToBSTR(input);
        try
        {
            return Marshal.PtrToStringBSTR(ptr);
        }
        finally
        {
            Marshal.ZeroFreeBSTR(ptr);
        }
    }

    public static bool IsNullOrEmpty(this SecureString input)
    {
        return input == null || input.Length == 0;
    }
}