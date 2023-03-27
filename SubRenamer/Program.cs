using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;

namespace SubRenamer
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        private static void Main()
        {
#if !DEBUG
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif

            Application.ApplicationExit += Application_ApplicationExit;

            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.Run(new MainForm());
        }

        private static int _exited;

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref _exited) != 1) return;
            var errMsg = $"异常细节: {Environment.NewLine}{e.Exception}";
            ErrorCatchAction("UI Error", errMsg);
            // Application.Exit();
        }

        private static void ErrorCatchAction(string type, string errorMsg)
        {
            var title = $"意外错误：{Application.ProductName} {"v" + Application.ProductVersion}";
            Process.Start(
                $"https://github.com/qwqcode/SubRenamer/issues/new?title={HttpUtility.UrlEncode(title, Encoding.UTF8)}&body={HttpUtility.UrlEncode(type + "\n" + errorMsg, Encoding.UTF8)}");
            MessageBox.Show(
                $@"{title} 程序即将退出，请发起 issue 来反馈，谢谢 {Environment.NewLine}{errorMsg}",
                $@"{Application.ProductName} {type}", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            // detach static event handlers
            Application.ApplicationExit -= Application_ApplicationExit;
            Application.ThreadException -= Application_ThreadException;
        }

        public static string GetVersionStr()
        {
            return "v" + Application.ProductVersion;
        }

        public static void OpenAuthorBlog()
        {
            Process.Start("https://qwqaq.com/?from=SubRenamer");
        }

        public static string GetAppName() => Assembly.GetExecutingAssembly().GetName().Name;

        public static string GetNowDatetime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public static void Log(params string[] textArr)
        {
            File.AppendAllText(Global.LogFilename,
                $"[{Program.GetNowDatetime()}]{string.Join("", textArr)}{Environment.NewLine}");
        }
    }
}