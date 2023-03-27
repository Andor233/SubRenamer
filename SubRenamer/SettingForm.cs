using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SubRenamer
{
    public partial class SettingForm : Form
    {
        private readonly MainForm _mainForm;

        public SettingForm(MainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            VersionText.Text = @"v" + Application.ProductVersion;

            RawSubtitleBuckup.Checked = AppSettings.RawSubtitleBackup;
            ListItemRemovePrompt.Checked = AppSettings.ListItemRemovePrompt;
            ListShowFileFullName.Checked = AppSettings.ListShowFileFullName;
            RenameVideo.Checked = AppSettings.RenameVideo;

            foreach (Control c in Controls) //遍历groupBox1内的所有控件
            {
                if (c is not CheckBox box) continue; //只遍历CheckBox控件
                box.CheckStateChanged += Setting_CheckedChanged;
            }
        }

        private static void Setting_CheckedChanged(object sender, EventArgs e)
        {
            var cb = (CheckBox)sender;
            AppSettings.IniFile.Write(cb.Name, cb.Checked ? "1" : "0");
        }

        private void UpdateLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
            Process.Start("https://github.com/qwqcode/SubRenamer/releases");

        private void GithubLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
            Process.Start("https://github.com/qwqcode/SubRenamer");

        private void AuthorLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
            Process.Start("https://github.com/qwqcode");

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) =>
            Process.Start("https://github.com/qwqcode/SubRenamer/issues/new");

        private void ListShowFileFullNameCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _mainForm.RefreshFileListUi();
        }

        private void BlogLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => Program.OpenAuthorBlog();

        private void RenameVideo_CheckedChanged(object sender, EventArgs e)
        {
            _mainForm.RefreshFileListUi();
        }
    }
}