using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SubRenamer.Global;

namespace SubRenamer
{
    public partial class MainForm
    {
        private readonly List<VsItem> _vsList = new();
        public MatchMode CurtMatchMode = MatchMode.Auto;

        // 匹配模式
        public enum MatchMode
        {
            Auto,
            Manu,
            Regex
        }

        private string[] GetItemValues(VsItem vsItem)
        {
            var showFullName = AppSettings.ListShowFileFullName;
            var subRenameDict = GetSubRenameDict(); // 重命名字幕文件路径词典
            var videoText = vsItem.Video != null ? (showFullName ? vsItem.Video : Path.GetFileName(vsItem.Video)) : "";
            var subText = vsItem.Sub != null ? (showFullName ? vsItem.Sub : Path.GetFileName(vsItem.Sub)) : "";

            // 显示预览内容
            if (!PreviewCheckBox.Checked)
                return new[]
                {
                    vsItem.MatchKey ?? "",
                    videoText,
                    subText,
                    vsItem.GetStatusStr()
                };
            videoText = !AppSettings.RenameVideo ? subText : videoText;

            subText = AppSettings.RenameVideo switch
            {
                false when (vsItem.Sub != null && subRenameDict.ContainsKey(vsItem.Sub)) => showFullName
                    ? subRenameDict[vsItem.Sub]
                    : Path.GetFileName(subRenameDict[vsItem.Sub]),
                true when (vsItem.Video != null && subRenameDict.ContainsKey(vsItem.Video)) => showFullName
                    ? subRenameDict[vsItem.Video]
                    : Path.GetFileName(subRenameDict[vsItem.Video]),
                _ => "(不修改)"
            };

            return new[]
            {
                vsItem.MatchKey ?? "",
                videoText,
                subText,
                vsItem.GetStatusStr()
            };
        }

        public void RefreshFileListUi(bool removeNull = true) => BeginInvoke((MethodInvoker)delegate
        {
            _RefreshFileListUi(removeNull);
        });

        private void _RefreshFileListUi(bool removeNull = true)
        {
            UpdateWinTitle();

            // 删除无效项
            if (removeNull) _vsList.RemoveAll(o => o.IsEmpty);
            foreach (ListViewItem item in FileListUi.Items)
            {
                if (item.Tag == null || !_vsList.Contains(item.Tag))
                    item.Remove();
            }

            foreach (var vsItem in _vsList)
            {
                var itemValues = GetItemValues(vsItem);
                var findItem = FileListUi.Items.Cast<ListViewItem>().ToList()
                    .Find(o => o.Tag != null && o.Tag == vsItem);
                if (findItem == null)
                {
                    var item = new ListViewItem(itemValues)
                    {
                        Tag = vsItem
                    };
                    FileListUi.Items.Add(item);
                }
                else
                {
                    UpdateFileListUiItem(findItem);
                }
            }

            // 预览修改模式
            if (PreviewCheckBox.Checked)
            {
                var renameFileType = !AppSettings.RenameVideo ? "字幕" : "视频";
                Video.Text = $@"{renameFileType}文件名";
                Subtitle.Text = @"修改为";
            }
            else
            {
                Video.Text = @"视频";
                Subtitle.Text = @"字幕";
            }
        }

        private void UpdateFileListUiItem(ListViewItem item)
        {
            if (item.Tag == null) return;
            var itemValues = GetItemValues((VsItem)item.Tag);
            if (item.SubItems[0].Text != itemValues[0])
                item.SubItems[0].Text = itemValues[0];
            for (var i = 1; i < itemValues.ToArray().Length; i++)
            {
                if (item.SubItems[i].Text != itemValues[i])
                    item.SubItems[i].Text = itemValues[i];
            }
        }

        // protected void MatchVideoSub() => Task.Factory.StartNew(() => _MatchVideoSub());

        private List<FileInfo> GetFileListByVsList(AppFileType fileType)
        {
            return fileType switch
            {
                AppFileType.Video => new List<FileInfo>(
                    _vsList.Where(o => o.Video != null).Select(o => o.VideoFileInfo)),
                AppFileType.Sub => new List<FileInfo>(_vsList.Where(o => o.Sub != null).Select(o => o.SubFileInfo)),
                _ => null
            };
        }

        // 匹配 视频 & 字幕 集数位置
        public void MatchVideoSub()
        {
            if (_vsList.Count <= 0) return;

            // VsItem
            TryHandleVsListMatch(AppFileType.Video);
            TryHandleVsListMatch(AppFileType.Sub);

            // 刷新文件列表
            RefreshFileListUi();
        }

