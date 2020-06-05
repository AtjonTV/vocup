using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using Vocup.Models;
using Vocup.Properties;
using Vocup.Util;

namespace Vocup.IO.Internal
{
    internal class VhfFile : VocupFile
    {
        public bool Read(string path, VocabularyBook book)
        {
            string plaintext;
            try
            {
                plaintext = ReadFile(path);
            }
            catch (NotSupportedException)
            {
                MessageBox.Show(Messages.VhfMustUpdate, Messages.VhfMustUpdateT, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            catch (FormatException)
            {
                MessageBox.Show(Messages.VhfCorruptFile, Messages.VhfCorruptFileT, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
            catch (CryptographicException)
            {
                MessageBox.Show(Messages.VhfCorruptFile, Messages.VhfCorruptFileT, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            using (var reader = new StringReader(plaintext))
            {
                var version = reader.ReadLine();
                var vhrCode = reader.ReadLine();
                var motherTongue = reader.ReadLine();
                var foreignLang = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(version) || !Version.TryParse(version, out var versionObj))
                {
                    MessageBox.Show(Messages.VhfInvalidVersion, Messages.VhfCorruptFileT, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                if (versionObj.CompareTo(AppInfo.FileVersion) == 1)
                {
                    MessageBox.Show(Messages.VhfMustUpdate, Messages.VhfMustUpdateT, MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                    return false;
                }

                if (vhrCode == null)
                {
                    MessageBox.Show(Messages.VhfInvalidVhrCode, Messages.VhfCorruptFileT, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                book.VhrCode = vhrCode;
                book.FilePath = path;

                if (string.IsNullOrWhiteSpace(motherTongue) ||
                    string.IsNullOrWhiteSpace(foreignLang) ||
                    motherTongue == foreignLang)
                {
                    MessageBox.Show(Messages.VhfInvalidLanguages, Messages.VhfCorruptFileT, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                book.MotherTongue = motherTongue;
                book.ForeignLang = foreignLang;

                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null) break;
                    var columns = line.Split('#');
                    if (columns.Length != 3)
                    {
                        MessageBox.Show(Messages.VhfInvalidRow, Messages.VhfCorruptFileT, MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }

                    var word = new VocabularyWord
                    {
                        Owner = book,
                        MotherTongue = columns[0],
                        ForeignLang = columns[1],
                        ForeignLangSynonym = columns[2]
                    };
                    book.Words.Add(word);
                }
            }

            return true;
        }

        public bool Write(string path, VocabularyBook book)
        {
            string raw;

            using (var writer = new StringWriter())
            {
                writer.WriteLine("1.0");
                writer.WriteLine(book.VhrCode);
                writer.WriteLine(book.MotherTongue);
                writer.WriteLine(book.ForeignLang);

                foreach (var word in book.Words)
                {
                    writer.Write(word.MotherTongue);
                    writer.Write('#');
                    writer.Write(word.ForeignLang);
                    writer.Write('#');
                    writer.WriteLine(word.ForeignLangSynonym ?? "");
                }

                raw = writer.ToString();
            }

            try
            {
                WriteFile(path, raw);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Messages.VocupFileWriteErrorEx, ex), Messages.VocupFileWriteErrorT,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
    }
}