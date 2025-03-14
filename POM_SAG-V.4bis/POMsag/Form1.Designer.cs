using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using POMsag.Styles;
using POMsag.Controls;

namespace POMsag
{
    partial class Form1
    {
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
            this.BackColor = ThemeColors.PrimaryBackground;
            this.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            // Configuration du menu avec les couleurs du thème
            this.mainMenu.BackColor = ThemeColors.SecondaryBackground;
            this.mainMenu.ForeColor = ThemeColors.PrimaryText;
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

            // Panneau principal qui contiendra tous les éléments
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.PrimaryBackground,
                Padding = new Padding(20)
            };

            // Titre principal
            var labelTitle = new Label
            {
                Text = "Transfert de Données POM",
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Light", 24, FontStyle.Regular),
                ForeColor = ThemeColors.PrimaryText,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Tableau de bord en haut (widgets horizontaux)
            var dashboardPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                Margin = new Padding(0, 0, 0, 20)
            };

            // Conteneur principal pour les contrôles de transfert
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.WhiteBackground,
                Padding = new Padding(20),
                BorderStyle = BorderStyle.None
            };
            ModernControlStyles.ApplyModernStyle(contentPanel, true);

            // Panel de test de connexion (en haut de contentPanel)
            var connectionTestPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10),
                BackColor = ThemeColors.SecondaryBackground,
                Margin = new Padding(0, 0, 0, 15)
            };

            // Bouton de test avec style arrondi
            var buttonTestConnection = new RoundedButton
            {
                Text = "Tester la connexion",
                Width = 200,
                Height = 40,
                BorderRadius = 8,
                IsPrimary = false,
                Location = new Point(10, 10)
            };
            buttonTestConnection.Click += ButtonTestConnection_Click;

            // Label d'état de connexion
            connectionStatusLabel = new Label
            {
                Text = "État: Non testé",
                AutoSize = false,
                Width = 400,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeColors.SecondaryText,
                Location = new Point(220, 15)
            };

            // Panel pour le filtre de date et sélection de table
            var selectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                Padding = new Padding(0, 10, 0, 10),
                Margin = new Padding(0, 10, 0, 10)
            };

            // ComboBox pour sélection de table
            var tableSelectLabel = ModernControlStyles.CreateSectionTitle("Sélectionnez les données à transférer");
            tableSelectLabel.Dock = DockStyle.Top;
            tableSelectLabel.Height = 30;
            tableSelectLabel.Margin = new Padding(0, 0, 0, 5);

            comboBoxTables = new ComboBox
            {
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Segoe UI", 11),
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 15)
            };
            comboBoxTables.Items.AddRange(new string[]
            {
                "Clients", "Commandes", "Produits",
                "LignesCommandes", "ReleasedProductsV2"
            });
            ModernControlStyles.ApplyModernStyle(comboBoxTables);

            // Checkbox et panel de dates
            checkBoxDateFilter = new CheckBox
            {
                Text = "Filtrer par date",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 11),
                ForeColor = ThemeColors.PrimaryText,
                Margin = new Padding(0, 0, 0, 5)
            };

            var dateFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ThemeColors.SecondaryBackground,
                Padding = new Padding(10, 10, 10, 10)
            };

            dateTimePickerStart = new DateTimePicker
            {
                Width = (dateFilterPanel.Width - 30) / 2,
                Height = 40,
                Font = new Font("Segoe UI", 11),
                Format = DateTimePickerFormat.Short,
                Location = new Point(10, 10)
            };

            dateTimePickerEnd = new DateTimePicker
            {
                Width = (dateFilterPanel.Width - 30) / 2,
                Height = 40,
                Font = new Font("Segoe UI", 11),
                Format = DateTimePickerFormat.Short,
                Location = new Point(dateTimePickerStart.Width + 20, 10)
            };

            // Panel pour le bouton de transfert (en bas)
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(0, 10, 0, 10)
            };

            // Bouton de transfert avec style moderne arrondi
            buttonTransfer = new RoundedButton
            {
                Text = "Démarrer le transfert",
                Width = 300,
                Height = 50,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                IsPrimary = true,
                BorderRadius = 10,
                Anchor = AnchorStyles.None,
                Location = new Point((buttonPanel.Width - 300) / 2, 15)
            };

            // Barre de progression moderne
            progressBar = new ModernProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 10,
                ProgressColor = ThemeColors.AccentColor,
                Visible = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            // Panel de statut (en bas)
            statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                BackColor = ThemeColors.SecondaryBackground,
                Visible = false,
                Margin = new Padding(0, 10, 0, 0)
            };

            // Titre du panel de statut
            var statusHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = ThemeColors.AccentColor
            };

            var statusTitle = new Label
            {
                Text = "Journal d'exécution",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0)
            };

            var clearStatusButton = new Button
            {
                Text = "Effacer",
                Dock = DockStyle.Right,
                Width = 80,
                BackColor = ThemeColors.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            clearStatusButton.Click += (s, e) => statusTextBox.Clear();

            var closeStatusButton = new Button
            {
                Text = "×",
                Dock = DockStyle.Right,
                Width = 40,
                BackColor = ThemeColors.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            closeStatusButton.Click += (s, e) => statusPanel.Visible = false;

            // Zone de texte de statut
            statusTextBox = new RichTextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.WhiteBackground,
                ForeColor = ThemeColors.PrimaryText,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // Assemblage des contrôles
            connectionTestPanel.Controls.Add(buttonTestConnection);
            connectionTestPanel.Controls.Add(connectionStatusLabel);

            dateFilterPanel.Controls.Add(dateTimePickerStart);
            dateFilterPanel.Controls.Add(dateTimePickerEnd);

            selectionPanel.Controls.Add(dateFilterPanel);
            selectionPanel.Controls.Add(checkBoxDateFilter);
            selectionPanel.Controls.Add(comboBoxTables);
            selectionPanel.Controls.Add(tableSelectLabel);

            buttonPanel.Controls.Add(buttonTransfer);

            statusHeaderPanel.Controls.Add(statusTitle);
            statusHeaderPanel.Controls.Add(clearStatusButton);
            statusHeaderPanel.Controls.Add(closeStatusButton);

            statusPanel.Controls.Add(statusTextBox);
            statusPanel.Controls.Add(statusHeaderPanel);

            // Ajouter les éléments principaux au contentPanel
            contentPanel.Controls.Add(selectionPanel);
            contentPanel.Controls.Add(connectionTestPanel);

            // Ajouter les éléments dans l'ordre correct (de bas en haut)
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(buttonPanel);
            mainPanel.Controls.Add(progressBar);
            mainPanel.Controls.Add(statusPanel);
            mainPanel.Controls.Add(dashboardPanel);
            mainPanel.Controls.Add(labelTitle);

            // Ajouter mainPanel et le menu au formulaire
            this.Controls.Add(mainPanel);
            this.Controls.Add(mainMenu);

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

            // Ajuster le bouton de transfert au centre lorsque le formulaire est redimensionné
            this.Resize += (s, e) =>
            {
                if (buttonPanel != null && buttonTransfer != null)
                    buttonTransfer.Location = new Point((buttonPanel.Width - buttonTransfer.Width) / 2, 15);
            };

            // Attacher un gestionnaire pour ajuster les DateTimePickers quand le panneau est redimensionné
            dateFilterPanel.Resize += (s, e) =>
            {
                if (dateTimePickerStart != null && dateTimePickerEnd != null)
                {
                    int width = (dateFilterPanel.ClientSize.Width - 30) / 2;
                    dateTimePickerStart.Width = width;
                    dateTimePickerEnd.Width = width;
                    dateTimePickerEnd.Location = new Point(width + 20, 10);
                }
            };

            // Créer et ajouter les widgets du tableau de bord
            CreateDashboardWidgets(dashboardPanel);
        }

        /// <summary>
        /// Crée les widgets pour le tableau de bord
        /// </summary>
        private void CreateDashboardWidgets(Panel dashboardPanel)
        {
            // Widget des transferts totaux
            var transfersWidget = new DashboardPanel
            {
                Title = "Transferts totaux",
                Value = "0",
                InfoText = "Transferts effectués",
                Size = new Size(220, 130),
                Location = new Point(0, 10)
            };

            // Widget des enregistrements
            var recordsWidget = new DashboardPanel
            {
                Title = "Enregistrements",
                Value = "0",
                InfoText = "Données transférées",
                Size = new Size(220, 130),
                Location = new Point(230, 10)
            };

            // Widget de l'état du système
            var statusWidget = new DashboardPanel
            {
                Title = "État du système",
                Value = "En ligne",
                InfoText = "Tous les services sont actifs",
                Size = new Size(220, 130),
                Location = new Point(460, 10)
            };

            // Ajouter les widgets au panneau
            dashboardPanel.Controls.Add(transfersWidget);
            dashboardPanel.Controls.Add(recordsWidget);
            dashboardPanel.Controls.Add(statusWidget);

            // Conserver les références pour mettre à jour les statistiques
            _dashboardWidgets = new List<DashboardPanel> { transfersWidget, recordsWidget, statusWidget };
        }

        #endregion

        // Déclaration des contrôles
        private ComboBox comboBoxTables;
        private ModernProgressBar progressBar;
        private CheckBox checkBoxDateFilter;
        private DateTimePicker dateTimePickerStart;
        private DateTimePicker dateTimePickerEnd;
        private Panel statusPanel;
        private RoundedButton buttonTransfer;
        private RichTextBox statusTextBox;
        private Label connectionStatusLabel;
        // private List<DashboardPanel> _dashboardWidgets;  // Supprimez ou commentez cette ligne

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