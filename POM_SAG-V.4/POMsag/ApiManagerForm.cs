// Fichier: POMsag/ApiManagerForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using POMsag.Models;

namespace POMsag
{
    public partial class ApiManagerForm : Form
    {
        private static class ColorPalette
        {
            public static Color PrimaryBackground = Color.FromArgb(247, 250, 252);
            public static Color SecondaryBackground = Color.FromArgb(237, 242, 247);
            public static Color PrimaryText = Color.FromArgb(45, 55, 72);
            public static Color AccentColor = Color.FromArgb(49, 130, 206);
            public static Color WhiteBackground = Color.White;
            public static Color BorderColor = Color.FromArgb(226, 232, 240);
            public static Color SecondaryText = Color.FromArgb(113, 128, 150); // Ajout de cette propriété
        }

        private readonly AppConfiguration _configuration;
        private ComboBox comboApis = new ComboBox();
        private ListView listEndpoints = new ListView();
        private Button buttonAddApi = new Button();
        private Button buttonEditApi = new Button();
        private Button buttonDeleteApi = new Button();
        private Button buttonAddEndpoint = new Button();
        private Button buttonEditEndpoint = new Button();
        private Button buttonDeleteEndpoint = new Button();

        // Labels pour les infos API
        private Label lblApiId = new Label();
        private Label lblApiName = new Label();
        private Label lblApiUrl = new Label();
        private Label lblApiAuth = new Label();
        private Label lblApiEndpoints = new Label();

        public ApiManagerForm(AppConfiguration configuration)
        {
            _configuration = configuration;
            InitializeComponent();
            LoadApis();
        }

        private void InitializeComponent()
        {
            this.Text = "Gestionnaire d'API";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorPalette.PrimaryBackground;
            this.Font = new Font("Segoe UI", 10);
            this.Padding = new Padding(15);

            // Titre principal
            var titleLabel = new Label
            {
                Text = "Gestionnaire des API et des Endpoints",
                Dock = DockStyle.Top,
                Height = 50,
                Font = new Font("Segoe UI Light", 18, FontStyle.Regular),
                ForeColor = ColorPalette.PrimaryText,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Description
            var descriptionLabel = new Label
            {
                Text = "Configurez les connexions aux API et leurs points d'accès. Chaque API peut avoir plusieurs endpoints pour accéder aux différentes ressources.",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 10),
                ForeColor = ColorPalette.SecondaryText,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Composants principaux
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 400,
                BorderStyle = BorderStyle.None,
                Panel1MinSize = 350,
                Panel2MinSize = 350
            };

            // ---- SECTION DES API (Panel gauche) ---- //

            // Panel pour la liste des API
            var apiPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.FixedSingle
            };

            var apiLabel = new Label
            {
                Text = "API configurées :",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColorPalette.PrimaryText
            };

            comboApis = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                Height = 30,
                Margin = new Padding(0, 10, 0, 10)
            };
            comboApis.SelectedIndexChanged += ComboApis_SelectedIndexChanged;

