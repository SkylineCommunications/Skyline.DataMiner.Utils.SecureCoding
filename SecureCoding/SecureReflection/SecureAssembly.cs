namespace Skyline.DataMiner.Utils.SecureCoding.SecureReflection
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using Skyline.DataMiner.Utils.SecureCoding.SecureIO;

    /// <summary>
    /// Provides methods to securely work with assemblies.
    /// </summary>
    public class SecureAssembly
    {
        private static Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

        /// <summary>
        /// Loads an assembly from the specified file path, ensuring the file exists, 
        /// the path is valid, and the assembly is signed. If any of these conditions 
        /// are not met, an appropriate exception is thrown. If the assembly is valid, 
        /// it is loaded and returned.
        /// </summary>
        /// <param name="assemblyPath">The full file path of the assembly to load.</param>
        /// <returns>
        /// An <see cref="Assembly"/> object representing the loaded assembly.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified file does not exist at the provided path.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the assembly path contains invalid characters.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Thrown when the assembly is unsigned and the attempt to load it is rejected.
        /// </exception>
        public static Assembly Load(string assemblyPath)
        {
            if (!System.IO.File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(assemblyPath);
            }

            if (!assemblyPath.IsPathValid())
            {
                throw new InvalidOperationException($"Assembly path '{assemblyPath}' contains invalid characters");
            }

            if (!IsSignedFile(assemblyPath))
            {
                throw new SecurityException("Attempt to load an unsigned assembly");
            }

            return Assembly.LoadFrom(assemblyPath);
        }

        private static bool IsSignedFile(string fileName)
        {
            var fileInfo = new WINTRUST_FILE_INFO(fileName);
            var trustData = new WINTRUST_DATA(fileInfo);

            IntPtr hWnd = IntPtr.Zero; // No UI
            uint result = WinVerifyTrust(hWnd, WINTRUST_ACTION_GENERIC_VERIFY_V2, trustData);

            return result == 0; // 0 means signature is valid
        }

        [DllImport("wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, WINTRUST_DATA pWVTData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WINTRUST_FILE_INFO
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
            public IntPtr pszFilePath;
            public IntPtr hFile = IntPtr.Zero;
            public IntPtr pgKnownSubject = IntPtr.Zero;

            public WINTRUST_FILE_INFO(string filePath)
            {
                pszFilePath = Marshal.StringToCoTaskMemAuto(filePath);
            }

            ~WINTRUST_FILE_INFO()
            {
                Marshal.FreeCoTaskMem(pszFilePath);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WINTRUST_DATA
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA));
            public IntPtr PolicyCallbackData = IntPtr.Zero;
            public IntPtr SIPClientData = IntPtr.Zero;
            public uint UIChoice = 2; // WTD_UI_NONE (No UI)
            public uint RevocationChecks = 0; // WTD_REVOKE_NONE
            public uint UnionChoice = 1; // WTD_CHOICE_FILE
            public IntPtr FileInfoPtr;
            public uint StateAction = 0; // WTD_STATEACTION_IGNORE
            public IntPtr StateData = IntPtr.Zero;
            public string URLReference = null;
            public uint ProvFlags = 0x00000040; // WTD_SAFER_FLAG
            public uint UIContext = 0;

            public WINTRUST_DATA(WINTRUST_FILE_INFO fileInfo)
            {
                FileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
                Marshal.StructureToPtr(fileInfo, FileInfoPtr, false);
            }

            ~WINTRUST_DATA()
            {
                Marshal.FreeCoTaskMem(FileInfoPtr);
            }
        }
    }
}