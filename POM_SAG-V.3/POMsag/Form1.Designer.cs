using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace POMsag
{
    partial class Form1
    {
        // Définition d'une palette de couleurs
        private static class ColorPalette
        {
            public static Color PrimaryBackground = Color.FromArgb(247, 250, 252); // Gris très clair
            public static Color SecondaryBackground = Color.FromArgb(237, 242, 247); // Gris plus foncé
            public static Color PrimaryText = Color.FromArgb(45, 55, 72); // Bleu-gris foncé
            public static Color AccentColor = Color.FromArgb(49, 130, 206); // Bleu moderne
            public static Color SecondaryText = Color.FromArgb(113, 128, 150); // Gris moyen
            public static Color BorderColor = Color.FromArgb(226, 232, 240); // Gris très léger
            public static Color WhiteBackground = Color.White;
            public static Color ErrorColor = Color.FromArgb(229, 62, 62); // Rouge pour erreurs
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // Création de la barre de menu principale
            this.mainMenu = new MenuStrip();
            this.fileMenuItem = new ToolStripMenuItem("Fichier");
            this.configMenuItem = new ToolStripMenuItem("Configuration");
            this.viewLogsMenuItem = new ToolStripMenuItem("Voir les logs");
            this.exitMenuItem = new ToolStripMenuItem("Quitter");
            this.helpMenuItem = new ToolStripMenuItem("Aide");
            this.aboutMenuItem = new ToolStripMenuItem("À propos");

            // Configuration générale du formulaire
            this.Text = "POM - Transfert de Données";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorPalette.PrimaryBackground;
            this.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            // Configuration du menu
            this.mainMenu.BackColor = ColorPalette.SecondaryBackground;
            this.mainMenu.ForeColor = ColorPalette.PrimaryText;
            this.mainMenu.Items.Add(this.fileMenuItem);
            this.mainMenu.Items.Add(this.helpMenuItem);

            // Configuration des éléments de menu
            this.fileMenuItem.DropDownItems.Add(this.configMenuItem);
            this.fileMenuItem.DropDownItems.Add(this.viewLogsMenuItem);
            this.fileMenuItem.DropDownItems.Add(new ToolStripSeparator());
            this.fileMenuItem.DropDownItems.Add(this.exitMenuItem);

            this.helpMenuItem.DropDownItems.Add(this.aboutMenuItem);

            // Événements du menu
            this.configMenuItem.Click += ConfigButton_Click;
            this.viewLogsMenuItem.Click += ViewLogs_Click;
            this.exitMenuItem.Click += (s, e) => this.Close();
            this.aboutMenuItem.Click += AboutMenuItem_Click;

            // Conteneur principal
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                BackColor = ColorPalette.PrimaryBackground
            };

            // Titre principal
            var labelTitle = new Label
            {
                Text = "Transfert de Données POM",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Light", 24, FontStyle.Regular),
                ForeColor = ColorPalette.PrimaryText,
                Height = 80,
                Margin = new Padding(0, 0, 0, 20)
            };

            // Conteneur pour les contrôles principaux
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.None
            };

            // Section de sélection des données
            var dataSelectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 250,
                BackColor = ColorPalette.WhiteBackground,
                Padding = new Padding(20)
            };

            // Label pour sélection des données
            var labelSelect = new Label
            {
                Text = "Sélectionnez les données à transférer",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = ColorPalette.PrimaryText
            };

            // ComboBox avec style moderne
            comboBoxTables = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 50,
                Font = new Font("Segoe UI", 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };
            comboBoxTables.Items.AddRange(new string[]
            {
                "Clients", "Commandes", "Produits",
                "LignesCommandes", "ReleasedProductsV2"
            });
            comboBoxTables.DropDownStyle = ComboBoxStyle.DropDownList;

            // Checkbox de filtrage avec style moderne
            checkBoxDateFilter = new CheckBox
            {
                Text = "Filtrer par date",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 12),
                ForeColor = ColorPalette.PrimaryText
            };

            // Conteneur pour les DateTimePickers
            var dateFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = ColorPalette.SecondaryBackground
            };

            // DateTimePickers avec style moderne
            dateTimePickerStart = new DateTimePicker
            {
                Dock = DockStyle.Left,
                Width = 500,
                Height = 50,
                Font = new Font("Segoe UI", 12),
                CalendarForeColor = ColorPalette.PrimaryText,
                CalendarMonthBackground = Color.White
            };

            dateTimePickerEnd = new DateTimePicker
            {
                Dock = DockStyle.Right,
                Width = 500,
                Height = 50,
                Font = new Font("Segoe UI", 12),
                CalendarForeColor = ColorPalette.PrimaryText,
                CalendarMonthBackground = Color.White
            };

            // Bouton de transfert avec style moderne
            buttonTransfer = new Button
            {
                Text = "Démarrer le transfert",
                Dock = DockStyle.Bottom,
                Height = 60,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance =
                {
                    BorderSize = 0,
                    MouseOverBackColor = Color.FromArgb(62, 142, 208)
                }
            };

            // Barre de progression
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 10,
                Style = ProgressBarStyle.Continuous,
                BackColor = ColorPalette.AccentColor,
                ForeColor = ColorPalette.AccentColor,
                Visible = false
            };

            // Panneau de statut
            statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = ColorPalette.SecondaryBackground,
                Visible = false
            };

            // Zone de texte de statut
            statusTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = ColorPalette.WhiteBackground,
                ForeColor = ColorPalette.PrimaryText,
                Font = new Font("Consolas", 10)
            };

            // Bouton de fermeture du statut
            var closeStatusButton = new Button
            {
                Text = "×",
                Dock = DockStyle.Right,
                Width = 40,
                BackColor = ColorPalette.ErrorColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closeStatusButton.Click += (s, e) => { statusPanel.Visible = false; };

            // Assemblage des contrôles
            statusPanel.Controls.Add(statusTextBox);
            statusPanel.Controls.Add(closeStatusButton);

            dateFilterPanel.Controls.Add(dateTimePickerStart);
            dateFilterPanel.Controls.Add(dateTimePickerEnd);

            dataSelectionPanel.Controls.Add(labelSelect);
            dataSelectionPanel.Controls.Add(comboBoxTables);
            dataSelectionPanel.Controls.Add(checkBoxDateFilter);
            dataSelectionPanel.Controls.Add(dateFilterPanel);

            contentPanel.Controls.Add(dataSelectionPanel);
            contentPanel.Controls.Add(progressBar);
            contentPanel.Controls.Add(buttonTransfer);
            contentPanel.Controls.Add(statusPanel);

            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(labelTitle);

            // Ajout du menu
            this.Controls.Add(mainMenu);
            this.Controls.Add(mainPanel);

            // Placement du menu
            this.MainMenuStrip = mainMenu;

            // Configuration initiale
            dateTimePickerStart.Value = DateTime.Now.AddMonths(-1);
            dateTimePickerEnd.Value = DateTime.Now;
            dateTimePickerStart.Enabled = false;
            dateTimePickerEnd.Enabled = false;
            buttonTransfer.Enabled = false;

            // Événements
            comboBoxTables.SelectedIndexChanged += ComboBoxTables_SelectedIndexChanged;
            checkBoxDateFilter.CheckedChanged += CheckBoxDateFilter_CheckedChanged;
            buttonTransfer.Click += ButtonTransfer_Click;
        }

        #endregion

        // Déclaration des contrôles
        private ComboBox comboBoxTables;
        private Button buttonTransfer;
        private ProgressBar progressBar;
        private CheckBox checkBoxDateFilter;
        private DateTimePicker dateTimePickerStart;
        private DateTimePicker dateTimePickerEnd;
        private Panel statusPanel;
        private TextBox statusTextBox;

        // Contrôles de menu
        private MenuStrip mainMenu;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem configMenuItem;
        private ToolStripMenuItem viewLogsMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem aboutMenuItem;
    }
}