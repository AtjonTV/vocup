using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SendFileTo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Vocup.Forms;
using Vocup.Properties;
using Vocup.Util;

namespace Vocup
{

    public partial class program_form : Form
    {

        //gibt an, ob die Datei geändert wurde
        bool edit_vokabelheft = false;

        //Gibt den Pfad zum Vokabelheft an
        string pfad_vokabelheft = "";

        //Übungsvariante

        string uebersetzungsrichtung = "";

        //Gibt den Code, der Vokabelheft-Datei an
        string vokabelheft_code = "";

        //Gibt das Start-Argument an
        string args_pfad = "";

        //Variable die bestimmt, ob eine neue Ergebnis-Datei erstellt werden muss
        bool save_new = false;

        //Liste der Vokabeln die ausgedruckt werden soll
        int[] vokabelliste;

        //von Mutter zu Fremdsprache oder umgekehrt drucken
        bool if_own_to_foreign;

        //Anzahl Vokabeln beim Drucken
        int anz_vok;

        //Anzahl zu druckende Seiten
        int anzahl_seiten;

        //Aktuelle zu druckende Seite
        int aktuelle_seite;

        //Vorder- oder Rückseite bei den Kärtchen
        bool if_foreside;

        //Papiereinzug
        bool is_front;

        public program_form(string[] args)
        {
            InitializeComponent();

            //Falls die exe mit einer Vokabelheft-Datei geöffnet wurde:
            if (args.Length != 0)
            {
                args_pfad = args[0];
            }
        }

        //Sobald die Form geladen wird
        private void program_form_Load(object sender, EventArgs e)
        {

            Update();
            Activate();

            //Schaut ob die zuletzt geöffnete Datei noch Existiert

            if (args_pfad != "")
            {
                FileInfo info = new FileInfo(args_pfad);
                if (info.Extension == ".vhf")
                {
                    //Öffnet die Vokabeldatei

                    readfile(args_pfad);
                    args_pfad = "";
                }
                else if (info.Extension == ".vdp")
                {
                    //Nicht löschen!!

                }
                else
                {
                    MessageBox.Show(Properties.language.messagebox_no_vhf,
                                             "Error",
                                             MessageBoxButtons.OK,
                                             MessageBoxIcon.Error);
                }
            }
            else if (File.Exists(Properties.Settings.Default.last_file) && Properties.Settings.Default.startscreen == "zuletzt")
            {
                readfile(Properties.Settings.Default.last_file); //Start-Screen festlegen
            }

        }

        private void program_form_Shown(object sender, EventArgs e)
        {
            //Falls nötig Datensicherung Wiederherstellen öffnen

            if (args_pfad != "")
            {
                FileInfo info_vdp = new FileInfo(args_pfad);
                if (info_vdp.Extension == ".vdp")
                {
                    //Öffnet die Datensicherung

                    restore_backup(args_pfad);
                    args_pfad = "";
                }
            }
            else if (Properties.Settings.Default.startscreen == "willkommensbild") //Willkommensbild anzeigen
            {
                //AboutBox anzeigen
                AboutBox about = new AboutBox();
                about.ShowDialog();

                //Start-Bild neu festlegen
                Properties.Settings.Default.startscreen = "zuletzt";

                Properties.Settings.Default.Save();
            }

            //Eventuell Updater ausschalten

            if (File.Exists(Path.Combine(Application.StartupPath, "updateroff.txt")))
            {
                nachUpdatesToolStripMenuItem.Enabled = false;
            }
            else if (Properties.Settings.Default.auto_update) //Eventuell nach Updates suchen
            {
                if (long.TryParse(Properties.Settings.Default.checked_for_updates, out long binary) &&
                    (DateTime.FromBinary(binary) - DateTime.Now).TotalDays >= 30.0) // New binary format instead of "{Year}|{DayOfYear}"
                {
                    try
                    {
                        search_update();
                        Properties.Settings.Default.checked_for_updates = DateTime.Now.ToBinary().ToString();
                    }
                    catch
                    {
                    }
                }
                else
                {
                    Properties.Settings.Default.checked_for_updates = DateTime.MinValue.ToBinary().ToString();
                    Properties.Settings.Default.Save();
                }
            }

            Properties.Settings.Default.startup_counter++;
            Properties.Settings.Default.Save();
        }

        //-----


        //Dialoge

