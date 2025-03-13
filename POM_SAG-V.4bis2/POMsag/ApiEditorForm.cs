using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using POMsag.Models;
using POMsag.Services;

namespace POMsag
{
    public partial class ApiEditorForm : Form
    {
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

        private readonly ApiManager _apiManager;
        private ApiDefinition _api;
        private bool _isNewApi;

        // Contrôles pour les propriétés générales
        private TextBox textBoxName;
        private TextBox textBoxDescription;
        private TextBox textBoxBaseUrl;
        private ComboBox comboBoxAuthType;

        // Contrôles pour les paramètres d'authentification
        private Panel authPanel;
        private Dictionary<ApiAuthType, Panel> authPanels = new Dictionary<ApiAuthType, Panel>();

        // Contrôles pour les endpoints
        private ListView listViewEndpoints;
        private Button buttonAddEndpoint;
        private Button buttonEditEndpoint;
        private Button buttonDeleteEndpoint;

        // Contrôles pour les paramètres globaux
        private DataGridView gridGlobalParams;

        // Boutons de validation
        private Button buttonSave;
        private Button buttonCancel;

        public ApiEditorForm(ApiDefinition api, ApiManager apiManager)
        {
            _apiManager = apiManager;
            _isNewApi = api == null;
            _api = api ?? new ApiDefinition();

            InitializeComponent();
            InitializeAuthPanels();
            LoadApiData();
        }

        private void InitializeComponent()
        {
            this.Text = _isNewApi ? "Ajouter une API" : "Modifier une API";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);
            this.BackColor = ColorPalette.PrimaryBackground;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // TabControl pour organiser les sections
            var tabControl = new TabControl
            {
                Dock = DockStyle.Top,
                Height = 580,
                Padding = new Point(10, 3)
            };

            // Onglet Général
            var tabGeneral = new TabPage
            {
                Text = "Général",
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            // Onglet Endpoints
            var tabEndpoints = new TabPage
            {
                Text = "Endpoints",
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            // Onglet Paramètres globaux
            var tabParams = new TabPage
            {
                Text = "Paramètres globaux",
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground
            };

            tabControl.Controls.Add(tabGeneral);
            tabControl.Controls.Add(tabEndpoints);
            tabControl.Controls.Add(tabParams);

            // Contenu de l'onglet Général
            var generalControls = CreateGeneralTabControls();
            tabGeneral.Controls.AddRange(generalControls);

            // Contenu de l'onglet Endpoints
            var endpointsControls = CreateEndpointsTabControls();
            tabEndpoints.Controls.AddRange(endpointsControls);

            // Contenu de l'onglet Paramètres globaux
            var paramsControls = CreateParamsTabControls();
            tabParams.Controls.AddRange(paramsControls);

            // Boutons de validation
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = ColorPalette.SecondaryBackground,
                Padding = new Padding(10)
            };

            buttonSave = new Button
            {
                Text = "Enregistrer",
                Width = 150,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(580, 10),
                DialogResult = DialogResult.OK
            };
            buttonSave.FlatAppearance.BorderSize = 0;
            buttonSave.Click += ButtonSave_Click;

            buttonCancel = new Button
            {
                Text = "Annuler",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.SecondaryText,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(740, 10),
                DialogResult = DialogResult.Cancel
            };
            buttonCancel.FlatAppearance.BorderSize = 0;

            buttonPanel.Controls.Add(buttonSave);
            buttonPanel.Controls.Add(buttonCancel);

            // Ajouter les contrôles au formulaire
            this.Controls.Add(tabControl);
            this.Controls.Add(buttonPanel);
            this.AcceptButton = buttonSave;
            this.CancelButton = buttonCancel;
        }