        // 自动匹配配置
        private int _mAutoBegin = int.MinValue;
        private string _mAutoEnd;

        // 手动匹配配置
        public string MManuVBegin = null;
        public string MManuVEnd = null;
        public string MManuSBegin = null;
        public string MManuSEnd = null;

        // 正则表达式匹配配置
        public Regex MRegxV = null;
        public Regex MRegxS = null;

        private void TryHandleVsListMatch(AppFileType fileType)
        {
            // 自动匹配数据归零
            _mAutoBegin = int.MinValue;
            _mAutoEnd = null;

            var fileList = GetFileListByVsList(fileType);
            foreach (var file in fileList)
            {
                var matchKey = GetMatchKeyByFileName(file.Name, fileType, fileList);

                var findVsItem = ((matchKey != null) ? _vsList.Find(o => o.MatchKey == matchKey) : null) ?? _vsList.Find(o =>
                {
                    return fileType switch
                    {
                        AppFileType.Video => o.Video == file.FullName,
                        AppFileType.Sub => o.Sub == file.FullName,
                        _ => false
                    };
                }); // By matchKey
                // 通过文件名查找到的现成的 VsItem
                // 仅更新数据
                if (findVsItem == null) continue;
                {
                    switch (fileType)
                    {
                        case AppFileType.Video:
                            findVsItem.Video = file.FullName;
                            findVsItem.Status = VsStatus.SubLack;
                            _vsList.RemoveAll(o => o != findVsItem && o.Video == findVsItem.Video); // 删除其他同类项目
                            break;
                        case AppFileType.Sub:
                            findVsItem.Sub = file.FullName;
                            findVsItem.Status = VsStatus.VideoLack;
                            _vsList.RemoveAll(o => o != findVsItem && o.Sub == findVsItem.Sub); // 删除其他同类项目
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
                    }

                    if (findVsItem.Video != null && findVsItem.Sub != null)
                        findVsItem.Status = VsStatus.Ready;

                    findVsItem.MatchKey = matchKey;
                    if (string.IsNullOrWhiteSpace(findVsItem.MatchKey))
                        findVsItem.Status = VsStatus.Unmatched;
                }
            }
        }

        // 获取匹配字符
        private string GetMatchKeyByFileName(string fileName, AppFileType fileType, List<FileInfo> fileList)
        {
            string matchKey = null;
            switch (CurtMatchMode)
            {
                case MatchMode.Auto:
                {
                    if (_mAutoBegin == int.MinValue) _mAutoBegin = GetEpisodePosByList(fileList); // 视频文件名集数开始位置
                    _mAutoEnd ??= GetEndStrByList(fileList, _mAutoBegin);
                    if (_mAutoBegin > -1 && _mAutoEnd != null)
                        matchKey = GetEpisodeByFileName(fileName, _mAutoBegin, _mAutoEnd); // 匹配字符
                    break;
                }
                case MatchMode.Manu when fileType == AppFileType.Video:
                    matchKey = GetMatchKeyByBeginEndStr(fileName, MManuVBegin, MManuVEnd);
                    break;
                case MatchMode.Manu:
                {
                    if (fileType == AppFileType.Sub)
                        matchKey = GetMatchKeyByBeginEndStr(fileName, MManuSBegin, MManuSEnd);
                    break;
                }
                case MatchMode.Regex when fileType == AppFileType.Video && MRegxV != null:
                    matchKey = GetMatchKeyByRegex(fileName, MRegxV);
                    break;
                case MatchMode.Regex:
                {
                    if (fileType == AppFileType.Sub && MRegxS != null)
                        matchKey = GetMatchKeyByRegex(fileName, MRegxS);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (string.IsNullOrWhiteSpace(matchKey)) matchKey = null;
            return matchKey;
        }

        public static string GetMatchKeyByRegex(string fileName, Regex regex)
        {
            if (regex == null || string.IsNullOrWhiteSpace(fileName)) return null;
            return regex.Match(fileName).Groups.Count < 2 ? null : regex.Match(fileName).Groups[1].Value;
        }

        public static string GetMatchKeyByBeginEndStr(string fileName, string start, string end)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            if (string.IsNullOrWhiteSpace(start) && string.IsNullOrWhiteSpace(end))
                return null; // 前后字符都没有，直接返回 null
            start ??= "";
            end ??= "";

            fileName = fileName.Trim();

            start = EscapeRegExp(start).Replace(@"\*", ".*");
            end = EscapeRegExp(end).Replace(@"\*", ".*");

            var calcPattern = $@"^{start}0*(.+?){end}$";
            var str = Regex.Match(fileName, calcPattern).Groups[1].Value;

            return str;
        }

        private static string EscapeRegExp(string str)
        {
            return MyRegex().Replace(str, @"\$&");
        }

        #region 自动匹配模式

        // 获取集数
        private static string GetEpisodeByFileName(string fileName, int beginPos, string endStr)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            if (beginPos <= -1) return null;
            if (beginPos >= fileName.Length) return null;
            var str = fileName[beginPos..];

            var result = str.TakeWhile(t => t.ToString() != endStr).Aggregate("", (current, t) => current + t);
            // 通过 endStr 获得集数

            result = result.TrimStart('0'); // 开头为零的情况：替换 0001 为 1
            result = result.Trim(); // 去掉前后空格

            return result;
        }

