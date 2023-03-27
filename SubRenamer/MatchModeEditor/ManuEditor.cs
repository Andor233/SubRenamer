using SubRenamer.Lib;
using System;
using System.Windows.Forms;
using static SubRenamer.Global;

namespace SubRenamer.MatchModeEditor
{
    public partial class ManuEditor : Form
    {
        private static readonly string MatchSign = "<X>".ToUpper();

        private static string _vRaw;
        private static string _sRaw;

        private string _vBegin;
        private string _vEnd;

        private string _sBegin;
        private string _sEnd;

        private readonly MainForm _mainForm;

        public ManuEditor(MainForm mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
        }

        private void ManuEditor_Load(object sender, EventArgs e)
        {
            V_Tpl.TextChanged += (_, _) => { MatchRuleUpdated(AppFileType.Video); };
            S_Tpl.TextChanged += (_, _) => { MatchRuleUpdated(AppFileType.Sub); };

            V_OpenBtn.Click += (_, _) =>
            {
                Utils.OpenFile(AppFileType.Video, opened: (fileName, _) =>
                {
                    _vRaw = fileName;
                    V_Tpl.Text = fileName;
                    MatchRuleUpdated(AppFileType.Video);
                });
            };
            S_OpenBtn.Click += (_, _) =>
            {
                Utils.OpenFile(AppFileType.Sub, opened: (fileName, _) =>
                {
                    _sRaw = fileName;
                    S_Tpl.Text = fileName;
                    MatchRuleUpdated(AppFileType.Sub);
                });
            };

            var mVBegin = _mainForm.MManuVBegin;
            var mVEnd = _mainForm.MManuVEnd;
            if (!string.IsNullOrWhiteSpace(mVBegin) && !string.IsNullOrWhiteSpace(mVEnd))
            {
                V_Tpl.Text = $@"{mVBegin}{MatchSign}{mVEnd}";
                MatchRuleUpdated(AppFileType.Video);
            }

            var mSBegin = _mainForm.MManuSBegin;
            var mSEnd = _mainForm.MManuSEnd;
            if (string.IsNullOrWhiteSpace(mSBegin) || string.IsNullOrWhiteSpace(mSEnd)) return;
            S_Tpl.Text = $@"{mSBegin}{MatchSign}{mSEnd}";
            MatchRuleUpdated(AppFileType.Sub);
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            _mainForm.MManuVBegin = _vBegin;
            _mainForm.MManuVEnd = _vEnd;
            _mainForm.MManuSBegin = _sBegin;
            _mainForm.MManuSEnd = _sEnd;
            Close();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MatchRuleUpdated(AppFileType fileType)
        {
            switch (fileType)
            {
                case AppFileType.Video:
                {
                    _vBegin = null;
                    _vEnd = null;
                    V_Matched.Text = @"未匹配";
                    var tpl = V_Tpl.Text.Trim();

                    if (string.IsNullOrWhiteSpace(tpl)) return;
                    var pos = tpl.ToUpper().IndexOf(MatchSign, StringComparison.Ordinal);
                    if (pos <= -1) return;
                    var afterPos = pos + MatchSign.Length;

                    _vBegin = tpl[..pos];
                    _vEnd = tpl.Substring(afterPos, tpl.Length - afterPos);
                    V_Matched.Text = @"匹配结果: " + MainForm.GetMatchKeyByBeginEndStr(_vRaw, _vBegin, _vEnd);
                    break;
                }
                case AppFileType.Sub:
                {
                    _sBegin = null;
                    _sEnd = null;
                    S_Matched.Text = @"未匹配";
                    var tpl = S_Tpl.Text.Trim();

                    if (string.IsNullOrWhiteSpace(tpl)) return;
                    var pos = tpl.ToUpper().IndexOf(MatchSign, StringComparison.Ordinal);
                    if (pos <= -1) return;
                    var afterPos = pos + MatchSign.Length;

                    _sBegin = tpl[..pos];
                    _sEnd = tpl.Substring(afterPos, tpl.Length - afterPos);
                    S_Matched.Text = @"匹配结果: " + MainForm.GetMatchKeyByBeginEndStr(_sRaw, _sBegin, _sEnd);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null);
            }
        }
    }
}