        private Control[] CreateGeneralTabControls()
        {
            var labelName = new Label
            {
                Text = "Nom de l'API :",
                Location = new Point(10, 20),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxName = new TextBox
            {
                Location = new Point(10, 45),
                Size = new Size(300, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelDescription = new Label
            {
                Text = "Description :",
                Location = new Point(10, 85),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxDescription = new TextBox
            {
                Location = new Point(10, 110),
                Size = new Size(500, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelBaseUrl = new Label
            {
                Text = "URL de base :",
                Location = new Point(10, 150),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            textBoxBaseUrl = new TextBox
            {
                Location = new Point(10, 175),
                Size = new Size(500, 30),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelAuthType = new Label
            {
                Text = "Type d'authentification :",
                Location = new Point(10, 215),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            comboBoxAuthType = new ComboBox
            {
                Location = new Point(10, 240),
                Size = new Size(300, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            comboBoxAuthType.Items.AddRange(Enum.GetNames(typeof(ApiAuthType)));
            comboBoxAuthType.SelectedIndexChanged += ComboBoxAuthType_SelectedIndexChanged;

            // Panneau pour les paramètres d'authentification
            authPanel = new Panel
            {
                Location = new Point(10, 280),
                Size = new Size(780, 200),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.WhiteBackground
            };

            return new Control[]
            {
                labelName, textBoxName,
                labelDescription, textBoxDescription,
                labelBaseUrl, textBoxBaseUrl,
                labelAuthType, comboBoxAuthType,
                authPanel
            };
        }

        private void InitializeAuthPanels()
        {
            // Panneau pour authentification par clé API
            var apiKeyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var labelApiKeyHeader = new Label
            {
                Text = "Clé API",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ColorPalette.PrimaryText
            };

            var labelHeaderName = new Label
            {
                Text = "Nom de l'en-tête :",
                Location = new Point(10, 30),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxHeaderName = new TextBox
            {
                Name = "HeaderName",
                Location = new Point(150, 30),
                Size = new Size(200, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelApiKey = new Label
            {
                Text = "Clé API :",
                Location = new Point(10, 65),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxApiKey = new TextBox
            {
                Name = "ApiKey",
                Location = new Point(150, 65),
                Size = new Size(350, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            apiKeyPanel.Controls.Add(textBoxApiKey);
            apiKeyPanel.Controls.Add(labelApiKey);
            apiKeyPanel.Controls.Add(textBoxHeaderName);
            apiKeyPanel.Controls.Add(labelHeaderName);
            apiKeyPanel.Controls.Add(labelApiKeyHeader);

            // Panneau pour authentification OAuth2
            var oauth2Panel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var labelOAuth2Header = new Label
            {
                Text = "OAuth 2.0",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ColorPalette.PrimaryText
            };

            var labelTokenUrl = new Label
            {
                Text = "URL du token :",
                Location = new Point(10, 30),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxTokenUrl = new TextBox
            {
                Name = "TokenUrl",
                Location = new Point(150, 30),
                Size = new Size(350, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelClientId = new Label
            {
                Text = "Client ID :",
                Location = new Point(10, 65),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxClientId = new TextBox
            {
                Name = "ClientId",
                Location = new Point(150, 65),
                Size = new Size(350, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelClientSecret = new Label
            {
                Text = "Client Secret :",
                Location = new Point(10, 100),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxClientSecret = new TextBox
            {
                Name = "ClientSecret",
                Location = new Point(150, 100),
                Size = new Size(350, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                PasswordChar = '•'
            };

            var labelResource = new Label
            {
                Text = "Resource :",
                Location = new Point(10, 135),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxResource = new TextBox
            {
                Name = "Resource",
                Location = new Point(150, 135),
                Size = new Size(350, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            oauth2Panel.Controls.Add(textBoxResource);
            oauth2Panel.Controls.Add(labelResource);
            oauth2Panel.Controls.Add(textBoxClientSecret);
            oauth2Panel.Controls.Add(labelClientSecret);
            oauth2Panel.Controls.Add(textBoxClientId);
            oauth2Panel.Controls.Add(labelClientId);
            oauth2Panel.Controls.Add(textBoxTokenUrl);
            oauth2Panel.Controls.Add(labelTokenUrl);
            oauth2Panel.Controls.Add(labelOAuth2Header);

            // Panneau pour authentification Basic
            var basicPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var labelBasicHeader = new Label
            {
                Text = "Authentification HTTP Basic",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ColorPalette.PrimaryText
            };

            var labelUsername = new Label
            {
                Text = "Nom d'utilisateur :",
                Location = new Point(10, 30),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxUsername = new TextBox
            {
                Name = "Username",
                Location = new Point(150, 30),
                Size = new Size(250, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            var labelPassword = new Label
            {
                Text = "Mot de passe :",
                Location = new Point(10, 65),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxPassword = new TextBox
            {
                Name = "Password",
                Location = new Point(150, 65),
                Size = new Size(250, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                PasswordChar = '•'
            };

            basicPanel.Controls.Add(textBoxPassword);
            basicPanel.Controls.Add(labelPassword);
            basicPanel.Controls.Add(textBoxUsername);
            basicPanel.Controls.Add(labelUsername);
            basicPanel.Controls.Add(labelBasicHeader);

            // Panneau pour authentification Bearer
            var bearerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var labelBearerHeader = new Label
            {
                Text = "Authentification Bearer Token",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ColorPalette.PrimaryText
            };

            var labelToken = new Label
            {
                Text = "Token :",
                Location = new Point(10, 30),
                AutoSize = true,
                ForeColor = ColorPalette.PrimaryText
            };

            var textBoxToken = new TextBox
            {
                Name = "Token",
                Location = new Point(150, 30),
                Size = new Size(400, 25),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText
            };

            bearerPanel.Controls.Add(textBoxToken);
            bearerPanel.Controls.Add(labelToken);
            bearerPanel.Controls.Add(labelBearerHeader);

            // Panneau pour l'absence d'authentification
            var nonePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = true
            };

            var labelNone = new Label
            {
                Text = "Aucune authentification requise",
                AutoSize = true,
                Location = new Point(20, 20),
                ForeColor = ColorPalette.SecondaryText,
                Font = new Font("Segoe UI", 10, FontStyle.Italic)
            };

            nonePanel.Controls.Add(labelNone);

            // Panneau pour authentification personnalisée
            var customPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            var labelCustomHeader = new Label
            {
                Text = "Authentification personnalisée",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = ColorPalette.PrimaryText
            };

            var dataGridViewCustomAuth = new DataGridView
            {
                Name = "CustomAuthGrid",
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                GridColor = ColorPalette.BorderColor,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            dataGridViewCustomAuth.Columns.Add("Key", "Clé");
            dataGridViewCustomAuth.Columns.Add("Value", "Valeur");

            customPanel.Controls.Add(dataGridViewCustomAuth);
            customPanel.Controls.Add(labelCustomHeader);

            // Ajouter les panneaux à la collection
            authPanels[ApiAuthType.None] = nonePanel;
            authPanels[ApiAuthType.ApiKey] = apiKeyPanel;
            authPanels[ApiAuthType.OAuth2] = oauth2Panel;
            authPanels[ApiAuthType.Basic] = basicPanel;
            authPanels[ApiAuthType.Bearer] = bearerPanel;
            authPanels[ApiAuthType.Custom] = customPanel;

            // Ajouter tous les panneaux au panneau principal
            foreach (var panel in authPanels.Values)
            {
                authPanel.Controls.Add(panel);
            }
        }

        private Control[] CreateEndpointsTabControls()
        {
            // Liste des endpoints
            listViewEndpoints = new ListView
            {
                Dock = DockStyle.Top,
                Height = 400,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BackColor = ColorPalette.WhiteBackground,
                ForeColor = ColorPalette.PrimaryText,
                BorderStyle = BorderStyle.FixedSingle
            };

            listViewEndpoints.Columns.Add("Nom", 150);
            listViewEndpoints.Columns.Add("Description", 200);
            listViewEndpoints.Columns.Add("Chemin", 200);
            listViewEndpoints.Columns.Add("Méthode", 80);
            listViewEndpoints.Columns.Add("Filtrage par date", 120);

            listViewEndpoints.SelectedIndexChanged += (s, e) =>
            {
                bool hasSelection = listViewEndpoints.SelectedItems.Count > 0;
                buttonEditEndpoint.Enabled = hasSelection;
                buttonDeleteEndpoint.Enabled = hasSelection;
            };

            listViewEndpoints.DoubleClick += (s, e) =>
            {
                if (listViewEndpoints.SelectedItems.Count > 0)
                {
                    EditEndpoint();
                }
            };

            // Panneau pour les boutons
            var endpointButtonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = ColorPalette.SecondaryBackground,
                Padding = new Padding(10),
                Location = new Point(0, 410)
            };

            buttonAddEndpoint = new Button
            {
                Text = "Ajouter un endpoint",
                Width = 180,
                Height = 35,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 8)
            };
            buttonAddEndpoint.FlatAppearance.BorderSize = 0;
            buttonAddEndpoint.Click += (s, e) => AddEndpoint();

            buttonEditEndpoint = new Button
            {
                Text = "Modifier",
                Width = 120,
                Height = 35,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(200, 8),
                Enabled = false
            };
            buttonEditEndpoint.FlatAppearance.BorderSize = 0;
            buttonEditEndpoint.Click += (s, e) => EditEndpoint();

            buttonDeleteEndpoint = new Button
            {
                Text = "Supprimer",
                Width = 120,
                Height = 35,
                BackColor = ColorPalette.ErrorColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(330, 8),
                Enabled = false
            };
            buttonDeleteEndpoint.FlatAppearance.BorderSize = 0;
            buttonDeleteEndpoint.Click += (s, e) => DeleteEndpoint();

            endpointButtonPanel.Controls.Add(buttonAddEndpoint);
            endpointButtonPanel.Controls.Add(buttonEditEndpoint);
            endpointButtonPanel.Controls.Add(buttonDeleteEndpoint);

            return new Control[] { listViewEndpoints, endpointButtonPanel };
        }

        private Control[] CreateParamsTabControls()
        {
            // Grille pour les paramètres globaux
            gridGlobalParams = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                GridColor = ColorPalette.BorderColor,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            gridGlobalParams.Columns.Add("Key", "Nom du paramètre");
            gridGlobalParams.Columns.Add("Value", "Valeur");

            var labelParams = new Label
            {
                Text = "Paramètres globaux (ajoutés à toutes les requêtes de cette API)",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = ColorPalette.PrimaryText,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            var buttonAddParam = new Button
            {
                Text = "Ajouter un paramètre",
                Width = 180,
                Height = 35,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 460)
            };
            buttonAddParam.FlatAppearance.BorderSize = 0;
            buttonAddParam.Click += (s, e) =>
            {
                gridGlobalParams.Rows.Add("", "");
            };

            var buttonRemoveParam = new Button
            {
                Text = "Supprimer le paramètre",
                Width = 180,
                Height = 35,
                BackColor = ColorPalette.ErrorColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(200, 460)
            };
            buttonRemoveParam.FlatAppearance.BorderSize = 0;
            buttonRemoveParam.Click += (s, e) =>
            {
                if (gridGlobalParams.SelectedRows.Count > 0)
                {
                    foreach (DataGridViewRow row in gridGlobalParams.SelectedRows)
                    {
                        gridGlobalParams.Rows.Remove(row);
                    }
                }
            };

            return new Control[] { labelParams, gridGlobalParams, buttonAddParam, buttonRemoveParam };
        }

        private void LoadApiData()
        {
            if (_api != null)
            {
                textBoxName.Text = _api.Name;
                textBoxDescription.Text = _api.Description;
                textBoxBaseUrl.Text = _api.BaseUrl;

                if (_isNewApi)
                {
                    comboBoxAuthType.SelectedIndex = 0; // None par défaut
                }
                else
                {
                    comboBoxAuthType.SelectedItem = _api.AuthType.ToString();
                    LoadAuthProperties();
                    LoadEndpoints();
                    LoadGlobalParams();
                }
            }
        }

        private void LoadAuthProperties()
        {
            var panel = authPanels[_api.AuthType];

            switch (_api.AuthType)
            {
                case ApiAuthType.ApiKey:
                    SetTextBoxValue(panel, "HeaderName", _api.AuthProperties.GetValueOrDefault("HeaderName", ""));
                    SetTextBoxValue(panel, "ApiKey", _api.AuthProperties.GetValueOrDefault("ApiKey", ""));
                    break;
                case ApiAuthType.OAuth2:
                    SetTextBoxValue(panel, "TokenUrl", _api.AuthProperties.GetValueOrDefault("TokenUrl", ""));
                    SetTextBoxValue(panel, "ClientId", _api.AuthProperties.GetValueOrDefault("ClientId", ""));
                    SetTextBoxValue(panel, "ClientSecret", _api.AuthProperties.GetValueOrDefault("ClientSecret", ""));
                    SetTextBoxValue(panel, "Resource", _api.AuthProperties.GetValueOrDefault("Resource", ""));
                    break;
                case ApiAuthType.Basic:
                    SetTextBoxValue(panel, "Username", _api.AuthProperties.GetValueOrDefault("Username", ""));
                    SetTextBoxValue(panel, "Password", _api.AuthProperties.GetValueOrDefault("Password", ""));
                    break;
                case ApiAuthType.Bearer:
                    SetTextBoxValue(panel, "Token", _api.AuthProperties.GetValueOrDefault("Token", ""));
                    break;
                case ApiAuthType.Custom:
                    var grid = panel.Controls.OfType<DataGridView>().FirstOrDefault(c => c.Name == "CustomAuthGrid");
                    if (grid != null)
                    {
                        grid.Rows.Clear();
                        foreach (var prop in _api.AuthProperties)
                        {
                            grid.Rows.Add(prop.Key, prop.Value);
                        }
                    }
                    break;
            }
        }

        private void SetTextBoxValue(Panel panel, string textBoxName, string value)
        {
            var textBox = panel.Controls.OfType<TextBox>().FirstOrDefault(c => c.Name == textBoxName);
            if (textBox != null)
            {
                textBox.Text = value;
            }
        }

        private void LoadEndpoints()
        {
            listViewEndpoints.Items.Clear();

            foreach (var endpoint in _api.Endpoints)
            {
                var item = new ListViewItem(endpoint.Name);
                item.SubItems.Add(endpoint.Description ?? "");
                item.SubItems.Add(endpoint.Path);
                item.SubItems.Add(endpoint.Method.ToString());
                item.SubItems.Add(endpoint.SupportsDateFiltering ? "Oui" : "Non");
                item.Tag = endpoint;

                listViewEndpoints.Items.Add(item);
            }
        }

        private void LoadGlobalParams()
        {
            gridGlobalParams.Rows.Clear();

            foreach (var param in _api.GlobalParameters)
            {
                gridGlobalParams.Rows.Add(param.Key, param.Value);
            }
        }

        private void ComboBoxAuthType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxAuthType.SelectedItem == null)
                return;

            // Masquer tous les panneaux d'authentification
            foreach (var panel in authPanels.Values)
            {
                panel.Visible = false;
            }

            // Afficher le panneau correspondant au type sélectionné
            ApiAuthType authType = (ApiAuthType)Enum.Parse(typeof(ApiAuthType), comboBoxAuthType.SelectedItem.ToString());
            if (authPanels.TryGetValue(authType, out Panel selectedPanel))
            {
                selectedPanel.Visible = true;
            }
        }

        private void AddEndpoint()
        {
            using (var form = new EndpointEditorForm(null))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var endpoint = form.Endpoint;

                    var item = new ListViewItem(endpoint.Name);
                    item.SubItems.Add(endpoint.Description ?? "");
                    item.SubItems.Add(endpoint.Path);
                    item.SubItems.Add(endpoint.Method.ToString());
                    item.SubItems.Add(endpoint.SupportsDateFiltering ? "Oui" : "Non");
                    item.Tag = endpoint;

                    listViewEndpoints.Items.Add(item);
                }
            }
        }

        private void EditEndpoint()
        {
            if (listViewEndpoints.SelectedItems.Count == 0)
                return;

            var selectedItem = listViewEndpoints.SelectedItems[0];
            var endpoint = (ApiEndpoint)selectedItem.Tag;

            using (var form = new EndpointEditorForm(endpoint))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    selectedItem.Text = endpoint.Name;
                    selectedItem.SubItems[1].Text = endpoint.Description ?? "";
                    selectedItem.SubItems[2].Text = endpoint.Path;
                    selectedItem.SubItems[3].Text = endpoint.Method.ToString();
                    selectedItem.SubItems[4].Text = endpoint.SupportsDateFiltering ? "Oui" : "Non";
                }
            }
        }

        private void DeleteEndpoint()
        {
            if (listViewEndpoints.SelectedItems.Count == 0)
                return;

            var selectedItem = listViewEndpoints.SelectedItems[0];
            var endpoint = (ApiEndpoint)selectedItem.Tag;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer l'endpoint '{endpoint.Name}' ?",
                "Confirmation de suppression",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                listViewEndpoints.Items.Remove(selectedItem);
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Valider les champs obligatoires
                if (string.IsNullOrWhiteSpace(textBoxName.Text))
                {
                    MessageBox.Show("Le nom de l'API est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBoxName.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (string.IsNullOrWhiteSpace(textBoxBaseUrl.Text))
                {
                    MessageBox.Show("L'URL de base est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBoxBaseUrl.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }

                // Mettre à jour les propriétés de l'API
                _api.Name = textBoxName.Text;
                _api.Description = textBoxDescription.Text;
                _api.BaseUrl = textBoxBaseUrl.Text;
                _api.AuthType = (ApiAuthType)Enum.Parse(typeof(ApiAuthType), comboBoxAuthType.SelectedItem.ToString());

                // Récupérer les propriétés d'authentification
                _api.AuthProperties = GetAuthProperties();

                // Récupérer les endpoints
                _api.Endpoints = GetEndpoints();

                // Récupérer les paramètres globaux
                _api.GlobalParameters = GetGlobalParameters();

                // Mettre à jour la date de modification
                _api.LastModifiedDate = DateTime.Now;

                // Sauvegarder l'API
                if (_apiManager.AddOrUpdateApi(_api))
                {
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show(
                        "Erreur lors de l'enregistrement de l'API. Vérifiez les informations saisies.",
                        "Erreur",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    this.DialogResult = DialogResult.None;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'enregistrement : {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                this.DialogResult = DialogResult.None;
            }
        }

        private Dictionary<string, string> GetAuthProperties()
        {
            var properties = new Dictionary<string, string>();
            var panel = authPanels[_api.AuthType];

            switch (_api.AuthType)
            {
                case ApiAuthType.ApiKey:
                    AddTextBoxValue(panel, "HeaderName", properties);
                    AddTextBoxValue(panel, "ApiKey", properties);
                    break;
                case ApiAuthType.OAuth2:
                    AddTextBoxValue(panel, "TokenUrl", properties);
                    AddTextBoxValue(panel, "ClientId", properties);
                    AddTextBoxValue(panel, "ClientSecret", properties);
                    AddTextBoxValue(panel, "Resource", properties);
                    break;
                case ApiAuthType.Basic:
                    AddTextBoxValue(panel, "Username", properties);
                    AddTextBoxValue(panel, "Password", properties);
                    break;
                case ApiAuthType.Bearer:
                    AddTextBoxValue(panel, "Token", properties);
                    break;
                case ApiAuthType.Custom:
                    var grid = panel.Controls.OfType<DataGridView>().FirstOrDefault(c => c.Name == "CustomAuthGrid");
                    if (grid != null)
                    {
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            var key = row.Cells["Key"].Value?.ToString();
                            var value = row.Cells["Value"].Value?.ToString();

                            if (!string.IsNullOrWhiteSpace(key))
                            {
                                properties[key] = value ?? "";
                            }
                        }
                    }
                    break;
            }

            return properties;
        }

        private void AddTextBoxValue(Panel panel, string textBoxName, Dictionary<string, string> properties)
        {
            var textBox = panel.Controls.OfType<TextBox>().FirstOrDefault(c => c.Name == textBoxName);
            if (textBox != null)
            {
                properties[textBoxName] = textBox.Text;
            }
        }

        private List<ApiEndpoint> GetEndpoints()
        {
            var endpoints = new List<ApiEndpoint>();

            foreach (ListViewItem item in listViewEndpoints.Items)
            {
                endpoints.Add((ApiEndpoint)item.Tag);
            }

            return endpoints;
        }

        private Dictionary<string, string> GetGlobalParameters()
        {
            var parameters = new Dictionary<string, string>();

            foreach (DataGridViewRow row in gridGlobalParams.Rows)
            {
                if (row.IsNewRow) continue;

                var key = row.Cells["Key"].Value?.ToString();
                var value = row.Cells["Value"].Value?.ToString();

                if (!string.IsNullOrWhiteSpace(key))
                {
                    parameters[key] = value ?? "";
                }
            }

            return parameters;
        }
    }
}