        //Hife
        private void hilfeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "Hilfe.chm");
        }

        //AboutBox
        private void infoÜberVTrainingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }

        //Infos
        private void infos_Click(object sender, EventArgs e)
        {
            EvaluationInfoDialog infos = new EvaluationInfoDialog();
            infos.ShowDialog();
        }

        private void infosZurBewertungToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EvaluationInfoDialog infos = new EvaluationInfoDialog();
            infos.ShowDialog();
        }

        //Optionen
        private void optionenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Optionen aufrufen

            int max_vorher = Properties.Settings.Default.max;

            string path_vhf_vorher = Properties.Settings.Default.path_vhf;

            SettingsDialog optionen = new SettingsDialog();

            optionen.ShowDialog();

            // Max Richtig

            if (max_vorher != Properties.Settings.Default.max)
            {
                int y;
                for (int i = 0; i < listView_vokabeln.Items.Count; i++)
                {

                    y = Convert.ToInt32(listView_vokabeln.Items[i].Tag.ToString());
                    if (y > 1 && y < Properties.Settings.Default.max + 1)
                    {
                        listView_vokabeln.Items[i].ImageIndex = 2;
                    }
                    else if (y >= Properties.Settings.Default.max + 1)
                    {
                        listView_vokabeln.Items[i].ImageIndex = 3;
                    }
                }
            }

            //Treeview aktualisieren, falls der Vokabelheft-Pfad sich geändert hat

            if (path_vhf_vorher != Properties.Settings.Default.path_vhf)
            {
                treeView.Nodes.Clear();
                treeView_Enter(sender, e);
            }

            //Autosave

            if (Properties.Settings.Default.auto_save == true)
            {
                if (pfad_vokabelheft != "" && listView_vokabeln.Enabled == true)
                {
                    savefile(false);
                }
            }

            infos_vokabelhefte_text();
        }

        //Sonderzeichen verwalten

        private void sonderzeichenVerwaltenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Dialog starten
            SpecialCharManage special_char = new SpecialCharManage();
            special_char.ShowDialog();
        }

        //Updates
        private void nachUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            search_update();
        }

        //-----

        //Treeview

        private void treeView_Enter(object sender, EventArgs e)
        {
            TreeNode node;

            //Hauptstamm bestimmen
            if (treeView.Nodes.Count == 0)
            {
                treeView.Nodes.Clear();

                DirectoryInfo info = new DirectoryInfo(Properties.Settings.Default.path_vhf);
                if (info.Exists)
                {
                    node = treeView.Nodes.Add(info.Name);

                    //alle untergeordneten Verzeichnisse einlesen  
                    AllSubDirectories(node);
                }
            }

            node = treeView.SelectedNode;
        }

        private void AllSubDirectories(TreeNode node)
        {
            node.Nodes.Clear();

            DirectoryInfo[] arrDirInfo;
            FileInfo[] arrFileInfo;

            DirectoryInfo personal_info = new DirectoryInfo(Properties.Settings.Default.path_vhf);

            string personal = personal_info.Parent.FullName + "\\";

            DirectoryInfo dirinfo = new DirectoryInfo(personal + node.FullPath);


            //Directories 
            try
            {
                arrDirInfo = dirinfo.GetDirectories();
            }
            catch
            {
                return;
            }


            foreach (DirectoryInfo info in arrDirInfo)
            {
                node.Nodes.Add(info.Name);
                node.ImageIndex = 1;
                node.SelectedImageIndex = 1;

                node.Tag = "";
            }

            //Files
            try
            {
                arrFileInfo = dirinfo.GetFiles();
            }
            catch
            {
                return;
            }
            foreach (FileInfo info in arrFileInfo)
            {
                if (info.Extension == ".vhf")
                {
                    TreeNode file = new TreeNode();

                    string file_name = Path.GetFileNameWithoutExtension(info.FullName);

                    file.Text = file_name;
                    node.Nodes.Add(file);
                    file.ImageIndex = 2;
                    file.SelectedImageIndex = 2;

                    file.Tag = info.FullName;
                }
            }

            treeView.Nodes[0].ImageIndex = 0;
        }

        private void treeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {

            foreach (TreeNode node in e.Node.Nodes)
            {
                node.Collapse();

                AllSubDirectories(node);

                // Icons bestimmen
                //string pfad = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\" + node.FullPath;

                DirectoryInfo personal_info = new DirectoryInfo(Properties.Settings.Default.path_vhf);


                string pfad = personal_info.Parent.FullName + "\\" + node.FullPath;

                DirectoryInfo info = new DirectoryInfo(pfad);


                if (info.Exists == false)
                {
                    node.ImageIndex = 2;
                    node.SelectedImageIndex = 2;
                }
            }
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Nodes.Count == 0)
            {
                AllSubDirectories(e.Node);
            }

            //string personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal); //Bevor Version 0.2.5.0
            //personal = personal + "\\" + this.treeView.SelectedNode; //Bevor Version 0.2.5.0


            //Schauen, dass es sich nicht um einen Ordner handelt
            string pfad_file = Convert.ToString(this.treeView.SelectedNode.Tag);

            if (pfad_file != "")
            {
                FileInfo info = new FileInfo(pfad_file);

                if (info.Exists == true && info.Extension == ".vhf")
                {
                    //Klasse readfile aufrufen

                    bool result = true;

                    if (edit_vokabelheft == true)
                    {
                        result = vokabelheft_ask_to_save();
                    }

                    if (result == true)
                    {
                        readfile(pfad_file);
                    }
                }
                else
                {
                    //Fehlermeldung

                    MessageBox.Show(Properties.language.treeview_doesnt_exists,
                                   Properties.language.name,
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);
                }
            }
        }

        private void treeView_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node == treeView.Nodes[0])
            {
                treeView.Nodes.Clear();

                //string personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                //personal = personal + "\\" + Properties.language.personal_directory;

                string personal = Properties.Settings.Default.path_vhf;

                TreeNode node;

                //node = treeView.Nodes.Add(Properties.language.personal_directory);

                DirectoryInfo dirinfo = new DirectoryInfo(Properties.Settings.Default.path_vhf);

                node = treeView.Nodes.Add(dirinfo.Name);

                // alle untergeordneten Verzeichnisse einlesen  
                AllSubDirectories(node);

                treeView.SelectedNode = node;
                treeView.Nodes[0].ImageIndex = 0;
            }
        }

        //-----


        //Neues Vokabelheft erstellen

        private void new_vokabelheft_Click(object sender, EventArgs e)
        {
            create_new_vokabelheft();
        }

        private void neueVocabeldateiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            create_new_vokabelheft();
        }

        //-----

        //Vokabelheft bearbeiten

        private void vokabelheft_optionen_Click(object sender, EventArgs e)
        {
            edit_vokabelheft_dialog();
        }

        private void vokabelheftOptionenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            edit_vokabelheft_dialog();
        }

        //-----


        // Öffnen

        private void open_vokabelheft_Click(object sender, EventArgs e)
        {
            open_file();
        }

        private void vokabelheftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            open_file();
        }

        //-----


        // Vokabel hinzufügen

        private void insert_vokabel_Click(object sender, EventArgs e)
        {
            add_vokabel(false, false);
        }

        private void neueVokabelHinzufügenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            add_vokabel(false, false);
        }
        //-----


        //Vokabel bearbeiten

        private void edit_vokabel_Click(object sender, EventArgs e)
        {
            edit_vokabel_dialog();
        }

        private void vokabelBearbeitenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            edit_vokabel_dialog();
        }


        //Vokabeln löschen

        private void delet_vokabel_Click(object sender, EventArgs e)
        {
            //Vokabel löschen (Toolbar)

            vokabel_delete();
        }

        private void vokabelLöschenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Vokabel löschen (Menü)

            vokabel_delete();
        }

        //-----

        //Vokabelheft speichern

        private void SpeichernToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                savefile(false);
            }
            catch
            {
                pfad_vokabelheft = "";
                savefile(false);
            }
        }

        private void SpeichernUnterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pfad_vokabelheft != "")
            {
                savefile(true);
            }
            else
            {
                savefile(false);
            }
        }

        private void save_vokabelheft_Click(object sender, EventArgs e)
        {
            try
            {
                savefile(false);
            }
            catch
            {
                pfad_vokabelheft = "";
                savefile(false);
            }
        }

        //-----

        //Vokabeln Üben

        private void practice_vokabelheft_Click(object sender, EventArgs e)
        {
            vokabeln_üben();
        }

        private void vokabelnÜbenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            vokabeln_üben();
        }


        //-----

        //Vokabelheft drucken

        private void print_vokabelheft_Click(object sender, EventArgs e)
        {
            print_file();
        }

        private void druckenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            print_file();
        }

        //-----

        //Nach Vokabel suchen

        private void search_vokabel_button_Click(object sender, EventArgs e)
        {
            search_vokabel(search_vokabel_field.Text);
        }

        private void search_vokabel_field_Enter(object sender, EventArgs e)
        {
            //Falls das Suchfeld einen Text enthält, fokus auf Button setzen
            if (search_vokabel_button.Enabled == true)
            {
                AcceptButton = search_vokabel_button;
            }
        }

        private void search_vokabel_field_TextChanged(object sender, EventArgs e)
        {
            //Falls kein Suchtext eingegeben wurde, Button deaktivieren

            if (search_vokabel_field.Text != "")
            {
                search_vokabel_button.Enabled = true;
                AcceptButton = search_vokabel_button;
            }
            else
            {
                search_vokabel_button.Enabled = false;
                AcceptButton = insert_vokabel;
            }
        }

        //-----

        //Vokabelheft schliessen

        private void vokabelheftSchliessenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //

            bool result = true;

            if (edit_vokabelheft == true)
            {
                result = vokabelheft_ask_to_save();
            }

            //Falls eine neue Ergebnisdatei gespeichert werden soll
            else if (save_new == true)
            {
                try
                {
                    savefile(false);
                }
                catch
                {

                }
            }

            if (result == true)
            {
                close_vokabelheft();

            }
        }
        //-----

        //ListView


        private void listView_vokabeln_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            // Garantiert, dass die Spaltenbreite von Column 0 und  3 immer gleich bleibt

            if (listView_vokabeln.Enabled == true)
            {
                if (e.ColumnIndex == 0)
                {
                    listView_vokabeln.Columns[0].Width = 20;
                }
                else if (e.ColumnIndex == 3)
                {
                    listView_vokabeln.Columns[3].Width = 100;
                }
            }
        }

        private void listView_vokabeln_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            //Verhindert, dass die Spaltenbreite von column 0 und 3 geändert werden kann

            if (listView_vokabeln.Enabled == true)
            {
                if (e.ColumnIndex == 0)
                {
                    e.Cancel = true;
                }

                else if (e.ColumnIndex == 3)
                {
                    e.Cancel = true;
                }
            }
        }

        private void listView_vokabeln_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            //Bestimmen, ob die Lösch- Bearbeitungsbuttons angezeigt werden sollen
            if (listView_vokabeln.SelectedItems.Count > 0)
            {
                delet_vokabel.Enabled = true;
                edit_vokabel.Enabled = true;

                vokabelLöschenToolStripMenuItem.Enabled = true;
                vokabelBearbeitenToolStripMenuItem.Enabled = true;
            }

            else if (listView_vokabeln.SelectedItems.Count == 0)
            {
                delet_vokabel.Enabled = false;
                edit_vokabel.Enabled = false;

                vokabelLöschenToolStripMenuItem.Enabled = false;
                vokabelBearbeitenToolStripMenuItem.Enabled = false;
            }
        }

        private void listView_vokabeln_ItemActivate(object sender, EventArgs e)
        {
            delet_vokabel.Enabled = true;
            vokabelLöschenToolStripMenuItem.Enabled = true;

            edit_vokabel.Enabled = true;
            vokabelBearbeitenToolStripMenuItem.Enabled = true;
        }

        private void listView_vokabeln_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            edit_vokabel_dialog();
        }

        private void listView_vokabeln_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            object tag = listView_vokabeln.Columns[e.Column].Tag;
            if (tag == null || tag.ToString() == "Ascending")
            {
                listView_vokabeln.ListViewItemSorter = new ListViewComparer(e.Column, SortOrder.Descending);
                listView_vokabeln.Update();
                listView_vokabeln.Columns[e.Column].Tag = "Descending";
            }
            else
            {
                listView_vokabeln.ListViewItemSorter = new ListViewComparer(e.Column, SortOrder.Ascending);
                listView_vokabeln.Update();
                listView_vokabeln.Columns[e.Column].Tag = "Ascending";
            }
        }

        //-----



        //***Methoden***


        // Datei einlesen

        private void readfile(string file_path)
        {
            //Cursor auf warten setzen
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                //Falls eine neue Ergebnis-Datei gespeichert werden soll
                if (save_new == true)
                {
                    savefile(false);
                }

                listView_vokabeln.Enabled = false;

                listView_vokabeln.BeginUpdate();

                vokabelheft_code = "";
                edit_vokabelheft = false;

                save_vokabelheft.Enabled = false;
                SpeichernToolStripMenuItem.Enabled = false;

                uebersetzungsrichtung = "";

                pfad_vokabelheft = "";

                save_new = false;


                // Read file and decrypt
                string plaintext;
                using (StreamReader reader = new StreamReader(file_path, Encoding.UTF8))
                    plaintext = Crypto.Decrypt(reader.ReadToEnd());

                // Zeilen der Datei in ein Array abspeichern
                string[] lines = plaintext.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                // Anzahl Arrayfelder bestimmen
                string version = lines[0];

                //Schauen ob Vokabeln vorhanden sind
                if (lines.Length > 4 && version == "1.0")
                {
                    //Anzahl vokabeln in einem String 
                    string[,] vokabeln = new string[lines.Length - 4, 3];

                    // vokabeln splitten
                    for (int linesnumb = 4; linesnumb <= lines.Length - 1; linesnumb++)
                    {
                        //linesnumb + 1 ^= Vokabel (in einem Array wird bei 0 begonnen) | 1. Vokabel bei zeile Nr. 5
                        string[] i = lines[linesnumb].Split('#');

                        //Vokabeln von lines zu vokabeln
                        vokabeln[linesnumb - 4, 0] = i[0];
                        vokabeln[linesnumb - 4, 1] = i[1];
                        vokabeln[linesnumb - 4, 2] = i[2];
                    }

                    //Header

                    listView_vokabeln.BeginUpdate();

                    if (listView_vokabeln.Items.Count > 0)
                    {
                        listView_vokabeln.Clear();
                    }
                    if (listView_vokabeln.Columns.Count > 0)
                    {
                        listView_vokabeln.Columns.Clear();
                    }

                    listView_vokabeln.Invalidate();

                    listView_vokabeln.Columns.Add("", 20);

                    //Header anpassen, falls Fenster maximiert ist
                    if (WindowState == FormWindowState.Maximized)
                    {
                        int size = (listView_vokabeln.Width - 20 - 100 - 22) / 2;

                        listView_vokabeln.Columns.Add(lines[2], size);
                        listView_vokabeln.Columns.Add(lines[3], size);
                    }
                    else
                    {
                        listView_vokabeln.Columns.Add(lines[2], 155);
                        listView_vokabeln.Columns.Add(lines[3], 200);
                    }

                    listView_vokabeln.Columns.Add(Words.LastPracticed, 100);

                    listView_vokabeln.EndUpdate();

                    //code speichern
                    vokabelheft_code = lines[1];

                    // Datei mit Ergebnissen aufrufen
                    string ergebnisse_pfad = Path.Combine(Properties.Settings.Default.path_vhr, vokabelheft_code + ".vhr");
                    FileInfo check_file = new FileInfo(ergebnisse_pfad);

                    if (check_file.Exists == true)
                    {
                        try
                        {
                            // Read and decrypt file
                            string plaintext2;
                            using (StreamReader ergebnisse_reader = new StreamReader(ergebnisse_pfad, Encoding.UTF8))
                                plaintext2 = Crypto.Decrypt(ergebnisse_reader.ReadToEnd());

                            // Zeilen der Datei in ein Array abspeichern
                            string[] ergebnisse_lines = plaintext2.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                            // Array splitten
                            string[,] ergebnisse = new string[ergebnisse_lines.Length - 2, 2];

                            //Übungsvariante speichern
                            uebersetzungsrichtung = ergebnisse_lines[1];

                            for (int counter = 2; counter <= ergebnisse_lines.Length - 1; counter++)
                            {
                                string[] i = ergebnisse_lines[counter].Split('#');

                                //Vokabeln von ergebnisse_lines zu ergebnisse

                                ergebnisse[counter - 2, 0] = i[0];
                                ergebnisse[counter - 2, 1] = i[1];
                            }

                            bool result = false;

                            //Vokabeln einlesen falls Ergebnisse vorhanden sind
                            //und der Pfad stimmt

                            if (ergebnisse_lines[0] == file_path && ergebnisse_lines.Length - 2 == lines.Length - 4)
                            {
                                result = true;
                            }

                            // Falls Pfad stimmt, aber nicht gleiche Anzahl Linien

                            else if (ergebnisse_lines[0] == file_path && ergebnisse_lines.Length - 2 != lines.Length - 4)
                            {
                                MessageBox.Show(Properties.language.messagebox_file_copy,
                                Properties.language.name,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                                vokabelheft_code = "";

                                save_new = true;
                            }

                            //Falls Pfad nicht stimmt , aber gleiche Anzahl Linien

                            else if (ergebnisse_lines[0] != file_path && ergebnisse_lines.Length - 2 == lines.Length - 4)
                            {

                                result = true;

                                //Überprüfen, ob es eine Datei zum Pfad gibt --> Falls nicht, wird der Pfad geändert. Ansonsten neue Datei erstellen

                                FileInfo info = new FileInfo(ergebnisse_lines[0]);

                                if (info.Exists == true)
                                {
                                    vokabelheft_code = "";
                                }

                                save_new = true;
                            }

                            //Falls Pfad und Anzahl Linien nicht stimmen

                            else if (ergebnisse_lines[0] != file_path && ergebnisse_lines.Length - 2 != lines.Length - 4)
                            {
                                MessageBox.Show(Properties.language.messagebox_file_copy,
                                Properties.language.name,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);


                                vokabelheft_code = "";

                                save_new = true;
                            }

                            //Falls Datei fehlerhaft ist
                            else
                            {
                                MessageBox.Show(Properties.language.messagebox_fautly_results,
                                    Properties.language.name,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);

                                vokabelheft_code = "";

                                save_new = true;
                            }


                            if (result == true)
                            {

                                //Max. beim üben ermitteln

                                int max = Properties.Settings.Default.max;

                                for (int voknum = 0; voknum < (vokabeln.Length) / 3; voknum++)
                                {

                                    int anzahl_geübt = Convert.ToInt32(ergebnisse[voknum, 0]);


                                    string[] subitems = new string[4];

                                    subitems[0] = "";
                                    subitems[1] = vokabeln[voknum, 0];
                                    subitems[2] = vokabeln[voknum, 1];


                                    //Falls ein Synonym vorhanden ist

                                    if (vokabeln[voknum, 2] != "")
                                    {
                                        subitems[2] = subitems[2] + "=" + vokabeln[voknum, 2];
                                    }

                                    subitems[3] = ergebnisse[voknum, 1];

                                    //Neues Listview-Item erstellen
                                    ListViewItem new_item = new ListViewItem(subitems);

                                    // Ergebniss eintragen


                                    if (anzahl_geübt == 0)
                                    {
                                        new_item.ImageIndex = 0;
                                    }

                                    else if (anzahl_geübt == 1)
                                    {
                                        new_item.ImageIndex = 1;
                                    }
                                    else if (anzahl_geübt > 1 && anzahl_geübt < max + 1)
                                    {
                                        new_item.ImageIndex = 2;
                                    }
                                    else if (anzahl_geübt >= max + 1)
                                    {
                                        new_item.ImageIndex = 3;
                                    }

                                    new_item.Tag = anzahl_geübt;

                                    //Item Hinzufügen
                                    listView_vokabeln.Items.Add(new_item);
                                }
                                //Statistik 

                            }
                            else
                            {
                                //Vokabeln einlesen falls ergebnisse nicht vorhanden sind

                                for (int voknum = 0; voknum < (vokabeln.Length) / 3; voknum++)
                                {
                                    string[] subitems = new string[4];

                                    subitems[0] = "";
                                    subitems[1] = vokabeln[voknum, 0];
                                    subitems[2] = vokabeln[voknum, 1];

                                    //Falls ein Synonym vorhanden ist
                                    if (vokabeln[voknum, 2] != "")
                                    {
                                        subitems[2] = subitems[2] + "=" + vokabeln[voknum, 2];
                                    }

                                    subitems[3] = "";

                                    ListViewItem new_item = new ListViewItem(subitems);

                                    new_item.ImageIndex = 0;
                                    new_item.Tag = 0;

                                    //Item Hinzufügen
                                    listView_vokabeln.Items.Add(new_item);
                                }
                            }
                        }
                        catch
                        {
                            MessageBox.Show(Properties.language.messagebox_fautly_results,
                                   Properties.language.name,
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);

                            FileInfo info = new FileInfo(ergebnisse_pfad);

                            try
                            {
                                info.Delete();
                            }
                            catch
                            {

                            }
                            readfile(file_path);

                        }
                    }
                    else
                    {
                        //Vokabeln einlesen falls ergebnisse nicht vorhanden sind

                        for (int voknum = 0; voknum < (vokabeln.Length) / 3; voknum++)
                        {
                            string foreign = vokabeln[voknum, 1];
                            if (!string.IsNullOrWhiteSpace(vokabeln[voknum, 2])) // Display a synonym
                                foreign += "=" + vokabeln[voknum, 2];
                            string[] subitems = { "", vokabeln[voknum, 0], foreign, "" }; /*new string[4];*/

                            ListViewItem new_item = new ListViewItem(subitems);

                            new_item.ImageIndex = 0;
                            new_item.Tag = 0;

                            listView_vokabeln.Items.Add(new_item); //Item Hinzufügen
                        }
                    }

                    //ListView sortieren
                    listView_vokabeln.ListViewItemSorter = new ListViewComparer(1, SortOrder.Ascending);

                    // Pfad in Vokabel_infos speichern
                    pfad_vokabelheft = file_path;

                    //Pfad speichern
                    Properties.Settings.Default.last_file = file_path;
                    Properties.Settings.Default.Save();

                    //Infos
                    infos_vokabelhefte_text();

                    listView_vokabeln.Enabled = true;
                    listView_vokabeln.BackColor = SystemColors.Window;

                    //group-boxes aktivieren
                    listView_vokabeln.GridLines = Properties.Settings.Default.GridLines;

                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    statistik.Visible = true;

                    practice_vokabelheft.Enabled = true;
                    vokabelnÜbenToolStripMenuItem.Enabled = true;

                    neueVokabelHinzufügenToolStripMenuItem.Enabled = true;
                    vokabelheftOptionenToolStripMenuItem.Enabled = true;

                    edit_vokabel.Enabled = false;
                    vokabelBearbeitenToolStripMenuItem.Enabled = false;

                    delet_vokabel.Enabled = false;
                    vokabelLöschenToolStripMenuItem.Enabled = false;

                    search_vokabel_field.Enabled = true;
                    search_vokabel_field.Text = "";

                    SpeichernUnterToolStripMenuItem.Enabled = true;

                    vokabelheftSchliessenToolStripMenuItem.Enabled = true;

                    druckenToolStripMenuItem.Enabled = true;
                    print_vokabelheft.Enabled = true;

                    vokabelheftSendenToolStripMenuItem.Enabled = true;

                    exportierenToolStripMenuItem.Enabled = true;

                    //Titelleiste
                    string file_name = Path.GetFileNameWithoutExtension(pfad_vokabelheft);

                    Text = $"{Words.Vocup} - {Path.GetFileNameWithoutExtension(pfad_vokabelheft)}";

                    //Falls save_new == true wird der neue Pfad in die Ergebnis-Datei geschrieben und falls vokabelheft_code = "" wird die ganze datei neu geschrieben
                    //--> Siehe Close()
                }
                else if (lines.Length == 4 && version == "1.0")
                {
                    vokabelheft_code = "";
                    // Listview vorbereiten

                    //Header
                    listView_vokabeln.Clear();
                    listView_vokabeln.Columns.Clear();
                    listView_vokabeln.Columns.Add("", 20);

                    //Header anpassen, falls Fenster maximiert ist
                    if (WindowState == FormWindowState.Maximized)
                    {
                        int size = (listView_vokabeln.Width - 20 - 100 - 22) / 2;

                        listView_vokabeln.Columns.Add(lines[2], size);
                        listView_vokabeln.Columns.Add(lines[3], size);
                    }
                    else
                    {
                        listView_vokabeln.Columns.Add(lines[2], 155);
                        listView_vokabeln.Columns.Add(lines[3], 200);
                    }

                    listView_vokabeln.Columns.Add(Words.LastPracticed, 100);

                    // Pfad in Vokabel_infos speichern
                    pfad_vokabelheft = file_path;
                    vokabelheft_code = lines[1];
                    edit_vokabelheft = false;

                    //Pfad speichern
                    Properties.Settings.Default.last_file = file_path;
                    Properties.Settings.Default.Save();

                    //Infos
                    infos_vokabelhefte_text();
                    listView_vokabeln.Enabled = true;
                    listView_vokabeln.BackColor = SystemColors.Window;

                    //group-boxes aktivieren

                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    statistik.Visible = true;

                    practice_vokabelheft.Enabled = false;
                    vokabelnÜbenToolStripMenuItem.Enabled = false;

                    search_vokabel_button.Enabled = false;
                    search_vokabel_field.Text = "";
                    search_vokabel_field.Enabled = false;

                    neueVokabelHinzufügenToolStripMenuItem.Enabled = true;
                    vokabelheftOptionenToolStripMenuItem.Enabled = true;

                    edit_vokabel.Enabled = false;
                    vokabelBearbeitenToolStripMenuItem.Enabled = false;

                    delet_vokabel.Enabled = false;
                    vokabelLöschenToolStripMenuItem.Enabled = false;

                    save_vokabelheft.Enabled = false;
                    SpeichernToolStripMenuItem.Enabled = false;
                    SpeichernUnterToolStripMenuItem.Enabled = true;

                    vokabelheftSchliessenToolStripMenuItem.Enabled = true;

                    druckenToolStripMenuItem.Enabled = false;
                    print_vokabelheft.Enabled = false;

                    vokabelheftSendenToolStripMenuItem.Enabled = false;

                    exportierenToolStripMenuItem.Enabled = false;


                    //Titelleiste
                    Text = $"{Words.Vocup} - {Path.GetFileNameWithoutExtension(pfad_vokabelheft)}";
                }
                else if (version != "1.0" && lines[0].Length <= 4 || lines[0].Length <= 4 && Convert.ToInt32(version) > 1)
                {

                    close_vokabelheft();

                    // Datei zu neu

                    DialogResult result = MessageBox.Show(Properties.language.messagebox_new_program_version,
                      Properties.language.name,
                      MessageBoxButtons.YesNo,
                      MessageBoxIcon.Information);

                    //Schauen, ob nach Updates gesucht werden soll

                    if (result == DialogResult.Yes)
                    {
                        search_update();
                    }

                }
                else if (lines[0].Length > 4 || lines.Length < 4)
                {
                    close_vokabelheft();

                    MessageBox.Show(Properties.language.messagebox_faulty_file,
                                    Properties.language.error,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
            }
            catch
            {
                close_vokabelheft();

                MessageBox.Show(Properties.language.messagebox_reading_fault,
                                 Properties.language.error,
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Error);

            }

            listView_vokabeln.EndUpdate();

            //Cursor auf normal setzen
            Cursor.Current = Cursors.Default;
        }

        // Fragen, ob Änderungen gespeichert werden sollen

        private bool vokabelheft_ask_to_save()
        {
            DialogResult result = MessageBox.Show(Properties.language.messagebox_ask_to_save,
                                          Properties.language.name,
                                          MessageBoxButtons.YesNoCancel,
                                          MessageBoxIcon.Question);

            if (result == DialogResult.Yes) // Falls ja geklickt wir, wird die Datei gespeichert und true zurückgeliefert
            {
                if (pfad_vokabelheft == "")
                {
                    bool saved = savefile(true);

                    if (saved == false)
                    {
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        savefile(false);
                    }
                    catch
                    {
                        bool saved = savefile(true);

                        if (saved == false)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            else if (result == DialogResult.No) // Falls nein geklickt wir, wird die Datei nicht gespeichert und true zurückgeliefert
            {
                edit_vokabelheft = false;
                return true;
            }
            else // Falls Abbrechen geklickt wir, wird die Datei nicht gespeichert und false zurückgeliefert
            {
                return false;
            }
        }

        //Vokabelheft speichern

        private bool savefile(bool saveAsNewFile)
        {
            Cursor.Current = Cursors.WaitCursor;

            string pfad = pfad_vokabelheft;

            bool saved;

            //Datei-Speichern-unter Dialogfeld öffnen
            if (string.IsNullOrWhiteSpace(pfad_vokabelheft) || saveAsNewFile)
            {
                SaveFileDialog save = new SaveFileDialog
                {
                    Title = Words.SaveVocabularyBook,
                    FileName = listView_vokabeln.Columns[1].Text + " - " + listView_vokabeln.Columns[2].Text,
                    InitialDirectory = Properties.Settings.Default.path_vhf,
                    Filter = Words.VocupVocabularyBookFile + " (*.vhf)|*.vhf"
                };

                if (save.ShowDialog() == DialogResult.OK)
                {
                    pfad = save.FileName;
                }
            }

            if (!string.IsNullOrWhiteSpace(pfad))
            {
                StringBuilder writer = new StringBuilder();

                writer.AppendLine("1.0");

                if (string.IsNullOrWhiteSpace(vokabelheft_code) || saveAsNewFile)
                {
                    vokabelheft_code = GenerateCode();
                }

                writer.AppendLine(vokabelheft_code);

                writer.AppendLine(listView_vokabeln.Columns[1].Text);
                writer.Append(listView_vokabeln.Columns[2].Text);

                if (listView_vokabeln.Items.Count > 0)
                {
                    writer.AppendLine();

                    //Vokabeln abspeichern
                    for (int i = 0; i < listView_vokabeln.Items.Count; i++)
                    {
                        if (listView_vokabeln.Items[i].SubItems[2].Text.Contains("="))
                        {
                            string[] x = listView_vokabeln.Items[i].SubItems[2].Text.Split('=');
                            writer.Append(listView_vokabeln.Items[i].SubItems[1].Text);
                            writer.Append('#');
                            writer.Append(x[0]);
                            writer.Append('#');
                            writer.Append(x[1]);
                        }
                        else
                        {
                            writer.Append(listView_vokabeln.Items[i].SubItems[1].Text);
                            writer.Append('#');
                            writer.Append(listView_vokabeln.Items[i].SubItems[2].Text);
                            writer.Append('#');
                        }
                        if (i < listView_vokabeln.Items.Count - 1)
                        {
                            writer.AppendLine();
                        }
                    }
                }

                //Verschlüsseln

                using (StreamWriter fileWriter = new StreamWriter(pfad, false, Encoding.UTF8))
                    fileWriter.Write(Crypto.Encrypt(writer.ToString()));

                //Ergebnisse Abspeichern

                StringBuilder writer2 = new StringBuilder();

                writer2.AppendLine(pfad);

                //Übungsvariante speichern

                writer2.Append(string.IsNullOrWhiteSpace(uebersetzungsrichtung) ? "1" : uebersetzungsrichtung);

                if (listView_vokabeln.Items.Count > 0)
                {
                    writer2.AppendLine();

                    for (int i = 0; i < listView_vokabeln.Items.Count; i++)
                    {
                        writer2.Append(listView_vokabeln.Items[i].Tag);
                        writer2.Append('#');
                        writer2.Append(listView_vokabeln.Items[i].SubItems[3].Text);

                        if (i < listView_vokabeln.Items.Count - 1)
                        {
                            writer2.AppendLine();
                        }
                    }
                }

                //Verschlüsseln

                using (StreamWriter fileWriter = new StreamWriter(Path.Combine(Properties.Settings.Default.path_vhr, vokabelheft_code + ".vhr"), false, Encoding.UTF8))
                    fileWriter.Write(Crypto.Encrypt(writer2.ToString()));

                if (!saveAsNewFile)
                {
                    edit_vokabelheft = false;
                    save_vokabelheft.Enabled = false;
                    SpeichernToolStripMenuItem.Enabled = false;
                    pfad_vokabelheft = pfad;

                    Properties.Settings.Default.last_file = pfad_vokabelheft;
                    Properties.Settings.Default.Save();
                }

                saved = true;
            }
            else //Falls im Speichern Dialog auf Abbrechen geklickt wurde
            {
                saved = false;
            }


            //TreeView aktualisieren

            bool node_0 = false;

            if (treeView.Nodes[0].IsExpanded == true)
            {
                node_0 = true;
            }
            AllSubDirectories(treeView.Nodes[0]);
            treeView.CollapseAll();
            if (node_0 == true)
            {
                treeView.Nodes[0].Expand();
            }

            //Titelleiste
            Text = $"{Words.Vocup} - {Path.GetFileNameWithoutExtension(pfad_vokabelheft)}";

            //Cursor auf normal setzen
            Cursor.Current = Cursors.Default;

            return saved;
        }

        //--> Code Generieren
        [Obsolete("program_form.GenerateCode() is deprecated. Please use VocabularyBook.GenerateVhrCode() instead.", false)]
        private string GenerateCode()
        {
            // No need for RNGCryptoServiceProvider here because this is not security critical.
            int number1 = '0', number2 = '9';
            int bigLetter1 = 'A', bigLetter2 = 'Z';
            int smallLetter1 = 'a', smallLetter2 = 'z';

            Random random = new Random();
            char[] code = new char[24];

            do
            {

                int i = 0;
                while (i < code.Length)
                {
                    int character = random.Next(number1, smallLetter2);
                    if ((character >= number1 && character <= number2) ||
                        (character >= bigLetter1 && character <= bigLetter2) ||
                        (character >= smallLetter1 && character <= smallLetter2))
                    {
                        code[i] = (char)character;
                        i++;
                    }
                }

            } while (File.Exists(Path.Combine(Properties.Settings.Default.path_vhr, code + ".vhr")));

            return new string(code);
        }

        // Öffnen Dialog

        private void open_file()
        {

            //Dateien öffnen

            listView_vokabeln.Enabled = false;

            OpenFileDialog open = new OpenFileDialog
            {
                Title = Words.OpenVocabularyBook,
                InitialDirectory = Properties.Settings.Default.path_vhf,
                Filter = Words.VocupVocabularyBookFile + " (*.vhf)|*.vhf"
            };

            if (open.ShowDialog() == DialogResult.OK)
            {
                //Datei einlesen

                bool result = true;
                if (edit_vokabelheft == true)
                {
                    result = vokabelheft_ask_to_save();
                }
                if (result == true)
                {

                    listView_vokabeln.BeginUpdate();

                    readfile(open.FileName);

                    listView_vokabeln.EndUpdate();

                    listView_vokabeln.Enabled = true;
                    listView_vokabeln.BackColor = SystemColors.Window;

                    treeView.SelectedNode = treeView.Nodes[0];

                }
            }
            else
            {
                listView_vokabeln.Enabled = true;
            }


            //TreeView aktualisieren

            bool node_0 = false;

            if (treeView.Nodes[0].IsExpanded == true)
            {
                node_0 = true;
            }
            AllSubDirectories(treeView.Nodes[0]);
            treeView.CollapseAll();
            if (node_0 == true)
            {
                treeView.Nodes[0].Expand();
            }
        }

        //neues Vokabelheft erstellen

        private void create_new_vokabelheft()
        {

            listView_vokabeln.Enabled = false;

            //Dialog starten
            VocabularyBookSettings add_vokabelheft = new VocabularyBookSettings();

            //Icon
            add_vokabelheft.Icon = Icon.FromHandle(Icons.blank_file.GetHicon());

            //Optionen abschalten
            add_vokabelheft.option_box.Enabled = false;

            //Setzt die mForm als Besitzer
            add_vokabelheft.Owner = this;


            DialogResult new_vokabelheft_result = add_vokabelheft.ShowDialog();

            if (DialogResult.OK == new_vokabelheft_result)
            {
                //Listview vorbereiten
                bool result = true;

                if (edit_vokabelheft == true)
                {
                    result = vokabelheft_ask_to_save();
                }

                //Falls eine neue Datei gespeichert werden soll
                else if (save_new == true)
                {
                    savefile(false);
                }

                if (result == true)
                {

                    listView_vokabeln.BeginUpdate();

                    listView_vokabeln.GridLines = Properties.Settings.Default.GridLines;

                    listView_vokabeln.Clear();
                    listView_vokabeln.Columns.Clear();

                    listView_vokabeln.BackColor = SystemColors.Window;

                    listView_vokabeln.Columns.Add("", 20);

                    //Header anpassen, falls Fenster maximiert ist
                    if (WindowState == FormWindowState.Maximized)
                    {
                        int size = (listView_vokabeln.Width - 20 - 100 - 22) / 2;

                        listView_vokabeln.Columns.Add(add_vokabelheft.TbMotherTongue.Text, size);
                        listView_vokabeln.Columns.Add(add_vokabelheft.TbForeignLang.Text, size);
                    }
                    else
                    {
                        listView_vokabeln.Columns.Add(add_vokabelheft.TbMotherTongue.Text, 155);
                        listView_vokabeln.Columns.Add(add_vokabelheft.TbForeignLang.Text, 200);
                    }

                    listView_vokabeln.Columns.Add(Words.LastPracticed, 100);

                    listView_vokabeln.Enabled = true;

                    listView_vokabeln.EndUpdate();


                    pfad_vokabelheft = "";

                    save_new = false;

                    vokabelheft_edited();

                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    statistik.Visible = true;

                    practice_vokabelheft.Enabled = false;
                    vokabelnÜbenToolStripMenuItem.Enabled = false;

                    neueVokabelHinzufügenToolStripMenuItem.Enabled = true;
                    vokabelheftOptionenToolStripMenuItem.Enabled = true;

                    delet_vokabel.Enabled = false;
                    vokabelLöschenToolStripMenuItem.Enabled = false;

                    search_vokabel_button.Enabled = false;
                    search_vokabel_field.Text = "";
                    search_vokabel_field.Enabled = false;

                    SpeichernUnterToolStripMenuItem.Enabled = true;

                    druckenToolStripMenuItem.Enabled = false;
                    print_vokabelheft.Enabled = false;

                    vokabelheftSendenToolStripMenuItem.Enabled = false;

                    exportierenToolStripMenuItem.Enabled = false;


                    infos_vokabelhefte_text();
                    insert_vokabel.Focus();
                    vokabelheft_code = "";

                    //Titelleiste

                    Text = Words.Vocup;

                    treeView.SelectedNode = treeView.Nodes[0];

                    //Uebersetzungsrichtung speichern

                    if (add_vokabelheft.RbModeAskForeignTongue.Checked == true)
                    {
                        uebersetzungsrichtung = "1";
                    }
                    else if (add_vokabelheft.RbModeAskMotherTongue.Checked == true)
                    {
                        uebersetzungsrichtung = "2";
                    }
                }
            }
            else
            {
                listView_vokabeln.Enabled = true;
            }
        }

        //Vokabelheft schliessen

        private void close_vokabelheft()
        {
            // Error-Dialog anzeigen

            listView_vokabeln.Clear();
            listView_vokabeln.Columns.Clear();
            listView_vokabeln.Enabled = false;
            listView_vokabeln.BackColor = DefaultBackColor;
            listView_vokabeln.Update();

            groupBox1.Enabled = false;
            groupBox2.Enabled = false;
            groupBox3.Enabled = false;
            statistik.Visible = false;

            practice_vokabelheft.Enabled = false;
            vokabelnÜbenToolStripMenuItem.Enabled = false;

            neueVokabelHinzufügenToolStripMenuItem.Enabled = false;
            vokabelheftOptionenToolStripMenuItem.Enabled = false;

            edit_vokabel.Enabled = false;
            vokabelBearbeitenToolStripMenuItem.Enabled = false;

            delet_vokabel.Enabled = false;
            vokabelLöschenToolStripMenuItem.Enabled = false;

            search_vokabel_field.Enabled = false;
            search_vokabel_field.Text = "";

            SpeichernUnterToolStripMenuItem.Enabled = false;
            save_vokabelheft.Enabled = false;
            SpeichernToolStripMenuItem.Enabled = false;

            vokabelheftSchliessenToolStripMenuItem.Enabled = false;

            druckenToolStripMenuItem.Enabled = false;
            print_vokabelheft.Enabled = false;

            vokabelheftSendenToolStripMenuItem.Enabled = false;

            exportierenToolStripMenuItem.Enabled = false;



            vokabelheft_code = "";
            edit_vokabelheft = false;

            uebersetzungsrichtung = "";

            pfad_vokabelheft = "";

            //Titelleiste

            Text = Properties.language.name;

            treeView.SelectedNode = treeView.Nodes[0];
        }

        //Vokabel hinzufügen

        private void add_vokabel(bool fertig, bool show_specialchars)
        {
            string own_language = listView_vokabeln.Columns[1].Text;
            string foreign_language = listView_vokabeln.Columns[2].Text;

            new_and_settings new_vokabel = new new_and_settings
            {
                Owner = this,
                Icon = Icon.FromHandle(Icons.add.GetHicon())
            };

            // Sprachen anzeigen
            new_vokabel.own_language_text.Text = own_language;
            new_vokabel.foreign_language_text.Text = foreign_language;
            new_vokabel.foreign_language2_text.Text = $"{foreign_language} ({Words.Synonym})";

            //Optionen abschalten

            new_vokabel.option_box.Enabled = false;

            //Abbrechen oder Fertig

            if (fertig == true)
            {
                new_vokabel.cancel_button.Text = Words.Finish;
                new_vokabel.cancel_button.TabIndex = 0;
                new_vokabel.AcceptButton = new_vokabel.cancel_button;

                new_vokabel.own_language.Select();

            }

            //Sonderzeichen-Dialog einschalten

            if (show_specialchars == true)
            {
                new_vokabel.show_specialchars_dialog = true;
            }

            DialogResult result = new_vokabel.ShowDialog();

            //Vokabel hinzufügen

            //Wird ausgeführt, sobald auf OK geklickt wird

            if (DialogResult.OK == result)
            {
                // Schauen, ob die Vokabel schon vorhanden ist

                bool search = false;

                for (int i = 0; i < listView_vokabeln.Items.Count; i++)
                {
                    if (listView_vokabeln.Items[i].SubItems[1].Text == new_vokabel.own_language.Text && listView_vokabeln.Items[i].SubItems[2].Text == new_vokabel.foreign_language.Text)
                    {
                        search = true;
                    }
                }

                //Falls die Vokabel noch nicht vorhanden ist: Schauen, ob ein Synonym eingegeben wurde

                if (search == false)
                {
                    if (new_vokabel.foreign_language_2.Text == "")
                    {
                        ListViewItem item = new ListViewItem(new string[] { "", new_vokabel.own_language.Text, new_vokabel.foreign_language.Text, "" });

                        item.ImageIndex = 0;
                        item.Tag = 0;
                        listView_vokabeln.Items.Add(item);

                        item.Selected = true;
                        item.Focused = true;
                        item.EnsureVisible();
                    }
                    else
                    {
                        ListViewItem item = new ListViewItem(new string[] { "", new_vokabel.own_language.Text, new_vokabel.foreign_language.Text + "=" + new_vokabel.foreign_language_2.Text, "" });

                        item.ImageIndex = 0;
                        item.Tag = 0;
                        listView_vokabeln.Items.Add(item);

                        item.Selected = true;
                        item.Focused = true;
                        item.EnsureVisible();
                    }

                    // Anzahl Vokabeln ermitteln

                    vokabelheft_edited();

                    practice_vokabelheft.Enabled = true;
                    vokabelnÜbenToolStripMenuItem.Enabled = true;

                    druckenToolStripMenuItem.Enabled = true;
                    print_vokabelheft.Enabled = true;

                    vokabelheftSendenToolStripMenuItem.Enabled = true;

                    exportierenToolStripMenuItem.Enabled = true;

                    infos_vokabelhefte_text();


                    //Neues Fenster öffnen

                    add_vokabel(true, new_vokabel.show_specialchars_dialog);

                }
                else
                {
                    MessageBox.Show(Properties.language.messagebox_vocable_exists,
                                 "Warnung",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Warning);

                    //Neues Fenster öffnen

                    add_vokabel(true, new_vokabel.show_specialchars_dialog);
                }
            }

            //Suchfenster anzeigen

            search_vokabel_field.Enabled = true;

        }

        //Vokabel bearbeiten

        private void edit_vokabel_dialog()
        {
            string own_language = listView_vokabeln.Columns[1].Text;
            string foreign_language = listView_vokabeln.Columns[2].Text;

            new_and_settings edit_vokabel = new new_and_settings();

            //Titel
            edit_vokabel.Text = Words.EditWord;

            //Icon
            edit_vokabel.Icon = Icon.FromHandle(Icons.edit.GetHicon());

            //Weiter_Button zu OK_Button machen
            edit_vokabel.ok_button.Text = Words.Ok;

            // Sprachen anzeigen
            edit_vokabel.own_language_text.Text = own_language;
            edit_vokabel.foreign_language_text.Text = foreign_language;
            edit_vokabel.foreign_language2_text.Text = $"{foreign_language} ({Words.Synonym})";

            //Vokabel einlesen

            string own_language_vokabel = listView_vokabeln.SelectedItems[0].SubItems[1].Text;

            string[] split = listView_vokabeln.SelectedItems[0].SubItems[2].Text.Split('=');


            string foreign_language_vokabel = split[0];
            string foreign_language_vokabel_2 = "";


            if (split.Length > 1)
            {
                foreign_language_vokabel_2 = split[1];
            }

            // Vokabel einlesen

            edit_vokabel.own_language.Text = own_language_vokabel;
            edit_vokabel.foreign_language.Text = foreign_language_vokabel;

            edit_vokabel.foreign_language_2.Text = foreign_language_vokabel_2;


            //Optionen einschalten

            edit_vokabel.option_box.Enabled = true;


            DialogResult result = edit_vokabel.ShowDialog();

            // Vokabel bearbeiten

            if (edit_vokabel.own_language.Text != own_language_vokabel || edit_vokabel.foreign_language.Text != foreign_language_vokabel || edit_vokabel.foreign_language_2.Text != foreign_language_vokabel_2 || edit_vokabel.reset_vocabel.Checked == true)
            {
                //Wird ausgeführt, sobald auf OK geklickt wird

                if (DialogResult.OK == result)
                {

                    // Schauen, ob die Vokabel bereits vorhanden ist 
                    bool search = false;

                    for (int i = 0; i < listView_vokabeln.Items.Count; i++)
                    {
                        if (listView_vokabeln.Items[i].SubItems[1].Text == edit_vokabel.own_language.Text
                            && listView_vokabeln.Items[i].SubItems[2].Text == edit_vokabel.foreign_language.Text
                            && !listView_vokabeln.Items[i].Selected)
                        {
                            search = true;
                            break;
                        }
                    }

                    if (search == false) // falls die Vokabel nicht vorhanden ist
                    {
                        //Schaut, ob Synonym eingegeben wurde

                        if (edit_vokabel.foreign_language_2.Text == "")
                        {

                            listView_vokabeln.SelectedItems[0].SubItems[1].Text = edit_vokabel.own_language.Text;
                            listView_vokabeln.SelectedItems[0].SubItems[2].Text = edit_vokabel.foreign_language.Text;

                        }

                        else
                        {
                            listView_vokabeln.SelectedItems[0].SubItems[1].Text = edit_vokabel.own_language.Text;
                            listView_vokabeln.SelectedItems[0].SubItems[2].Text = edit_vokabel.foreign_language.Text + "=" + edit_vokabel.foreign_language_2.Text;

                        }

                        listView_vokabeln.SelectedItems[0].EnsureVisible();

                        // Schauen, ob die Ergebnisse zurückgesetzt werden sollen

                        if (edit_vokabel.reset_vocabel.Checked == true)
                        {
                            listView_vokabeln.SelectedItems[0].ImageIndex = 0;
                            listView_vokabeln.SelectedItems[0].Tag = 0;

                        }
                        vokabelheft_edited();

                    }
                    else // Falls die Vokabel bereits vorhanden ist
                    {
                        MessageBox.Show(Properties.language.messagebox_cant_edit,
                                     "Warnung",
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Warning);
                    }

                    infos_vokabelhefte_text();
                }
            }

            insert_vokabel.Focus();
        }

        //Vokabel löschen

        private void vokabel_delete()
        {
            listView_vokabeln.BeginUpdate();

            int i = listView_vokabeln.FocusedItem.Index;

            if (listView_vokabeln.Items.Count > 1)
            {

                if (i == 0)
                {
                    listView_vokabeln.FocusedItem.Remove();
                    listView_vokabeln.Items[0].Selected = true;

                }
                else
                {
                    listView_vokabeln.FocusedItem.Remove();
                    listView_vokabeln.Items[i - 1].Selected = true;
                }
            }
            else if (listView_vokabeln.Items.Count == 1)
            {
                listView_vokabeln.FocusedItem.Remove();

                practice_vokabelheft.Enabled = false;
                vokabelnÜbenToolStripMenuItem.Enabled = false;

                edit_vokabel.Enabled = false;
                vokabelBearbeitenToolStripMenuItem.Enabled = false;

                delet_vokabel.Enabled = false;
                vokabelLöschenToolStripMenuItem.Enabled = false;

                search_vokabel_button.Enabled = false;
                search_vokabel_field.Enabled = false;


                print_vokabelheft.Enabled = false;
                druckenToolStripMenuItem.Enabled = false;

                vokabelheftSendenToolStripMenuItem.Enabled = false;

                exportierenToolStripMenuItem.Enabled = false;
            }

            vokabelheft_edited();
            infos_vokabelhefte_text();


            listView_vokabeln.EndUpdate();

            insert_vokabel.Focus();
        }

        //Vokabelheft Optionen

        private void edit_vokabelheft_dialog()
        {
            //Dialog starten
            VocabularyBookSettings edit_vokabelheft_dialog = new VocabularyBookSettings();

            //Titel
            edit_vokabelheft_dialog.Text = Words.EditVocabularyBook;

            //Icon
            edit_vokabelheft_dialog.Icon = Icon.FromHandle(Icons.settings_file.GetHicon());

            //Sprachen
            edit_vokabelheft_dialog.TbMotherTongue.Text = listView_vokabeln.Columns[1].Text;
            edit_vokabelheft_dialog.TbForeignLang.Text = listView_vokabeln.Columns[2].Text;

            //Optionen einschalten
            edit_vokabelheft_dialog.option_box.Enabled = true;

            //Übersetzungsrichtung

            if (uebersetzungsrichtung == "2")
            {
                edit_vokabelheft_dialog.RbModeAskMotherTongue.Checked = true;
            }
            else
            {
                edit_vokabelheft_dialog.RbModeAskForeignTongue.Checked = true;
            }

            DialogResult edit_vokabelheft_result = edit_vokabelheft_dialog.ShowDialog();

            if (DialogResult.OK == edit_vokabelheft_result)
            {
                //Listview vorbereiten

                listView_vokabeln.BeginUpdate();
                if (listView_vokabeln.Columns[1].Text != edit_vokabelheft_dialog.TbMotherTongue.Text || listView_vokabeln.Columns[2].Text != edit_vokabelheft_dialog.TbForeignLang.Text)
                {
                    listView_vokabeln.Columns[1].Text = edit_vokabelheft_dialog.TbMotherTongue.Text;
                    listView_vokabeln.Columns[2].Text = edit_vokabelheft_dialog.TbForeignLang.Text;
                    vokabelheft_edited();
                }
                listView_vokabeln.EndUpdate();

                if (edit_vokabelheft_dialog.CbResetResults.Checked == true)
                {
                    int count = listView_vokabeln.Items.Count;

                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            listView_vokabeln.Items[i].ImageIndex = 0;
                            listView_vokabeln.Items[i].Tag = 0;
                        }
                    }
                }

                //Uebersetzungsrichtung speichern

                if (edit_vokabelheft_dialog.RbModeAskMotherTongue.Checked == true)
                {
                    uebersetzungsrichtung = "2";
                }
                else
                {
                    uebersetzungsrichtung = "1";
                }

                vokabelheft_edited();

            }
            infos_vokabelhefte_text();

            insert_vokabel.Focus();
        }


        //Vokabeln Üben

        private void vokabeln_üben()
        {

            int anzvok = listView_vokabeln.Items.Count;
            int anz_noch_nicht = 0;
            int anz_falsch = 0;
            int anz_richtig = 0;
            int anz_fertig = 0;

            ArrayList array_falsch = new ArrayList();
            ArrayList array_richtig = new ArrayList();
            ArrayList array_noch_nicht = new ArrayList();
            ArrayList array_zeitlich = new ArrayList();


            // Von allen Vokabeln den Index in eine ArrayList speichern

            for (int i = 0; i < anzvok; i++)
            {
                if (listView_vokabeln.Items[i].ImageIndex == 0)
                {
                    anz_noch_nicht++;
                    array_noch_nicht.Add(listView_vokabeln.Items[i].Index);
                }
                else if (listView_vokabeln.Items[i].ImageIndex == 1)
                {
                    anz_falsch++;
                    array_falsch.Add(listView_vokabeln.Items[i].Index);
                }
                else if (listView_vokabeln.Items[i].ImageIndex == 2)
                {
                    anz_richtig++;
                    array_richtig.Add(listView_vokabeln.Items[i].Index);
                }
                else if (listView_vokabeln.Items[i].ImageIndex == 3)
                {
                    anz_fertig++;
                }
            }

            if (anz_fertig != anzvok)
            {

                vokabel_count_practise_dialog choose_dialog = new vokabel_count_practise_dialog();

                //schauen welche Buttons anzuzeigen sind

                //Variablen schreiben

                choose_dialog.anzahl_gesamt = anz_noch_nicht + anz_falsch + anz_richtig;
                choose_dialog.anzahl_noch_nicht = anz_noch_nicht;
                choose_dialog.anzahl_falsch = anz_falsch;
                choose_dialog.anzahl_richtig = anz_richtig;

                if (listView_vokabeln.Items.Count - anz_fertig < 40)
                {
                    choose_dialog.vokabeln_40.Enabled = false;
                }
                if (listView_vokabeln.Items.Count - anz_fertig < 30)
                {
                    choose_dialog.vokabeln_30.Enabled = false;
                }

                if (listView_vokabeln.Items.Count - anz_fertig < 20)
                {
                    choose_dialog.vokabeln_20.Enabled = false;
                    choose_dialog.vokabeln_alle.TabIndex = 0;
                    choose_dialog.AcceptButton = choose_dialog.vokabeln_alle;
                }

                //Maximale Anzahl Vokabeln festlegen
                choose_dialog.anzahl.Maximum = anz_falsch + anz_noch_nicht + anz_richtig;

                //Optionen falls nötig ausschalten

                if (anz_noch_nicht == 0)
                {
                    choose_dialog.art_noch_nicht.Enabled = false;
                }
                else if (anz_noch_nicht == anzvok - anz_fertig)
                {
                    choose_dialog.art_alle.Enabled = false;
                    choose_dialog.art_falsch.Enabled = false;
                    choose_dialog.art_richtig.Enabled = false;
                }

                if (anz_falsch == 0)
                {
                    choose_dialog.art_falsch.Enabled = false;
                }
                else if (anz_falsch == anzvok - anz_fertig)
                {
                    choose_dialog.art_alle.Enabled = false;
                    choose_dialog.art_noch_nicht.Enabled = false;
                    choose_dialog.art_richtig.Enabled = false;
                }

                if (anz_richtig == 0)
                {
                    choose_dialog.art_richtig.Enabled = false;
                }
                else if (anz_richtig == anzvok - anz_fertig)
                {
                    choose_dialog.art_alle.Enabled = false;
                    choose_dialog.art_noch_nicht.Enabled = false;
                    choose_dialog.art_falsch.Enabled = false;
                }

                //Feststellen, ob Vokabeln bereits geübt wurden

                bool no_times = false;

                if (anz_noch_nicht == anzvok)
                {
                    no_times = true;
                }

                if (no_times == true)
                {
                    choose_dialog.zeitlich_kuerzlich.Enabled = false;
                    choose_dialog.zeitlich_laengst.Enabled = false;
                }


                //Dialog starten
                choose_dialog.ShowDialog();

                int anzahl = 0;

                //Alle üben
                if (choose_dialog.button == "alle")
                {
                    if (choose_dialog.art_noch_nicht.Checked == true)
                    {
                        anzahl = anz_noch_nicht;
                    }
                    else if (choose_dialog.art_falsch.Checked == true)
                    {
                        anzahl = anz_falsch;
                    }
                    else if (choose_dialog.art_richtig.Checked == true)
                    {
                        anzahl = anz_richtig;
                    }

                    else
                    {
                        if (choose_dialog.zeitlich_kuerzlich.Checked == true || choose_dialog.zeitlich_laengst.Checked == true)
                        {
                            anzahl = listView_vokabeln.Items.Count - anz_fertig - anz_noch_nicht;
                        }
                        else
                        {
                            anzahl = listView_vokabeln.Items.Count - anz_fertig;
                        }
                    }
                }
                //20 üben
                else if (choose_dialog.button == "20")
                {
                    anzahl = 20;
                }
                //30 üben
                else if (choose_dialog.button == "30")
                {
                    anzahl = 30;
                }
                //40 üben
                else if (choose_dialog.button == "40")
                {
                    anzahl = 40;
                }
                else if (choose_dialog.button == "individuell")
                {
                    anzahl = Convert.ToInt32(choose_dialog.anzahl.Value);
                }

                //Falls der Dialog nicht abgebrochen wurde

                if (anzahl > 0)
                {
                    int anz_ueben_noch_nicht = 0;
                    int anz_ueben_falsch = 0;
                    int anz_ueben_richtig = 0;

                    //Genau Anzahl ermitteln, falls als Art alle angegeben wurde

                    if (choose_dialog.art_alle.Checked == true)
                    {
                        //Falsch

                        double x = anzahl * Properties.Settings.Default.prozent_falsch / 100;

                        anz_ueben_falsch = Convert.ToInt32(Math.Round(x, 1, MidpointRounding.AwayFromZero));

                        //Richtig

                        x = anzahl * Properties.Settings.Default.prozent_richtig / 100;

                        anz_ueben_richtig = Convert.ToInt32(Math.Round(x, 1, MidpointRounding.AwayFromZero));

                        //Noch nicht

                        anz_ueben_noch_nicht = anzahl - anz_ueben_falsch - anz_ueben_richtig;
                    }

                    //Genaue Anzahl definieren, falls nur Vokabeln, die noch nie geübt wurden ausgewählt wurde

                    else if (choose_dialog.art_noch_nicht.Checked == true)
                    {
                        anz_ueben_noch_nicht = anzahl;
                    }

                    // "" "" "" "" "" "", die falsch geübt wurden

                    else if (choose_dialog.art_falsch.Checked == true)
                    {
                        anz_ueben_falsch = anzahl;
                    }

                    // "" "" "" "" "" "", die richtig geübt wurden

                    else if (choose_dialog.art_richtig.Checked == true)
                    {
                        anz_ueben_richtig = anzahl;
                    }


                    int falsch = anz_ueben_falsch;
                    int richtig = anz_ueben_richtig;
                    int noch_nicht = anz_ueben_noch_nicht;

                    //Falls es zeitliche Einschränkungen giebt

                    if (choose_dialog.zeitlich_laengst.Checked == true || choose_dialog.zeitlich_kuerzlich.Checked == true)
                    {
                        if (choose_dialog.art_alle.Checked == true)
                        {

                            for (int i = 0; i < anzvok; i++)
                            {
                                if (listView_vokabeln.Items[i].ImageIndex != 0 && listView_vokabeln.Items[i].ImageIndex != 3)
                                {
                                    string[] split = listView_vokabeln.Items[i].SubItems[3].Text.Replace(' ', ':').Replace('.', ':').Split(':');

                                    double[] zahl = new double[2];


                                    zahl[0] = i;

                                    if (listView_vokabeln.Items[i].SubItems[3].Text == "")
                                    {
                                        zahl[1] = 0;
                                    }
                                    else if (split.Length == 3)
                                    {
                                        zahl[1] = Convert.ToDouble(split[2] + split[1] + split[0] + "0000");
                                    }
                                    else
                                    {
                                        zahl[1] = Convert.ToDouble(split[2] + split[1] + split[0] + split[3] + split[4]);
                                    }

                                    array_zeitlich.Add(zahl);
                                }
                            }
                        }

                        else if (choose_dialog.art_falsch.Checked == true)
                        {

                            for (int i = 0; i < anzvok; i++)
                            {
                                if (listView_vokabeln.Items[i].ImageIndex == 1)
                                {
                                    string[] split = listView_vokabeln.Items[i].SubItems[3].Text.Replace(' ', ':').Replace('.', ':').Split(':');

                                    double[] zahl = new double[2];


                                    zahl[0] = i;

                                    if (listView_vokabeln.Items[i].SubItems[3].Text == "")
                                    {
                                        zahl[1] = 0;
                                    }
                                    else if (split.Length == 3)
                                    {
                                        zahl[1] = Convert.ToDouble(split[2] + split[1] + split[0] + "0000");
                                    }
                                    else
                                    {
                                        zahl[1] = Convert.ToDouble(split[2] + split[1] + split[0] + split[3] + split[4]);
                                    }

                                    array_zeitlich.Add(zahl);
                                }

                            }
                        }
                        else if (choose_dialog.art_richtig.Checked == true)
                        {
                            for (int i = 0; i < anzvok; i++)
                            {
                                if (listView_vokabeln.Items[i].ImageIndex == 2)
                                {
                                    string[] split = listView_vokabeln.Items[i].SubItems[3].Text.Replace(' ', ':').Replace('.', ':').Split(':');

                                    double[] zahl = new double[2];


                                    zahl[0] = i;

                                    if (listView_vokabeln.Items[i].SubItems[3].Text == "")
                                    {
                                        zahl[1] = 0;
                                    }
                                    else if (split.Length == 3)
                                    {
                                        zahl[1] = Convert.ToDouble(split[2] + split[1] + split[0] + "0000");
                                    }
                                    else
                                    {
                                        zahl[1] = Convert.ToDouble(split[2] + split[1] + split[0] + split[3] + split[4]);
                                    }

                                    array_zeitlich.Add(zahl);
                                }
                            }
                        }
                        if (choose_dialog.zeitlich_kuerzlich.Checked == true)
                        {
                            ArraySorter sorter = new ArraySorter(0, SortOrder.Descending);
                            array_zeitlich.Sort(sorter);
                        }
                        else
                        {
                            ArraySorter sorter = new ArraySorter(0, SortOrder.Ascending);
                            array_zeitlich.Sort(sorter);
                        }

                    }

                    // Übung zusammenstellen

                    string[,] practise_list = new string[anzahl, 8];

                    if (choose_dialog.zeitlich_alle.Checked == true)
                    {

                        //Zufalls-Generatoren

                        //Random rand = new Random();

                        //Random zufallszahl = new Random();

                        RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
                        RNGCryptoServiceProvider zufallszahl = new RNGCryptoServiceProvider();
                        byte[] value = new byte[1];

                        byte min = Convert.ToByte(1);
                        byte max = Convert.ToByte(3);



                        for (int i = 1; i <= anzahl; i++)
                        {
                            //Falls alle Art Vokabel geübt werden sollen

                            rand.GetBytes(value);

                            // Die Zahlen umrechnen
                            double divisor = 256F / (max - min + 1);

                            value[0] = (byte)((value[0] / divisor) + min);

                            int z = Convert.ToInt32(value[0]);

                            if (z == 1 && richtig == 0)
                            {
                                z = 2;
                            }
                            if (z == 2 && falsch == 0)
                            {
                                z = 3;
                            }

                            if (z == 3 && noch_nicht == 0)
                            {
                                z = 1;
                            }

                            if (z == 1 && richtig == 0)
                            {
                                z = 2;
                            }

                            if (z == 1 && array_richtig.Count == 0)
                            {
                                z = 2;
                            }
                            if (z == 2 && array_falsch.Count == 0)
                            {
                                z = 3;
                            }

                            if (z == 3 && array_noch_nicht.Count == 0)
                            {
                                z = 1;
                            }
                            if (z == 1 && array_richtig.Count == 0)
                            {
                                z = 2;
                            }

                            if (choose_dialog.art_noch_nicht.Checked == true)
                            {
                                z = 3;
                            }
                            else if (choose_dialog.art_falsch.Checked == true)
                            {
                                z = 2;
                            }
                            else if (choose_dialog.art_richtig.Checked == true)
                            {
                                z = 1;
                            }

                            if (z == 1)
                            {
                                //Daten der Vokabel aus dem ListView auslesen

                                //zufällige Vokabel auslesen

                                //int y = zufallszahl.Next(0, array_richtig.Count);

                                zufallszahl.GetBytes(value);

                                // Die Zahlen umrechnen
                                divisor = 256F / ((array_richtig.Count - 1) - 0 + 1);

                                value[0] = (byte)((value[0] / divisor) + 0);

                                int y = Convert.ToInt32(value[0]);

                                practise_list[i - 1, 0] = array_richtig[y].ToString();
                                practise_list[i - 1, 1] = Convert.ToString(listView_vokabeln.Items[Convert.ToInt32(array_richtig[y])].Tag);
                                practise_list[i - 1, 2] = listView_vokabeln.Items[Convert.ToInt32(array_richtig[y])].SubItems[1].Text;
                                string[] temp = listView_vokabeln.Items[Convert.ToInt32(array_richtig[y])].SubItems[2].Text.Split('=');

                                practise_list[i - 1, 3] = temp[0];
                                if (temp.Length == 2)
                                {
                                    practise_list[i - 1, 4] = temp[1];
                                }

                                practise_list[i - 1, 5] = listView_vokabeln.Items[Convert.ToInt32(array_richtig[y])].SubItems[3].Text;
                                array_richtig.RemoveAt(y);
                                richtig--;


                            }
                            else if (z == 2)
                            {
                                //Daten der Vokabel aus dem ListView auslesen

                                //zufällige Vokabel auslesen

                                //int y = zufallszahl.Next(0, array_falsch.Count);

                                zufallszahl.GetBytes(value);

                                // Die Zahlen umrechnen
                                divisor = 256.0 / ((array_falsch.Count - 1) - 0 + 1);

                                value[0] = (byte)((value[0] / divisor) + 0);

                                int y = Convert.ToInt32(value[0]);

                                practise_list[i - 1, 0] = array_falsch[y].ToString();
                                practise_list[i - 1, 1] = Convert.ToString(listView_vokabeln.Items[Convert.ToInt32(array_falsch[y])].Tag);
                                practise_list[i - 1, 2] = listView_vokabeln.Items[Convert.ToInt32(array_falsch[y])].SubItems[1].Text;
                                string[] temp = listView_vokabeln.Items[Convert.ToInt32(array_falsch[y])].SubItems[2].Text.Split('=');
                                practise_list[i - 1, 3] = temp[0];
                                if (temp.Length == 2)
                                {
                                    practise_list[i - 1, 4] = temp[1];
                                }
                                practise_list[i - 1, 5] = listView_vokabeln.Items[Convert.ToInt32(array_falsch[y])].SubItems[3].Text;
                                array_falsch.RemoveAt(y);
                                falsch--;
                            }
                            else
                            {
                                //Daten der Vokabel aus dem ListView auslesen


                                //zufällige Vokabel auslesen

                                //int y = zufallszahl.Next(0, array_noch_nicht.Count);

                                zufallszahl.GetBytes(value);

                                // Die Zahlen umrechnen
                                divisor = 256F / ((array_noch_nicht.Count - 1) - 0 + 1);

                                value[0] = (byte)((value[0] / divisor) + 0);

                                int y = Convert.ToInt32(value[0]);

                                practise_list[i - 1, 0] = array_noch_nicht[y].ToString();
                                practise_list[i - 1, 1] = Convert.ToString(listView_vokabeln.Items[Convert.ToInt32(array_noch_nicht[y])].Tag);
                                practise_list[i - 1, 2] = listView_vokabeln.Items[Convert.ToInt32(array_noch_nicht[y])].SubItems[1].Text;
                                string[] temp = listView_vokabeln.Items[Convert.ToInt32(array_noch_nicht[y])].SubItems[2].Text.Split('=');
                                practise_list[i - 1, 3] = temp[0];
                                if (temp.Length == 2)
                                {
                                    practise_list[i - 1, 4] = temp[1];
                                }
                                practise_list[i - 1, 5] = listView_vokabeln.Items[Convert.ToInt32(array_noch_nicht[y])].SubItems[3].Text;
                                array_noch_nicht.RemoveAt(y);
                                noch_nicht--;
                            }
                        }
                    }
                    else if (choose_dialog.zeitlich_kuerzlich.Checked == true || choose_dialog.zeitlich_laengst.Checked == true)
                    {
                        for (int i = 1; i <= anzahl; i++)
                        {
                            double voc = ((double[])array_zeitlich[0])[0];

                            practise_list[i - 1, 0] = voc.ToString();
                            practise_list[i - 1, 1] = Convert.ToString(listView_vokabeln.Items[Convert.ToInt32(voc)].Tag);
                            practise_list[i - 1, 2] = listView_vokabeln.Items[Convert.ToInt32(voc)].SubItems[1].Text;
                            string[] temp = listView_vokabeln.Items[Convert.ToInt32(voc)].SubItems[2].Text.Split('=');
                            practise_list[i - 1, 3] = temp[0];
                            if (temp.Length == 2)
                            {
                                practise_list[i - 1, 4] = temp[1];
                            }
                            practise_list[i - 1, 5] = listView_vokabeln.Items[Convert.ToInt32(voc)].SubItems[3].Text;
                            array_zeitlich.RemoveAt(0);
                        }
                    }

                    //ListView ausschalten
                    listView_vokabeln.Visible = false;

                    //Neues Üben-Dialog-Fenster erstellen
                    practise_dialog vokabelheft_üben = new practise_dialog();

                    //Icon
                    vokabelheft_üben.Icon = Icon.FromHandle(Icons.practise.GetHicon());

                    //Sprachen anzeigen
                    vokabelheft_üben.foreign_language_text.Text = listView_vokabeln.Columns[2].Text;
                    vokabelheft_üben.own_language_text.Text = listView_vokabeln.Columns[1].Text;

                    //Falls von Fremdsprache nach Muttersprache geübt werden soll
                    if (uebersetzungsrichtung == "2")
                    {
                        vokabelheft_üben.practise_groupbox.Text = string.Format(Words.TranslateFromTo, listView_vokabeln.Columns[2].Text, listView_vokabeln.Columns[1].Text);
                        vokabelheft_üben.uebersetzungsrichtung = "2";
                    }
                    //Falls von Muttersprache nach Fremdsprache geübt werden soll
                    else
                    {
                        vokabelheft_üben.practise_groupbox.Text = string.Format(Words.TranslateFromTo, listView_vokabeln.Columns[1].Text, listView_vokabeln.Columns[2].Text);
                        vokabelheft_üben.uebersetzungsrichtung = "1";
                    }

                    //Variablen füllen

                    vokabelheft_üben.anz_vok = anzahl;

                    vokabelheft_üben.practise_list = practise_list;

                    //Falls nötig Form vergrössern

                    if (Properties.Settings.Default.selber_bewerten == true)
                    {
                        vokabelheft_üben.Size = new Size(vokabelheft_üben.Size.Width, 370);

                        vokabelheft_üben.sonderzeichen_button.Location = new Point(vokabelheft_üben.sonderzeichen_button.Location.X, vokabelheft_üben.sonderzeichen_button.Location.Y + 30);
                        vokabelheft_üben.fortfahren_button.Location = new Point(vokabelheft_üben.fortfahren_button.Location.X, vokabelheft_üben.fortfahren_button.Location.Y + 30);
                        vokabelheft_üben.abbrechen_button.Location = new Point(vokabelheft_üben.abbrechen_button.Location.X, vokabelheft_üben.abbrechen_button.Location.Y + 30);

                        vokabelheft_üben.statistik_groupbox.Location = new Point(vokabelheft_üben.statistik_groupbox.Location.X, vokabelheft_üben.statistik_groupbox.Location.Y + 30);

                    }

                    //mForm als Besitzer festlegen

                    vokabelheft_üben.Owner = this;

                    //Dialog starten

                    vokabelheft_üben.ShowDialog();

                    if (vokabelheft_üben.DialogResult == DialogResult.Cancel || vokabelheft_üben.DialogResult == DialogResult.OK)
                    {
                        //Practise_list zurückholen

                        string[,] result_list = vokabelheft_üben.practise_list;


                        if (Properties.Settings.Default.show_practise_result_list == true)
                        {
                            //Auswertung anzeigen

                            practise_result_list auswertung = new practise_result_list();

                            //Icon

                            auswertung.Icon = Icon.FromHandle(Icons.statistics.GetHicon());

                            auswertung.own_language = listView_vokabeln.Columns[1].Text;
                            auswertung.foreign_language = listView_vokabeln.Columns[2].Text;
                            auswertung.result_list = result_list;
                            auswertung.anzahl = anzahl;

                            //Statistik übernehmen

                            if (anzahl == 1)
                                auswertung.groupBox1.Text = Words.OverallOneWord + ":";
                            else
                                auswertung.groupBox1.Text = string.Format(Words.OverallXWords, anzahl) + ":";

                            auswertung.anzahl_falsch.Text = vokabelheft_üben.anzahl_falsch.Text;
                            auswertung.anzahl_nicht.Text = vokabelheft_üben.anzahl_noch_nicht.Text;
                            auswertung.anzahl_richtig.Text = vokabelheft_üben.anzahl_richtig.Text;
                            auswertung.anzahl_teilweise_richtig.Text = vokabelheft_üben.anzahl_teilweise.Text;

                            if (Convert.ToInt32(auswertung.anzahl_nicht.Text) != anzahl)
                            {
                                //Note berechnen
                                decimal note;
                                if (Properties.Settings.Default.notensystem == "de")
                                {
                                    note = 7 - Math.Round(((Convert.ToDecimal(auswertung.anzahl_richtig.Text) + Convert.ToDecimal(auswertung.anzahl_teilweise_richtig.Text) / 2) * 5 / (anzahl - Convert.ToInt32(auswertung.anzahl_nicht.Text))) + 1, 1, MidpointRounding.AwayFromZero);

                                    //Hintergrundfarbe bestimmen

                                    if (note > 3)
                                    {
                                        auswertung.note.BackColor = Color.FromArgb(255, 192, 203);
                                    }
                                    else if (note > 2 && note <= 3)
                                    {
                                        auswertung.note.BackColor = Color.FromArgb(255, 215, 0);
                                    }
                                    else if (note >= 1 && note <= 2)
                                    {
                                        auswertung.note.BackColor = Color.FromArgb(144, 238, 144);
                                    }
                                }
                                else
                                {
                                    note = Math.Round(((Convert.ToDecimal(auswertung.anzahl_richtig.Text) + Convert.ToDecimal(auswertung.anzahl_teilweise_richtig.Text) / 2) * 5 / (anzahl - Convert.ToInt32(auswertung.anzahl_nicht.Text))) + 1, 1, MidpointRounding.AwayFromZero);

                                    //Hintergrundfarbe bestimmen

                                    if (note < 4)
                                    {
                                        auswertung.note.BackColor = Color.FromArgb(255, 192, 203);
                                    }
                                    else if (note >= 4 && note < 5)
                                    {
                                        auswertung.note.BackColor = Color.FromArgb(255, 215, 0);
                                    }
                                    else if (note >= 5)
                                    {
                                        auswertung.note.BackColor = Color.FromArgb(144, 238, 144);
                                    }
                                }

                                //Prozent berechnen
                                auswertung.prozent.Text = Convert.ToString(Math.Round((Convert.ToDecimal(auswertung.anzahl_richtig.Text) + Convert.ToDecimal(auswertung.anzahl_teilweise_richtig.Text) / 2) / (anzahl - Convert.ToInt32(auswertung.anzahl_nicht.Text)) * 100, 0, MidpointRounding.AwayFromZero)) + "%";

                                //Note anzeigen
                                auswertung.note.Text = Convert.ToString(note);
                            }
                            else
                            {
                                auswertung.note.Text = "-";
                                auswertung.prozent.Text = "-";
                            }
                            //Dialog starten

                            auswertung.ShowDialog();

                            if (auswertung.checkBox.Checked == true)
                            {
                                Properties.Settings.Default.show_practise_result_list = false;
                                Properties.Settings.Default.Save();
                            }
                        }
                        //Änderungen speichern

                        for (int i = 0; i < (anzahl); i++)
                        {
                            //Tag ändern
                            listView_vokabeln.Items[Convert.ToInt32(result_list[i, 0])].Tag = Convert.ToInt32(result_list[i, 1]);

                            if (Convert.ToInt32(result_list[i, 1]) == 0)
                            {
                                listView_vokabeln.Items[Convert.ToInt32(result_list[i, 0])].ImageIndex = 0;
                            }

                            else if (Convert.ToInt32(result_list[i, 1]) == 1)
                            {
                                listView_vokabeln.Items[Convert.ToInt32(result_list[i, 0])].ImageIndex = 1;
                            }
                            else if (Convert.ToInt32(result_list[i, 1]) > 1 && Convert.ToInt32(result_list[i, 1]) < Properties.Settings.Default.max + 1)
                            {
                                listView_vokabeln.Items[Convert.ToInt32(result_list[i, 0])].ImageIndex = 2;
                            }

                            else if (Convert.ToInt32(result_list[i, 1]) >= Properties.Settings.Default.max + 1)
                            {
                                listView_vokabeln.Items[Convert.ToInt32(result_list[i, 0])].ImageIndex = 3;
                            }

                            //Zuletzt geübt ändern
                            listView_vokabeln.Items[Convert.ToInt32(result_list[i, 0])].SubItems[3].Text = result_list[i, 5];
                        }

                        //Vokabelheft als geändert markieren
                        vokabelheft_edited();
                        infos_vokabelhefte_text();


                        listView_vokabeln.Visible = true;
                    }
                }

                insert_vokabel.Focus();
            }
            else
            {
                MessageBox.Show(Properties.language.no_vocables_to_learn,
                            Properties.language.name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
            }

        }

        //Vokabelhefte zusammenführen

        private void vokabelhefteZusammenführenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //Dialog starten

                MergeFiles merge = new MergeFiles();
                DialogResult result = merge.ShowDialog();

                //Falls auf Speichern geklickt wurde

                if (result == DialogResult.OK)
                {

                    //Cursor auf warten setzen

                    Cursor.Current = Cursors.WaitCursor;

                    //Variablen vorbereiten

                    int error = 0;
                    ArrayList errors = new ArrayList();

                    int to_new = 0;
                    ArrayList to_new_files = new ArrayList();


                    string pfad_speichern = merge.pfad;
                    string[] files = new string[merge.LbFiles.Items.Count];

                    string own_language = merge.TbMotherTongue.Text;
                    string foreign_language = merge.TbForeignLang.Text;

                    bool take_results = merge.CbKeepResults.Checked;

                    //Liste mit den Vokabeln und Ergebnissen, falls gewünscht und vorhanden
                    List<string[]> vocables_list = new List<string[]>();

                    //Files in array abspeichern
                    for (int i = 0; i < merge.LbFiles.Items.Count; i++)
                    {
                        files[i] = merge.LbFiles.Items[i].ToString();
                    }

                    //Files lesen und abspeichern

                    for (int i = 0; i < merge.LbFiles.Items.Count; i++)
                    {
                        try
                        {
                            // Datei lesen
                            //Datei entschlüsseln

                            string plaintext;
                            using (StreamReader reader = new StreamReader(files[i], Encoding.UTF8))
                                plaintext = Crypto.Decrypt(reader.ReadToEnd());

                            // Zeilen der Datei in ein Array abspeichern

                            string[] lines = plaintext.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                            // Anzahl Arrayfelder bestimmen

                            int anzlines = lines.Length;

                            string version = lines[0];

                            //Schauen ob Vokabeln vorhanden sind

                            if (anzlines > 4 && version == "1.0")
                            {
                                //Anzahl Vokabeln

                                int anz_vok = anzlines - 4;

                                //Daten ergebnissen

                                string[,] data_ergebnisse = new string[anz_vok, 2];

                                // Datei mit Ergebnissen aufrufen

                                string ergebnisse_pfad = Properties.Settings.Default.path_vhr + "\\" + lines[1] + ".vhr";
                                FileInfo check_file = new FileInfo(ergebnisse_pfad);

                                if (take_results == true && check_file.Exists == true)
                                {
                                    try
                                    {
                                        //Entschlüsseln
                                        string plaintext2;
                                        using (StreamReader ergebnisse_reader = new StreamReader(ergebnisse_pfad, Encoding.UTF8))
                                            plaintext2 = Crypto.Decrypt(ergebnisse_reader.ReadToEnd());

                                        // Zeilen der Datei in ein Array abspeichern
                                        string[] ergebnisse_lines = plaintext2.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);


                                        //Ergebnisse splitten
                                        for (int j = 0; j <= anz_vok; j++)
                                        {
                                            try
                                            {
                                                string[] data = ergebnisse_lines[j + 2].Split('#');

                                                data_ergebnisse[j, 0] = data[0];
                                                data_ergebnisse[j, 1] = data[1];
                                            }
                                            catch
                                            {

                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }

                                //Falls Ergebnisse erwünsch aber keine vorhanden
                                else
                                {
                                    //Ergebnisse splitten
                                    for (int j = 0; j <= anz_vok; j++)
                                    {
                                        try
                                        {
                                            data_ergebnisse[j, 0] = "0";
                                            data_ergebnisse[j, 1] = "";
                                        }
                                        catch
                                        {

                                        }
                                    }
                                }

                                //Daten Vokabeln und Ergebnisse in List<string [] > abspeichern

                                for (int k = 0; k <= anz_vok; k++)
                                {
                                    try
                                    {

                                        //Array mit den Daten einer Vokabel

                                        string[] data_vocable = new string[5];

                                        string[] data_split_vok = lines[k + 4].Split('#');

                                        //Daten in Array abspeichern
                                        data_vocable[0] = data_split_vok[0];
                                        data_vocable[1] = data_split_vok[1];
                                        data_vocable[2] = data_split_vok[2];
                                        data_vocable[3] = data_ergebnisse[k, 0];
                                        data_vocable[4] = data_ergebnisse[k, 1];


                                        //Schauen ob datein bereits vorhanden
                                        bool exists = false;
                                        for (int p = 0; p < vocables_list.Count; p++)
                                        {
                                            string[] search = vocables_list[p];

                                            if (search[0].Replace(" ", "").ToUpper() == data_vocable[0].Replace(" ", "").ToUpper() && search[1].Replace(" ", "").ToUpper() == data_vocable[1].Replace(" ", "").ToUpper())
                                            {
                                                exists = true;
                                            }

                                        }

                                        //Daten in List abspeichern
                                        if (exists == false)
                                        {
                                            //Richtige Position herausfinden

                                            int place = 0;
                                            bool added = false;
                                            for (int n = 0; n < vocables_list.Count; n++)
                                            {
                                                place++;
                                                string[] ntext = vocables_list[n];

                                                if (String.Compare(ntext[0], data_vocable[0]) > 0) //Eintrag n kleiner als neue Vokabel
                                                {
                                                    vocables_list.Insert(place - 1, data_vocable); //vor dieser Stelle einsetzen
                                                    added = true;
                                                    break; //und Schleife verlassen
                                                }
                                            }
                                            if (added == false) //noch nicht eingesetzt?
                                            {
                                                vocables_list.Add(data_vocable);
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }

                                //Liste sortieren

                            }

                            else if (anzlines == 4 && version == "1.0")
                            {
                                //Es sind keine Vokabeln vorhanden
                            }

                            //Falls die Datei eine neuere Version von Vocup benötigt

                            else if (version != "1.0" && lines[0].Length <= 4 || lines[0].Length <= 4 && Convert.ToInt32(version) > 1)
                            {
                                to_new++;

                                //Dateiname von den zu neuen Dateien auslesen und speichern
                                FileInfo info = new FileInfo(files[i]);

                                to_new_files.Add(info.Name);
                            }

                            else
                            {
                                error++;

                                //Dateiname von fehlerhaften Datei auslesen und Speichern
                                FileInfo info = new FileInfo(files[i]);

                                errors.Add(info.Name);
                            }
                        }
                        catch
                        {
                            error++;

                            //Dateiname von fehlerhaften Datei auslesen und Speichern
                            FileInfo info = new FileInfo(files[i]);

                            errors.Add(info.Name);
                        }
                    }

                    //Datei abspeichern
                    StringBuilder writer = new StringBuilder();
                    writer.AppendLine("1.0");

                    //Code generieren
                    string code = GenerateCode();
                    writer.AppendLine(code);

                    writer.AppendLine(own_language);
                    writer.Append(foreign_language);

                    if (vocables_list.Count > 0)
                    {
                        //Vokabeln abspeichern

                        for (int i = 0; i < vocables_list.Count; i++)
                        {
                            string[] data = vocables_list[i];

                            writer.AppendLine();
                            writer.Append(data[0]);
                            writer.Append('#');
                            writer.Append(data[1]);
                            writer.Append('#');
                            writer.Append(data[2]);
                        }

                        //Verschlüsseln
                        using (StreamWriter file_writer = new StreamWriter(pfad_speichern, false, Encoding.UTF8))
                            file_writer.Write(Crypto.Encrypt(writer.ToString()));
                    }

                    //Ergebnisse abspeichern

                    if (take_results && vocables_list.Count > 0)
                    {
                        StringBuilder writer_ergebnisse = new StringBuilder();
                        writer_ergebnisse.AppendLine(pfad_speichern);

                        //Übungsvariante speichern
                        writer_ergebnisse.Append("1");

                        for (int i = 0; i < vocables_list.Count; i++)
                        {
                            string[] data = vocables_list[i];

                            writer_ergebnisse.AppendLine();
                            writer_ergebnisse.Append(data[3]);
                            writer_ergebnisse.Append('#');
                            writer_ergebnisse.Append(data[4]);
                        }

                        using (StreamWriter ergebnisse_writer = new StreamWriter(Path.Combine(Properties.Settings.Default.path_vhr, code + ".vhr"), false, Encoding.UTF8))
                            ergebnisse_writer.Write(Crypto.Encrypt(writer_ergebnisse.ToString()));
                    }

                    //TreeView aktualisieren

                    AllSubDirectories(treeView.Nodes[0]);
                    treeView.Nodes[0].Collapse();

                    //Fehlermeldungen anzeigen

                    if (error > 0)
                    {
                        string message;

                        if (error == 1)
                        {
                            message = Properties.language.merge_error_1 + Environment.NewLine + Environment.NewLine + "- " + errors[0];
                        }
                        else
                        {
                            message = error.ToString() + " " + Properties.language.merge_error_over_1 + Environment.NewLine;

                            for (int i = 0; i < errors.Count; i++)
                            {
                                message = message + Environment.NewLine + "- " + errors[i];
                            }
                        }

                        MessageBox.Show(message, Properties.language.name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    if (to_new > 0)
                    {
                        string message;

                        if (to_new == 1)
                        {
                            message = Properties.language.merge_to_new_1 + Environment.NewLine + Environment.NewLine + "- " + to_new_files[0];

                        }
                        else
                        {
                            message = to_new.ToString() + " " + Properties.language.merge_to_new_over + Environment.NewLine;

                            for (int i = 0; i < to_new_files.Count; i++)
                            {
                                message = message + Environment.NewLine + "- " + to_new_files[i];
                            }
                        }

                        DialogResult result_dialog = MessageBox.Show(message, Properties.language.name, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        //Schauen, ob nach Updates gesucht werden soll

                        if (result_dialog == DialogResult.Yes)
                        {
                            search_update();
                        }

                    }

                    //Dialog anzeigen, dass die Vokabelhefte erfolgreich zusammengeführt worden sind.

                    if (error == 0 && to_new == 0)
                    {
                        MessageBox.Show(Properties.language.merge_success,
                         Properties.language.name,
                         MessageBoxButtons.OK,
                         MessageBoxIcon.Information);
                    }
                }

                //Cursor auf normal setzen

                Cursor.Current = Cursors.Default;
            }
            catch
            {
                //Fehlermeldung

                MessageBox.Show(Properties.language.merge_error,
                                                    Properties.language.name,
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Error);


                //TreeView aktualisieren

                AllSubDirectories(treeView.Nodes[0]);
                treeView.Nodes[0].Collapse();
            }
        }

        //Datensicherung erstellen

        private void datensicherungErstellenToolStripMenuItem_Click(object sender, EventArgs e)
        {

            //Variablen für Fehlermeldungen
            int error_vhf = 0;
            int error_vhr = 0;
            int error_chars = 0;
            bool error = false;

            ArrayList error_vhf_name = new ArrayList();

            ArrayList error_chars_name = new ArrayList();

            bool file_exists = false;
            string temp_pfad = "";

            backup_add backup_dialog = new backup_add();

            DialogResult result = backup_dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                //Backup erstellen beim ersetzen, falls ein Fehler auftauchen sollte
                FileInfo pfad_info = new FileInfo(backup_dialog.pfad);
                temp_pfad = Path.GetTempFileName();

                if (pfad_info.Exists == true)
                {
                    file_exists = true;
                    pfad_info.CopyTo(temp_pfad, true);
                }

                //Backup erstellen
                //Daten zusammenstellen
                //Cursor auf warten setzen
                Cursor.Current = Cursors.WaitCursor;

                Update();

                ArrayList files_vhf = new ArrayList();

                List<string> files_vhr = new List<string>();
                List<string> files_chars = new List<string>();

                if (backup_dialog.alle_vokabelhefte.Checked == true)
                {
                    DirectoryInfo check_personal = new DirectoryInfo(Properties.Settings.Default.path_vhf);

                    if (check_personal.Exists == true)
                    {
                        ArrayList subfolders = new ArrayList();

                        subfolders.Add(check_personal.FullName);

                        do
                        {
                            try
                            {
                                int position = subfolders.Count - 1;

                                DirectoryInfo folder_info = new DirectoryInfo(subfolders[position].ToString());

                                DirectoryInfo[] folders = folder_info.GetDirectories();

                                //Unterordner einlesen
                                for (int i = 0; i < folders.Length; i++)
                                {
                                    subfolders.Add(folders[i].FullName);
                                }

                                //Vokabelhefte suchen
                                FileInfo[] files = folder_info.GetFiles("*.vhf");

                                for (int i = 0; i < files.Length; i++)
                                {
                                    try
                                    {
                                        string[] coresponding_files = new string[2];
                                        coresponding_files[0] = files[i].FullName;
                                        coresponding_files[1] = "";
                                        files_vhf.Add(coresponding_files);
                                    }
                                    catch
                                    {
                                        //Vokabelheft konnte nicht in die ArrayList geschrieben werden
                                        error_vhf++;
                                        error_vhf_name.Add(files[i].Name);
                                    }
                                }

                                subfolders.RemoveAt(position);
                            }
                            catch
                            {
                            }
                        }
                        while (subfolders.Count > 0);
                    }
                }

                //Vokabelhefte einlesen, die im Listbox vorhanden sind
                if (backup_dialog.folgende_vokabelhefte.Checked == true && backup_dialog.listbox_vokabelhefte.Items.Count > 0)
                {
                    for (int i = 0; i < backup_dialog.listbox_vokabelhefte.Items.Count; i++)
                    {
                        try
                        {
                            bool contains_file = false;

                            for (int j = 0; j < files_vhf.Count; j++)
                            {
                                if (backup_dialog.listbox_vokabelhefte.Items[i].ToString() == ((string[])files_vhf[j])[0])
                                {
                                    contains_file = true;
                                }
                            }

                            if (contains_file == false)
                            {
                                string[] corresponding_files = new string[2];
                                corresponding_files[0] = backup_dialog.listbox_vokabelhefte.Items[i].ToString();
                                corresponding_files[1] = "";

                                files_vhf.Add(corresponding_files);
                            }
                        }
                        catch //Vokabelheft konnte nicht in die ArrayList geschrieben werden
                        {
                            error_vhf++;
                            FileInfo info = new FileInfo(backup_dialog.listbox_vokabelhefte.Items[i].ToString());
                            error_vhf_name.Add(info.Name);
                        }
                    }
                }

                //Ergebnisse einlesen falls notwendig

                if (backup_dialog.gewaehlte_ergebnisse.Checked == true)
                {
                    for (int i = 0; i < files_vhf.Count; i++)
                    {
                        try
                        {
                            string vhf_name = ((string[])files_vhf[i])[0];

                            //Datei entschlüsseln
                            string plaintext;
                            using (StreamReader reader = new StreamReader(vhf_name, Encoding.UTF8))
                                plaintext = Crypto.Decrypt(reader.ReadToEnd());

                            // Zeilen der Datei in ein Array abspeichern
                            string[] lines = plaintext.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                            FileInfo info = new FileInfo(Properties.Settings.Default.path_vhr + "\\" + lines[1] + ".vhr");

                            if (info.Exists == true)
                            {
                                string[] corresponding_files = new string[2];

                                corresponding_files[0] = vhf_name;
                                corresponding_files[1] = lines[1];

                                files_vhf[i] = corresponding_files;

                                files_vhr.Add(info.FullName);
                            }
                        }
                        catch //Ergebnisse konnten nicht in die ArrayList geschrieben werden
                        {
                            error_vhr++;
                        }
                    }
                }
                else if (backup_dialog.alle_ergebnisse.Checked == true)
                {
                    //Korespondierende Ergebnisse einlesen 
                    for (int i = 0; i < files_vhf.Count; i++)
                    {
                        try
                        {
                            string vhf_name = ((string[])files_vhf[i])[0];

                            //Datei entschlüsseln
                            string plaintext;
                            using (StreamReader reader = new StreamReader(vhf_name, Encoding.UTF8))
                                plaintext = Crypto.Decrypt(reader.ReadToEnd());

                            // Zeilen der Datei in ein Array abspeichern
                            string[] lines = plaintext.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                            FileInfo info = new FileInfo(Properties.Settings.Default.path_vhr + "\\" + lines[1] + ".vhr");

                            if (info.Exists == true)
                            {
                                string[] corresponding_files = new string[2];

                                corresponding_files[0] = vhf_name;
                                corresponding_files[1] = lines[1];

                                files_vhf[i] = corresponding_files;
                            }
                        }
                        catch
                        {

                        }
                    }

                    //Alle Ergebnisse in files_vhr einlesen
                    files_vhr.AddRange(new DirectoryInfo(Properties.Settings.Default.path_vhr).GetFiles("*.vhr").Select(x => x.FullName));
                }

                //Sonderzeichen sichern falls nötig

                for (int i = 0; i < backup_dialog.listbox_special_chars.Items.Count; i++)
                {
                    if (backup_dialog.listbox_special_chars.GetItemChecked(i) == true)
                    {
                        files_chars.Add(Properties.Settings.Default.path_vhr + "\\specialchar\\" + backup_dialog.listbox_special_chars.Items[i].ToString() + ".txt");
                    }
                }
                //Corresponding_files in MemoryStream speichern

                string correspond = "";

                if (files_vhf.Count > 0)
                {
                    for (int i = 0; i < files_vhf.Count; i++)
                    {
                        string[] vhf_vhr = new string[2];
                        vhf_vhr[0] = ((string[])files_vhf[i])[0];
                        vhf_vhr[1] = ((string[])files_vhf[i])[1];

                        //Datei-Pfade durch lokale variablen ersetzen

                        //vhf_vhr[0] = vhf_vhr[0].Replace(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\" + Properties.language.personal_directory, "%vhf%");

                        vhf_vhr[0] = vhf_vhr[0].Replace(Properties.Settings.Default.path_vhf, "%vhf%");

                        vhf_vhr[0] = vhf_vhr[0].Replace(Properties.Settings.Default.path_vhr, "%vhr%");
                        vhf_vhr[0] = vhf_vhr[0].Replace(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "%personal%");
                        vhf_vhr[0] = vhf_vhr[0].Replace(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "%desktop%");
                        vhf_vhr[0] = vhf_vhr[0].Replace(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "%program%");
                        vhf_vhr[0] = vhf_vhr[0].Replace(Environment.GetFolderPath(Environment.SpecialFolder.System), "%system%");

                        correspond = correspond + i.ToString() + "|" + vhf_vhr[0] + "|" + vhf_vhr[1];

                        if (i < files_vhf.Count - 1)
                        {
                            correspond = correspond + Environment.NewLine;
                        }
                    }
                }

                //Chars.log erstellen
                string chars = string.Join(Environment.NewLine, files_chars.Select(x => Path.GetFileName(x)));

                //vhr.log erstellen
                string vhr = string.Join(Environment.NewLine, files_vhr.Select(x => Path.GetFileName(x)));


                //MemoryStreams erstellen

                MemoryStream correspond_memory_stream = new MemoryStream(Encoding.UTF8.GetBytes(correspond));
                MemoryStream chars_memory_stream = new MemoryStream(Encoding.UTF8.GetBytes(chars));
                MemoryStream vhr_memory_stream = new MemoryStream(Encoding.UTF8.GetBytes(vhr));


                //Dateien in den Backup-Zip-Folder speichern
                FileStream zip_file = new FileStream(backup_dialog.pfad, FileMode.Create, FileAccess.Write);
                ZipOutputStream zip_stream = new ZipOutputStream(zip_file);

                zip_stream.SetLevel(8);

                byte[] buffer = new byte[4096];

                try
                {
                    //Corresponing_files-Datei erstellen

                    ZipEntry correspond_entry = new ZipEntry("vhf_vhr.log");

                    correspond_entry.CompressionMethod = CompressionMethod.Deflated;
                    correspond_entry.DateTime = DateTime.Now;
                    correspond_entry.Size = correspond_memory_stream.Length;

                    zip_stream.PutNextEntry(correspond_entry);


                    StreamUtils.Copy(correspond_memory_stream, zip_stream, buffer);

                    correspond_memory_stream.Close();



                    //Vhr-Datei erstellen

                    ZipEntry vhr_entry = new ZipEntry("vhr.log");

                    vhr_entry.CompressionMethod = CompressionMethod.Deflated;
                    vhr_entry.DateTime = DateTime.Now;
                    vhr_entry.Size = vhr_memory_stream.Length;

                    zip_stream.PutNextEntry(vhr_entry);

                    StreamUtils.Copy(vhr_memory_stream, zip_stream, buffer);

                    vhr_memory_stream.Close();


                    //chars-Datei erstellen

                    ZipEntry chars_entry = new ZipEntry("chars.log");

                    chars_entry.CompressionMethod = CompressionMethod.Deflated;
                    chars_entry.DateTime = DateTime.Now;
                    chars_entry.Size = chars_memory_stream.Length;

                    zip_stream.PutNextEntry(chars_entry);

                    StreamUtils.Copy(chars_memory_stream, zip_stream, buffer);

                    chars_memory_stream.Close();


                    //Vokabelhefte speichern

                    for (int i = 0; i < files_vhf.Count; i++)
                    {
                        try
                        {
                            string file_name = ((string[])files_vhf[i])[0];
                            FileInfo info = new FileInfo(file_name);

                            if (info.Exists == true)
                            {
                                ZipEntry entry = new ZipEntry(@"vhf\" + i + ".vhf");

                                entry.CompressionMethod = CompressionMethod.Deflated;

                                entry.DateTime = File.GetLastWriteTime(file_name);
                                entry.Size = info.Length;

                                zip_stream.PutNextEntry(entry);


                                FileStream stream = new FileStream(file_name, FileMode.Open);

                                StreamUtils.Copy(stream, zip_stream, buffer);

                                stream.Close();
                            }
                        }
                        catch
                        {
                            //Vokabelheft konnte nicht nicht kopiert werden

                            error_vhf++;

                            FileInfo info = new FileInfo(((string[])files_vhf[i])[0]);

                            error_vhf_name.Add(info.Name);
                        }
                    }

                    //Ergebnisse abspeichern

                    for (int i = 0; i < files_vhr.Count; i++)
                    {
                        try
                        {

                            string file_name = files_vhr[i].ToString();
                            FileInfo info = new FileInfo(file_name);

                            if (info.Exists == true)
                            {

                                ZipEntry entry = new ZipEntry(@"vhr\" + info.Name);

                                entry.CompressionMethod = CompressionMethod.Deflated;

                                entry.DateTime = File.GetLastWriteTime(file_name);
                                entry.Size = info.Length;

                                zip_stream.PutNextEntry(entry);


                                FileStream stream = new FileStream(file_name, FileMode.Open);

                                StreamUtils.Copy(stream, zip_stream, buffer);

                                stream.Close();
                            }
                        }
                        catch //Ergebnisse konnten nicht kopiert werden
                        {
                            error_vhr++;
                        }
                    }

                    //Sonderzeichentabellen sichern

                    for (int i = 0; i < files_chars.Count; i++)
                    {
                        try
                        {

                            string file_name = files_chars[i].ToString();
                            FileInfo info = new FileInfo(file_name);

                            if (info.Exists == true)
                            {

                                ZipEntry entry = new ZipEntry(@"chars\" + info.Name);

                                entry.CompressionMethod = CompressionMethod.Deflated;

                                entry.DateTime = File.GetLastWriteTime(file_name);
                                entry.Size = info.Length;

                                zip_stream.PutNextEntry(entry);


                                FileStream stream = new FileStream(file_name, FileMode.Open);

                                StreamUtils.Copy(stream, zip_stream, buffer);

                                stream.Close();
                            }
                        }
                        catch
                        {
                            //Ergebnisse konnten nicht nicht kopiert werden

                            error_chars++;

                            FileInfo info = new FileInfo(files_chars[i].ToString());

                            error_chars_name.Add(info.Name);
                        }
                    }
                    zip_stream.Close();
                    zip_file.Close();
                }
                catch
                {
                    zip_stream.Close();
                    zip_file.Close();

                    error = true;

                    //Cursor auf normal setzen

                    Cursor.Current = Cursors.Default;

                    FileInfo info = new FileInfo(backup_dialog.pfad);
                    info.Delete();

                    if (file_exists == true)
                    {
                        FileInfo temp_file = new FileInfo(temp_pfad);

                        temp_file.MoveTo(backup_dialog.pfad);
                    }

                    //Fehlermeldung anzeigen, falls ein allgemeiner Fehler aufgetaucht ist

                    MessageBox.Show(Properties.language.messagebox_backup_error,
                                     Properties.language.error,
                                     MessageBoxButtons.OK,
                                     MessageBoxIcon.Error);
                }
                zip_stream.Close();
                zip_file.Close();

                //Cursor auf normal setzen

                Cursor.Current = Cursors.Default;


                //Fehlermeldungen anzeigen, falls gewisse Dateien nicht gesichert werden konnten

                if (error_vhf > 0)
                {
                    string messange = Properties.language.messagebox_backup_error_vhf + Environment.NewLine;

                    for (int i = 0; i < error_vhf; i++)
                    {
                        messange = messange + Environment.NewLine + error_vhf_name[i].ToString();
                    }

                    MessageBox.Show(messange,
                                    Properties.language.error,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);

                }
                if (error_vhr > 0)
                {
                    string messange = error_vhr.ToString() + " " + Properties.language.messagebox_backup_error_vhr;


                    MessageBox.Show(messange,
                                    Properties.language.error,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
                if (error_chars > 0)
                {
                    string messange = Properties.language.messagebox_backup_error_chars + Environment.NewLine;

                    for (int i = 0; i < error_chars; i++)
                    {
                        messange = messange + Environment.NewLine + error_chars_name[i].ToString();
                    }

                    MessageBox.Show(messange,
                                    Properties.language.error,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }

                //Meldung anzeigen, dass der Prozess erfolgreich war

                if (error == false && error_vhf == 0 && error_vhr == 0 && error_chars == 0)
                {
                    MessageBox.Show(Properties.language.messagebox_backup_success,
                                       Properties.language.name,
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Information);
                }
            }

        }

        //Datensicherung wiederherstellen

        private void restore_backup(string file_path)
        {
            //Neue Form vorbereiten
            backup_restore restore = new backup_restore();

            //Falls ein Backup geöffnet wurde
            if (file_path != "")
            {
                restore.path_field.Text = file_path;
                restore.path_button.Enabled = false;
                restore.path_backup = file_path;
            }


            //Fragen, ob das Vokabelheft gespeichert werden soll

            bool result = true;

            if (edit_vokabelheft == true)
            {
                result = vokabelheft_ask_to_save();
            }

            else if (save_new == true)
            {
                try
                {
                    savefile(false);
                }
                catch
                {

                }
            }

            if (result == true)
            {


                //Falls auf wiederherstellen geklickt wurde
                if (restore.ShowDialog() == DialogResult.OK)
                {
                    //Variablen für Fehlermeldungen
                    int error_vhf = 0;
                    int error_vhr = 0;
                    int error_chars = 0;
                    bool error = false;

                    ArrayList error_vhf_name = new ArrayList();
                    ArrayList error_chars_name = new ArrayList();

                    try
                    {

                        //Cursor auf Warten setzen
                        Cursor.Current = Cursors.WaitCursor;
                        Update();


                        //Schliesst das geöffnete Vokabelheft
                        close_vokabelheft();

                        //Backup-Datei vorbereiten

                        ZipFile backup_file = new ZipFile(restore.path_backup);

                        //Vokabelhefte wiederherstellen
                        if (restore.vhf_restore.Count > 0)
                        {
                            for (int i = 0; i < restore.vhf_restore.Count; i++)
                            {
                                try
                                {

                                    string[] temp = ((string[])restore.vhf_restore[i]);

                                    ZipEntry entry = backup_file.GetEntry(@"vhf\" + temp[0] + ".vhf");

                                    byte[] buffer = new byte[entry.Size + 4096];

                                    FileInfo info = new FileInfo(temp[1]);

                                    if (Directory.Exists(info.DirectoryName) == false)
                                    {
                                        Directory.CreateDirectory(info.DirectoryName);
                                    }


                                    FileStream writer = new FileStream(temp[1], FileMode.Create);

                                    StreamUtils.Copy(backup_file.GetInputStream(entry), writer, buffer);

                                    writer.Close();
                                }
                                catch
                                {
                                    error_vhf++;

                                    string[] temp = ((string[])restore.vhf_restore[i]);
                                    error_vhf_name.Add(temp[1]);
                                }
                            }
                        }

                        //Ergebnisse wiederherstellen

                        if (restore.vhr_restore.Count > 0)
                        {
                            for (int i = 0; i < restore.vhr_restore.Count; i++)
                            {
                                try
                                {
                                    ZipEntry entry = backup_file.GetEntry(@"vhr\" + restore.vhr_restore[i]);

                                    byte[] buffer = new byte[entry.Size + 4096];


                                    FileStream writer = new FileStream(Properties.Settings.Default.path_vhr + "\\" + restore.vhr_restore[i], FileMode.Create);

                                    StreamUtils.Copy(backup_file.GetInputStream(entry), writer, buffer);

                                    writer.Close();
                                }

                                catch
                                {
                                    error_vhr++;
                                }
                            }
                        }

                        //Sonderzeichentabellen sichern

                        if (restore.chars_restore.Count > 0)
                        {

                            for (int i = 0; i < restore.chars_restore.Count; i++)
                            {
                                try
                                {
                                    ZipEntry entry = backup_file.GetEntry(@"chars\" + restore.chars_restore[i]);

                                    byte[] buffer = new byte[entry.Size + 4096];

                                    if (Directory.Exists(Properties.Settings.Default.path_vhr + "\\specialchar\\") == false)
                                    {
                                        Directory.CreateDirectory(Properties.Settings.Default.path_vhr + "\\specialchar\\");
                                    }

                                    FileStream writer = new FileStream(Properties.Settings.Default.path_vhr + "\\specialchar\\" + restore.chars_restore[i], FileMode.Create);

                                    StreamUtils.Copy(backup_file.GetInputStream(entry), writer, buffer);

                                    writer.Close();
                                }

                                catch
                                {
                                    error_chars++;

                                    error_chars_name.Add(restore.chars_restore[i]);
                                }
                            }
                        }

                        backup_file.Close();


                        //TreeView aktualisieren

                        bool node_0 = false;

                        if (treeView.Nodes[0].IsExpanded == true)
                        {
                            node_0 = true;
                        }
                        AllSubDirectories(treeView.Nodes[0]);
                        treeView.CollapseAll();
                        if (node_0 == true)
                        {
                            treeView.Nodes[0].Expand();
                        }

                        Cursor.Current = Cursors.Default;
                        Update();
                    }

                    catch
                    {
                        error = true;

                        Cursor.Current = Cursors.Default;

                        //fehlermeldung anzeigen
                        MessageBox.Show(Properties.language.messagebox_backup_restore_error,
                               Properties.language.error,
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                    }

                    //Falls nötig Fehlermeldungen anzeigen

                    if (error_vhf > 0)
                    {
                        string messange = Properties.language.messagebox_backup_restore_error_vhf + Environment.NewLine;

                        for (int i = 0; i < error_vhf_name.Count; i++)
                        {
                            messange = messange + Environment.NewLine + error_vhf_name[i];
                        }

                        //Fehlermeldung anzeigen
                        MessageBox.Show(messange,
                               Properties.language.error,
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                    }
                    if (error_vhr > 0)
                    {
                        //Fehlermeldung anzeigen
                        MessageBox.Show(error_vhr.ToString() + " " + Properties.language.messagebox_backup_restore_error_vhr,
                               Properties.language.error,
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                    }
                    if (error_chars > 0)
                    {
                        string messange = Properties.language.messagebox_backup_restore_error_chars + Environment.NewLine;

                        for (int i = 0; i < error_chars_name.Count; i++)
                        {
                            messange = messange + Environment.NewLine + error_chars_name[i];
                        }

                        //Fehlermeldung anzeigen
                        MessageBox.Show(messange,
                               Properties.language.error,
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                    }

                    //Dialog anzeigen, dass der Prozess erfolgreich war

                    if (error == false && error_vhf == 0 && error_vhr == 0 && error_chars == 0)
                    {
                        MessageBox.Show(Properties.language.messagebox_backup_restore_success,
                                  Properties.language.name,
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                    }
                }
            }

        }

        private void datensicherungWiederherstellenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            restore_backup("");
        }

        //Vokabelheft drucken

        private void print_file()
        {
            if (pfad_vokabelheft != "" && pfad_vokabelheft != null)
            {

                //Dialog starten der abfrägt, was gedruckt werden soll

                choose_vocables_for_print choose_vocables = new choose_vocables_for_print();

                //Icon

                choose_vocables.Icon = Icon.FromHandle(Icons.print.GetHicon());

                //Vokabeln in ListBox eintragen

                choose_vocables.vocable_state = new int[listView_vokabeln.Items.Count];

                for (int i = 0; i < listView_vokabeln.Items.Count; i++)
                {
                    choose_vocables.listbox.Items.Add(listView_vokabeln.Items[i].SubItems[1].Text + " - " + listView_vokabeln.Items[i].SubItems[2].Text, true);

                    //Status ins Array eintragen

                    choose_vocables.vocable_state[i] = Convert.ToInt32(listView_vokabeln.Items[i].Tag);

                }

                //Checkboxen falls nötig ausblenden

                if (Convert.ToInt32(noch_nicht_text.Text) == 0)
                {
                    choose_vocables.checkBox_noch_nie.Enabled = false;
                }
                if (Convert.ToInt32(falsch_text.Text) == 0)
                {
                    choose_vocables.checkBox_falsch.Enabled = false;
                }
                if (Convert.ToInt32(richtig_text.Text) == 0)
                {
                    choose_vocables.checkBox_mindestens_einmal.Enabled = false;
                }
                if (Convert.ToInt32(fertig_text.Text) == 0)
                {
                    choose_vocables.checkBox_fertig.Enabled = false;
                }


                //Dialog anzeigen

                DialogResult choose_vocables_result = choose_vocables.ShowDialog();

                //Vokabeln in Vokabelliste speichern

                if (choose_vocables_result == DialogResult.OK)
                {
                    int status = 0;

                    //Feststellen, wie viele Vokabeln markiert wurden

                    int count = 0;

                    for (int i = 0; i < choose_vocables.listbox.Items.Count; i++)
                    {
                        if (choose_vocables.listbox.GetItemCheckState(i) == CheckState.Checked)
                        {
                            count++;
                        }
                    }

                    if (count == 0)
                    {
                        print_file();
                    }
                    else
                    {
                        //Array erstellen

                        int[] list = new int[count];

                        for (int i = 0; i < choose_vocables.listbox.Items.Count; i++)
                        {
                            if (choose_vocables.listbox.GetItemCheckState(i) == CheckState.Checked)
                            {
                                list[status] = i;
                                status++;

                            }
                        }
                        //überschreibt die Vokabelliste
                        vokabelliste = list;
                        //Anzahl Vokabeln schreiben
                        anz_vok = count;


                        //Liste
                        if (choose_vocables.radioButton_liste.Checked == true)
                        {
                            //Feststellen, ob von Mutter- zu Fremdsprache, oder umgekehrt gedruckt werden soll
                            if (choose_vocables.own_foreign.Checked == true)
                            {
                                if_own_to_foreign = true;
                            }
                            else
                            {
                                if_own_to_foreign = false;
                            }

                            //Den Druckdialog starten
                            PrintDialog dialog = new PrintDialog();

                            dialog.AllowCurrentPage = false;
                            dialog.AllowSomePages = false;
                            dialog.UseEXDialog = true;

                            DialogResult result = dialog.ShowDialog();

                            if (result == DialogResult.OK)
                            {
                                printDocument_list.PrinterSettings = dialog.PrinterSettings;
                                printDocument_list.DocumentName = "Vokabelliste";
                                printDocument_list.Print();
                            }
                        }

                        //Kärtchen
                        else
                        {
                            //Anzahl Seiten ermitteln

                            double anz_vokD = (double)anz_vok;

                            double i = Math.Ceiling(anz_vokD / 16);

                            anzahl_seiten = (int)i;

                            //---

                            PrintCardsDialog dialog = new PrintCardsDialog();

                            //Anzahl seiten
                            dialog.LbPaperCount.Text = anzahl_seiten.ToString();

                            //Papiereinzug

                            if (Properties.Settings.Default.papiereinzug == false)
                            {
                                dialog.RbFrontSide.Checked = false;
                                dialog.RbRearSide.Checked = true;
                            }
                            else
                            {
                                dialog.RbFrontSide.Checked = true;
                                dialog.RbRearSide.Checked = false;
                            }

                            //Dialog starten
                            DialogResult result = dialog.ShowDialog();

                            if (result == DialogResult.Ignore)
                            {
                                //Falls Die VorderSeite gedruckt werden soll

                                if_foreside = true;

                                //Ermittle Papiereinzug

                                is_front = dialog.RbFrontSide.Checked;
                                Properties.Settings.Default.papiereinzug = is_front;
                                Properties.Settings.Default.Save();

                                dialog.Close();

                                //Drucken

                                PrintDialog print_dialog = new PrintDialog();
                                print_dialog.AllowCurrentPage = false;
                                print_dialog.AllowSomePages = false;
                                print_dialog.UseEXDialog = true;

                                DialogResult print_result = print_dialog.ShowDialog();

                                if (print_result == DialogResult.OK)
                                {

                                    printDocument_cards.PrinterSettings = print_dialog.PrinterSettings;
                                    printDocument_cards.DocumentName = "Vokabel Kärtchen";
                                    printDocument_cards.Print();


                                    //Dialog nochmals starten
                                    //anderer Button deaktivieren

                                    PrintCardsDialog dialog2 = new PrintCardsDialog();

                                    dialog2.BtnPrintForeside.Enabled = false;
                                    dialog2.BtnPrintBackside.Enabled = true;

                                    dialog2.LbPaperCount.Text = anzahl_seiten.ToString();

                                    //Papiereinzug


                                    if (Properties.Settings.Default.papiereinzug == false)
                                    {
                                        dialog2.RbFrontSide.Checked = false;
                                        dialog2.RbRearSide.Checked = true;
                                    }
                                    else
                                    {
                                        dialog2.RbRearSide.Checked = false;
                                        dialog2.RbFrontSide.Checked = true;
                                    }

                                    DialogResult result2 = dialog2.ShowDialog();

                                    if (result2 == DialogResult.OK)
                                    {
                                        if_foreside = false;

                                        //Papiereinzug

                                        is_front = dialog2.RbFrontSide.Checked;
                                        Properties.Settings.Default.papiereinzug = is_front;
                                        Properties.Settings.Default.Save();

                                        //Drucken

                                        printDocument_cards.Print();

                                    }
                                }
                            }
                            else if (result == DialogResult.OK)
                            {

                                if_foreside = false;

                                //Ermittle Papiereinzug

                                is_front = dialog.RbFrontSide.Checked;
                                Properties.Settings.Default.papiereinzug = is_front;
                                Properties.Settings.Default.Save();

                                //Drucken

                                dialog.Close();

                                //Drucken

                                PrintDialog print_dialog = new PrintDialog
                                {
                                    AllowCurrentPage = false,
                                    AllowSomePages = false
                                };

                                DialogResult print_result = print_dialog.ShowDialog();

                                if (print_result == DialogResult.OK)
                                {

                                    printDocument_cards.PrinterSettings = print_dialog.PrinterSettings;
                                    printDocument_cards.DocumentName = "Vokabel Kärtchen";
                                    printDocument_cards.Print();

                                    //Dialog nochmals starten
                                    //anderer Button deaktivieren

                                    PrintCardsDialog dialog3 = new PrintCardsDialog();

                                    dialog3.BtnPrintForeside.Enabled = true;
                                    dialog3.BtnPrintBackside.Enabled = false;

                                    dialog3.LbPaperCount.Text = anzahl_seiten.ToString();


                                    //Papiereinzug


                                    if (Properties.Settings.Default.papiereinzug == false)
                                    {
                                        dialog3.RbFrontSide.Checked = false;
                                        dialog3.RbRearSide.Checked = true;
                                    }
                                    else
                                    {
                                        dialog3.RbRearSide.Checked = false;
                                        dialog3.RbFrontSide.Checked = true;
                                    }

                                    DialogResult result3 = dialog3.ShowDialog();

                                    if (result3 == DialogResult.Ignore)
                                    {
                                        if_foreside = true;

                                        //Papiereinzug

                                        is_front = dialog3.RbFrontSide.Checked;
                                        Properties.Settings.Default.papiereinzug = is_front;
                                        Properties.Settings.Default.Save();


                                        //Drucken

                                        printDocument_cards.Print();

                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Falls die Datei noch nicht gespeichert wurde
            else
            {
                MessageBox.Show(Properties.language.have_to_save,
                                   Properties.language.name,
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
            }

        }
        //Liste drucken
        private void printDocument_list_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {

            Graphics g = e.Graphics;
            g.PageUnit = GraphicsUnit.Display;

            //Schrift
            Font font = new Font("Arial", 10);
            Font font_bold = new Font("Arial", 10, FontStyle.Bold);
            Font font_vocable = new Font("Arial", 8);

            //stift
            Pen pen = new Pen(Brushes.Black, 1);

            //Ein zentriertes Format für Schrift erstellen
            StringFormat format_center = new StringFormat();
            format_center.Alignment = StringAlignment.Center;

            //Rechtsbündig
            StringFormat format_near = new StringFormat();
            format_near.Alignment = StringAlignment.Near;

            //Ränder
            int left = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Left, 1, MidpointRounding.AwayFromZero));
            int right = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Right, 1, MidpointRounding.AwayFromZero));
            int top = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Top, 1, MidpointRounding.AwayFromZero));
            int bottom = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Bottom, 1, MidpointRounding.AwayFromZero));


            //Seitenzahl ganz oben
            g.DrawString(Words.Site + " " + aktuelle_seite.ToString(), font, Brushes.Black, new Point(414 - left, 25 - left), format_center);

            //Dateiname ermitteln
            FileInfo file_name = new FileInfo(pfad_vokabelheft);

            if (aktuelle_seite == 1)
            {
                if (if_own_to_foreign == true)
                {
                    g.DrawString(file_name.Name.Remove(file_name.Name.Length - 4) + ":  " + listView_vokabeln.Columns[1].Text + " - " + listView_vokabeln.Columns[2].Text, font_bold, Brushes.Black, new Point(414 - left, 40 - top), format_center);
                }
                else
                {
                    g.DrawString(file_name.Name.Remove(file_name.Name.Length - 4) + ":  " + listView_vokabeln.Columns[2].Text + " - " + listView_vokabeln.Columns[1].Text, font_bold, Brushes.Black, new Point(414 - left, 40 - top), format_center);
                }
            }


            //Linien und Wörter einfügen

            int noch_nicht_gedruckt = anz_vok - (aktuelle_seite - 1) * 42;
            int vok_beginnen = (aktuelle_seite - 1) * 42 + 1;

            //Falls volle Seiten gedruckt werden können
            if (noch_nicht_gedruckt >= 42)
            {
                //Oberste Linie
                g.DrawLine(pen, 60 - left, 65 - top, 767 - left, 65 - top);
                //Mittellinie
                g.DrawLine(pen, 413 - left, 65 - top, 413 - left, 1115 - top);
                //Seitenlinien
                g.DrawLine(pen, 60 - left, 65 - top, 60 - left, 1115 - top);
                g.DrawLine(pen, 767 - left, 65 - top, 767 - left, 1115 - top);
                //unterste Linie
                //g.DrawLine(pen, 60 - left, 1095 - top, 767 - left, 1120 - top);

                for (int i = 0; i < 42; i++)
                {

                    SizeF size_own = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_vocable);
                    SizeF size_foreign = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_vocable);

                    //Falls der Text zu gross ist
                    if (size_own.Width > 413 - 62 - left)
                    {
                        bool is_good;
                        int font_size = 8;
                        do
                        {
                            font_size--;
                            Font font_new = new Font("Arial", font_size);

                            SizeF string_size = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new);

                            if (string_size.Width > 413 - 62 - left && font_size > 1)
                            {
                                is_good = false;
                            }
                            else
                            {
                                is_good = true;

                                //kleinerer Text schreiben
                                if (if_own_to_foreign == true)
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);
                                }
                                else
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);

                                }
                            }

                        } while (is_good == false);

                    }
                    //Falls Text nicht zu gross
                    else
                    {
                        if (if_own_to_foreign == true)
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_vocable, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);
                        }
                        else
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_vocable, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);
                        }
                    }
                    //Falls Text zu gross || Synonym
                    if (size_foreign.Width > 413 - 62 - left)
                    {
                        bool is_good;
                        int font_size = 8;
                        do
                        {
                            font_size--;
                            Font font_new = new Font("Arial", font_size);

                            SizeF string_size = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_new);

                            if (string_size.Width > 413 - 62 - left && font_size > 1)
                            {
                                is_good = false;
                            }
                            else
                            {
                                is_good = true;

                                //kleinerer Text schreiben
                                if (if_own_to_foreign == true)
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_new, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);
                                }
                                else
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_new, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);

                                }
                            }

                        } while (is_good == false);

                    }
                    //Falls Text nicht zu gross
                    else
                    {
                        if (if_own_to_foreign == true)
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_vocable, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);
                        }
                        else
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_vocable, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);
                        }
                    }


                    //Untere Linie zeichnen
                    g.DrawLine(pen, 60 - left, 90 + i * 25 - top, 767 - left, 90 + i * 25 - top);
                }
            }
            //Falls letzte Seite, und nicht voll
            else
            {
                //Oberste Linie
                g.DrawLine(pen, 60 - left, 65 - top, 767 - left, 65 - top);
                //Mittellinie
                g.DrawLine(pen, 413 - left, 65 - top, 413 - left, 65 + 25 * noch_nicht_gedruckt - top);
                //Seitenlinien
                g.DrawLine(pen, 60 - left, 65 - top, 60 - left, 65 + 25 * noch_nicht_gedruckt - top);
                g.DrawLine(pen, 767 - left, 65 - top, 767 - left, 65 + 25 * noch_nicht_gedruckt - top);


                for (int i = 0; i < noch_nicht_gedruckt; i++)
                {

                    SizeF size_own = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_vocable);
                    SizeF size_foreign = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_vocable);

                    //Falls der Text zu gross ist
                    if (size_own.Width > 413 - 62 - left)
                    {
                        bool is_good;
                        int font_size = 8;
                        do
                        {
                            font_size--;
                            Font font_new = new Font("Arial", font_size);

                            SizeF string_size = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new);

                            if (string_size.Width > 413 - 62 - left && font_size > 1)
                            {
                                is_good = false;
                            }
                            else
                            {
                                is_good = true;

                                //kleinerer Text schreiben
                                if (if_own_to_foreign == true)
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);
                                }
                                else
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);
                                }
                            }

                        } while (is_good == false);

                    }
                    //Falls Text nicht zu gross
                    else
                    {
                        if (if_own_to_foreign == true)
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_vocable, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);
                        }
                        else
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_vocable, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);
                        }
                    }
                    //Falls Text zu gross || Synonym
                    if (size_foreign.Width > 413 - 62 - left)
                    {
                        bool is_good;
                        int font_size = 8;
                        do
                        {
                            font_size--;
                            Font font_new = new Font("Arial", font_size);

                            SizeF string_size = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_new);

                            if (string_size.Width > 413 - 62 - left && font_size > 1)
                            {
                                is_good = false;
                            }
                            else
                            {
                                is_good = true;

                                //kleinerer Text schreiben
                                if (if_own_to_foreign == true)
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_new, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);
                                }
                                else
                                {
                                    g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_new, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);

                                }
                            }

                        } while (is_good == false);

                    }
                    //Falls Text nicht zu gross
                    else
                    {
                        if (if_own_to_foreign == true)
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_vocable, Brushes.Black, new Point(415 - left, 70 + i * 25 - top), format_near);
                        }
                        else
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font_vocable, Brushes.Black, new Point(62 - left, 70 + i * 25 - top), format_near);
                        }
                    }

                    //Untere Linie zeichnen
                    g.DrawLine(pen, 60 - left, 90 + i * 25 - top, 767 - left, 90 + i * 25 - top);
                }
            }


            //Schauen, ob noch mehr Seiten gedruckt werden müssen

            if (aktuelle_seite != anzahl_seiten)
            {
                e.HasMorePages = true;
                aktuelle_seite++;
            }
            else
            {
                e.HasMorePages = false;
            }

        }
        private void printDocument_list_BeginPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            //Anzahl Seiten festlegen

            double anz_vokD = anz_vok;

            double i = Math.Ceiling(anz_vokD / 42);

            anzahl_seiten = (int)i;
            aktuelle_seite = 1;
        }


        //Kärtchen drucken
        private void printDocument_cards_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.PageUnit = GraphicsUnit.Display;
            //1/100 zoll * 0.254 = mm
            //1169|827 (Daten A4-Seite)
            //Seitenränder abfragen

            int left = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Left, 1, MidpointRounding.AwayFromZero));
            int right = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Right, 1, MidpointRounding.AwayFromZero));
            int top = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Top, 1, MidpointRounding.AwayFromZero));
            int bottom = Convert.ToInt32(Math.Round(e.PageSettings.PrintableArea.Bottom, 1, MidpointRounding.AwayFromZero));

            //Stift

            Pen pen = new Pen(Color.Black, 1);

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;

            Font font = new Font("Arial", 12);

            //Vorderseite
            if (if_foreside == true)
            {
                //Linien zeichnen

                //Vertikal
                g.DrawLine(pen, 207 - left, 0, 207 - left, 1180);
                g.DrawLine(pen, 413 - left, 0, 413 - left, 1180);
                g.DrawLine(pen, 620 - left, 0, 620 - left, 1180);

                //Horizontal
                g.DrawLine(pen, 0, 292 - top, 866, 292 - top);
                g.DrawLine(pen, 0, 585 - top, 866, 585 - top);
                g.DrawLine(pen, 0, 877 - top, 866, 877 - top);


                //Seite rotieren ||X-Koordinaten negativ, Y-Koordinaten positiv
                g.RotateTransform(-90);


                //Linien und Wörter einfügen

                int noch_nicht_gedruckt = anz_vok - (aktuelle_seite - 1) * 16;
                int vok_beginnen = (aktuelle_seite - 1) * 16 + 1;


                //Falls noch mehr Seiten gedruckt werden müssen

                if (noch_nicht_gedruckt >= 16)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        //Koordinaten abfragen
                        int[] coordinates = get_coordinates(i + 1);

                        //Grösse des Textes abfragen

                        SizeF size_string = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font);

                        int height = Convert.ToInt32(size_string.Height / 2);


                        //Vokabel schreiben
                        //Schriftgrösse anpassen

                        //Falls Text zu gross, string auf mehrere Zeilen aufteilen falls möglich
                        if (size_string.Width > 292)
                        {

                            bool is_good = false;
                            int font_size = 12;

                            if (listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text.Trim().Contains(" ") == true)
                            {
                                //Falls der String leerschläge enthält

                                string[] splitter = listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text.Trim().Split(' ');

                                do
                                {
                                    Font font_new = new Font("Arial", font_size);

                                    for (int y = 1; y < splitter.Length; y++)
                                    {
                                        string part1 = "";
                                        string part2 = "";

                                        for (int x = 1; x <= splitter.Length - y; x++)
                                        {
                                            part1 = part1 + " " + splitter[x - 1];

                                            if (x == splitter.Length - y)
                                            {
                                                for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                {
                                                    part2 = part2 + " " + splitter[z];
                                                }
                                            }
                                        }


                                        SizeF size_part1 = g.MeasureString(part1, font_new);
                                        SizeF size_part2 = g.MeasureString(part2, font_new);

                                        if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                        {
                                            is_good = true;

                                            //zwei Zeilen schreiben

                                            g.DrawString(part1, font_new, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height - 20), format);
                                            g.DrawString(part2, font_new, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height + 20), format);

                                            break;
                                        }
                                    }

                                    if (is_good == false)
                                    {
                                        font_size--;
                                    }

                                } while (is_good == false);
                            }
                            else
                            {
                                do
                                {
                                    font_size--;
                                    Font font_new = new Font("Arial", font_size);

                                    SizeF string_size = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new);

                                    if (string_size.Width > 292 && font_size > 1)
                                    {
                                        is_good = false;
                                    }
                                    else
                                    {
                                        is_good = true;

                                        //kleinerer Text schreiben
                                        g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height), format);
                                    }

                                } while (is_good == false);
                            }
                        }
                        //Falls Text nicht zu gross
                        else
                        {
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height), format);
                        }

                    }

                }

                //Falls dies die letzte Seite ist
                else
                {
                    for (int i = 0; i < noch_nicht_gedruckt; i++)
                    {
                        //Koordinaten abfragen
                        int[] coordinates = get_coordinates(i + 1);

                        //Grösse des Textes abfragen

                        SizeF size_string = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font);

                        int height = Convert.ToInt32(size_string.Height / 2);


                        //Vokabel schreiben
                        //Schriftgrösse anpassen

                        //Falls Text zu gross, string auf mehrere Zeilen aufteilen falls möglich
                        if (size_string.Width > 292)
                        {

                            bool is_good = false;
                            int font_size = 12;

                            if (listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text.Trim().Contains(" ") == true)
                            {
                                //Falls der String leerschläge enthält

                                string[] splitter = listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text.Trim().Split(' ');

                                do
                                {
                                    Font font_new = new Font("Arial", font_size);

                                    for (int y = 1; y < splitter.Length; y++)
                                    {
                                        string part1 = "";
                                        string part2 = "";

                                        for (int x = 1; x <= splitter.Length - y; x++)
                                        {
                                            part1 = part1 + " " + splitter[x - 1];

                                            if (x == splitter.Length - y)
                                            {
                                                for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                {
                                                    part2 = part2 + " " + splitter[z];
                                                }
                                            }
                                        }


                                        SizeF size_part1 = g.MeasureString(part1, font_new);
                                        SizeF size_part2 = g.MeasureString(part2, font_new);

                                        if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                        {
                                            is_good = true;

                                            //zwei Zeilen schreiben

                                            g.DrawString(part1, font_new, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height - 20), format);
                                            g.DrawString(part2, font_new, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height + 20), format);

                                            break;
                                        }


                                    }

                                    if (is_good == false)
                                    {
                                        font_size--;
                                    }

                                } while (is_good == false);


                            }

                            else
                            {

                                do
                                {
                                    font_size--;
                                    Font font_new = new Font("Arial", font_size);

                                    SizeF string_size = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new);

                                    if (string_size.Width > 292 && font_size > 1)
                                    {
                                        is_good = false;
                                    }

                                    else
                                    {
                                        is_good = true;

                                        //kleinerer Text schreiben
                                        g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height), format);
                                    }

                                } while (is_good == false);
                            }
                        }
                        else
                        {
                            //Falls Text nicht zu gross
                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height), format);
                        }

                    }

                    //nicht benötigte Linien entfernen

                    g.RotateTransform(+90);



                    if (noch_nicht_gedruckt <= 4)
                    {
                        Rectangle[] rect = { new Rectangle(0, 292 - top + 1, 866, 1180) };

                        g.FillRectangles(Brushes.White, rect);
                    }
                    else if (noch_nicht_gedruckt > 4 && noch_nicht_gedruckt <= 8)
                    {
                        Rectangle[] rect = { new Rectangle(0, 585 - top + 1, 866, 1180) };

                        g.FillRectangles(Brushes.White, rect);
                    }
                    else if (noch_nicht_gedruckt > 8 && noch_nicht_gedruckt <= 12)
                    {
                        Rectangle[] rect = { new Rectangle(0, 877 - top - top + 1, 866, 1180) };

                        g.FillRectangles(Brushes.White, rect);
                    }

                    //Vertikale Linien


                    //Vertikal
                    //g.DrawLine(pen, 207 - left, 0, 207 - left, 1180);
                    //g.DrawLine(pen, 413 - left, 0, 413 - left, 1180);
                    //g.DrawLine(pen, 620 - left, 0, 620 - left, 1180);

                    ////Horizontal
                    //g.DrawLine(pen, 0, 292 - top, 866, 292 - top);
                    //g.DrawLine(pen, 0, 585 - top, 866, 585 - top);
                    //g.DrawLine(pen, 0, 877 - top, 866, 877 - top);

                    int[] cordinate_rect2 = new int[4];


                    if (noch_nicht_gedruckt < 4)
                    {
                        cordinate_rect2[1] = 0;
                        cordinate_rect2[3] = 1180;
                    }
                    else if (noch_nicht_gedruckt > 4 && noch_nicht_gedruckt < 8)
                    {
                        cordinate_rect2[1] = 292 - top + 1;
                        cordinate_rect2[3] = 888;
                    }
                    else if (noch_nicht_gedruckt > 8 & noch_nicht_gedruckt < 12)
                    {
                        cordinate_rect2[1] = 585 - top + 1;
                        cordinate_rect2[3] = 593;
                    }
                    else if (noch_nicht_gedruckt > 12 & noch_nicht_gedruckt < 16)
                    {
                        cordinate_rect2[1] = 877 - top + 1;
                        cordinate_rect2[3] = 298;
                    }

                    if (noch_nicht_gedruckt == 1 || noch_nicht_gedruckt == 5 || noch_nicht_gedruckt == 9 || noch_nicht_gedruckt == 13)
                    {
                        cordinate_rect2[0] = 207 - left + 1;
                        cordinate_rect2[2] = 650;

                        Rectangle[] rect2 = { new Rectangle(cordinate_rect2[0], cordinate_rect2[1], cordinate_rect2[2], cordinate_rect2[3]) };
                        g.FillRectangles(Brushes.White, rect2);
                    }
                    else if (noch_nicht_gedruckt == 2 || noch_nicht_gedruckt == 6 || noch_nicht_gedruckt == 10 || noch_nicht_gedruckt == 14)
                    {
                        cordinate_rect2[0] = 413 - left + 1;
                        cordinate_rect2[2] = 435;


                        Rectangle[] rect2 = { new Rectangle(cordinate_rect2[0], cordinate_rect2[1], cordinate_rect2[2], cordinate_rect2[3]) };
                        g.FillRectangles(Brushes.White, rect2);
                    }
                    else if (noch_nicht_gedruckt == 3 || noch_nicht_gedruckt == 7 || noch_nicht_gedruckt == 11)
                    {
                        cordinate_rect2[0] = 620 - left + 1;
                        cordinate_rect2[2] = 218;


                        Rectangle[] rect2 = { new Rectangle(cordinate_rect2[0], cordinate_rect2[1], cordinate_rect2[2], cordinate_rect2[3]) };
                        g.FillRectangles(Brushes.White, rect2);
                    }
                    g.RotateTransform(-90);
                }

                //Pfeil zeichnen
                if (aktuelle_seite == anzahl_seiten || aktuelle_seite == 1)
                {
                    //rotieren

                    g.RotateTransform(+90);
                    Font pfeil = new Font("Arial", 12, FontStyle.Bold);

                    g.DrawString("↑", pfeil, Brushes.Black, new Point(413 - left - 30, 0), format);
                    g.DrawString("↑", pfeil, Brushes.Black, new Point(413 - left + 30, 0), format);
                }

            }

            //Rückseite
            else
            {
                //Seite rotieren ||X-Koordinaten positiv, Y-Koordinaten negativ
                g.RotateTransform(+90);


                int noch_nicht_gedruckt;
                int vok_beginnen;

                if (is_front == true)
                {
                    noch_nicht_gedruckt = anz_vok - (anzahl_seiten - aktuelle_seite) * 16;
                    vok_beginnen = ((anzahl_seiten) - (aktuelle_seite)) * 16 + 1;

                }
                else
                {
                    noch_nicht_gedruckt = anz_vok - (aktuelle_seite - 1) * 16;
                    vok_beginnen = (aktuelle_seite - 1) * 16 + 1;

                }

                //Falls noch mehr Seiten gedruckt werden müssen
                if (noch_nicht_gedruckt >= 16)
                {

                    //Positionsverschiebung der Rückseite
                    int links_rechts_verschiebung = -3;

                    for (int i = 0; i < 16; i++)
                    {
                        //Positionszugabe ändern
                        switch (links_rechts_verschiebung)
                        {
                            case 1:
                                links_rechts_verschiebung = -1;
                                break;
                            case -1:
                                links_rechts_verschiebung = -3;
                                break;
                            case -3:
                                links_rechts_verschiebung = 3;
                                break;
                            case 3:
                                links_rechts_verschiebung = 1;
                                break;
                        }


                        //Koordinaten abfragen
                        int[] coordinates = get_coordinates(i + 1 + links_rechts_verschiebung);

                        //Grösse des Textes abfragen

                        SizeF size_string = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font);

                        int height = Convert.ToInt32(size_string.Height / 2);

                        //Falls es ein Synonym gibt
                        if (listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Contains("=") == true)
                        {
                            string[] split_text = listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Split('=');

                            SizeF size_foreign = g.MeasureString(split_text[0], font);
                            SizeF size_synonym = g.MeasureString(split_text[1], font);


                            if (size_foreign.Width > 292)
                            {

                                bool is_good = false;
                                int font_size = 12;

                                if (split_text[0].Trim().Contains(" ") == true)
                                {
                                    //Falls der String leerschläge enthält

                                    string[] splitter = split_text[0].Trim().Split(' ');

                                    do
                                    {
                                        Font font_new = new Font("Arial", font_size);

                                        for (int y = 1; y < splitter.Length; y++)
                                        {
                                            string part1 = "";
                                            string part2 = "";

                                            for (int x = 1; x <= splitter.Length - y; x++)
                                            {
                                                part1 = part1 + " " + splitter[x - 1];

                                                if (x == splitter.Length - y)
                                                {
                                                    for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                    {
                                                        part2 = part2 + " " + splitter[z];
                                                    }
                                                }
                                            }


                                            SizeF size_part1 = g.MeasureString(part1, font_new);
                                            SizeF size_part2 = g.MeasureString(part2, font_new);

                                            if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                            {
                                                is_good = true;

                                                //zwei Zeilen schreiben


                                                g.DrawString(part1, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 60 - height), format);
                                                g.DrawString(part2, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 20 - height), format);

                                                break;
                                            }


                                        }

                                        if (is_good == false)
                                        {
                                            font_size--;
                                        }

                                    } while (is_good == false);


                                }

                            }
                            else
                            {
                                //Foreign normal schreiben
                                g.DrawString(split_text[0], font, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 20 - height), format);
                            }

                            //Trennlinie zeichnen

                            g.DrawLine(pen, new Point((coordinates[0] * (-1)) - top - 10, (coordinates[1] * (-1)) + left - height / 2), new Point((coordinates[0] * (-1)) - top + 10, (coordinates[1] * (-1)) + left - height / 2));


                            //Falls Synonym zu gross
                            if (size_foreign.Width > 292)
                            {

                                bool is_good = false;
                                int font_size = 12;

                                if (split_text[1].Trim().Contains(" ") == true)
                                {
                                    //Falls der String leerschläge enthält

                                    string[] splitter = split_text[1].Trim().Split(' ');

                                    do
                                    {
                                        Font font_new = new Font("Arial", font_size);

                                        for (int y = 1; y < splitter.Length; y++)
                                        {
                                            string part1 = "";
                                            string part2 = "";

                                            for (int x = 1; x <= splitter.Length - y; x++)
                                            {
                                                part1 = part1 + " " + splitter[x - 1];

                                                if (x == splitter.Length - y)
                                                {
                                                    for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                    {
                                                        part2 = part2 + " " + splitter[z];
                                                    }
                                                }
                                            }


                                            SizeF size_part1 = g.MeasureString(part1, font_new);
                                            SizeF size_part2 = g.MeasureString(part2, font_new);

                                            if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                            {
                                                is_good = true;

                                                //zwei Zeilen schreiben


                                                g.DrawString(part1, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 20), format);
                                                g.DrawString(part2, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 60), format);

                                                break;
                                            }
                                        }

                                        if (is_good == false)
                                        {
                                            font_size--;
                                        }

                                    } while (is_good == false);
                                }
                            }
                            else
                            {
                                //Synonym normal schreiben
                                g.DrawString(split_text[1], font, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 20), format);
                            }
                            //Falls es kein Synonym gibt
                        }
                        else
                        {
                            //Schriftgrösse anpassen
                            //Falls Text zu gross

                            //Falls Text zu gross, string auf mehrere Zeilen aufteilen falls möglich
                            if (size_string.Width > 292)
                            {

                                bool is_good = false;
                                int font_size = 12;

                                if (listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Trim().Contains(" ") == true)
                                {
                                    //Falls der String leerschläge enthält

                                    string[] splitter = listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Trim().Split(' ');

                                    do
                                    {
                                        Font font_new = new Font("Arial", font_size);

                                        for (int y = 1; y < splitter.Length; y++)
                                        {
                                            string part1 = "";
                                            string part2 = "";

                                            for (int x = 1; x <= splitter.Length - y; x++)
                                            {
                                                part1 = part1 + " " + splitter[x - 1];

                                                if (x == splitter.Length - y)
                                                {
                                                    for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                    {
                                                        part2 = part2 + " " + splitter[z];
                                                    }
                                                }
                                            }


                                            SizeF size_part1 = g.MeasureString(part1, font_new);
                                            SizeF size_part2 = g.MeasureString(part2, font_new);

                                            if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                            {
                                                is_good = true;

                                                //zwei Zeilen schreiben


                                                g.DrawString(part1, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 20), format);
                                                g.DrawString(part2, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 20), format);

                                                break;
                                            }
                                        }

                                        if (is_good == false)
                                        {
                                            font_size--;
                                        }

                                    } while (is_good == false);
                                }
                                else
                                {

                                    do
                                    {
                                        font_size--;
                                        Font font_new = new Font("Arial", font_size);

                                        SizeF string_size = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new);

                                        if (string_size.Width > 292 && font_size > 1)
                                        {
                                            is_good = false;
                                        }

                                        else
                                        {
                                            is_good = true;

                                            //kleinerer Text schreiben
                                            g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[1].Text, font_new, Brushes.Black, new Point(coordinates[0] + top, coordinates[1] - left - height), format);
                                        }

                                    } while (is_good == false);
                                }
                            }
                            else
                            {
                                //Normal schreiben
                                g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height), format);
                            }
                        }
                    }
                    //Falls letzte Seite
                }
                else
                {
                    //Positionsverschiebung der Rückseite
                    int links_rechts_verschiebung = -3;

                    for (int i = 0; i < noch_nicht_gedruckt; i++)
                    {

                        //Positionszugabe ändern
                        switch (links_rechts_verschiebung)
                        {
                            case 1:
                                links_rechts_verschiebung = -1;
                                break;
                            case -1:
                                links_rechts_verschiebung = -3;
                                break;
                            case -3:
                                links_rechts_verschiebung = 3;
                                break;
                            case 3:
                                links_rechts_verschiebung = 1;
                                break;
                        }

                        //Koordinaten abfragen
                        int[] coordinates = get_coordinates(i + 1 + links_rechts_verschiebung);

                        //Grösse des Textes abfragen

                        SizeF size_string = g.MeasureString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font);

                        int height = Convert.ToInt32(size_string.Height / 2);

                        //Schriftgrösse anpassen

                        //Vokabel schreiben
                        //Falls es ein Synonym gibt
                        if (listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Contains("=") == true)
                        {
                            string[] split_text = listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Split('=');

                            SizeF size_foreign = g.MeasureString(split_text[0], font);
                            SizeF size_synonym = g.MeasureString(split_text[1], font);

                            //Falls Foreign zu gross
                            if (size_foreign.Width > 292)
                            {

                                bool is_good = false;
                                int font_size = 12;

                                if (split_text[0].Trim().Contains(" ") == true)
                                {
                                    //Falls der String leerschläge enthält

                                    string[] splitter = split_text[0].Trim().Split(' ');

                                    do
                                    {
                                        Font font_new = new Font("Arial", font_size);

                                        for (int y = 1; y < splitter.Length; y++)
                                        {
                                            string part1 = "";
                                            string part2 = "";

                                            for (int x = 1; x <= splitter.Length - y; x++)
                                            {
                                                part1 = part1 + " " + splitter[x - 1];

                                                if (x == splitter.Length - y)
                                                {
                                                    for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                    {
                                                        part2 = part2 + " " + splitter[z];
                                                    }
                                                }
                                            }


                                            SizeF size_part1 = g.MeasureString(part1, font_new);
                                            SizeF size_part2 = g.MeasureString(part2, font_new);

                                            if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                            {
                                                is_good = true;

                                                //zwei Zeilen schreiben


                                                g.DrawString(part1, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 60 - height), format);
                                                g.DrawString(part2, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 20 - height), format);

                                                break;
                                            }


                                        }

                                        if (is_good == false)
                                        {
                                            font_size--;
                                        }

                                    } while (is_good == false);


                                }


                            }
                            else
                            {
                                //Foreign normal schreiben
                                g.DrawString(split_text[0], font, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 20 - height), format);
                            }

                            //Trennlinie zeichnen

                            g.DrawLine(pen, new Point((coordinates[0] * (-1)) - top - 10, (coordinates[1] * (-1)) + left - height / 2), new Point((coordinates[0] * (-1)) - top + 10, (coordinates[1] * (-1)) + left - height / 2));

                            //Falls synonym zu gross
                            if (size_foreign.Width > 292)
                            {

                                bool is_good = false;
                                int font_size = 12;

                                if (split_text[1].Trim().Contains(" ") == true)
                                {
                                    //Falls der String leerschläge enthält

                                    string[] splitter = split_text[1].Trim().Split(' ');

                                    do
                                    {
                                        Font font_new = new Font("Arial", font_size);

                                        for (int y = 1; y < splitter.Length; y++)
                                        {
                                            string part1 = "";
                                            string part2 = "";

                                            for (int x = 1; x <= splitter.Length - y; x++)
                                            {
                                                part1 = part1 + " " + splitter[x - 1];

                                                if (x == splitter.Length - y)
                                                {
                                                    for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                    {
                                                        part2 = part2 + " " + splitter[z];
                                                    }
                                                }
                                            }


                                            SizeF size_part1 = g.MeasureString(part1, font_new);
                                            SizeF size_part2 = g.MeasureString(part2, font_new);

                                            if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                            {
                                                is_good = true;

                                                //zwei Zeilen schreiben


                                                g.DrawString(part1, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 20), format);
                                                g.DrawString(part2, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 60), format);

                                                break;
                                            }


                                        }

                                        if (is_good == false)
                                        {
                                            font_size--;
                                        }

                                    } while (is_good == false);


                                }

                            }

                            else
                            {
                                //Synonym normal schreiben
                                g.DrawString(split_text[1], font, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 20), format);

                            }

                        }
                        //Falls es kein Synonym gibt
                        else
                        {
                            //Schriftgrösse anpassen
                            //Falls Text zu gross
                            if (size_string.Width > 292)
                            {

                                bool is_good = false;
                                int font_size = 12;

                                if (listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Trim().Contains(" ") == true)
                                {
                                    //Falls der String leerschläge enthält

                                    string[] splitter = listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text.Trim().Split(' ');

                                    do
                                    {
                                        Font font_new = new Font("Arial", font_size);

                                        for (int y = 1; y < splitter.Length; y++)
                                        {
                                            string part1 = "";
                                            string part2 = "";

                                            for (int x = 1; x <= splitter.Length - y; x++)
                                            {
                                                part1 = part1 + " " + splitter[x - 1];

                                                if (x == splitter.Length - y)
                                                {
                                                    for (int z = splitter.Length - y; z < splitter.Length; z++)
                                                    {
                                                        part2 = part2 + " " + splitter[z];
                                                    }
                                                }
                                            }


                                            SizeF size_part1 = g.MeasureString(part1, font_new);
                                            SizeF size_part2 = g.MeasureString(part2, font_new);

                                            if (size_part1.Width <= 292 && size_part2.Width <= 292)
                                            {
                                                is_good = true;

                                                //zwei Zeilen schreiben


                                                g.DrawString(part1, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height - 20), format);
                                                g.DrawString(part2, font_new, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height + 20), format);

                                                break;
                                            }
                                        }

                                        if (is_good == false)
                                        {
                                            font_size--;
                                        }

                                    } while (is_good == false);
                                }

                            }
                            else
                            {
                                //Text normal schreiben
                                g.DrawString(listView_vokabeln.Items[vokabelliste[vok_beginnen - 1 + i]].SubItems[2].Text, font, Brushes.Black, new Point((coordinates[0] * (-1)) - top, (coordinates[1] * (-1)) + left - height), format);
                            }
                        }
                    }
                }
            }



            //Schauen, ob noch mehr Seiten gedruckt werden müssen

            if (aktuelle_seite != anzahl_seiten)
            {
                e.HasMorePages = true;
                aktuelle_seite++;
            }
            else
            {
                e.HasMorePages = false;
            }
        }
        private void printDocument_cards_BeginPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            //Anzahl Seiten festlegen

            double anz_vokD = (double)anz_vok;

            double i = Math.Ceiling(anz_vokD / 16);

            anzahl_seiten = (int)i;
            aktuelle_seite = 1;

        }
        //Koordinaten verlangen
        private int[] get_coordinates(int number)
        {
            switch (number)
            {
                case 01: return new int[] { -146, 103 };
                case 02: return new int[] { -146, 310 };
                case 03: return new int[] { -146, 516 };
                case 04: return new int[] { -146, 723 };
                case 05: return new int[] { -438, 103 };
                case 06: return new int[] { -438, 310 };
                case 07: return new int[] { -438, 516 };
                case 08: return new int[] { -438, 723 };
                case 09: return new int[] { -731, 103 };
                case 10: return new int[] { -731, 310 };
                case 11: return new int[] { -731, 516 };
                case 12: return new int[] { -731, 723 };
                case 13: return new int[] { -1023, 103 };
                case 14: return new int[] { -1023, 310 };
                case 15: return new int[] { -1023, 516 };
                default: return new int[] { -1023, 723 };
            }
        }


        //E-Mail senden

        private void vokabelheftSendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pfad_vokabelheft != "" && edit_vokabelheft != true)
            {
                FileInfo info = new FileInfo(pfad_vokabelheft);

                if (info.Exists == true)
                {
                    try
                    {
                        MAPI mapi = new MAPI();
                        mapi.AddAttachment(pfad_vokabelheft);
                        // TODO: Add parameters for mail subject
                        mapi.SendMailPopup(string.Format(Words.MailSubjectVocabularyBook), "");
                    }
                    catch
                    {

                    }
                }
                else
                {
                    //Fehlermeldung

                    MessageBox.Show(Properties.language.treeview_doesnt_exists,
                                   Properties.language.name,
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);
                }
            }
            else //Falls die Datei noch nicht gespeichert wurde
            {
                MessageBox.Show(Properties.language.have_to_save_mail,
                                   Properties.language.name,
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
            }
        }

        //Importieren

        private void importierenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Datei importieren

            //Öffnen Dialog starten
            OpenFileDialog import_file = new OpenFileDialog
            {
                Title = Words.Import,
                Filter = "CSV (*.csv)|*.csv"
            };

            //Falls auf öffnen geklickt wurde
            if (import_file.ShowDialog() == DialogResult.OK)
            {
                //Cursor auf warten setzen
                Cursor.Current = Cursors.WaitCursor;

                string pfad_import = import_file.FileName;

                //Bereits geöffnete Datei speichern

                bool result = true;

                if (edit_vokabelheft == true)
                {
                    result = vokabelheft_ask_to_save();
                }
                //Falls Datei mit Ergebnissen nochmals gespeichert werden soll

                else if (save_new == true)
                {
                    savefile(false);
                }

                if (result == true)
                {
                    //Datei einlesen

                    try
                    {
                        // Datei lesen

                        StreamReader reader = new StreamReader(pfad_import, Encoding.Default);

                        string[] lines = reader.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                        reader.Close();


                        //Neue Liste generieren
                        List<string[]> import_vocab_list = new List<string[]>();

                        //gesplittete Vokabeln zur Liste hinzufügen
                        for (int i = 0; i < lines.Length; i++)
                        {
                            //Falss die Linie den Seperator enthält
                            if (lines[i].Contains(";") == true && lines[i].Length != 1)
                            {
                                string[] vocab = lines[i].Split(';');

                                //Richtige Position herausfinden
                                int place = 0;
                                bool added = false;
                                for (int n = 0; n < import_vocab_list.Count; n++)
                                {
                                    place++;
                                    string[] ntext = import_vocab_list[n];

                                    if (String.Compare(ntext[0], vocab[0]) > 0) //Eintrag n kleiner als neue Vokabel
                                    {
                                        import_vocab_list.Insert(place - 1, vocab); //vor dieser Stelle einsetzen
                                        added = true;
                                        break; //und Schleife verlassen
                                    }
                                }
                                if (added == false) //noch nicht eingesetzt?
                                {
                                    import_vocab_list.Add(vocab);
                                }

                            }
                        }
                        if (import_vocab_list.Count == 0)
                        {
                            //gesplittete Vokabeln zur Liste hinzufügen
                            for (int i = 0; i < lines.Length; i++)
                            {
                                //Falss die Linie den Seperator enthält
                                if (lines[i].Contains('\t') == true && lines[i].Length != 1)
                                {

                                    string[] vocab = lines[i].Split('\t');

                                    //Richtige Position herausfinden

                                    int place = 0;
                                    bool added = false;
                                    for (int n = 0; n < import_vocab_list.Count; n++)
                                    {
                                        place++;
                                        string[] ntext = import_vocab_list[n];

                                        if (String.Compare(ntext[0], vocab[0]) > 0) //Eintrag n kleiner als neue Vokabel
                                        {
                                            import_vocab_list.Insert(place - 1, vocab); //vor dieser Stelle einsetzen
                                            added = true;
                                            break; //und Schleife verlassen
                                        }
                                    }
                                    if (added == false) //noch nicht eingesetzt?
                                    {
                                        import_vocab_list.Add(vocab);
                                    }
                                }
                            }
                        }


                        //form vorbereiten

                        listView_vokabeln.Enabled = false;

                        listView_vokabeln.BeginUpdate();

                        listView_vokabeln.GridLines = Properties.Settings.Default.GridLines;

                        listView_vokabeln.Clear();
                        listView_vokabeln.Columns.Clear();

                        listView_vokabeln.BackColor = SystemColors.Window;

                        listView_vokabeln.Columns.Add("", 20);

                        //Header anpassen, falls Fenster maximiert ist
                        if (WindowState == FormWindowState.Maximized)
                        {
                            int size = (listView_vokabeln.Width - 20 - 100 - 22) / 2;

                            listView_vokabeln.Columns.Add(Words.MotherTongue, size);
                            listView_vokabeln.Columns.Add(Words.ForeignLanguage, size);
                        }
                        else
                        {
                            listView_vokabeln.Columns.Add(Words.MotherTongue, 155);
                            listView_vokabeln.Columns.Add(Words.ForeignLanguage, 200);
                        }


                        listView_vokabeln.Columns.Add(Words.LastPracticed, 100);

                        //Vokabeln einlesen

                        for (int i = 0; i < import_vocab_list.Count; i++)
                        {
                            string[] vocab = import_vocab_list[i];

                            ListViewItem item = new ListViewItem(new[] { "", vocab[0], vocab[1], "" })
                            {
                                ImageIndex = 0,
                                Tag = 0
                            };

                            listView_vokabeln.Items.Add(item);
                        }


                        listView_vokabeln.EndUpdate();

                        //Infos

                        listView_vokabeln.Enabled = true;
                        listView_vokabeln.BackColor = SystemColors.Window;

                        //group-boxes aktivieren

                        listView_vokabeln.GridLines = Properties.Settings.Default.GridLines;

                        groupBox1.Enabled = true;
                        groupBox2.Enabled = true;
                        groupBox3.Enabled = true;
                        statistik.Visible = true;

                        practice_vokabelheft.Enabled = true;
                        vokabelnÜbenToolStripMenuItem.Enabled = true;

                        neueVokabelHinzufügenToolStripMenuItem.Enabled = true;
                        vokabelheftOptionenToolStripMenuItem.Enabled = true;

                        edit_vokabel.Enabled = false;
                        vokabelBearbeitenToolStripMenuItem.Enabled = false;

                        delet_vokabel.Enabled = false;
                        vokabelLöschenToolStripMenuItem.Enabled = false;

                        search_vokabel_field.Enabled = true;
                        search_vokabel_field.Text = "";

                        SpeichernUnterToolStripMenuItem.Enabled = true;

                        vokabelheftSchliessenToolStripMenuItem.Enabled = true;

                        druckenToolStripMenuItem.Enabled = true;
                        print_vokabelheft.Enabled = true;

                        vokabelheftSendenToolStripMenuItem.Enabled = true;


                        exportierenToolStripMenuItem.Enabled = true;



                        //Titelleiste
                        Text = Words.Vocup;

                        insert_vokabel.Focus();

                        vokabelheft_code = "";
                        pfad_vokabelheft = "";

                        save_new = false;

                        vokabelheft_edited();

                    }
                    catch
                    {
                        MessageBox.Show(Properties.language.can_not_import,
                                    Properties.language.name,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);

                    }

                }

            }

            //Cursor auf normal setzen
            Cursor.Current = Cursors.Default;
        }

        //Exportieren

        private void exportierenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Exportieren

            //Speichern-Dialog starten

            SaveFileDialog export_file = new SaveFileDialog
            {
                Title = Words.Export,
                Filter = "CSV (*.csv)|*.csv"
            };

            //Falls auf speichern geklickt wurde

            if (export_file.ShowDialog() == DialogResult.OK)
            {
                //Cursor auf warten setzen
                Cursor.Current = Cursors.WaitCursor;

                using (StreamWriter writer = new StreamWriter(export_file.FileName, false, Encoding.Unicode))
                {
                    writer.Write(listView_vokabeln.Columns[1].Text);
                    writer.Write('\t');
                    writer.Write(listView_vokabeln.Columns[2].Text);
                    writer.WriteLine();
                    foreach (ListViewItem item in listView_vokabeln.Items)
                    {
                        writer.WriteLine();
                        writer.Write($"\"{item.SubItems[1].Text.Replace("\"", "\"\"")}\"\t");
                        writer.Write($"\"{item.SubItems[2].Text.Replace("\"", "\"\"")}\"");
                    }
                }

                //Cursor auf normal setzen
                Cursor.Current = Cursors.Default;
            }
        }

        //Nach updates suchen

        private void update_splash()
        {
            update_windows updater = new update_windows();
            updater.ShowDialog();
        }
        private void search_update()
        {
            Thread update_thread = new Thread(update_splash);
            update_thread.Start();

            Update();

            //Nach updates suchen

            K_Updater.Settings KSettings;
            KSettings.AuthenticateMode = K_Updater.SelfUpdate.authentication.none;
            KSettings.AuthenticateUsername = "";
            KSettings.AuthenticatePassword = "";
            KSettings.CurrentAppVersion = Application.ProductVersion;
            KSettings.Language = K_Updater.SelfUpdate.language.german;
            KSettings.Proxy = "";
            KSettings.ProxyUsername = "";
            KSettings.ProxyPassword = "";
            KSettings.UpdatePath = "http://update.vocup.ch";

            K_Updater.SelfUpdate SUpdate = new K_Updater.SelfUpdate(KSettings);

            K_Updater.UpdateCheckResult KResult = SUpdate.Check();


            //Nach updates suchen

            if (KResult.Code == 0)
            {
                update_thread.Abort();
                Activate();

                MessageBox.Show("Es sind keine neuen Updates verfügbar.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (KResult.Code == 1)
            {
                update_thread.Abort();
                Activate();

                DialogResult result = MessageBox.Show("Es ist ein neues Update verfügbar.\r\n\r\n" +
                                                      "Neue Version: " + KResult.NewVersion + "\r\n\r\n" +
                                                      "Beschreibung:\r\n\r\n" + KResult.Description + "\r\n\r\n" +
                                                      "Möchten Sie die neue Version herunterladen?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                bool do_update = true;

                if (result == DialogResult.Yes)
                {
                    if (edit_vokabelheft == true)
                    {
                        DialogResult result2 = MessageBox.Show(Properties.language.save_before_update,
                                                 Application.ProductName,
                                                 MessageBoxButtons.YesNoCancel,
                                                 MessageBoxIcon.Warning);
                        if (result2 == DialogResult.Yes)
                        {
                            savefile(false);

                            edit_vokabelheft = false;
                        }
                        else if (result2 == DialogResult.No)
                        {
                            edit_vokabelheft = false;
                        }
                        else if (result2 == DialogResult.Cancel)
                        {
                            do_update = false;
                        }
                    }

                    //Update durchführen
                    if (do_update == true)
                    {
                        SUpdate.DoUpdate();
                    }

                }
            }
            //Falls Fehler aufgetaucht sind
            else
            {
                update_thread.Abort();
                Activate();

                MessageBox.Show("Während der Updateprüfung ist ein Fehler aufgetreten.\r\n\r\n" +
                                "Fehlercode: " + KResult.Code.ToString() + "\r\n" +
                                "Beschreibung: " + KResult.Description.ToString() + "\r\n",
                                Application.ProductName,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        /// <summary>
        /// Updates the text of the statistics group box.
        /// </summary>
        private void infos_vokabelhefte_text()
        {
            //Anzahl Vokabeln ermitteln

            int anzvok = listView_vokabeln.Items.Count;
            int anz_noch_nicht = 0;
            int anz_falsch = 0;
            int anz_richtig = 0;
            int anz_fertig = 0;

            foreach (ListViewItem item in listView_vokabeln.Items)
            {
                if (item.ImageIndex == 0)
                {
                    anz_noch_nicht++;
                }
                else if (item.ImageIndex == 1)
                {
                    anz_falsch++;
                }
                else if (item.ImageIndex == 2)
                {
                    anz_richtig++;
                }
                else if (item.ImageIndex == 3)
                {
                    anz_fertig++;
                }
            }

            //Text generieren

            noch_nicht_text.Text = anz_noch_nicht.ToString();
            falsch_text.Text = anz_falsch.ToString();
            richtig_text.Text = anz_richtig.ToString();
            fertig_text.Text = anz_fertig.ToString();
            gesamt_text.Text = anzvok.ToString();
        }

        //Vokabelheft als verändert anzeigen

        private void vokabelheft_edited()
        {
            edit_vokabelheft = true;

            // Speichern-Symbole anzeigen oder Automatisch Speichern

            if (Properties.Settings.Default.auto_save == true && pfad_vokabelheft != "")
            {
                savefile(false);
            }
            else
            {
                save_vokabelheft.Enabled = true;
                SpeichernToolStripMenuItem.Enabled = true;
            }
        }

        //Nach Vokabel suchen

        private void search_vokabel(string search_text)
        {
            //Suchanfrage bearbeiten

            //Easter Egg

            search_text = search_text.ToUpper();

            if (search_text == "EASTER EGG")
            {
                //Bereits geöffnete Datei speichern
                savefile(false);

                //form vorbereiten
                listView_vokabeln.BeginUpdate();
                listView_vokabeln.Enabled = false;

                vokabelheft_code = "";
                edit_vokabelheft = false;

                save_vokabelheft.Enabled = false;
                SpeichernToolStripMenuItem.Enabled = false;

                uebersetzungsrichtung = "";

                pfad_vokabelheft = "";

                save_new = false;

                if (listView_vokabeln.Items.Count > 0)
                {
                    listView_vokabeln.Clear();
                }
                if (listView_vokabeln.Columns.Count > 0)
                {
                    listView_vokabeln.Columns.Clear();
                }

                listView_vokabeln.Columns.Add("", 20);

                //Header anpassen, falls Fenster maximiert ist
                if (WindowState == FormWindowState.Maximized)
                {
                    int size = (listView_vokabeln.Width - 20 - 100 - 22) / 2;

                    listView_vokabeln.Columns.Add("Deutsch", size);
                    listView_vokabeln.Columns.Add("Esperanto", size);
                }
                else
                {
                    listView_vokabeln.Columns.Add("Deutsch", 155);
                    listView_vokabeln.Columns.Add("Esperanto", 200);
                }

                listView_vokabeln.Columns.Add(Words.LastPracticed, 100);


                //Vokabeln einlesen
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "klicken", "klaki", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "chatten", "babili", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Bildschirm", "ekrano", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Fenster", "fenestro", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Browser", "retumilo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Computer", "komputilo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Link", "ligilo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Linux", "Linukso", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Macintosh", "Makintoŝo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Webseiten", "paĝaro", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Webseite", "retpaĝo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "eMail-Adresse", "retpoŝto", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Server", "servilo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Benutzername", "uzantnomo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Kennwort", "pasvorto", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Windows", "Vindozo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Datei", "dosiero", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Ordner", "dosierujo", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Herunterladen", "elŝuti", "" }));
                listView_vokabeln.Items.Add(new ListViewItem(new string[] { "", "Internet", "interreto", "" }));


                for (int i = 0; i < listView_vokabeln.Items.Count; i++)
                {
                    listView_vokabeln.Items[i].ImageIndex = 0;
                    listView_vokabeln.Items[i].Tag = 0;
                }

                //Infos

                infos_vokabelhefte_text();

                listView_vokabeln.Enabled = true;
                listView_vokabeln.BackColor = SystemColors.Window;

                //group-boxes aktivieren

                listView_vokabeln.GridLines = Properties.Settings.Default.GridLines;

                groupBox1.Enabled = true;
                groupBox2.Enabled = true;
                groupBox3.Enabled = true;
                statistik.Visible = true;

                practice_vokabelheft.Enabled = true;
                vokabelnÜbenToolStripMenuItem.Enabled = true;

                neueVokabelHinzufügenToolStripMenuItem.Enabled = true;
                vokabelheftOptionenToolStripMenuItem.Enabled = true;

                edit_vokabel.Enabled = false;
                vokabelBearbeitenToolStripMenuItem.Enabled = false;

                delet_vokabel.Enabled = false;
                vokabelLöschenToolStripMenuItem.Enabled = false;

                search_vokabel_field.Enabled = true;
                search_vokabel_field.Text = "";

                SpeichernUnterToolStripMenuItem.Enabled = true;

                vokabelheftSchliessenToolStripMenuItem.Enabled = true;

                druckenToolStripMenuItem.Enabled = true;
                print_vokabelheft.Enabled = true;

                vokabelheftSendenToolStripMenuItem.Enabled = true;

                exportierenToolStripMenuItem.Enabled = true;


                vokabelheft_edited();

                //Vokabeln sortieren

                listView_vokabeln.ListViewItemSorter = new ListViewComparer(1, SortOrder.Ascending);

                listView_vokabeln.EndUpdate();

                search_vokabel_field.Text = "";
            }
            else if (search_text == "LISTVIEW.GRIDLINES = TRUE" || search_text == "LISTVIEW.GRIDLINES = FALSE")
            {
                if (search_text == "LISTVIEW.GRIDLINES = TRUE")
                {
                    listView_vokabeln.GridLines = true;
                    Properties.Settings.Default.GridLines = true;
                }
                else
                {
                    listView_vokabeln.GridLines = false;
                    Properties.Settings.Default.GridLines = false;
                }
                Properties.Settings.Default.Save();

                search_vokabel_field.Text = "";
            }


            //EasterEgg 2
            else if (search_text == "TRANSPARENT")
            {
                Opacity = 0.8;
                search_vokabel_field.Text = "";
            }

            else if (search_text == "WELCOMESCREEN")
            {
                Properties.Settings.Default.startscreen = "willkommensbild";
                Properties.Settings.Default.Save();
            }
            else
            {
                // ListView durchsuchen

                //Index bestimmen der durchsucht werden soll

                int index_of = 0;

                //Wird ausgeführt, falls ein Item markiert ist

                if (listView_vokabeln.SelectedItems.Count > 0)
                {

                    //Falls das Letzte Item markiert ist, wird der Index auf 0 gesetzt

                    if (listView_vokabeln.FocusedItem.Index + 1 == listView_vokabeln.Items.Count)
                    {
                        index_of = 0;
                    }

                    //Ansonsten wird der Index um 1 erhöht

                    else
                    {
                        index_of = listView_vokabeln.FocusedItem.Index + 1;
                    }
                }

                //Die Variable dient dazu, damit es keine Endlosschleife gibt 

                int controll = 0;

                for (int i = index_of; i < listView_vokabeln.Items.Count; i++)
                {

                    controll++;

                    //Wird ausgeführt, sobald ein Treffer gefunden wurde

                    if (listView_vokabeln.Items[i].SubItems[1].Text.ToUpper().Contains(search_text) == true || listView_vokabeln.Items[i].SubItems[2].Text.ToUpper().Contains(search_text) == true)
                    {
                        listView_vokabeln.BeginUpdate();

                        listView_vokabeln.Focus();
                        listView_vokabeln.Items[i].Selected = true;
                        listView_vokabeln.Items[i].Focused = true;
                        listView_vokabeln.Items[i].EnsureVisible();
                        listView_vokabeln.EndUpdate();

                        //Grün aufblinken

                        search_vokabel_field.BackColor = Color.FromArgb(144, 238, 144);
                        search_vokabel_field.Update();
                        Thread.Sleep(300);

                        search_vokabel_field.BackColor = Color.White;
                        search_vokabel_field.Update();
                        break;
                    }

                    //Verhindert eine Endlosschleife

                    if (controll == listView_vokabeln.Items.Count + 1)
                    {
                        //Rot aufblinken

                        search_vokabel_field.BackColor = Color.FromArgb(255, 192, 203);
                        search_vokabel_field.Update();
                        Thread.Sleep(300);

                        search_vokabel_field.BackColor = Color.White;
                        search_vokabel_field.Update();
                        break;
                    }

                    //Falls der Index beim letzten Item liegt, springt der Index zum ersten Item

                    if (i == listView_vokabeln.Items.Count - 1)
                    {
                        i = -1;
                    }
                }


                //Fokus auf Einfügen-Button zurücksetzen
            }
            AcceptButton = insert_vokabel;

            insert_vokabel.Update();
        }

        //-----

        //Falls das Fenster maximiert ode auf Standartgrösse geändert wird

        private void program_form_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                if (listView_vokabeln.Enabled == true && listView_vokabeln.Visible == true)
                {
                    //Header-Breite im Listview verändern

                    int size = (listView_vokabeln.Size.Width - 20 - 100 - 22) / 2;

                    listView_vokabeln.Columns[1].Width = size;
                    listView_vokabeln.Columns[2].Width = size;
                }
            }
            else if (WindowState == FormWindowState.Normal)
            {
                if (listView_vokabeln.Enabled == true && listView_vokabeln.Visible == true)
                {
                    splitContainer.SplitterDistance = 150;
                    listView_vokabeln.Columns[1].Width = 155;
                    listView_vokabeln.Columns[2].Width = 200;
                }
            }
        }


        //Beenden

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {

            bool result = true;

            if (edit_vokabelheft == true)
            {
                result = vokabelheft_ask_to_save();
            }

            //Falls eine neue Ergebnis-Datei gespeichert werden soll
            else if (save_new == true)
            {
                try
                {
                    savefile(false);
                }
                catch
                {
                    Close();
                }
            }

            if (result == true)
            {
                Close();
            }
        }

        private void program_form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (edit_vokabelheft)
            {
                e.Cancel = !vokabelheft_ask_to_save();
            }
            else if (save_new) //Falls eine neue Ergebnis-Datei gespeichert werden soll
            {
                try
                {
                    savefile(false);
                    e.Cancel = false;
                }
                catch
                {
                    e.Cancel = true;
                }
            }
        }

        //-----

    }

    //ListView sortieren

    public class ListViewComparer : IComparer
    {
        private readonly int col;
        private readonly SortOrder order;

        public ListViewComparer(int col, SortOrder order)
        {
            this.col = col;
            this.order = order;
        }

        public int Compare(object x, object y)
        {
            //Falls die Symbole sortiert werden sollen
            if (col == 0)
            {
                ListViewItem item1, item2;
                item1 = (ListViewItem)x;
                item2 = (ListViewItem)y;
                if (this.order == SortOrder.Ascending)
                    return item1.ImageIndex.CompareTo(item2.ImageIndex);
                else
                    return item2.ImageIndex.CompareTo(item1.ImageIndex);
            }

            //Falls das Datum sortiert werden soll
            else if (col == 3)
            {
                ListViewItem item1, item2;
                item1 = (ListViewItem)x;
                item2 = (ListViewItem)y;

                string[] splitter1 = item1.SubItems[3].Text.Replace(' ', ':').Replace('.', ':').Split(':');
                string[] splitter2 = item2.SubItems[3].Text.Replace(' ', ':').Replace('.', ':').Split(':');

                double datum1;
                double datum2;

                if (splitter1.Length == 5)
                {
                    datum1 = Convert.ToDouble(splitter1[2] + splitter1[1] + splitter1[0] + splitter1[3] + splitter1[4]);
                }
                else if (splitter1.Length == 3)
                {
                    datum1 = Convert.ToDouble(splitter1[2] + splitter1[1] + splitter1[0] + "0000");
                }
                else
                {
                    datum1 = 0;
                }


                if (splitter2.Length == 5)
                {
                    datum2 = Convert.ToDouble(splitter2[2] + splitter2[1] + splitter2[0] + splitter2[3] + splitter2[4]);
                }
                else if (splitter2.Length == 3)
                {
                    datum2 = Convert.ToDouble(splitter2[2] + splitter2[1] + splitter2[0] + "0000");
                }
                else
                {
                    datum2 = 0;
                }


                if (this.order == SortOrder.Ascending)
                    return datum1.CompareTo(datum2);
                else
                    return datum2.CompareTo(datum1);
            }
            else //Falls die Vokabeln sortiert werden sollen
            {
                ListViewItem item1, item2;
                item1 = (ListViewItem)x;
                item2 = (ListViewItem)y;
                if (this.order == SortOrder.Ascending)
                    return item1.SubItems[col].Text.CompareTo(item2.SubItems[col].Text);
                else
                    return
                  item2.SubItems[col].Text.CompareTo(item1.SubItems[col].Text);
            }
        }

    }
    public class ArraySorter : IComparer
    {
        private readonly int col;
        private readonly SortOrder order;

        public ArraySorter(int col, SortOrder order)
        {
            this.col = col;
            this.order = order;
        }

        public int Compare(object x, object y)
        {
            double[] item1, item2;
            item1 = (double[])x;
            item2 = (double[])y;
            if (this.order == SortOrder.Ascending)
                return item1[1].CompareTo(item2[1]);
            else
                return item2[1].CompareTo(item1[1]);
        }
    }

    //-----
}