            // Panel d'informations de l'API sélectionnée
            var apiInfoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0, 10, 0, 0)
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
            lblApiId.Anchor = AnchorStyles.Left;
            apiInfoContent.Controls.Add(lblApiId, 1, 0);

            apiInfoContent.Controls.Add(new Label { Text = "Nom :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 1);
            lblApiName.Anchor = AnchorStyles.Left;
            apiInfoContent.Controls.Add(lblApiName, 1, 1);

            apiInfoContent.Controls.Add(new Label { Text = "URL :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 2);
            lblApiUrl.Anchor = AnchorStyles.Left;
            apiInfoContent.Controls.Add(lblApiUrl, 1, 2);

            apiInfoContent.Controls.Add(new Label { Text = "Authentification :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 3);
            lblApiAuth.Anchor = AnchorStyles.Left;
            apiInfoContent.Controls.Add(lblApiAuth, 1, 3);

            apiInfoContent.Controls.Add(new Label { Text = "Endpoints :", Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Left }, 0, 4);
            lblApiEndpoints.Anchor = AnchorStyles.Left;
            apiInfoContent.Controls.Add(lblApiEndpoints, 1, 4);

            apiInfoPanel.Controls.Add(apiInfoContent);
            apiInfoPanel.Controls.Add(apiInfoTitle);

            // Boutons pour les API
            var apiButtonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(0, 10, 0, 0)
            };

            buttonAddApi = new Button
            {
                Text = "Ajouter",
                Width = 120,
                Height = 35,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0),
                Location = new Point(0, 5)
            };
            buttonAddApi.FlatAppearance.BorderSize = 0;

            buttonEditApi = new Button
            {
                Text = "Modifier",
                Width = 120,
                Height = 35,
                Left = 130,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 0, 10, 0),
                Location = new Point(130, 5)
            };
            buttonEditApi.FlatAppearance.BorderSize = 0;

            buttonDeleteApi = new Button
            {
                Text = "Supprimer",
                Width = 120,
                Height = 35,
                Left = 260,
                BackColor = Color.FromArgb(229, 62, 62),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(260, 5)
            };
            buttonDeleteApi.FlatAppearance.BorderSize = 0;

            buttonAddApi.Click += ButtonAddApi_Click;
            buttonEditApi.Click += ButtonEditApi_Click;
            buttonDeleteApi.Click += ButtonDeleteApi_Click;

            apiButtonsPanel.Controls.AddRange(new Control[] { buttonAddApi, buttonEditApi, buttonDeleteApi });

            // ---- SECTION DES ENDPOINTS (Panel droit) ---- //

            // Panel pour les endpoints
            var endpointPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = ColorPalette.WhiteBackground,
                BorderStyle = BorderStyle.FixedSingle
            };

            var endpointLabel = new Label
            {
                Text = "Endpoints disponibles :",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColorPalette.PrimaryText
            };

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

            // Boutons pour les endpoints
            var endpointButtonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(0, 10, 0, 0)
            };

            buttonAddEndpoint = new Button
            {
                Text = "Ajouter",
                Width = 120,
                Height = 35,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0),
                Location = new Point(0, 5)
            };
            buttonAddEndpoint.FlatAppearance.BorderSize = 0;

            buttonEditEndpoint = new Button
            {
                Text = "Modifier",
                Width = 120,
                Height = 35,
                Left = 130,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 0, 10, 0),
                Location = new Point(130, 5)
            };
            buttonEditEndpoint.FlatAppearance.BorderSize = 0;

            buttonDeleteEndpoint = new Button
            {
                Text = "Supprimer",
                Width = 120,
                Height = 35,
                Left = 260,
                BackColor = Color.FromArgb(229, 62, 62),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(260, 5)
            };
            buttonDeleteEndpoint.FlatAppearance.BorderSize = 0;

            buttonAddEndpoint.Click += ButtonAddEndpoint_Click;
            buttonEditEndpoint.Click += ButtonEditEndpoint_Click;
            buttonDeleteEndpoint.Click += ButtonDeleteEndpoint_Click;

            endpointButtonsPanel.Controls.AddRange(new Control[] { buttonAddEndpoint, buttonEditEndpoint, buttonDeleteEndpoint });

            // Bouton fermer en bas
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = ColorPalette.PrimaryBackground
            };

            var closeButton = new Button
            {
                Text = "Fermer",
                Width = 150,
                Height = 40,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Right,
                Location = new Point(bottomPanel.Width - 170, 10),
                DialogResult = DialogResult.Cancel
            };
            closeButton.FlatAppearance.BorderSize = 1;
            closeButton.FlatAppearance.BorderColor = ColorPalette.BorderColor;

            // Ajout d'un message informatif
            var infoLabel = new Label
            {
                Text = "Les modifications sont automatiquement enregistrées.",
                ForeColor = ColorPalette.SecondaryText,
                AutoSize = true,
                Location = new Point(10, 20)
            };

            bottomPanel.Controls.Add(closeButton);
            bottomPanel.Controls.Add(infoLabel);

            // Assembler les panneaux
            apiPanel.Controls.Add(apiInfoPanel);
            apiPanel.Controls.Add(apiButtonsPanel);
            apiPanel.Controls.Add(comboApis);
            apiPanel.Controls.Add(apiLabel);

            endpointPanel.Controls.Add(listEndpoints);
            endpointPanel.Controls.Add(endpointButtonsPanel);
            endpointPanel.Controls.Add(endpointLabel);

            splitContainer.Panel1.Controls.Add(apiPanel);
            splitContainer.Panel2.Controls.Add(endpointPanel);

            this.Controls.Add(bottomPanel);
            this.Controls.Add(splitContainer);
            this.Controls.Add(descriptionLabel);
            this.Controls.Add(titleLabel);

            // Gérer le redimensionnement pour les boutons
            this.Resize += (s, e) =>
            {
                closeButton.Location = new Point(bottomPanel.Width - 170, 10);
            };

            // Ajuster splitContainer après redimensionnement
            this.Resize += (s, e) =>
            {
                if (Width > 1200)
                    splitContainer.SplitterDistance = Width / 2 - 50;
                else
                    this.Load += (s, e) =>
                    {
                        // Calculer une distance de séparation sécuritaire
                        // qui respecte les contraintes Panel1MinSize et Panel2MinSize
                        int safeDistance = Math.Max(
                            splitContainer.Panel1MinSize,
                            Math.Min(
                                this.Width / 2,
                                this.Width - splitContainer.Panel2MinSize - 10
                            )
                        );
                        splitContainer.SplitterDistance = safeDistance;
                    };
            };
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
            // Ouvrir un formulaire pour ajouter une nouvelle API
            using (var form = new ApiEditForm(null))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _configuration.AddOrUpdateApi(form.ApiConfig);
                    LoadApis();
                }
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
    }
}