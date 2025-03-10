// Fichier: POMsag/ApiManagerForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using POMsag.Models;
using POMsag.Services;

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
            public static Color SecondaryText = Color.FromArgb(113, 128, 150);
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
        private SplitContainer splitContainer;
        private Button closeButton;

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

        private void ApplySafeSplitterDistance(SplitContainer container)
        {
            try
            {
                if (container == null || !container.IsHandleCreated || container.Width <= 0)
                    return;

                // S'assurer que le container a une largeur suffisante
                if (container.Width <= (container.Panel1MinSize + container.Panel2MinSize + 10))
                    return;

                // Calculer une valeur sécuritaire
                int maxPossibleDistance = container.Width - container.Panel2MinSize - 10;
                int minRequiredDistance = container.Panel1MinSize;

                // Valeur idéale : 50% de la largeur si possible
                int desiredDistance = container.Width / 2;

                // S'assurer que la valeur reste dans les limites
                int safeDistance = Math.Max(minRequiredDistance, Math.Min(desiredDistance, maxPossibleDistance));

                // Appliquer la valeur
                container.SplitterDistance = safeDistance;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur ou simplement l'ignorer
                LoggerService.LogException(ex, "Ajustement du SplitterDistance");
            }
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

            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BorderStyle = BorderStyle.None
                // Ne définissez PAS les propriétés Panel1MinSize et Panel2MinSize ici
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

            closeButton = new Button
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
            this.Resize += Form_Resize;

            // Ajouter l'événement Load
            this.Load += Form_Load;
        }

        // Dans ApiManagerForm.cs, modifiez la méthode Form_Load comme ceci:
        private void Form_Load(object sender, EventArgs e)
        {
            // Définir les tailles minimales des panneaux seulement après le chargement
            splitContainer.Panel1MinSize = 100;
            splitContainer.Panel2MinSize = 100;

            // Attendre que la manipulation des propriétés du formulaire soit sécuritaire
            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    // S'assurer que le formulaire est complètement chargé et a une taille
                    if (splitContainer.Width > 300) // Valeur de sécurité minimale
                    {
                        int safeDistance = Math.Max(
                            splitContainer.Panel1MinSize,
                            Math.Min(
                                this.Width / 2,
                                this.Width - splitContainer.Panel2MinSize - 50
                            )
                        );
                        splitContainer.SplitterDistance = safeDistance;
                    }
                }
                catch (Exception ex)
                {
                    // Journaliser l'erreur
                    LoggerService.LogException(ex, "Erreur lors de l'initialisation du SplitterDistance");
                }
            }));
        }
        private void Form_Resize(object sender, EventArgs e)
        {
            if (closeButton != null)
                closeButton.Location = new Point(closeButton.Parent.Width - 170, 10);

            ApplySafeSplitterDistance(splitContainer);
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
    }
}