        // 遍历所有 list 中的项目，尝试得到集数开始位置
        private int GetEpisodePosByList(IReadOnlyList<FileInfo> list)
        {
            var aIndex = 0;
            var bIndex = 1;
            var beginPos = -1;

            while (true)
            {
                try
                {
                    var result = GetEpisodePosByTwoStr(list[aIndex].Name, list[bIndex].Name);
                    beginPos = result;
                    break;
                }
                catch
                {
                    aIndex++;
                    bIndex++;
                    if (aIndex >= list.Count || bIndex >= list.Count) break;
                }
            }

            return beginPos;
        }

        // 通过比对两个文件名中 数字 不同的部分来得到 集数 的位置
        private static int GetEpisodePosByTwoStr(string strA, string strB)
        {
            var numGrpA = MyRegex1().Matches(strA);
            var numGrpB = MyRegex1().Matches(strB);
            var beginPos = -1;

            for (var i = 0; i < numGrpA.Count; i++)
            {
                var a = numGrpA[i];
                var b = numGrpB[i];
                if (a.Value == b.Value || a.Index != b.Index) continue;
                // 若两个 val 不同，则记录位置
                beginPos = numGrpA[i].Index;
                break;
            }

            if (beginPos == -1) throw new Exception("beginPos == -1");

            return beginPos;
        }

        // 获取终止字符
        private static string GetEndStrByList(IReadOnlyCollection<FileInfo> list, int beginPos)
        {
            if (list.Count < 2) return null;
            if (beginPos <= -1) return null;

            var fileName = list.Where(o => o.Name.Length > beginPos && MyRegex2().IsMatch(o.Name[beginPos..][0].ToString())).ToList()[0].Name; // 获取开始即是数字的文件名
            fileName = fileName[beginPos..]; // 从指定开始位置 (beginPos) 开始读取数字（忽略开始位置前的所有内容）
            var grp = MyRegex1().Matches(fileName);
            if (grp.Count <= 0) return null;
            var firstNum = grp[0];
            var afterNumStrIndex = firstNum.Index + firstNum.Length; // 数字后面的第一个字符 index

            // 不把特定字符（空格等）作为结束字符
            var strTmp = fileName[afterNumStrIndex..];

            return (from t in strTmp where t.ToString() != " " select t.ToString()).FirstOrDefault();
        }

        #endregion

        private void StartRename()
        {
            var subRenameDict = GetSubRenameDict();
            if (!subRenameDict.Any()) return;
            SwitchPreview(true);
            Task.Factory.StartNew(() => _StartRename(subRenameDict));
        }

        /// 执行改名操作
        private void _StartRename(Dictionary<string, string> subRenameDict)
        {
            Program.Log($"[=============== 开始执行改名操作  ===============]");

            var btnRawText = "";
            Invoke((MethodInvoker)delegate
            {
                StartBtn.Enabled = false;
                btnRawText = StartBtn.Text;
                StartBtn.Text = @"改名中...";
            });
            var errTotal = 0;

            foreach (var subRename in subRenameDict)
            {
                try
                {
                    _RenameOnce(subRename);
                    Program.Log("[成功]", $"[\"{subRename.Key}\"=>\"{subRename.Value}\"]");
                    Invoke((MethodInvoker)delegate { RefreshFileListUi(); });
                }
                catch (Exception e)
                {
                    Program.Log("[错误]",
                        $"[\"{subRename.Key}\"=>\"{subRename.Value}\"]{Environment.NewLine}  ==> {e.Message}");
                    errTotal++;
                }
            }

            if (errTotal > 0)
                Process.Start(LogFilename);

            Invoke((MethodInvoker)delegate
            {
                StartBtn.Text = btnRawText;
                StartBtn.Enabled = true;
            });
        }

