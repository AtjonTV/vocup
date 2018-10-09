using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Vocup.Models;
using Vocup.Properties;

namespace Vocup
{
    public partial class SettingsDialog : Form
    {
        private Settings settings;

        public SettingsDialog()
        {
            InitializeComponent();
            Icon = Icon.FromHandle(Icons.Settings.GetHicon());
            settings = Settings.Default;
        }

        private void SettingsDialog_Load(object sender, EventArgs e)
        {
            // Startbild   
            RbRecentFile.Checked = settings.StartScreen == (int)StartScreen.LastFile || settings.StartScreen == (int)StartScreen.AboutBox;

            // Vokabelheft automatisch speichern
            CbAutoSave.Checked = settings.AutoSave;

            // Automatisches Update
            CbDisableInternetServices.Checked = settings.DisableInternetServices;

            // ListView
            CbGridLines.Checked = settings.GridLines;
            CbColumnResize.Checked = settings.ColumnResize;

            // Pfad Vokabelhefte
            TbVhfPath.Text = settings.VhfPath;

            // Pfad Ergebnisse
            TbVhrPath.Text = settings.VhrPath;

            // Selber bewerten
            CbManualCheck.Checked = settings.UserEvaluates;

            // Eingabefelder mit Farbe hervorheben
            CbColoredTextfield.Checked = settings.PracticeInputBackColor != SystemColors.Window;

            // Teilweise richtig
            checkbox_leerschl�ge.Checked = settings.EvaluateTolerateWhiteSpace;
            checkbox_satzzeichen.Checked = settings.EvaluateToleratePunctuationMark;
            checkbox_sonderzeichen.Checked = settings.EvaluateTolerateSpecialChar;
            checkbox_artikel.Checked = settings.EvaluateTolerateArticle;
            checkbox_synonyme.Checked = settings.EvaluateTolerateNoSynonym;

            // Fortfahren-Button
            CbSingleContinueButton.Checked = settings.PracticeFastContinue;

            // Kl�nge
            CbAcousticFeedback.Checked = settings.PracticeSoundFeedback;

            // Auswertung
            CbPracticeResult.Checked = settings.PracticeShowResultList;

            // Notensystem
            if (settings.PracticeGradeCulture == "de-DE")
                CbEvaluationSystem.SelectedItem = "Deutschland";
            else
                CbEvaluationSystem.SelectedItem = "Schweiz";


            // Anzahl richtig
            TrbRepetitions.Value = settings.MaxPracticeCount;

            // Max von trackbar-anzahl_richtig_falsch ermitteln
            TrbWrongRigtht.Maximum = 10 - TrbUnknown.Value;

            // Prozentualer Anteil an noch nicht ge�bten Vokabeln
            anzahl_noch_nicht_label.Text = settings.PracticePercentageUnpracticed + "%";
            TrbUnknown.Value = settings.PracticePercentageUnpracticed / 10;

            // Prozentualer Anteil an falsch ge�bten Vokabeln
            anzahl_falsch_label.Text = settings.PracticePercentageWrong + "%";

            // Prozentualer Anteil an richtig ge�bten Vokabeln
            anzahl_richtig_label.Text = settings.PracticePercentageCorrect + "%";

            // Trackbar anzahl_falsch_richtig
            TrbWrongRigtht.Maximum = (settings.PracticePercentageCorrect + settings.PracticePercentageWrong) / 10 - 1;
            TrbWrongRigtht.Value = settings.PracticePercentageCorrect / 10;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Einstellungen speichern

            // Startbild
            settings.StartScreen = RbRecentFile.Checked ? (int)StartScreen.LastFile : (int)StartScreen.None;

            // Vokabelheft automatisch speichern
            settings.AutoSave = CbAutoSave.Checked;

            // Automatisches Update
            settings.DisableInternetServices = CbDisableInternetServices.Checked;

            // ListView
            settings.GridLines = CbGridLines.Checked;
            settings.ColumnResize = CbColumnResize.Checked;

            // Pfad Vokabelhefte
            settings.VhfPath = TbVhfPath.Text;

            // Pfad Ergebnisse
            settings.VhrPath = TbVhrPath.Text;

            // Auswertung
            settings.PracticeShowResultList = CbPracticeResult.Checked;

            // Notensystem
            if (CbEvaluationSystem.SelectedItem.ToString() == "Deutschland")
            {
                settings.PracticeGradeCulture = "de-DE";
            }
            else
            {
                settings.PracticeGradeCulture = "de-CH";
            }

            // �bersetzungen selber bewerten
            settings.UserEvaluates = CbManualCheck.Checked;

            // Eingabefelder mit Farbe hervorheben
            settings.PracticeInputBackColor = CbColoredTextfield.Checked ? Color.FromArgb(250, 250, 150) : SystemColors.Window;

            // Teilweise richtig
            settings.EvaluateTolerateWhiteSpace = checkbox_leerschl�ge.Checked;
            settings.EvaluateToleratePunctuationMark = checkbox_satzzeichen.Checked;
            settings.EvaluateTolerateSpecialChar = checkbox_sonderzeichen.Checked;
            settings.EvaluateTolerateArticle = checkbox_artikel.Checked;
            settings.EvaluateTolerateNoSynonym = checkbox_synonyme.Checked;

            // Fortfahren-Button
            settings.PracticeFastContinue = CbSingleContinueButton.Checked;

            // Akustische R�ckmeldung
            settings.PracticeSoundFeedback = CbAcousticFeedback.Checked;

            // Anzahl richtig
            settings.MaxPracticeCount = TrbRepetitions.Value;

            // Prozentuale Anteile
            settings.PracticePercentageUnpracticed = TrbUnknown.Value * 10;
            settings.PracticePercentageCorrect = TrbWrongRigtht.Value * 10;
            settings.PracticePercentageWrong = (10 - TrbUnknown.Value - TrbWrongRigtht.Value) * 10;

            // Einstellungen speichern
            settings.Save();

            // Dialogfenster beenden
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnResetStartScreen_Click(object sender, EventArgs e)
        {
            settings.StartScreen = (int)StartScreen.AboutBox;
            RbRecentFile.Checked = true;
        }

        private void BtnResetPractice_Click(object sender, EventArgs e)
        {
            // Einstellungen zur�cksetzen

            TrbRepetitions.Value = 3;

            TrbUnknown.Value = 5;
            TrbWrongRigtht.Value = 2;
        }

        private void TrbUnknown_ValueChanged(object sender, EventArgs e)
        {
            anzahl_noch_nicht_label.Text = TrbUnknown.Value * 10 + "%";
            TrbWrongRigtht.Maximum = 10 - TrbUnknown.Value - 1;

            if (TrbUnknown.Value == 8)
            {
                TrbWrongRigtht.Enabled = false;
                anzahl_falsch_label.Text = "10%";
                anzahl_richtig_label.Text = "10%";
            }
            else
            {
                TrbWrongRigtht.Enabled = true;
                anzahl_richtig_label.Text = TrbWrongRigtht.Value * 10 + "%";
                anzahl_falsch_label.Text = (10 - TrbUnknown.Value - TrbWrongRigtht.Value) * 10 + "%";
            }
        }

        private void TrbWrongRight_ValueChanged(object sender, EventArgs e)
        {
            anzahl_richtig_label.Text = TrbWrongRigtht.Value * 10 + "%";
            anzahl_falsch_label.Text = (10 - TrbUnknown.Value - TrbWrongRigtht.Value) * 10 + "%";
        }

        private void CbPracticeResult_CheckedChanged(object sender, EventArgs e)
        {
            CbEvaluationSystem.Enabled = CbPracticeResult.Checked;
        }

        // Verzeichnis f�r VHF-Dateien ausw�hlen
        private void BtnVhfPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = settings.VhfPath;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    TbVhfPath.Text = fbd.SelectedPath;
                }
            }
        }

        // Verzeichnis f�r VHR-Dateien ausw�hlen
        private void BtnVhrPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = settings.VhrPath;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    TbVhrPath.Text = fbd.SelectedPath;
                }
            }
        }
    }
}