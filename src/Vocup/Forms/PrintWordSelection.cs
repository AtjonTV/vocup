using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Vocup.Models;
using Vocup.Properties;
using Vocup.Util;

namespace Vocup.Forms
{
    public partial class PrintWordSelection : Form
    {
        private readonly VocabularyBook book;
        private bool invertSides;
        private int pageNumber = 1;

        private readonly List<VocabularyWord> printList = new List<VocabularyWord>();
        private int wordNumber;

        public PrintWordSelection(VocabularyBook book)
        {
            InitializeComponent();
            Icon = Icon.FromHandle(Icons.Print.GetHicon());

            this.book = book;

            ListBox.BeginUpdate();
            foreach (var word in book.Words)
                ListBox.Items.Add($"{word.MotherTongue} - {word.ForeignLangText}", true);
            ListBox.EndUpdate();

            CbUnpracticed.Enabled = book.Statistics.Unpracticed > 0;
            CbWronglyPracticed.Enabled = book.Statistics.WronglyPracticed > 0;
            CbCorrectlyPracticed.Enabled = book.Statistics.CorrectlyPracticed > 0;
            CbFullyPracticed.Enabled = book.Statistics.FullyPracticed > 0;
        }

        private void BtnCheckAll_Click(object sender, EventArgs e)
        {
            SetItemsChecked(x => true, true);
            BtnContinue.Enabled = true;
            BtnContinue.Focus(); //Fokus auf weiter-Button
        }

        private void BtnUncheckAll_Click(object sender, EventArgs e)
        {
            SetItemsChecked(x => true, false);
            BtnContinue.Enabled = false;
        }

        private void BtnContinue_Click(object sender, EventArgs e)
        {
            for (var i = 0; i < book.Words.Count; i++) // Compose print list
                if (ListBox.GetItemChecked(i))
                    printList.Add(book.Words[i]);

            var dialog = new PrintDialog
            {
                AllowCurrentPage = false,
                AllowSomePages = false,
                UseEXDialog = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                invertSides = RbAskForMotherTongue.Checked;

                PrintList.PrinterSettings = dialog.PrinterSettings;
                PrintList.DocumentName = book.Name ?? Words.Vocup;
                PrintList.Print();
            }

            dialog.Dispose();
        }

        private void ListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            BtnContinue.Enabled = ListBox.CheckedItems.Count > 0;
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var selection = CbUnpracticed.Checked || CbWronglyPracticed.Checked || CbCorrectlyPracticed.Checked ||
                            CbFullyPracticed.Checked;

            BtnCheckAll.Enabled = !selection;
            BtnUncheckAll.Enabled = !selection;

            if (selection)
            {
                SetItemsChecked(x => x.PracticeState == PracticeState.Unpracticed, CbUnpracticed.Checked);
                SetItemsChecked(x => x.PracticeState == PracticeState.WronglyPracticed, CbWronglyPracticed.Checked);
                SetItemsChecked(x => x.PracticeState == PracticeState.CorrectlyPracticed, CbCorrectlyPracticed.Checked);
                SetItemsChecked(x => x.PracticeState == PracticeState.FullyPracticed, CbFullyPracticed.Checked);
            }
            else
            {
                SetItemsChecked(x => true, true);
            }
        }

        private void SetItemsChecked(Func<VocabularyWord, bool> predicate, bool value)
        {
            ListBox.BeginUpdate();

            for (var i = 0; i < book.Words.Count; i++)
                if (predicate(book.Words[i]))
                    ListBox.SetItemChecked(i, value);

            ListBox.EndUpdate();
        }

        private void PrintList_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            g.PageUnit = GraphicsUnit.Display;

            var hoffset = 0;
            var sitePrintedWords = 0;
            var tableBegin = 0;

            var sideOffset = 2;
            var lineOffset = 2;
            var lineThickness = 1;
            var textMinHeight = 17;

            using (var siteFont = new Font("Arial", 11))
            using (var centerFormat = new StringFormat {Alignment = StringAlignment.Center})
            {
                g.DrawString($"{Words.Site} {pageNumber}", siteFont, Brushes.Black,
                    e.MarginBounds.SetHeight(20).Move(0, -20), centerFormat);
            }

            using (var titleFont = new Font("Arial", 12, FontStyle.Bold))
            using (var centerFormat = new StringFormat {Alignment = StringAlignment.Center})
            {
                var name = string.IsNullOrWhiteSpace(book.Name) ? "" : book.Name + ": ";
                var left = invertSides ? book.ForeignLang : book.MotherTongue;
                var right = invertSides ? book.MotherTongue : book.ForeignLang;
                var title = $"{name}{left} - {right}";

                g.DrawString(title, titleFont, Brushes.Black, e.MarginBounds.MarginTop(hoffset).SetHeight(25),
                    centerFormat);
                hoffset += 25;
                tableBegin = hoffset;
            }

            using (var font = new Font("Arial", 10))
            using (var nearFormat = new StringFormat {Alignment = StringAlignment.Near})
            using (var pen = new Pen(Brushes.Black, lineThickness))
            {
                for (;; wordNumber++, sitePrintedWords++) // loop through printList
                {
                    var rect = e.MarginBounds.MarginTop(hoffset += lineOffset);
                    g.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Top); // Draw horizontal lines
                    hoffset += (int) pen.Width + lineOffset;

                    if (wordNumber >= printList.Count) break;

                    rect = e.MarginBounds.MarginTop(hoffset);
                    var word = printList[wordNumber];
                    var left = new Rectangle(rect.X, rect.Y, rect.Width / 2, rect.Height).MarginSide(sideOffset);
                    var right = new Rectangle(left.Right, rect.Y, rect.Width / 2, rect.Height).MarginSide(sideOffset)
                        .MarginLeft(lineThickness); // right column is smaller than the left one because of the line
                    var leftText = invertSides ? word.ForeignLangText : word.MotherTongue;
                    var rightText = invertSides ? word.MotherTongue : word.ForeignLangText;

                    var leftSize = g.MeasureString(leftText, font, left.Size, nearFormat, out var leftChars,
                        out var leftLines);
                    var rightSize = g.MeasureString(rightText, font, right.Size, nearFormat, out var rightChars,
                        out var rightLines);
                    var missingChars = leftChars < leftText.Length || rightChars < rightText.Length;
                    var textHeight = (int) Math.Max(textMinHeight, Math.Max(leftSize.Height, rightSize.Height));

                    if (sitePrintedWords > 0 && (missingChars || textHeight > rect.Height))
                    {
                        e.HasMorePages = true;
                        break;
                    }

                    g.DrawString(leftText, font, Brushes.Black, left, nearFormat);
                    g.DrawString(rightText, font, Brushes.Black, right, nearFormat);
                    hoffset += textHeight;
                }
            }

            using (var pen = new Pen(Brushes.Black, lineThickness)) // Draw vertical lines
            {
                var table = e.MarginBounds.MarginTop(tableBegin + lineOffset)
                    .SetHeight(hoffset - tableBegin - lineThickness - 2 * lineOffset);
                g.DrawLine(pen, table.Left, table.Top, table.Left, table.Bottom);
                g.DrawLine(pen, table.Right, table.Top, table.Right, table.Bottom);
                var middleX = table.Left + table.Width / 2;
                g.DrawLine(pen, middleX, table.Top, middleX, table.Bottom);
            }

            pageNumber++;
        }
    }
}