        private void _RenameOnce(KeyValuePair<string, string> subRename)
        {
            var vsFile = _vsList.Find(o => ((AppSettings.RenameVideo) ? o.Video : o.Sub) == subRename.Key);
            if (vsFile == null) throw new Exception("找不到修改项");
            if (vsFile.Status == VsStatus.Done) return; // 无需再改名了
            if (vsFile.Status != VsStatus.Ready && vsFile.Status != VsStatus.Fatal) throw new Exception("当然状态无法修改");
            if (vsFile.Video == null || vsFile.Sub == null) throw new Exception("字幕/视频文件不完整");

            var before = new FileInfo(subRename.Key);
            var after = new FileInfo(subRename.Value);

            // 若无需修改
            if (before.FullName.Equals(after.FullName))
            {
                vsFile.Status = VsStatus.Done;
                throw new Exception($"文件名名未修改，因为改名后的文件已存在，无需改名");
            }

            // 若原文件不存在
            if (!before.Exists)
            {
                vsFile.Status = VsStatus.Fatal;
                throw new Exception($"字幕源文件不存在");
            }

            // 执行备份
            if (AppSettings.RawSubtitleBackup)
            {
                try
                {
                    // 前字幕文件 和 后字幕文件 若是在同一个目录下
                    if (before.DirectoryName == after.DirectoryName && File.Exists(before.FullName))
                        BackupFile(before.FullName);

                    if (File.Exists(after.FullName))
                        BackupFile(after.FullName);
                }
                catch (Exception e)
                {
                    throw new Exception($"改名前备份发生错误 {e.GetType().FullName} {e}");
                }
            }

            // 执行更名
            try
            {
                if (before.DirectoryName == after.DirectoryName)
                {
                    if (File.Exists(after.FullName)) File.Delete(after.FullName); // 若后文件存在，则先删除 (上面有备份的)
                    File.Move(before.FullName, after.FullName); // 前后字幕相同目录，执行改名
                }
                else
                {
                    File.Copy(before.FullName, after.FullName, true); // 前后字幕不同文件，执行复制
                }

                vsFile.Status = VsStatus.Done;
            }
            catch (Exception e)
            {
                // 更名失败
                vsFile.Status = VsStatus.Fatal;
                throw new Exception($"改名发生错误 {e.GetType().FullName} {e}");
            }
        }

        private static void BackupFile(string filename)
        {
            if (!File.Exists(filename)) return;
            var bkFolder = Path.Combine(Path.GetDirectoryName(filename) ?? string.Empty, "SubBackup/");
            if (!Directory.Exists(bkFolder)) Directory.CreateDirectory(bkFolder);

            var bkDistFile = Path.Combine(bkFolder, Path.GetFileName(filename));
            if (File.Exists(bkDistFile)) // 解决文件重名问题
                bkDistFile = Path.Combine(
                    bkFolder,
                    Path.GetFileNameWithoutExtension(filename) +
                    $".{Program.GetNowDatetime()}{Path.GetExtension(filename)}"
                );

            File.Copy(filename, bkDistFile, true);
        }

        // 获取修改的字幕文件名 (原始完整路径->修改后完整路径)
        private Dictionary<string, string> GetSubRenameDict()
        {
            var dict = new Dictionary<string, string>();
            if (_vsList.Count <= 0)
                return dict;

            foreach (var item in _vsList.Where(item => item.Video != null && item.Sub != null))
            {
                if (!AppSettings.RenameVideo)
                {
                    var videoName = Path.GetFileNameWithoutExtension(item.VideoFileInfo.Name); // 去掉后缀的视频文件名
                    var subAfterFilename = videoName + item.SubFileInfo.Extension; // 修改的字幕文件名
                    if (item.VideoFileInfo.DirectoryName != null)
                        dict[item.SubFileInfo.FullName] =
                            Path.Combine(item.VideoFileInfo.DirectoryName, subAfterFilename);
                }
                else
                {
                    // 改视频文件名模式
                    var subName = Path.GetFileNameWithoutExtension(item.SubFileInfo.Name);
                    var videoAfterFilename = subName + item.VideoFileInfo.Extension; // 修改的字幕文件名
                    if (item.VideoFileInfo.DirectoryName != null)
                        dict[item.VideoFileInfo.FullName] =
                            Path.Combine(item.VideoFileInfo.DirectoryName, videoAfterFilename);
                }
            }


            return dict;
        }

        [GeneratedRegex("[.*+?^${}()|[\\]\\\\]")]
        private static partial Regex MyRegex();
        [GeneratedRegex("(\\d+)")]
        private static partial Regex MyRegex1();
        [GeneratedRegex("^\\d+$")]
        private static partial Regex MyRegex2();
    }
}