using SubRenamer.MatchModeEditor;
using System;
using System.Windows.Forms;

namespace SubRenamer
{
    public partial class RuleEditor : Form
    {
        private readonly MainForm _mainForm;

        public RuleEditor(MainForm mainForm)
        {
            this._mainForm = mainForm;
            InitializeComponent();
        }

        private void RuleEditor_Load(object sender, EventArgs e)
        {
            var curtMode = _mainForm.CurtMatchMode;
            switch (curtMode)
            {
                case MainForm.MatchMode.Auto:
                    ModeBtn_Auto.Checked = true;
                    break;
                case MainForm.MatchMode.Manu:
                    ModeBtn_Manu.Checked = true;
                    break;
                case MainForm.MatchMode.Regex:
                    ModeBtn_Regex.Checked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ModeBtn_Auto_CheckedChanged(object sender, EventArgs e)
        {
            _mainForm.CurtMatchMode = MainForm.MatchMode.Auto;
        }

        private void ModeBtn_Manu_CheckedChanged(object sender, EventArgs e)
        {
            _mainForm.CurtMatchMode = MainForm.MatchMode.Manu;
        }

        private void ModeBtn_Regex_CheckedChanged(object sender, EventArgs e)
        {
            _mainForm.CurtMatchMode = MainForm.MatchMode.Regex;
        }

        private void EditBtn_Manu_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ModeBtn_Manu.PerformClick();
            var form = new ManuEditor(_mainForm);
            form.ShowDialog();
        }

        private void EditBtn_Regex_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ModeBtn_Regex.PerformClick();
            var form = new RegexEditor(_mainForm);
            form.ShowDialog();
        }

        private void RuleEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mainForm.MatchVideoSub();
        }

        private void Copyright_Click(object sender, EventArgs e) => Program.OpenAuthorBlog();
    }
}
