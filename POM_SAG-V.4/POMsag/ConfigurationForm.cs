using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using POMsag.Models;
using POMsag.Services;
using System.Collections.Generic;
using System.Net.Http;

namespace POMsag
{
    public partial class ConfigurationForm : Form
    {
        private static class ColorPalette
        {
            public static Color PrimaryBackground = Color.FromArgb(247, 250, 252);
            public static Color SecondaryBackground = Color.FromArgb(237, 242, 247);
            public static Color PrimaryText = Color.FromArgb(45, 55, 72);
            public static Color AccentColor = Color.FromArgb(49, 130, 206);
            public static Color SecondaryText = Color.FromArgb(113, 128, 150);
            public static Color BorderColor = Color.FromArgb(226, 232, 240);
            public static Color WhiteBackground = Color.White;
            public static Color ErrorColor = Color.FromArgb(229, 62, 62);
            public static Color SuccessColor = Color.FromArgb(72, 187, 120);
        }

        private readonly AppConfiguration _configuration;
        private TabControl tabControl;
        private NumericUpDown numericMaxRecords;
        private TextBox textBoxServer;
        private TextBox textBoxDatabase;
        private TextBox textBoxUser;
        private TextBox textBoxPassword;

        // API Manager Controls
        private ComboBox comboApis;
        private ListView listEndpoints;
        private Button buttonAddApi;
        private Button buttonEditApi;
        private Button buttonDeleteApi;
        private Button buttonAddEndpoint;
        private Button buttonEditEndpoint;
        private Button buttonDeleteEndpoint;
        private SplitContainer splitContainer;

        // Labels pour les infos API
        private Label lblApiId;
        private Label lblApiName;
        private Label lblApiUrl;
        private Label lblApiAuth;
        private Label lblApiEndpoints;

        // Contrôles sélection des champs
        private ComboBox comboSourceType;
        private ComboBox comboEntity;

        public ConfigurationForm(AppConfiguration configuration)
        {
            _configuration = configuration;
            InitializeComponent();
            LoadApis();
        }

        private void InitializeComponent()
        {
            this.Text = "Configuration de l'Application";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);
            this.BackColor = ColorPalette.PrimaryBackground;

            // Titre principal
            var titleLabel = new Label
            {
                Text = "Configuration Centrale",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI Light", 18, FontStyle.Regular),
                ForeColor = ColorPalette.PrimaryText,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // Création du TabControl avec style moderne
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Padding = new Point(10, 3)
            };

            // Onglet Base de données
            var tabDatabase = new TabPage
            {
                Text = "Base de données",
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            // Onglet Gestionnaire d'API
            var tabApiManager = new TabPage
            {
                Text = "Gestionnaire d'API",
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            // Onglet Sélection des champs
            var tabFields = new TabPage
            {
                Text = "Sélection des champs",
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            tabControl.Controls.Add(tabDatabase);
            tabControl.Controls.Add(tabApiManager);
            tabControl.Controls.Add(tabFields);

            // Contenu de l'onglet Base de données
            var dbControls = CreateDatabaseTabControls();
            tabDatabase.Controls.AddRange(dbControls);

            // Contenu de l'onglet Gestionnaire d'API
            var apiManagerControls = CreateApiManagerTabControls();
            tabApiManager.Controls.Add(apiManagerControls);

            // Contenu de l'onglet Sélection des champs
            var fieldsControls = CreateFieldsTabControls();
            tabFields.Controls.AddRange(fieldsControls);

            // Bouton Enregistrer commun
            var buttonSave = new Button
            {
                Text = "Enregistrer et Fermer",
                Size = new Size(200, 40),
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Margin = new Padding(0, 10, 0, 0)
            };

            buttonSave.FlatAppearance.BorderSize = 0;
            buttonSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 142, 208);
            buttonSave.Click += ButtonSave_Click;

            // Layout principal
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            mainPanel.Controls.Add(tabControl);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10, 10, 10, 10)
            };

            bottomPanel.Controls.Add(buttonSave);

