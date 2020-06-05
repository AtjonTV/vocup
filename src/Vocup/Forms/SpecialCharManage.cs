using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vocup.Properties;
using Vocup.Util;

namespace Vocup.Forms
{
    public partial class SpecialCharManage : Form
    {
        private const string InvalidChars = "#=:\\/|<>*?\"";
        private readonly Color redBgColor = Color.FromArgb(255, 192, 203);
        private readonly string specialCharDir = AppInfo.SpecialCharDirectory;

        public SpecialCharManage()
        {
            InitializeComponent();
            Icon = Icon.FromHandle(Icons.Alphabet.GetHicon());
        }

        private void SpecialCharManage_Load(object sender, EventArgs e)
        {
            RefreshListbox();
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            LanguageList.SelectedIndex = -1;
            BtnNew.Enabled = false;
            TbLanguage.Enabled = true;
            TbLanguage.Text = "";
            TbChars.Enabled = true;
            TbChars.Text = "";
            TbLanguage.Focus();
        }

        private void TbLanguage_TbChars_TextChanged(object sender, EventArgs e)
        {
            //�berpr�fen, das Textfeld nicht erlaubte zeichen enth�lt
            var charsValid = !TbLanguage.Text.ContainsAny(InvalidChars);
            TbLanguage.BackColor = charsValid ? Color.Empty : redBgColor;

            if (!string.IsNullOrWhiteSpace(TbLanguage.Text) &&
                !string.IsNullOrWhiteSpace(TbChars.Text) &&
                charsValid)
            {
                BtnSave.Enabled = true;
                AcceptButton = BtnSave;
            }
            else
            {
                BtnSave.Enabled = false;
                AcceptButton = BtnClose;
            }
        }

        //Sonderzeichen-Tabelle l�schen
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                var language = LanguageList.Items[LanguageList.SelectedIndex].ToString();

                //Datei mit den Sonderzeichen l�schen
                var info = new FileInfo(Path.Combine(specialCharDir, language + ".txt"));
                if (info.Exists)
                    info.Delete();

                //Sprache aus der Listbox l�schen
                LanguageList.Items.RemoveAt(LanguageList.SelectedIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Messages.SpecialCharDeleteError, TbLanguage.Text, ex),
                    Messages.SpecialCharDeleteErrorT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshListbox();
            }
        }

        //Sonderzeichen-Tabelle speichern
        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(specialCharDir);

                if (LanguageList.SelectedIndex == -1) // New item
                {
                    for (var i = 0; i < LanguageList.Items.Count; i++)
                        if (TbLanguage.Text == LanguageList.Items[i].ToString())
                        {
                            MessageBox.Show(Messages.SpecialCharAlreadyExists, "", MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                    LanguageList.Items.Add(TbLanguage.Text);
                }

                using (var writer = new StreamWriter(Path.Combine(specialCharDir, TbLanguage.Text + ".txt")))
                {
                    TbChars.Text = TbChars.Text.Replace(" ", "");
                    writer.Write(string.Join(Environment.NewLine, TbChars.Text.ToCharArray()));
                }

                // Delete old file if language was changed
                if (LanguageList.SelectedIndex != -1 &&
                    TbLanguage.Text != LanguageList.Items[LanguageList.SelectedIndex].ToString())
                {
                    var info = new FileInfo(Path.Combine(specialCharDir,
                        LanguageList.Items[LanguageList.SelectedIndex] + ".txt"));
                    info.Delete();

                    LanguageList.Items[LanguageList.SelectedIndex] = TbLanguage.Text;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Messages.SpecialCharSaveError, TbLanguage.Text, ex),
                    Messages.SpecialCharSaveErrorT, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            TbLanguage.Text = "";
            TbChars.Text = "";
            LanguageList.SelectedIndex = -1;
        }

        private void LanguageList_SelectedIndexChanged(object sender, EventArgs e)
        {
            BtnNew.Enabled = true;
            TbChars.Text = "";
            TbLanguage.Text = "";

            if (LanguageList.SelectedIndex == -1)
            {
                RefreshListbox();

                BtnDelete.Enabled = false;
                TbChars.Enabled = false;
                TbLanguage.Enabled = false;
            }
            else
            {
                BtnDelete.Enabled = true;
                TbChars.Enabled = true;
                TbLanguage.Enabled = true;

                LoadLanguage();
            }
        }

        /// <summary>
        ///     Clears the ListBox items and reloads all existing entries from disk.
        /// </summary>
        private void RefreshListbox()
        {
            LanguageList.BeginUpdate();
            LanguageList.Items.Clear();

            var directory_info = new DirectoryInfo(specialCharDir);
            if (!directory_info.Exists)
            {
                LanguageList.EndUpdate();
                return;
            }

            LanguageList.Items.AddRange(directory_info
                .EnumerateFiles("*.txt", SearchOption.TopDirectoryOnly)
                .Select(info => Path.GetFileNameWithoutExtension(info.FullName))
                .ToArray());

            LanguageList.EndUpdate();
        }

        private void LoadLanguage()
        {
            try
            {
                var info = new FileInfo(Path.Combine(specialCharDir,
                    LanguageList.Items[LanguageList.SelectedIndex] + ".txt"));
                if (!info.Exists)
                    RefreshListbox();

                using (var reader = new StreamReader(info.FullName, Encoding.UTF8))
                {
                    var builder = new StringBuilder();

                    while (!reader.EndOfStream) builder.Append(reader.ReadLine().Trim().Substring(0, 1));

                    TbChars.Text = builder.ToString();
                }

                TbLanguage.Text = LanguageList.Items[LanguageList.SelectedIndex].ToString();
            }
            catch
            {
                RefreshListbox();
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}