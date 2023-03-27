using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using static SubRenamer.Global;

namespace SubRenamer
{
    public partial class VsItemEditor : Form
    {
        private readonly MainForm _mainForm;
        private readonly List<VsItem> _vsList;
        private VsItem _vsItem;

        public VsItemEditor(MainForm mainForm, List<VsItem> vsList, VsItem vsItem)
        {
            _mainForm = mainForm;
            _vsList = vsList;
            _vsItem = vsItem;
            InitializeComponent();

            MainToolTip.SetToolTip(PrevItemBtn, "上一个项目");
            MainToolTip.SetToolTip(NextItemBtn, "下一个项目");
            MainToolTip.SetToolTip(AddItemBtn, "新增一个项目");
            MainToolTip.SetToolTip(RemoveItemBtn, "删除此项目");
            MainToolTip.SetToolTip(Video_ClearBtn, "删除视频");
            MainToolTip.SetToolTip(Video_SelectFileBtn, "选择新视频");
            MainToolTip.SetToolTip(Sub_ClearBtn, "删除字幕");
            MainToolTip.SetToolTip(Sub_SelectFileBtn, "选择新字幕");
        }

        private void VsItemEditor_Load(object sender, EventArgs e)
        {
            RefreshByVsItem();
        }

        private void RefreshByVsItem()
        {
            if (_vsItem == null) return;
            MatchKey_TextBox.Text = _vsItem.MatchKey ?? "";

            Video_TextBox.Text = _vsItem.Video ?? "";
            Video_TextBox.SelectionStart = Video_TextBox.Text.Length;

            Sub_TextBox.Text = _vsItem.Sub ?? "";
            Sub_TextBox.SelectionStart = Sub_TextBox.Text.Length;

            PageNum.Text = $@"{_vsList.IndexOf(_vsItem) + 1}/{_vsList.Count}";
        }

        private void MatchKey_TextBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MatchKey_TextBox.Text)) return;
            _vsItem.MatchKey = MatchKey_TextBox.Text.Trim();
            UpdateStatus();
        }

        private void Video_SelectFileBtn_Click(object sender, EventArgs e)
        {
            var filename = OpenFileSelectDialog(VideoExts);
            if (filename != null)
            {
                Video_TextBox.Text = filename;
                _vsItem.Video = !string.IsNullOrWhiteSpace(filename) ? filename : null;
                UpdateStatus();
            }

            Video_TextBox.SelectionStart = Video_TextBox.Text.Length;
        }

        private void Sub_SelectFileBtn_Click(object sender, EventArgs e)
        {
            var filename = OpenFileSelectDialog(SubExts);
            if (filename != null)
            {
                Sub_TextBox.Text = filename;
                _vsItem.Sub = !string.IsNullOrWhiteSpace(filename) ? filename : null;
                UpdateStatus();
            }

            Sub_TextBox.SelectionStart = Sub_TextBox.Text.Length;
        }

        private static string OpenFileSelectDialog(IEnumerable<string> exts)
        {
            using var fbd = new CommonOpenFileDialog();
            fbd.Filters.Add(new CommonFileDialogFilter("媒体文件", string.Join(";", exts)));
            var result = fbd.ShowDialog();

            if (result == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(fbd.FileName))
            {
                return fbd.FileName;
            }

            return null;
        }

        private void UpdateStatus()
        {
            if (string.IsNullOrWhiteSpace(_vsItem.MatchKey))
                _vsItem.Status = VsStatus.Unmatched;

            if (_vsItem.Video != null)
                _vsItem.Status = VsStatus.SubLack;

            if (_vsItem.Sub != null)
                _vsItem.Status = VsStatus.VideoLack;

            if (_vsItem.Video != null && _vsItem.Sub != null)
                _vsItem.Status = VsStatus.Ready;
        }

        private void Video_ClearBtn_Click(object sender, EventArgs e)
        {
            _vsItem.Video = null;
            Video_TextBox.Text = "";
        }

        private void Sub_ClearBtn_Click(object sender, EventArgs e)
        {
            _vsItem.Sub = null;
            Sub_TextBox.Text = "";
        }

        private void AddItemBtn_Click(object sender, EventArgs e)
        {
            CreateItem();
            _mainForm.RefreshFileListUi(removeNull: false);
        }

        private void CreateItem()
        {
            _vsItem = new VsItem();
            _vsList.Add(_vsItem);
            RefreshByVsItem();
        }

        private void RemoveItemBtn_Click(object sender, EventArgs e)
        {
            if (AppSettings.ListItemRemovePrompt)
            {
                var result = MessageBox.Show($@"你要删除当前编辑的项目吗？", @"删除编辑项", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.No) return;
            }

            var pos = _vsList.IndexOf(_vsItem);
            if (pos < 0) return;
            _vsList.Remove(_vsItem);
            if (_vsList.Count > 0)
            {
                _vsItem = pos < _vsList.Count ? _vsList[pos] : _vsList[pos - 1];
                RefreshByVsItem();
            }
            else
            {
                CreateItem();
            }

            _mainForm.RefreshFileListUi(removeNull: false);
        }

        private void PrevItemBtn_Click(object sender, EventArgs e)
        {
            var pos = _vsList.IndexOf(_vsItem) - 1;
            if (pos < 0) return;
            _mainForm.RefreshFileListUi(removeNull: false);
            _vsItem = _vsList[pos];
            RefreshByVsItem();
        }

        private void NextItemBtn_Click(object sender, EventArgs e)
        {
            var pos = _vsList.IndexOf(_vsItem) + 1;
            if (pos >= _vsList.Count) return;
            _mainForm.RefreshFileListUi(removeNull: false);
            _vsItem = _vsList[pos];
            RefreshByVsItem();
        }

        private void VsItemEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mainForm.RefreshFileListUi();
        }
    }
}