            // Assembler le formulaire
            this.Controls.Add(mainPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(titleLabel);

            // Initialisation des contrôles
            CreateApiManagerControls();
        }

        private Control[] CreateDatabaseTabControls()
        {
            // Section Base de données
            var labelDbSection = new Label
            {
                Text = "Configuration Base de Données",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 10)
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(20)
            };

            var labelServer = new Label
            {
                Text = "Serveur :",
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Location = new Point(0, 10)
            };

            textBoxServer = new TextBox
            {
                Location = new Point(0, 30),
                Size = new Size(520, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelDatabase = new Label
            {
                Text = "Base de données :",
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Location = new Point(0, 70)
            };

            textBoxDatabase = new TextBox
            {
                Location = new Point(0, 90),
                Size = new Size(520, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelUser = new Label
            {
                Text = "Utilisateur :",
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Location = new Point(0, 130)
            };

            textBoxUser = new TextBox
            {
                Location = new Point(0, 150),
                Size = new Size(250, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelPassword = new Label
            {
                Text = "Mot de passe :",
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Location = new Point(270, 130)
            };

            textBoxPassword = new TextBox
            {
                Location = new Point(270, 150),
                Size = new Size(250, 30),
                PasswordChar = '•',
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelMaxRecords = new Label
            {
                Text = "Nombre d'articles à récupérer par défaut (0 pour tout récupérer) :",
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Location = new Point(0, 200)
            };

            numericMaxRecords = new NumericUpDown
            {
                Location = new Point(0, 220),
                Size = new Size(520, 30),
                Minimum = 0,
                Maximum = 5000,
                Increment = 100,
                Value = _configuration.MaxRecords,
                ThousandsSeparator = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            // Info importante
            var labelRecordsWarning = new Label
            {
                Text = "Attention : Récupérer tous les articles peut prendre du temps et causer des problèmes de performance.",
                ForeColor = ColorPalette.ErrorColor,
                Location = new Point(0, 260),
                Size = new Size(520, 40),
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };

            panel.Controls.Add(labelServer);
            panel.Controls.Add(textBoxServer);
            panel.Controls.Add(labelDatabase);
            panel.Controls.Add(textBoxDatabase);
            panel.Controls.Add(labelUser);
            panel.Controls.Add(textBoxUser);
            panel.Controls.Add(labelPassword);
            panel.Controls.Add(textBoxPassword);
            panel.Controls.Add(labelMaxRecords);
            panel.Controls.Add(numericMaxRecords);
            panel.Controls.Add(labelRecordsWarning);

            // Remplir les champs avec les valeurs actuelles
            ParseAndFillConnectionString(_configuration.DatabaseConnectionString);

            return new Control[] { labelDbSection, panel };
        }

        private Control CreateApiManagerTabControls()
        {
            // Créer le SplitContainer pour diviser l'écran
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BorderStyle = BorderStyle.None,
                Panel1MinSize = 300,
                Panel2MinSize = 300,
                SplitterDistance = 450
            };

            // Panel gauche (sélection et détails API)
            var apiTableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Définir les hauteurs des lignes
            apiTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Label
            apiTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // ComboBox
            apiTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); // Info Panel
            apiTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Buttons

            var apiLabel = new Label
            {
                Text = "API configurées :",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColorPalette.PrimaryText
            };

            comboApis = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                Margin = new Padding(0, 5, 0, 10)
            };
            comboApis.SelectedIndexChanged += ComboApis_SelectedIndexChanged;

            // Panel d'informations de l'API sélectionnée
            var apiInfoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0, 5, 0, 10)
            };

            var apiInfoTitle = new Label
            {
                Text = "Détails de l'API",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ColorPalette.PrimaryText
            };

            var apiInfoContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(5),
                BackColor = ColorPalette.SecondaryBackground
            };

            apiInfoContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            apiInfoContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

            // Labels pour les infos API
            apiInfoContent.Controls.Add(new Label { Text = "ID :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 0);
            lblApiId = new Label { Anchor = AnchorStyles.Left };
            apiInfoContent.Controls.Add(lblApiId, 1, 0);

            apiInfoContent.Controls.Add(new Label { Text = "Nom :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 1);
            lblApiName = new Label { Anchor = AnchorStyles.Left };
            apiInfoContent.Controls.Add(lblApiName, 1, 1);

            apiInfoContent.Controls.Add(new Label { Text = "URL :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 2);
            lblApiUrl = new Label { Anchor = AnchorStyles.Left };
            apiInfoContent.Controls.Add(lblApiUrl, 1, 2);

            apiInfoContent.Controls.Add(new Label { Text = "Authentification :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 3);
            lblApiAuth = new Label { Anchor = AnchorStyles.Left };
            apiInfoContent.Controls.Add(lblApiAuth, 1, 3);

            apiInfoContent.Controls.Add(new Label { Text = "Endpoints :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 4);
            lblApiEndpoints = new Label { Anchor = AnchorStyles.Left };
            apiInfoContent.Controls.Add(lblApiEndpoints, 1, 4);

            apiInfoPanel.Controls.Add(apiInfoContent);
            apiInfoPanel.Controls.Add(apiInfoTitle);

            // Boutons pour les API
            var apiButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0),
                BackColor = ColorPalette.SecondaryBackground
            };

            buttonAddApi = new Button
            {
                Text = "Ajouter",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0)
            };
            buttonAddApi.FlatAppearance.BorderSize = 0;

            buttonEditApi = new Button
            {
                Text = "Modifier",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0)
            };
            buttonEditApi.FlatAppearance.BorderSize = 0;

            buttonDeleteApi = new Button
            {
                Text = "Supprimer",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.ErrorColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonDeleteApi.FlatAppearance.BorderSize = 0;

            apiButtonsPanel.Controls.Add(buttonAddApi);
            apiButtonsPanel.Controls.Add(buttonEditApi);
            apiButtonsPanel.Controls.Add(buttonDeleteApi);

            // Ajouter les éléments au TableLayout
            apiTableLayout.Controls.Add(apiLabel, 0, 0);
            apiTableLayout.Controls.Add(comboApis, 0, 1);
            apiTableLayout.Controls.Add(apiInfoPanel, 0, 2);
            apiTableLayout.Controls.Add(apiButtonsPanel, 0, 3);

            // Panel droit (endpoints)
            var endpointTableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Définir les hauteurs des lignes
            endpointTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Label
            endpointTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Buttons
            endpointTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // ListView

            var endpointLabel = new Label
            {
                Text = "Endpoints disponibles :",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColorPalette.PrimaryText
            };

            // Boutons pour les endpoints
            var endpointButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0),
                BackColor = ColorPalette.SecondaryBackground
            };

            buttonAddEndpoint = new Button
            {
                Text = "Ajouter",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0)
            };
            buttonAddEndpoint.FlatAppearance.BorderSize = 0;

            buttonEditEndpoint = new Button
            {
                Text = "Modifier",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0)
            };
            buttonEditEndpoint.FlatAppearance.BorderSize = 0;

            buttonDeleteEndpoint = new Button
            {
                Text = "Supprimer",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.ErrorColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonDeleteEndpoint.FlatAppearance.BorderSize = 0;

            endpointButtonsPanel.Controls.Add(buttonAddEndpoint);
            endpointButtonsPanel.Controls.Add(buttonEditEndpoint);
            endpointButtonsPanel.Controls.Add(buttonDeleteEndpoint);

            listEndpoints = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = ColorPalette.WhiteBackground,
                ForeColor = ColorPalette.PrimaryText,
                BorderStyle = BorderStyle.FixedSingle,
                GridLines = true
            };
            listEndpoints.Columns.Add("Nom", 150);
            listEndpoints.Columns.Add("Chemin", 220);
            listEndpoints.Columns.Add("Méthode", 70);
            listEndpoints.Columns.Add("Filtre par date", 120);

            // Ajouter les éléments au TableLayout des endpoints
            endpointTableLayout.Controls.Add(endpointLabel, 0, 0);
            endpointTableLayout.Controls.Add(endpointButtonsPanel, 0, 1);
            endpointTableLayout.Controls.Add(listEndpoints, 0, 2);

            // Assigner les panneaux aux panels du SplitContainer
            splitContainer.Panel1.Controls.Add(apiTableLayout);
            splitContainer.Panel2.Controls.Add(endpointTableLayout);

            return splitContainer;
        }

        private void CreateApiManagerControls()
        {
            // Attacher les événements aux boutons
            buttonAddApi.Click += ButtonAddApi_Click;
            buttonEditApi.Click += ButtonEditApi_Click;
            buttonDeleteApi.Click += ButtonDeleteApi_Click;
            buttonAddEndpoint.Click += ButtonAddEndpoint_Click;
            buttonEditEndpoint.Click += ButtonEditEndpoint_Click;
            buttonDeleteEndpoint.Click += ButtonDeleteEndpoint_Click;
        }

        private Control[] CreateFieldsTabControls()
        {
            // Section titre
            var labelFieldsSection = new Label
            {
                Text = "Configuration des champs à transférer",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = ColorPalette.PrimaryText,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // ComboBox pour la source de données
            var labelSourceType = new Label
            {
                Text = "Source de données :",
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Location = new Point(0, 10)
            };

            comboSourceType = new ComboBox
            {
                Location = new Point(0, 30),
                Size = new Size(250, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };
            comboSourceType.Items.AddRange(new string[] { "API POM", "Dynamics 365" });
            comboSourceType.SelectedIndex = 0;

            // ComboBox pour l'entité
            var labelEntity = new Label
            {
                Text = "Entité :",
                ForeColor = ColorPalette.PrimaryText,
                AutoSize = true,
                Location = new Point(270, 10)
            };

            comboEntity = new ComboBox
            {
                Location = new Point(270, 30),
                Size = new Size(250, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            // Bouton pour ouvrir le formulaire de sélection
            var buttonOpenFieldSelection = new Button
            {
                Text = "Configurer les champs",
                Location = new Point(0, 70),
                Size = new Size(520, 40),
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonOpenFieldSelection.FlatAppearance.BorderSize = 0;

            // Explications
            var labelExplanation = new Label
            {
                Text = "Ce module vous permet de sélectionner les champs que vous souhaitez inclure lors des transferts de données.\n\n" +
                      "1. Sélectionnez d'abord le type de source de données (API POM ou Dynamics 365)\n" +
                      "2. Choisissez l'entité (table) dont vous souhaitez configurer les champs\n" +
                      "3. Cliquez sur \"Configurer les champs\" pour ouvrir la fenêtre de sélection\n\n" +
                      "Le logiciel va alors interroger l'API pour découvrir tous les champs disponibles et vous permettre de les sélectionner.",
                Location = new Point(0, 130),
                Size = new Size(520, 180),
                ForeColor = ColorPalette.PrimaryText
            };

            // Info importante
            var labelWarning = new Label
            {
                Text = "Note : Cette analyse peut prendre un peu de temps en fonction de la taille des données et de la vitesse de réponse de l'API.",
                Location = new Point(0, 320),
                Size = new Size(520, 50),
                ForeColor = ColorPalette.SecondaryText,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };

            panel.Controls.Add(labelSourceType);
            panel.Controls.Add(comboSourceType);
            panel.Controls.Add(labelEntity);
            panel.Controls.Add(comboEntity);
            panel.Controls.Add(buttonOpenFieldSelection);
            panel.Controls.Add(labelExplanation);
            panel.Controls.Add(labelWarning);

            // Événements
            comboSourceType.SelectedIndexChanged += (s, e) =>
            {
                comboEntity.Items.Clear();
                if (comboSourceType.SelectedIndex == 0) // API POM
                {
                    var pomApi = _configuration.ConfiguredApis.FirstOrDefault(a => a.ApiId == "pom");
                    if (pomApi != null)
                    {
                        foreach (var endpoint in pomApi.Endpoints)
                        {
                            comboEntity.Items.Add(endpoint.Name);
                        }
                    }
                    else
                    {
                        comboEntity.Items.AddRange(new string[] { "clients", "commandes", "produits", "lignescommandes" });
                    }
                }
                else // Dynamics 365
                {
                    var dynamicsApi = _configuration.ConfiguredApis.FirstOrDefault(a => a.ApiId == "dynamics");
                    if (dynamicsApi != null)
                    {
                        foreach (var endpoint in dynamicsApi.Endpoints)
                        {
                            comboEntity.Items.Add(endpoint.Name);
                        }
                    }
                    else
                    {
                        comboEntity.Items.AddRange(new string[] { "ReleasedProductsV2" });
                    }
                }

                if (comboEntity.Items.Count > 0)
                    comboEntity.SelectedIndex = 0;
            };

            buttonOpenFieldSelection.Click += (s, e) =>
            {
                string sourceType = comboSourceType.SelectedIndex == 0 ? "pom" : "dynamics";
                string entity = comboEntity.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(entity))
                {
                    MessageBox.Show("Veuillez sélectionner une entité.", "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Réutiliser le service d'analyse de schéma si disponible via Form1
                var mainForm = Application.OpenForms.OfType<Form1>().FirstOrDefault();
                SchemaAnalysisService schemaService;
                DynamicsApiService dynamicsService;
                HttpClient http;

                if (mainForm != null && mainForm.SchemaAnalysisService != null)
                {
                    // Utiliser les services de Form1
                    schemaService = mainForm.SchemaAnalysisService;
                }
                else
                {
                    // Créer de nouveaux services
                    http = new HttpClient
                    {
                        BaseAddress = new Uri(_configuration.ApiUrl)
                    };

                    if (!string.IsNullOrEmpty(_configuration.ApiKey))
                    {
                        http.DefaultRequestHeaders.Add("X-Api-Key", _configuration.ApiKey);
                    }

                    dynamicsService = new DynamicsApiService(
                        _configuration.TokenUrl,
                        _configuration.ClientId,
                        _configuration.ClientSecret,
                        _configuration.Resource,
                        _configuration.DynamicsApiUrl,
                        _configuration.MaxRecords);

                    schemaService = new SchemaAnalysisService(dynamicsService, http, _configuration);
                }

                // Ouvrir le formulaire de sélection
                using (var fieldSelectionForm = new FieldSelectionForm(_configuration, schemaService, entity, sourceType))
                {
                    fieldSelectionForm.ShowDialog();
                }
            };

            // Déclencher l'événement pour remplir le combobox entité
            comboSourceType.SelectedIndex = 0;

            return new Control[] { labelFieldsSection, panel };
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Sauvegarder les configurations
                string connectionString = BuildConnectionString();
                int maxRecords = (int)numericMaxRecords.Value;

                _configuration.SaveConfiguration(
                    connectionString,
                    maxRecords,
                    ""  // SpecificItemNumber vide
                );

                MessageBox.Show(
                    "Configuration enregistrée avec succès!",
                    "Succès",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'enregistrement : {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void LoadApis()
        {
            comboApis.Items.Clear();

            foreach (var api in _configuration.ConfiguredApis)
            {
                comboApis.Items.Add(api.Name);
            }

            if (comboApis.Items.Count > 0)
                comboApis.SelectedIndex = 0;
            else
            {
                // Désactiver les boutons d'édition et de suppression si aucune API
                buttonEditApi.Enabled = false;
                buttonDeleteApi.Enabled = false;
                buttonAddEndpoint.Enabled = false;
                buttonEditEndpoint.Enabled = false;
                buttonDeleteEndpoint.Enabled = false;
            }

            UpdateEndpointsList();
            UpdateApiDetails();
        }

        private void UpdateEndpointsList()
        {
            listEndpoints.Items.Clear();

            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            foreach (var endpoint in api.Endpoints)
            {
                var item = new ListViewItem(endpoint.Name);
                item.SubItems.Add(endpoint.Path);
                item.SubItems.Add(endpoint.Method);
                item.SubItems.Add(endpoint.SupportsDateFiltering ? "Oui" : "Non");

                listEndpoints.Items.Add(item);
            }

            // Activer/désactiver les boutons d'endpoints
            buttonEditEndpoint.Enabled = buttonDeleteEndpoint.Enabled = listEndpoints.Items.Count > 0;
        }

        private void UpdateApiDetails()
        {
            if (comboApis.SelectedIndex < 0)
            {
                // Effacer les détails si aucune API sélectionnée
                lblApiId.Text = "";
                lblApiName.Text = "";
                lblApiUrl.Text = "";
                lblApiAuth.Text = "";
                lblApiEndpoints.Text = "";
                return;
            }

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            // Mettre à jour les labels de détails
            lblApiId.Text = api.ApiId;
            lblApiName.Text = api.Name;
            lblApiUrl.Text = api.BaseUrl;

            string authType = "";
            switch (api.AuthType)
            {
                case AuthenticationType.None: authType = "Aucune"; break;
                case AuthenticationType.ApiKey: authType = "Clé API"; break;
                case AuthenticationType.OAuth2ClientCredentials: authType = "OAuth 2.0"; break;
                case AuthenticationType.Basic: authType = "Basique (user/pass)"; break;
                case AuthenticationType.Custom: authType = "Personnalisée"; break;
            }

            lblApiAuth.Text = authType;
            lblApiEndpoints.Text = api.Endpoints.Count.ToString();
        }

        private void ComboApis_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEndpointsList();
            UpdateApiDetails();
        }

        private void ButtonAddApi_Click(object sender, EventArgs e)
        {
            try
            {
                // Journalisation du début de l'opération
                LoggerService.Log("Début de l'ajout d'une nouvelle API");

                // Créer une nouvelle API avec des valeurs par défaut
                var newApi = new ApiConfiguration(
                    "new_api_" + DateTime.Now.Ticks.ToString().Substring(0, 8),
                    "Nouvelle API",
                    "https://");

                // S'assurer que la liste des endpoints est bien initialisée à vide
                newApi.Endpoints = new List<ApiEndpoint>();

                // Journalisation de la création de l'objet API
                LoggerService.Log($"Nouvelle API créée temporairement: ID={newApi.ApiId}, Nom={newApi.Name}");

                // Ouvrir le formulaire d'édition pour cette nouvelle API
                using (var form = new ApiEditForm(newApi))
                {
                    LoggerService.Log("Ouverture du formulaire d'édition d'API");

                    DialogResult result = form.ShowDialog();

                    LoggerService.Log($"Résultat du dialogue: {result}");

                    if (result == DialogResult.OK)
                    {
                        // Vérifier si l'API a été correctement configurée
                        if (string.IsNullOrWhiteSpace(form.ApiConfig.ApiId) ||
                            string.IsNullOrWhiteSpace(form.ApiConfig.Name) ||
                            string.IsNullOrWhiteSpace(form.ApiConfig.BaseUrl))
                        {
                            LoggerService.Log("API mal configurée: champs obligatoires manquants");

                            MessageBox.Show(
                                "L'API n'a pas été correctement configurée. Assurez-vous de remplir tous les champs obligatoires.",
                                "Configuration incomplète",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        // Vérifier que l'API ID est unique
                        if (_configuration.ConfiguredApis.Any(a => a.ApiId == form.ApiConfig.ApiId))
                        {
                            LoggerService.Log($"ID d'API en double: {form.ApiConfig.ApiId}");

                            MessageBox.Show(
                                "Un API avec cet identifiant existe déjà. Veuillez utiliser un identifiant unique.",
                                "ID en double",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        // Ajouter l'API à la configuration
                        LoggerService.Log($"Ajout de l'API à la configuration: {form.ApiConfig.ApiId}");

                        // S'assurer à nouveau que les endpoints sont bien indépendants
                        if (form.ApiConfig.Endpoints == null)
                            form.ApiConfig.Endpoints = new List<ApiEndpoint>();

                        _configuration.AddOrUpdateApi(form.ApiConfig);

                        // Recharger la liste des API
                        LoggerService.Log("Rechargement de la liste des APIs");
                        LoadApis();

                        // Sélectionner la nouvelle API
                        int index = comboApis.Items.IndexOf(form.ApiConfig.Name);
                        LoggerService.Log($"Index de la nouvelle API dans la liste: {index}");

                        if (index >= 0)
                            comboApis.SelectedIndex = index;

                        LoggerService.Log($"API ajoutée avec succès: {form.ApiConfig.Name}");

                        MessageBox.Show(
                            $"L'API '{form.ApiConfig.Name}' a été ajoutée avec succès.",
                            "Succès",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        LoggerService.Log("Ajout d'API annulé par l'utilisateur");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Erreur lors de l'ajout d'une nouvelle API");
                MessageBox.Show(
                    $"Une erreur s'est produite lors de l'ajout de l'API: {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ButtonEditApi_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            // Ouvrir un formulaire pour modifier l'API
            using (var form = new ApiEditForm(api))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _configuration.AddOrUpdateApi(form.ApiConfig);
                    LoadApis();
                }
            }
        }

        private void ButtonDeleteApi_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            if (MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'API '{api.Name}' ?\n\nCette action supprimera également tous les endpoints associés.",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _configuration.RemoveApi(api.ApiId);
                LoadApis();
            }
        }

        private void ButtonAddEndpoint_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];

            // Ouvrir un formulaire pour ajouter un nouveau endpoint
            using (var form = new EndpointEditForm(null))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    api.Endpoints.Add(form.Endpoint);
                    _configuration.AddOrUpdateApi(api);
                    UpdateEndpointsList();
                }
            }
        }

        private void ButtonEditEndpoint_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0 || listEndpoints.SelectedItems.Count == 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];
            var endpointName = listEndpoints.SelectedItems[0].Text;
            var endpoint = api.Endpoints.FirstOrDefault(ep => ep.Name == endpointName);

            if (endpoint == null)
                return;

            // Ouvrir un formulaire pour modifier le endpoint
            using (var form = new EndpointEditForm(endpoint))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    int index = api.Endpoints.FindIndex(ep => ep.Name == endpointName);
                    if (index >= 0)
                        api.Endpoints[index] = form.Endpoint;

                    _configuration.AddOrUpdateApi(api);
                    UpdateEndpointsList();
                }
            }
        }

        private void ButtonDeleteEndpoint_Click(object sender, EventArgs e)
        {
            if (comboApis.SelectedIndex < 0 || listEndpoints.SelectedItems.Count == 0)
                return;

            var api = _configuration.ConfiguredApis[comboApis.SelectedIndex];
            var endpointName = listEndpoints.SelectedItems[0].Text;

            if (MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'endpoint '{endpointName}' ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                api.Endpoints.RemoveAll(ep => ep.Name == endpointName);
                _configuration.AddOrUpdateApi(api);
                UpdateEndpointsList();
            }
        }

        private void ParseAndFillConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                textBoxServer.Text = "";
                textBoxDatabase.Text = "";
                textBoxUser.Text = "";
                textBoxPassword.Text = "";
                return;
            }

            var parts = connectionString.Split(';')
                .Select(part => part.Split('='))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

            if (parts.TryGetValue("Server", out string server))
                textBoxServer.Text = server;

            if (parts.TryGetValue("Database", out string database))
                textBoxDatabase.Text = database;

            if (parts.TryGetValue("User Id", out string user))
                textBoxUser.Text = user;

            if (parts.TryGetValue("Password", out string password))
                textBoxPassword.Text = password;
        }

        private string BuildConnectionString()
        {
            return $"Server={textBoxServer.Text};Database={textBoxDatabase.Text};" +
                   $"User Id={textBoxUser.Text};Password={textBoxPassword.Text};" +
                   "TrustServerCertificate=True;Encrypt=False";
        }