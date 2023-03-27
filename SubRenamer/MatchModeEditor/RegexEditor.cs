using SubRenamer.Lib;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static SubRenamer.Global;

namespace SubRenamer.MatchModeEditor
{
    public partial class RegexEditor : Form
    {
        private readonly MainForm _mainForm;

        public RegexEditor(MainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
        }

        private void RegexEditor_Load(object sender, EventArgs e)
        {
            if (_mainForm.MRegxV != null)
                VideoRegex.Text = _mainForm.MRegxV.ToString();
            if (_mainForm.MRegxS != null)
                SubRegex.Text = _mainForm.MRegxS.ToString();

            V_Test_OpenBtn.Click += (_, _) =>
            {
                Utils.OpenFile(AppFileType.Video, opened: (fileName, fileType) =>
                {
                    V_TestStr.Text = fileName;
                    TestStrRematch(fileType);
                });
            };
            S_Test_OpenBtn.Click += (_, _) =>
            {
                Utils.OpenFile(AppFileType.Sub, opened: (fileName, fileType) =>
                {
                    S_TestStr.Text = fileName;
                    TestStrRematch(fileType);
                });
            };

            VideoRegex.TextChanged += (_, _) => TestStrRematch(AppFileType.Video, false);
            SubRegex.TextChanged += (_, _) => TestStrRematch(AppFileType.Sub, false);
            V_TestStr.TextChanged += (_, _) => TestStrRematch(AppFileType.Video, false);
            S_TestStr.TextChanged += (_, _) => TestStrRematch(AppFileType.Sub, false);
        }

        private Regex GetRegexInstance(AppFileType fileType, bool displayAlert = true)
        {
            var regxStr = fileType switch
            {
                AppFileType.Video => VideoRegex.Text,
                AppFileType.Sub => SubRegex.Text,
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(regxStr)) return null;

            Regex regex = null;
            try
            {
                regex = new Regex(regxStr.Trim());
            }
            catch (Exception ex)
            {
                if (displayAlert)
                {
                    var label = fileType switch
                    {
                        AppFileType.Video => "视频",
                        AppFileType.Sub => "字幕",
                        _ => ""
                    };
                    MessageBox.Show($@"{label} 正则语法有误 {ex.Message}{Environment.NewLine}{ex.StackTrace}", @"正则语法错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return regex;
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            _mainForm.MRegxV = GetRegexInstance(AppFileType.Video);
            _mainForm.MRegxS = GetRegexInstance(AppFileType.Sub);

            Close();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void TestStrRematch(AppFileType fileType, bool displayAlert = true)
        {
            var regex = GetRegexInstance(fileType, displayAlert);
            switch (fileType)
            {
                case AppFileType.Video:
                    V_TestResult.Text = MainForm.GetMatchKeyByRegex(V_TestStr.Text.Trim(), regex);
                    break;
                case AppFileType.Sub:
                    S_TestResult.Text = MainForm.GetMatchKeyByRegex(S_TestStr.Text.Trim(), regex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
            }
        }

        private void RegexTestLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://regexr.com/");
        }

        private void LearnRegexLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/ziishaned/learn-regex/blob/master/translations/README-cn.md");
        }
    }
}