using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace FeederCopy.App.Services.Updates
{
    internal static class WindowsDataProtection
    {
        private const int CryptProtectUiForbidden = 0x1;
        private const string SettingsPurpose = "FeederCopy.UpdateToken.v1";

        public static string Protect(string value, string purpose = SettingsPurpose)
        {
            if (value.Length == 0)
                return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(value);
            byte[]? protectedBytes = null;
            try
            {
                protectedBytes = Transform(
                    plainBytes,
                    Encoding.UTF8.GetBytes(purpose),
                    protect: true);
                return Convert.ToBase64String(protectedBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plainBytes);
                if (protectedBytes != null)
                    CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }

        public static string Unprotect(string value, string purpose = SettingsPurpose)
        {
            if (value.Length == 0)
                return string.Empty;

            byte[] protectedBytes;
            try
            {
                protectedBytes = Convert.FromBase64String(value);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "The saved secret is not in a valid protected format.", ex);
            }

            byte[]? plainBytes = null;
            try
            {
                plainBytes = Transform(
                    protectedBytes,
                    Encoding.UTF8.GetBytes(purpose),
                    protect: false);
                return Encoding.UTF8.GetString(plainBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
                if (plainBytes != null)
                    CryptographicOperations.ZeroMemory(plainBytes);
            }
        }

        private static byte[] Transform(byte[] input, byte[] entropy, bool protect)
        {
            DataBlob inputBlob = CreateBlob(input);
            DataBlob entropyBlob = CreateBlob(entropy);
            DataBlob outputBlob = default;

            try
            {
                bool success = protect
                    ? CryptProtectData(
                        ref inputBlob, null, ref entropyBlob,
                        IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out outputBlob)
                    : CryptUnprotectData(
                        ref inputBlob, IntPtr.Zero, ref entropyBlob,
                        IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out outputBlob);

                if (!success)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var result = new byte[outputBlob.Length];
                if (outputBlob.Length > 0)
                    Marshal.Copy(outputBlob.Data, result, 0, outputBlob.Length);
                return result;
            }
            finally
            {
                ClearAndFree(inputBlob, localAlloc: false);
                ClearAndFree(entropyBlob, localAlloc: false);
                ClearAndFree(outputBlob, localAlloc: true);
            }
        }

        private static DataBlob CreateBlob(byte[] value)
        {
            if (value.Length == 0)
                return default;
            IntPtr data = Marshal.AllocHGlobal(value.Length);
            Marshal.Copy(value, 0, data, value.Length);
            return new DataBlob { Length = value.Length, Data = data };
        }

        private static void ClearAndFree(DataBlob blob, bool localAlloc)
        {
            if (blob.Data == IntPtr.Zero)
                return;
            if (blob.Length > 0)
                Marshal.Copy(new byte[blob.Length], 0, blob.Data, blob.Length);
            if (localAlloc)
                LocalFree(blob.Data);
            else
                Marshal.FreeHGlobal(blob.Data);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int Length;
            public IntPtr Data;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptProtectData(
            ref DataBlob dataIn, string? description, ref DataBlob optionalEntropy,
            IntPtr reserved, IntPtr promptStruct, int flags, out DataBlob dataOut);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptUnprotectData(
            ref DataBlob dataIn, IntPtr description, ref DataBlob optionalEntropy,
            IntPtr reserved, IntPtr promptStruct, int flags, out DataBlob dataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);
    }
}
