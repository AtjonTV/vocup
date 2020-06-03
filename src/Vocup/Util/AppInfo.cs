using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Vocup.Properties;

namespace Vocup.Util
{
    /// <summary>
    ///     Provides util methods for application metadata.
    /// </summary>
    public static class AppInfo
    {
        /// <summary>
        ///     Defines invalid characters for a path and thereby many strings in Vocup.
        /// </summary>
        public const string InvalidPathChars = "#=:\\/|<>*?\"";

        private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        /// <summary>
        ///     Gets the directory where custom special char files are stored.
        /// </summary>
        public static string SpecialCharDirectory { get; } = Path.Combine(Settings.Default.VhrPath, "specialchar");

        public static Version FileVersion => new Version(1, 0);

        public static string ProductName { get; }
            = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product;

        public static string CopyrightInfo { get; }
            = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

        public static Version GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        ///     Returns the product version of the currently running instance.
        /// </summary>
        /// <param name="length">Count of version numbers. Range from 1 to 4.</param>
        public static string GetVersion(int length)
        {
            if (length < 1 || length > 4)
                throw new ArgumentOutOfRangeException(nameof(length));
            var version = Application.ProductVersion;
            for (var i = 0; i < 4 - length; i++)
                version = version.Remove(version.LastIndexOf('.'));
            return version;
        }

        public static bool IsUwp()
        {
            if (SystemInfo.IsWindows10())
            {
                var length = 0;
                var sb = new StringBuilder(0);
                GetCurrentPackageFullName(ref length, sb);
                sb = new StringBuilder(length);
                var result = GetCurrentPackageFullName(ref length, sb);
                return result != APPMODEL_ERROR_NO_PACKAGE;
            }

            return false;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength,
            StringBuilder packageFullName);
    }
}