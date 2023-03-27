using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using static SubRenamer.Global;

namespace SubRenamer
{
    public partial class MainForm : Form
    {
        private static SettingForm _settingForm;

        public MainForm()
        {
            InitializeComponent();

            UpdateWinTitle();
            _settingForm = new SettingForm(this); // 设置窗体

            InitShortcut(); // 初始化快捷键
        }

        // 窗口加载完后
        private void MainForm_Load(object sender, EventArgs e)
        {
            SetWindowTheme(FileListUi.Handle, "Explorer", null);
        }

        private void UpdateWinTitle()
        {
            Text = !AppSettings.RenameVideo ? "字幕文件批量改名 (SubRenamer)" : "视频文件批量改名 (VideoRenamer)";

            Text += $@" {Program.GetVersionStr()}"; // 追加版本号
        }

        #region 各种操作

        // 导入 文件
        private void OpenFile()
        {
            using var fbd = new CommonOpenFileDialog
            {
                Multiselect = true,
            };
            fbd.Filters.Add(new CommonFileDialogFilter("视频或字幕文件",
                string.Join(";", VideoExts.Concat(SubExts).ToList())));
            var result = fbd.ShowDialog();

            if (result != CommonFileDialogResult.Ok || !fbd.FileNames.Any()) return;
            SwitchPreview(false);

            foreach (var fileName in fbd.FileNames) FileListAdd(new FileInfo(fileName));

            MatchVideoSub();
        }

        // 导入 文件夹
        private void OpenFolder()
        {
            using var fbd = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Multiselect = true
            };
            var result = fbd.ShowDialog();

            if (result != CommonFileDialogResult.Ok || !fbd.FileNames.Any()) return;
            SwitchPreview(false);

            foreach (var folderPath in fbd.FileNames)
            {
                var folder = new DirectoryInfo(folderPath);
                var files = folder.GetFiles("*");

                // 添加所有 视频/字幕 文件
                foreach (var file in files) FileListAdd(file);
            }

            MatchVideoSub();
        }

        // 文件添加
        private void FileListAdd(FileSystemInfo file)
        {
            AppFileType fileType;
            if (VideoExts.Contains(file.Extension.ToLower()))
                fileType = AppFileType.Video;
            else if (SubExts.Contains(file.Extension.ToLower()))
                fileType = AppFileType.Sub;
            else return;

            var vsItem = new VsItem();
            switch (fileType)
            {
                case AppFileType.Video when _vsList.Exists(o => o.Video == file.FullName):
                    return; // 重名排除
                case AppFileType.Video:
                    vsItem.Video = file.FullName;
                    break;
                case AppFileType.Sub when _vsList.Exists(o => o.Sub == file.FullName):
                    return;
                case AppFileType.Sub:
                    vsItem.Sub = file.FullName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            vsItem.Status = VsStatus.Unmatched;
            _vsList.Add(vsItem);
        }

        // 全选操作
        private void SelectListAll()
        {
            foreach (ListViewItem item in FileListUi.Items)
                item.Selected = true;
        }

        // 重新匹配
        private void ReMatch()
        {
            MatchVideoSub();
        }

        // 打开 VsItem 编辑器
        private void OpenVsItemEditor(VsItem vsItem)
        {
            var form = new VsItemEditor(this, _vsList, vsItem);
            form.ShowDialog();
        }

        // 打开规则编辑器
        private void OpenRuleEditor()
        {
            var form = new RuleEditor(this);
            form.ShowDialog();
        }

        private void EditListSelectedItems()
        {
            if (FileListUi.SelectedItems.Count > 0)
            {
                OpenVsItemEditor((VsItem)FileListUi.SelectedItems[0].Tag);
            }
        }

        // 拖拽文件 Enter
        private void FileListUi_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Move : DragDropEffects.None; // 拖拽数据是否为文件
        }

        // 拖拽文件 Drop
        private void FileListUi_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data?.GetData(DataFormats.FileDrop);

            if (files != null)
                foreach (var fileName in files)
                    FileListAdd(new FileInfo(fileName));

