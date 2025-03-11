// Fichier: POMsag/ApiEditForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using POMsag.Models;
using POMsag.Services;
using System.Collections.Generic;
using System.ComponentModel;


namespace POMsag
{
    public partial class ApiEditForm : Form
    {
        private static class ColorPalette
        {
            public static Color PrimaryBackground = Color.FromArgb(247, 250, 252);
            public static Color SecondaryBackground = Color.FromArgb(237, 242, 247);
            public static Color PrimaryText = Color.FromArgb(45, 55, 72);
            public static Color AccentColor = Color.FromArgb(49, 130, 206);
            public static Color WhiteBackground = Color.White;
        }

        private TextBox textBoxApiId;
        private TextBox textBoxName;
        private TextBox textBoxBaseUrl;
        private ComboBox comboBoxAuthType;
        private Panel authPanel;

        // Ajouter l'attribut pour empêcher la sérialisation
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ApiConfiguration ApiConfig { get; private set; }

        public ApiEditForm(ApiConfiguration api)
        {
            InitializeComponent();

            if (api != null)
            {
                // Édition d'une API existante
                textBoxApiId.Text = api.ApiId;
                textBoxApiId.Enabled = false; // Ne pas modifier l'ID d'une API existante
                textBoxName.Text = api.Name;
                textBoxBaseUrl.Text = api.BaseUrl;
                comboBoxAuthType.SelectedIndex = (int)api.AuthType;

                // Charger les paramètres d'authentification
                UpdateAuthPanel(api.AuthType);
                LoadAuthParameters(api);

                ApiConfig = api;
            }
            else
            {
                // Nouvelle API
                ApiConfig = new ApiConfiguration("", "", "");
                comboBoxAuthType.SelectedIndex = 0;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Édition d'API";
            this.Size = new Size(700, 700);  // Encore plus grand
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorPalette.PrimaryBackground;
            this.Font = new Font("Segoe UI", 10);
            this.MinimumSize = new Size(650, 600);  // Taille minimale augmentée
            this.FormBorderStyle = FormBorderStyle.Sizable;  // Fenêtre redimensionnable
            this.Padding = new Padding(20);  // Marge autour du formulaire entier

            // Titre en haut
            var titleLabel = new Label
            {
                Text = "Édition d'API",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                ForeColor = ColorPalette.PrimaryText,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Section principale avec défilement 
            var mainScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 0)
            };

            // Conteneur principal
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                Width = 650
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Formulaire
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Boutons

            // Formulaire
            var formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = ColorPalette.WhiteBackground,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 0, 20)
            };

