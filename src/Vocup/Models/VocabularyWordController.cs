﻿using System.Windows.Forms;

namespace Vocup.Models
{
    public class VocabularyWordController
    {
        private readonly ListViewItem.ListViewSubItem foreignLangColumn;
        private readonly ListViewItem.ListViewSubItem motherTongueColumn;
        private readonly ListViewItem.ListViewSubItem praticeDateColumn;

        public VocabularyWordController(VocabularyWord vocabularyWord)
        {
            ListViewItem = new ListViewItem {Tag = vocabularyWord};
            motherTongueColumn = ListViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(ListViewItem, ""));
            foreignLangColumn = ListViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(ListViewItem, ""));
            praticeDateColumn = ListViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(ListViewItem, ""));

            VocabularyWord = vocabularyWord;
            VocabularyWord.PropertyChanged += (a0, a1) => UpdateUI();
            UpdateUI();
        }

        public ListViewItem ListViewItem { get; }
        public VocabularyWord VocabularyWord { get; }

        private void UpdateUI()
        {
            ListViewItem.ImageIndex = (int) VocabularyWord.PracticeState;

            motherTongueColumn.Text = VocabularyWord.MotherTongue;
            foreignLangColumn.Text = VocabularyWord.ForeignLangText;
            if (VocabularyWord.PracticeDate == default)
                praticeDateColumn.Text = "";
            else
                praticeDateColumn.Text =
                    $"{VocabularyWord.PracticeDate.ToShortDateString()} {VocabularyWord.PracticeDate.ToShortTimeString()}";
        }
    }
}