            MatchVideoSub();
        }

        private void FileListUi_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (FileListUi.FocusedItem == null
                || FileListUi.Bounds.Contains(e.Location) != true) return;
            var selectedItem = (VsItem)FileListUi.SelectedItems[0].Tag;
            var m = new ContextMenuStrip();
            var items = m.Items;
            items.Add(new ToolStripMenuItem("编辑", null, (_, _) => EditListSelectedItems(), Keys.F3));
            items.Add("-");
            items.Add(new ToolStripMenuItem("删除", null, (_, _) => RemoveListSelectedItems(), Keys.Delete));
            var discardVideo = new ToolStripMenuItem("丢弃视频", null,
                (_, _) => DiscardListSelectedItemsFile(AppFileType.Video));
            var discardSub = new ToolStripMenuItem("丢弃字幕", null,
                (_, _) => DiscardListSelectedItemsFile(AppFileType.Sub));
            if (string.IsNullOrWhiteSpace(selectedItem.Video)) discardVideo.Enabled = false;
            if (string.IsNullOrWhiteSpace(selectedItem.Sub)) discardSub.Enabled = false;
            items.Add(discardVideo);
            items.Add(discardSub);
            items.Add("-");
            items.Add(new ToolStripMenuItem("全选", null, (_, _) => SelectListAll(), Keys.Control | Keys.A));
            items.Add("-");
            var openVideoFolder = new ToolStripMenuItem("打开视频文件夹", null,
                (_, _) => OpenExplorerFile(selectedItem.Video));
            var openSubFolder =
                new ToolStripMenuItem("打开字幕文件夹", null, (_, _) => OpenExplorerFile(selectedItem.Sub));
            if (string.IsNullOrWhiteSpace(selectedItem.Video)) openVideoFolder.Enabled = false;
            if (string.IsNullOrWhiteSpace(selectedItem.Sub)) openSubFolder.Enabled = false;
            items.Add(openVideoFolder);
            items.Add(openSubFolder);
            items.Add("-");
            items.Add(new ToolStripMenuItem("复制改名命令至剪切板", null, (_, _) => CopyRenameCommand()));
            m.Show(FileListUi, new Point(e.X, e.Y));
        }

        // 主窗体尺寸发生变化
        private void MainForm_Resize(object sender, EventArgs e)
        {
            // 自适应文件列表字段宽度
            int calcPathInfoWidth =
                (FileListUi.Width - (FileListUi.Columns[0].Width + FileListUi.Columns[3].Width + 8)) / 2;
            FileListUi.Columns[1].Width = calcPathInfoWidth;
            FileListUi.Columns[2].Width = calcPathInfoWidth;
        }

        // 显示预览 勾选
        private void PreviewCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SwitchPreview(PreviewCheckBox.Checked, true);
        }

        private void SwitchPreview(bool value, bool force = false)
        {
            if (!force && value == PreviewCheckBox.Checked) return;

            PreviewCheckBox.Checked = value;
            RefreshFileListUi();
        }

        // 清空列表
        private void ClearListAll()
        {
            if (_vsList.Count == 0 && FileListUi.Items.Count == 0)
                return;

            if (AppSettings.ListItemRemovePrompt)
            {
                var result = MessageBox.Show(@"你要清空列表吗？", @"清空列表", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) return;
            }

            FileListUi.Items.Clear();
            _vsList.Clear();

            RefreshFileListUi();
        }

        private void DiscardListSelectedItemsFile(AppFileType fileType)
        {
            if (FileListUi.SelectedItems.Count <= 0) return;

            var label = fileType switch
            {
                AppFileType.Video => "视频",
                AppFileType.Sub => "字幕",
                _ => ""
            };

            if (AppSettings.ListItemRemovePrompt)
            {
                var result = MessageBox.Show($@"你要丢弃选定项目的{label}吗？源文件不会被删除", $@"丢弃所选项的{label}", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.No) return;
            }

            foreach (ListViewItem item in FileListUi.SelectedItems)
            {
                if (item.Tag == null)
                {
                    item.Remove();
                    continue;
                }

                var vsItem = (VsItem)item.Tag;
                if (fileType.Equals(AppFileType.Video)) vsItem.Video = null;
                if (fileType.Equals(AppFileType.Sub)) vsItem.Sub = null;
            }

            RefreshFileListUi();
        }

        // 删除选定的项目
        private void RemoveListSelectedItems()
        {
            if (FileListUi.SelectedItems.Count <= 0) return;

            if (AppSettings.ListItemRemovePrompt)
            {
                var result = MessageBox.Show(@"你要删除选定的项目吗？", @"删除所选项", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.No) return;
            }

            foreach (ListViewItem item in FileListUi.SelectedItems)
            {
                if (item.Tag == null)
                {
                    item.Remove();
                    continue;
                }

                var vsItem = (VsItem)item.Tag;
                _vsList.Remove(vsItem);
                item.Remove();
            }

            RefreshFileListUi();
        }

        private void CopyRenameCommand()
        {
            var cmd = "";
            var renameDict = GetSubRenameDict();
            if (renameDict.Count < 0) return;
            var selectedList = new List<ListViewItem>(FileListUi.SelectedItems.OfType<ListViewItem>());
            foreach (var rename in renameDict)
            {
                if (FileListUi.SelectedItems.Count > 0
                    && !selectedList.Exists(o => o.Tag != null && ((VsItem)o.Tag).Sub == rename.Key)) continue;
                cmd += $"mv \"{rename.Key}\" \"{rename.Value}\"{Environment.NewLine}";
            }

            Clipboard.SetDataObject(cmd.Trim());
        }

        #endregion

        #region 点击事件

        private void TopMenu_OpenFileBtn_Click(object sender, EventArgs e) => OpenFile();
        private void R_OpenFileBtn_Click(object sender, EventArgs e) => OpenFile();
        private void TopMenu_OpenFolderBtn_Click(object sender, EventArgs e) => OpenFolder();
        private void R_OpenFolderBtn_Click(object sender, EventArgs e) => OpenFolder();
        private void TopMenu_Rule_Click(object sender, EventArgs e) => OpenRuleEditor();
        private void TopMenu_Setting_Click(object sender, EventArgs e) => _settingForm.ShowDialog();
        private void TopMenu_ReMatch_Click(object sender, EventArgs e) => ReMatch();
        private void TopMenu_ClearAll_Click(object sender, EventArgs e) => ClearListAll();
        private void StartBtn_Click(object sender, EventArgs e) => StartRename();
        private void R_EditBtn_Click(object sender, EventArgs e) => EditListSelectedItems();
        private void R_RemoveBtn_Click(object sender, EventArgs e) => RemoveListSelectedItems();
        private void R_ReMatchBtn_Click(object sender, EventArgs e) => ReMatch();
        private void R_ClearAllBtn_Click(object sender, EventArgs e) => ClearListAll();
        private void R_RuleBtn_Click(object sender, EventArgs e) => OpenRuleEditor();
        private void R_SettingBtn_Click(object sender, EventArgs e) => _settingForm.ShowDialog();
        private void CopyrightText_Click(object sender, EventArgs e) => Program.OpenAuthorBlog();

        #endregion

        #region 快捷键

        // 快捷键操作
        private const Keys OpenFileKey = Keys.Control | Keys.O;
        private const Keys OpenFolderKey = Keys.Control | Keys.Shift | Keys.O;
        private const Keys ReMatchKey = Keys.Control | Keys.R;
        private const Keys ClearAllKey = Keys.Control | Keys.N;

        // 初始化快捷键
        private void InitShortcut()
        {
            // 快捷键显示
            TopMenu_OpenFileBtn.ShortcutKeys = OpenFileKey;
            TopMenu_OpenFolderBtn.ShortcutKeys = OpenFolderKey;
            TopMenu_ReMatch.ShortcutKeys = ReMatchKey;
            TopMenu_ClearAll.ShortcutKeys = ClearAllKey;
        }

        // 快捷键操作
        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            switch (keys)
            {
                case OpenFileKey:
                    TopMenu_OpenFileBtn.PerformClick();
                    return true;
                case OpenFolderKey:
                    TopMenu_OpenFolderBtn.PerformClick();
                    return true;
                case ReMatchKey:
                    TopMenu_ReMatch.PerformClick();
                    return true;
                case ClearAllKey:
                    TopMenu_ClearAll.PerformClick();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keys);
            }
        }

        // 列表快捷键
        private void FileListUi_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F3:
                    EditListSelectedItems();
                    break;
                case Keys.Delete:
                    RemoveListSelectedItems();
                    break;
                case Keys.A when e.Control:
                    SelectListAll();
                    break;
            }
        }

        #endregion

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        private static void OpenExplorerFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (!File.Exists(filePath)) return;
            var args = $"/select, \"{filePath}\"";
            Process.Start("explorer.exe", args);
        }
    }
}