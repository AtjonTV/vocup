using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Vocup.Models;
using Vocup.Properties;
using Vocup.Util;

namespace Vocup.Forms
{
    public partial class VocabularyBookSettings : Form
    {
        private const string InvalidChars = "#=:\\/|<>*?\"";
        private readonly Color redBgColor = Color.FromArgb(255, 192, 203);
        private readonly SpecialCharKeyboard specialCharDialog;
        private readonly VocabularyBook book;

        private VocabularyBookSettings()
        {
            InitializeComponent();
            specialCharDialog = new SpecialCharKeyboard();
            specialCharDialog.Initialize(this);
            specialCharDialog.VisibleChanged += (a0, a1) => BtnSpecialChar.Enabled = true;
            specialCharDialog.RegisterTextBox(TbMotherTongue);
        }

        public VocabularyBookSettings(out VocabularyBook book) : this()
        {
            book = new VocabularyBook();
            this.book = book;

            Text = Words.CreateVocabularyBook;
            Icon = Icon.FromHandle(Icons.blank_file.GetHicon());
            GroupOptions.Enabled = false;
        }

        public VocabularyBookSettings(VocabularyBook book) : this()
        {
            this.book = book;

            Text = Words.EditVocabularyBook;
            Icon = Icon.FromHandle(Icons.settings_file.GetHicon());
            TbMotherTongue.Text = book.MotherTongue;
            TbForeignLang.Text = book.ForeignLang;
            RbModeAskForeignLang.Checked = book.PracticeMode == PracticeMode.AskForForeignLang;
            RbModeAskMotherTongue.Checked = book.PracticeMode == PracticeMode.AskForMotherTongue;
            GroupOptions.Enabled = true;
        }

        private void TextBox_Enter(object sender, EventArgs e)
        {
            specialCharDialog.RegisterTextBox((TextBox)sender);
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            bool mValid = !TbMotherTongue.Text.ContainsAny(InvalidChars);
            TbMotherTongue.BackColor = mValid ? Color.White : redBgColor;
            bool fValid = !TbForeignLang.Text.ContainsAny(InvalidChars);
            TbForeignLang.BackColor = fValid ? Color.White : redBgColor;

            if (mValid && fValid &&
                !string.IsNullOrWhiteSpace(TbMotherTongue.Text) &&
                !string.IsNullOrWhiteSpace(TbForeignLang.Text) &&
                TbMotherTongue.Text != TbForeignLang.Text)
            {
                BtnOK.Enabled = true;
                AcceptButton = BtnOK;
            }
            else
            {
                BtnOK.Enabled = false;
                AcceptButton = BtnCancel;
            }
        }

        private void BtnSpecialChar_Click(object sender, EventArgs e)
        {
            specialCharDialog.Show();
            BtnSpecialChar.Enabled = false;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            book.MotherTongue = TbMotherTongue.Text;
            book.ForeignLang = TbForeignLang.Text;
            book.PracticeMode = RbModeAskForeignLang.Checked ? PracticeMode.AskForForeignLang : PracticeMode.AskForMotherTongue;

            if (CbResetResults.Checked)
            {
                foreach (VocabularyWord word in book.Words)
                {
                    word.PracticeStateNumber = 0;
                }
            }
        }
    }
}