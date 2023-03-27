using SubRenamer.Lib;
using System.Runtime.CompilerServices;

namespace SubRenamer
{
    public abstract class AppSettings
    {
        public static bool RawSubtitleBackup
        {
            get => GetBoolVal(defaultVal: true);
            set => WriteBoolVal(value);
        }

        public static bool ListItemRemovePrompt
        {
            get => GetBoolVal(defaultVal: true);
            set => WriteBoolVal(value);
        }

        public static bool ListShowFileFullName
        {
            get => GetBoolVal(defaultVal: false);
            set => WriteBoolVal(value);
        }

        public static bool RenameVideo
        {
            get => GetBoolVal(defaultVal: false);
            set => WriteBoolVal(value);
        }

        #region Utils

        public static readonly IniFile IniFile = new();

        private static bool GetBoolVal(bool defaultVal = false, [CallerMemberName] string key = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return defaultVal;
            var defaultValStr = defaultVal ? "1" : "0";
            return IniFile.Read(key, defaultValStr).Equals("1");
        }

        private static void WriteBoolVal(bool val, [CallerMemberName] string key = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            IniFile.Write(key, val ? "1" : "0");
        }

        #endregion
    }
}