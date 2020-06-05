using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using Vocup.IO;
using Vocup.Models;
using Vocup.Properties;
using Vocup.Util;

namespace Vocup.Forms
{
    public partial class CreateBackup : Form
    {
        public CreateBackup()
        {
            InitializeComponent();
            Icon = Icon.FromHandle(Icons.DatabaseAdd.GetHicon());
        }

        private void Form_Load(object sender, EventArgs e)
        {
            // Check for vocabulary book files
            var booksInfo = new DirectoryInfo(Settings.Default.VhfPath);
            var count = 0;
            if (booksInfo.Exists &&
                (count = booksInfo.EnumerateFiles("*.vhf", SearchOption.AllDirectories).Count()) > 0)
            {
                CbSaveAllBooks.Checked = true;
                CbSaveAllBooks.Enabled = true;
            }

            CbSaveAllBooks.Text = string.Format(CbSaveAllBooks.Text, count);

            // Check for special char files
            var specialCharInfo = new DirectoryInfo(AppInfo.SpecialCharDirectory);
            if (specialCharInfo.Exists)
            {
                foreach (var file in specialCharInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
                    ListSpecialChars.Items.Add(Path.GetFileNameWithoutExtension(file.FullName));

                if (ListSpecialChars.Items.Count > 0) GroupSpecialChar.Enabled = true;
            }
        }

        private void CbSaveAllBooks_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void ListSpecialChars_SelectedValueChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        /// <summary>
        ///     Enables or disables the AcceptButton depending on the user's settings.
        /// </summary>
        private void UpdateUI()
        {
            BtnCreateBackup.Enabled = CbSaveAllBooks.Checked || ListVocabularyBooks.Items.Count > 0 ||
                                      ListSpecialChars.CheckedItems.Count > 0;
        }

        private void BtnAddVocabularyBook_Click(object sender, EventArgs e)
        {
            using (var add_file = new OpenFileDialog
            {
                Title = Words.AddVocabularyBooks,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Filter = Words.VocupVocabularyBookFile + " (*.vhf)|*.vhf",
                Multiselect = true
            })
            {
                if (add_file.ShowDialog() == DialogResult.OK)
                {
                    foreach (var path in add_file.FileNames)
                        if (!ListVocabularyBooks.Items.Contains(path))
                            // ExecuteBackup will prevent the user from including the same file twice in a backup
                            ListVocabularyBooks.Items.Add(path);

                    UpdateUI();
                }
            }
        }

        private void BtnDeleteVocabularyBook_Click(object sender, EventArgs e)
        {
            while (ListVocabularyBooks.SelectedItems.Count > 0)
                ListVocabularyBooks.Items.Remove(ListVocabularyBooks.SelectedItems[0]);

            BtnDeleteVocabularyBook.Enabled = false;

            UpdateUI();
        }

        private void ListVocabularyBooks_SelectedIndexChanged(object sender, EventArgs e)
        {
            BtnDeleteVocabularyBook.Enabled = ListVocabularyBooks.SelectedItems.Count > 0;
        }

        private void BtnCreateBackup_Click(object sender, EventArgs e)
        {
            using (var save = new SaveFileDialog
            {
                Title = Words.SaveBackup,
                Filter = Words.VocupBackupFile + " (*.vdp)|*.vdp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            })
            {
                if (save.ShowDialog() == DialogResult.OK)
                {
                    DialogResult = DialogResult.OK;
                    ExecuteBackup(save.FileName, CbSaveAllBooks.Checked,
                        ListVocabularyBooks.Items.Cast<string>().Select(x => new FileInfo(x)));
                }
                else
                {
                    DialogResult = DialogResult.Cancel;
                }
            }
        }

        private void ExecuteBackup(string path, bool allBooks, IEnumerable<FileInfo> additionalFiles)
        {
            Cursor.Current = Cursors.WaitCursor;

            using (var saveStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(saveStream, ZipArchiveMode.Create))
            {
                var backup = new BackupMeta();
                var booksInfo = new DirectoryInfo(Settings.Default.VhfPath);
                var counter = 0;

                if (allBooks)
                    AddBooks(booksInfo.EnumerateFiles("*.vhf", SearchOption.AllDirectories), archive, backup,
                        ref counter);

                additionalFiles = additionalFiles
                    .Where(info =>
                        !allBooks || !info.FullName.StartsWith(Settings.Default.VhfPath,
                            StringComparison.OrdinalIgnoreCase));
                AddBooks(additionalFiles, archive, backup, ref counter);

                AddSpecialChars(ListSpecialChars.CheckedItems.Cast<string>(), archive, backup);
                backup.Write(archive);
            }

            Cursor.Current = Cursors.Default;
        }

        private void AddBooks(IEnumerable<FileInfo> files, ZipArchive archive, BackupMeta backup, ref int counter)
        {
            foreach (var fileInfo in files)
            {
                var book = new VocabularyBook();
                if (VocabularyFile.ReadVhfFile(fileInfo.FullName, book))
                {
                    var vhr = VocabularyFile.ReadVhrFile(book);
                    var success = TryAddFile(fileInfo.FullName, archive, $"vhf\\{counter}.vhf");
                    var success2 = false;

                    if (success && vhr && CbSaveResults.Checked)
                        success2 = TryAddFile(Path.Combine(Settings.Default.VhrPath, book.VhrCode + ".vhr"), archive,
                            $"vhr\\{book.VhrCode}.vhr");
                    if (success)
                    {
                        backup.Books.Add(new BackupMeta.BookMeta(counter, BackupMeta.ShrinkPath(fileInfo.FullName),
                            success2 ? book.VhrCode : ""));
                        counter++;

                        if (success2) backup.Results.Add(book.VhrCode + ".vhr");
                    }
                }
            }
        }

        private void AddSpecialChars(IEnumerable<string> files, ZipArchive archive, BackupMeta backup)
        {
            foreach (var fileInfo in files.Select(path =>
                new FileInfo(Path.Combine(AppInfo.SpecialCharDirectory, path + ".txt"))))
                if (TryAddFile(fileInfo.FullName, archive, "chars\\" + fileInfo.Name))
                    backup.SpecialChars.Add(fileInfo.Name);
        }

        private bool TryAddFile(string source, ZipArchive archive, string destination)
        {
            try
            {
                var info = new FileInfo(source);
                using (var file = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var entry = archive.CreateEntry(destination);
                    entry.LastWriteTime = new DateTimeOffset(info.LastWriteTimeUtc);
                    using (var stream = entry.Open())
                    {
                        file.CopyTo(stream);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}