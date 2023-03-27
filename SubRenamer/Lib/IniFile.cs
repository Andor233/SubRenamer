using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SubRenamer.Lib
{
    public partial class IniFile
    {
        private readonly string _path;
        private static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;


        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value,
            string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string @default,
            StringBuilder retVal, int size, string filePath);

        public IniFile(string iniPath = null)
        {
            _path = new FileInfo(iniPath ?? AppName + ".ini").FullName.ToString();
        }

        public string Read(string key, string defaultVal = "", string section = null)
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section ?? AppName, key, defaultVal, retVal, 255, _path);
            return retVal.ToString();
        }

        public void Write(string key, string value, string section = null)
        {
            WritePrivateProfileString(section ?? AppName, key, value, _path);
        }

        public void DeleteKey(string key, string section = null)
        {
            Write(key, null, section ?? AppName);
        }

        public void DeleteSection(string section = null)
        {
            Write(null, null, section ?? AppName);
        }

        public bool KeyExists(string key, string section = null)
        {
            return Read(key, section).Length > 0;
        }
    }
}