            // Utiliser un TableLayoutPanel pour les champs
            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 5,  // ID, Nom, URL, Auth Type, Auth Panel
                AutoSize = true,
                Width = 580
            };

            // Définir les largeurs des colonnes
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));  // Labels
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));   // Champs

            // Hauteur des rangées automatique
            for (int i = 0; i < 4; i++)
            {
                formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Panneau d'authentification

            // Labels et champs de saisie
            var labelApiId = new Label
            {
                Text = "ID de l'API :",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            textBoxApiId = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 15)
            };

            var labelName = new Label
            {
                Text = "Nom :",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            textBoxName = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 15)
            };

            var labelBaseUrl = new Label
            {
                Text = "URL de base :",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            textBoxBaseUrl = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 15)
            };

            var labelAuthType = new Label
            {
                Text = "Type d'authentification :",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            comboBoxAuthType = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 15)
            };

            // Remplir les types d'authentification
            comboBoxAuthType.Items.AddRange(new object[]
            {
        "Aucune",
        "Clé API",
        "OAuth 2.0 (Client Credentials)",
        "Basic",
        "Personnalisée"
            });

            comboBoxAuthType.SelectedIndexChanged += ComboBoxAuthType_SelectedIndexChanged;

            // Panel pour les paramètres d'authentification
            authPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10),
                MinimumSize = new Size(0, 100)
            };

            // Ajouter les contrôles au layout
            formLayout.Controls.Add(labelApiId, 0, 0);
            formLayout.Controls.Add(textBoxApiId, 1, 0);
            formLayout.Controls.Add(labelName, 0, 1);
            formLayout.Controls.Add(textBoxName, 1, 1);
            formLayout.Controls.Add(labelBaseUrl, 0, 2);
            formLayout.Controls.Add(textBoxBaseUrl, 1, 2);
            formLayout.Controls.Add(labelAuthType, 0, 3);
            formLayout.Controls.Add(comboBoxAuthType, 1, 3);

            // Ajouter le panneau d'authentification dans une cellule séparée
            var authContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            authContainer.Controls.Add(authPanel);
            formLayout.Controls.Add(authContainer, 1, 4);

            // Boutons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0),
                AutoSize = true
            };

            var buttonCancel = new Button
            {
                Text = "Annuler",
                Width = 120,
                Height = 40,
                Margin = new Padding(10, 0, 0, 0),
                DialogResult = DialogResult.Cancel
            };

            var buttonSave = new Button
            {
                Text = "Enregistrer",
                Width = 120,
                Height = 40,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(10, 0, 0, 0),
                DialogResult = DialogResult.OK
            };
            buttonSave.FlatAppearance.BorderSize = 0;

            buttonSave.Click += ButtonSave_Click;

            // Assembler les éléments
            buttonPanel.Controls.Add(buttonCancel);
            buttonPanel.Controls.Add(buttonSave);

            formPanel.Controls.Add(formLayout);
            mainPanel.Controls.Add(formPanel, 0, 0);
            mainPanel.Controls.Add(buttonPanel, 0, 1);

            mainScrollPanel.Controls.Add(mainPanel);

            this.Controls.Add(mainScrollPanel);
            this.Controls.Add(titleLabel);

            this.AcceptButton = buttonSave;
            this.CancelButton = buttonCancel;
        }

        private void ComboBoxAuthType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var authType = (AuthenticationType)comboBoxAuthType.SelectedIndex;
            UpdateAuthPanel(authType);
        }

        private void UpdateAuthPanel(AuthenticationType authType)
        {
            authPanel.Controls.Clear();

            // Utiliser un TableLayoutPanel pour une meilleure disposition
            var authLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Width = 500
            };

            // Définir les largeurs de colonnes - Augmenter l'espace pour les libellés
            authLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));  // Labels - plus large
            authLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));   // Champs

            int rowIndex = 0;

            switch (authType)
            {
                case AuthenticationType.ApiKey:
                    // Configuration pour l'API Key
                    authLayout.RowCount = 2;

                    // Nom de l'en-tête
                    var labelHeaderName = new Label
                    {
                        Text = "Nom de l'en-tête :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxHeaderName = new TextBox
                    {
                        Name = "HeaderName",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 15)
                    };

                    // Valeur
                    var labelValue = new Label
                    {
                        Text = "Valeur :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxValue = new TextBox
                    {
                        Name = "Value",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 5)
                    };

                    authLayout.Controls.Add(labelHeaderName, 0, rowIndex);
                    authLayout.Controls.Add(textBoxHeaderName, 1, rowIndex++);
                    authLayout.Controls.Add(labelValue, 0, rowIndex);
                    authLayout.Controls.Add(textBoxValue, 1, rowIndex++);
                    break;

                case AuthenticationType.OAuth2ClientCredentials:
                    // Configuration pour OAuth 2.0
                    authLayout.RowCount = 4;

                    // URL du token
                    var labelTokenUrl = new Label
                    {
                        Text = "URL du token :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxTokenUrl = new TextBox
                    {
                        Name = "TokenUrl",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 15)
                    };

                    // Client ID
                    var labelClientId = new Label
                    {
                        Text = "Client ID :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxClientId = new TextBox
                    {
                        Name = "ClientId",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 15)
                    };

                    // Client Secret
                    var labelClientSecret = new Label
                    {
                        Text = "Client Secret :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxClientSecret = new TextBox
                    {
                        Name = "ClientSecret",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 15),
                        PasswordChar = '*'
                    };

                    // Resource
                    var labelResource = new Label
                    {
                        Text = "Resource :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxResource = new TextBox
                    {
                        Name = "Resource",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 5)
                    };

                    authLayout.Controls.Add(labelTokenUrl, 0, rowIndex);
                    authLayout.Controls.Add(textBoxTokenUrl, 1, rowIndex++);
                    authLayout.Controls.Add(labelClientId, 0, rowIndex);
                    authLayout.Controls.Add(textBoxClientId, 1, rowIndex++);
                    authLayout.Controls.Add(labelClientSecret, 0, rowIndex);
                    authLayout.Controls.Add(textBoxClientSecret, 1, rowIndex++);
                    authLayout.Controls.Add(labelResource, 0, rowIndex);
                    authLayout.Controls.Add(textBoxResource, 1, rowIndex++);
                    break;

                case AuthenticationType.Basic:
                    // Configuration pour Basic Auth
                    authLayout.RowCount = 2;

                    // Nom d'utilisateur
                    var labelUsername = new Label
                    {
                        Text = "Nom d'utilisateur :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxUsername = new TextBox
                    {
                        Name = "Username",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 15)
                    };

                    // Mot de passe
                    var labelPassword = new Label
                    {
                        Text = "Mot de passe :",
                        AutoSize = true,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    var textBoxPassword = new TextBox
                    {
                        Name = "Password",
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 5, 0, 5),
                        PasswordChar = '*'
                    };

                    authLayout.Controls.Add(labelUsername, 0, rowIndex);
                    authLayout.Controls.Add(textBoxUsername, 1, rowIndex++);
                    authLayout.Controls.Add(labelPassword, 0, rowIndex);
                    authLayout.Controls.Add(textBoxPassword, 1, rowIndex++);
                    break;
            }

            authPanel.Controls.Add(authLayout);

            // S'assurer que le formulaire se redimensionne correctement
            this.PerformLayout();
        }

        private void LoadAuthParameters(ApiConfiguration api)
        {
            // Si le panneau d'authentification n'a pas de contrôles ou n'est pas correctement initialisé
            if (authPanel == null || authPanel.Controls.Count == 0)
            {
                LoggerService.Log("Le panneau d'authentification n'a pas de contrôles ou n'est pas initialisé");
                return;
            }

            try
            {
                // Récupérer le TableLayoutPanel qui contient les contrôles
                var authLayout = authPanel.Controls[0] as TableLayoutPanel;
                if (authLayout == null)
                {
                    LoggerService.Log("Impossible de trouver le TableLayoutPanel dans le panneau d'authentification");
                    return;
                }

                switch (api.AuthType)
                {
                    case AuthenticationType.ApiKey:
                        TextBox headerNameTextBox = null;
                        TextBox valueTextBox = null;

                        // Parcourir les contrôles pour trouver les TextBox par leur nom
                        foreach (Control control in authLayout.Controls)
                        {
                            if (control is TextBox textBox)
                            {
                                if (textBox.Name == "HeaderName")
                                    headerNameTextBox = textBox;
                                else if (textBox.Name == "Value")
                                    valueTextBox = textBox;
                            }
                        }

                        // Définir les valeurs si les TextBox ont été trouvés
                        if (headerNameTextBox != null && api.AuthParameters.TryGetValue("HeaderName", out string headerName))
                            headerNameTextBox.Text = headerName;

                        if (valueTextBox != null && api.AuthParameters.TryGetValue("Value", out string value))
                            valueTextBox.Text = value;
                        break;

                    case AuthenticationType.OAuth2ClientCredentials:
                        TextBox tokenUrlTextBox = null;
                        TextBox clientIdTextBox = null;
                        TextBox clientSecretTextBox = null;
                        TextBox resourceTextBox = null;

                        // Parcourir les contrôles pour trouver les TextBox par leur nom
                        foreach (Control control in authLayout.Controls)
                        {
                            if (control is TextBox textBox)
                            {
                                if (textBox.Name == "TokenUrl")
                                    tokenUrlTextBox = textBox;
                                else if (textBox.Name == "ClientId")
                                    clientIdTextBox = textBox;
                                else if (textBox.Name == "ClientSecret")
                                    clientSecretTextBox = textBox;
                                else if (textBox.Name == "Resource")
                                    resourceTextBox = textBox;
                            }
                        }

                        // Définir les valeurs si les TextBox ont été trouvés
                        if (tokenUrlTextBox != null && api.AuthParameters.TryGetValue("TokenUrl", out string tokenUrl))
                            tokenUrlTextBox.Text = tokenUrl;

                        if (clientIdTextBox != null && api.AuthParameters.TryGetValue("ClientId", out string clientId))
                            clientIdTextBox.Text = clientId;

                        if (clientSecretTextBox != null && api.AuthParameters.TryGetValue("ClientSecret", out string clientSecret))
                            clientSecretTextBox.Text = clientSecret;

                        if (resourceTextBox != null && api.AuthParameters.TryGetValue("Resource", out string resource))
                            resourceTextBox.Text = resource;
                        break;

                    case AuthenticationType.Basic:
                        TextBox usernameTextBox = null;
                        TextBox passwordTextBox = null;

                        // Parcourir les contrôles pour trouver les TextBox par leur nom
                        foreach (Control control in authLayout.Controls)
                        {
                            if (control is TextBox textBox)
                            {
                                if (textBox.Name == "Username")
                                    usernameTextBox = textBox;
                                else if (textBox.Name == "Password")
                                    passwordTextBox = textBox;
                            }
                        }

                        // Définir les valeurs si les TextBox ont été trouvés
                        if (usernameTextBox != null && api.AuthParameters.TryGetValue("Username", out string username))
                            usernameTextBox.Text = username;

                        if (passwordTextBox != null && api.AuthParameters.TryGetValue("Password", out string password))
                            passwordTextBox.Text = password;
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Erreur lors du chargement des paramètres d'authentification");
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            // Valider les champs obligatoires
            if (string.IsNullOrWhiteSpace(textBoxApiId.Text) ||
                string.IsNullOrWhiteSpace(textBoxName.Text) ||
                string.IsNullOrWhiteSpace(textBoxBaseUrl.Text))
            {
                MessageBox.Show(
                    "Les champs ID, Nom et URL de base sont obligatoires.",
                    "Erreur de validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                this.DialogResult = DialogResult.None;
                return;
            }

            // Mettre à jour la configuration de l'API
            ApiConfig.ApiId = textBoxApiId.Text;
            ApiConfig.Name = textBoxName.Text;
            ApiConfig.BaseUrl = textBoxBaseUrl.Text;
            ApiConfig.AuthType = (AuthenticationType)comboBoxAuthType.SelectedIndex;

            // Récupérer les paramètres d'authentification
            ApiConfig.AuthParameters.Clear();

            switch (ApiConfig.AuthType)
            {
                case AuthenticationType.ApiKey:
                    if (authPanel.Controls["HeaderName"] is TextBox headerNameTextBox)
                        ApiConfig.AuthParameters["HeaderName"] = headerNameTextBox.Text;

                    if (authPanel.Controls["Value"] is TextBox valueTextBox)
                        ApiConfig.AuthParameters["Value"] = valueTextBox.Text;
                    break;

                case AuthenticationType.OAuth2ClientCredentials:
                    if (authPanel.Controls["TokenUrl"] is TextBox tokenUrlTextBox)
                        ApiConfig.AuthParameters["TokenUrl"] = tokenUrlTextBox.Text;

                    if (authPanel.Controls["ClientId"] is TextBox clientIdTextBox)
                        ApiConfig.AuthParameters["ClientId"] = clientIdTextBox.Text;

                    if (authPanel.Controls["ClientSecret"] is TextBox clientSecretTextBox)
                        ApiConfig.AuthParameters["ClientSecret"] = clientSecretTextBox.Text;

                    if (authPanel.Controls["Resource"] is TextBox resourceTextBox)
                        ApiConfig.AuthParameters["Resource"] = resourceTextBox.Text;
                    break;

                case AuthenticationType.Basic:
                    if (authPanel.Controls["Username"] is TextBox usernameTextBox)
                        ApiConfig.AuthParameters["Username"] = usernameTextBox.Text;

                    if (authPanel.Controls["Password"] is TextBox passwordTextBox)
                        ApiConfig.AuthParameters["Password"] = passwordTextBox.Text;
                    break;
            }

            // Ajouter des journalisations pour le débogage
            LoggerService.Log($"API sauvegardée: ID={ApiConfig.ApiId}, Nom={ApiConfig.Name}, URL={ApiConfig.BaseUrl}");
            LoggerService.Log($"Type d'authentification: {ApiConfig.AuthType}");
            LoggerService.Log($"Nombre de paramètres d'authentification: {ApiConfig.AuthParameters.Count}");

            // Définir le DialogResult à OK pour fermer le formulaire
            this.DialogResult = DialogResult.OK;
        }
    }
}