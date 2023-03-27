using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SubRenamer
{
    public abstract class Global
    {
        #region 常量

        public static readonly string LogFilename =
            Path.Combine(Application.StartupPath, $"{Program.GetAppName()}.log");

        public static readonly List<string> VideoExts = new List<string>
            { ".mkv", ".mp4", "flv", ".avi", ".mov", ".rmvb", ".wmv", ".mpg", ".avs" };

        public static readonly List<string> SubExts = new List<string> { ".srt", ".ass", ".ssa", ".sub", ".idx" };

        #endregion

        /// <summary>
        /// 文件类型
        /// </summary>
        public enum AppFileType
        {
            Video,
            Sub
        }
    }
}