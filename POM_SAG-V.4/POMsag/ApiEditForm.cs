// Fichier: POMsag/ApiEditForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using POMsag.Models;
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
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorPalette.PrimaryBackground;
            this.Font = new Font("Segoe UI", 10);

            // Section principale
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Formulaire
            var formPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 350,
                BackColor = ColorPalette.WhiteBackground,
                Padding = new Padding(20)
            };

            // Champs du formulaire
            var labelApiId = new Label { Text = "ID de l'API :", AutoSize = true };
            textBoxApiId = new TextBox { Width = 520, Margin = new Padding(0, 0, 0, 10) };

            var labelName = new Label { Text = "Nom :", AutoSize = true };
            textBoxName = new TextBox { Width = 520, Margin = new Padding(0, 0, 0, 10) };

            var labelBaseUrl = new Label { Text = "URL de base :", AutoSize = true };
            textBoxBaseUrl = new TextBox { Width = 520, Margin = new Padding(0, 0, 0, 10) };

            var labelAuthType = new Label { Text = "Type d'authentification :", AutoSize = true };
            comboBoxAuthType = new ComboBox
            {
                Width = 520,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 10)
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

            // Panel pour les paramètres d'authentification (dynamique)
            authPanel = new Panel
            {
                Width = 520,
                Height = 150,
                Margin = new Padding(0, 10, 0, 0)
            };

            // Boutons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };

            var buttonCancel = new Button
            {
                Text = "Annuler",
                Width = 100,
                Height = 35,
                Margin = new Padding(10, 0, 0, 0),
                DialogResult = DialogResult.Cancel
            };

            var buttonSave = new Button
            {
                Text = "Enregistrer",
                Width = 100,
                Height = 35,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            buttonSave.FlatAppearance.BorderSize = 0;

            buttonSave.Click += ButtonSave_Click;

            // Assembler les éléments
            buttonPanel.Controls.Add(buttonCancel);
            buttonPanel.Controls.Add(buttonSave);

            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };

            flowLayout.Controls.AddRange(new Control[]
            {
                labelApiId, textBoxApiId,
                labelName, textBoxName,
                labelBaseUrl, textBoxBaseUrl,
                labelAuthType, comboBoxAuthType,
                authPanel
            });

            formPanel.Controls.Add(flowLayout);

            mainPanel.Controls.Add(formPanel);
            mainPanel.Controls.Add(buttonPanel);

            this.Controls.Add(mainPanel);

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

            switch (authType)
            {
                case AuthenticationType.ApiKey:
                    // Champs pour authentification par clé API
                    var labelHeaderName = new Label { Text = "Nom de l'en-tête :", AutoSize = true };
                    var textBoxHeaderName = new TextBox
                    {
                        Name = "HeaderName",
                        Width = 500,
                        Margin = new Padding(0, 0, 0, 10)
                    };

                    var labelValue = new Label { Text = "Valeur :", AutoSize = true };
                    var textBoxValue = new TextBox
                    {
                        Name = "Value",
                        Width = 500
                    };

                    authPanel.Controls.AddRange(new Control[]
                    {
                        labelHeaderName, textBoxHeaderName,
                        labelValue, textBoxValue
                    });
                    break;

                case AuthenticationType.OAuth2ClientCredentials:
                    // Champs pour OAuth 2.0
                    var labelTokenUrl = new Label { Text = "URL du token :", AutoSize = true };
                    var textBoxTokenUrl = new TextBox
                    {
                        Name = "TokenUrl",
                        Width = 500,
                        Margin = new Padding(0, 0, 0, 10)
                    };

                    var labelClientId = new Label { Text = "Client ID :", AutoSize = true };
                    var textBoxClientId = new TextBox
                    {
                        Name = "ClientId",
                        Width = 500,
                        Margin = new Padding(0, 0, 0, 10)
                    };

                    var labelClientSecret = new Label { Text = "Client Secret :", AutoSize = true };
                    var textBoxClientSecret = new TextBox
                    {
                        Name = "ClientSecret",
                        Width = 500,
                        Margin = new Padding(0, 0, 0, 10),
                        PasswordChar = '*'
                    };

                    var labelResource = new Label { Text = "Resource :", AutoSize = true };
                    var textBoxResource = new TextBox
                    {
                        Name = "Resource",
                        Width = 500
                    };

                    authPanel.Controls.AddRange(new Control[]
                    {
                        labelTokenUrl, textBoxTokenUrl,
                        labelClientId, textBoxClientId,
                        labelClientSecret, textBoxClientSecret,
                        labelResource, textBoxResource
                    });
                    break;

                case AuthenticationType.Basic:
                    // Champs pour authentification Basic
                    var labelUsername = new Label { Text = "Nom d'utilisateur :", AutoSize = true };
                    var textBoxUsername = new TextBox
                    {
                        Name = "Username",
                        Width = 500,
                        Margin = new Padding(0, 0, 0, 10)
                    };

                    var labelPassword = new Label { Text = "Mot de passe :", AutoSize = true };
                    var textBoxPassword = new TextBox
                    {
                        Name = "Password",
                        Width = 500,
                        PasswordChar = '*'
                    };

                    authPanel.Controls.AddRange(new Control[]
                    {
                        labelUsername, textBoxUsername,
                        labelPassword, textBoxPassword
                    });
                    break;
            }
        }

        private void LoadAuthParameters(ApiConfiguration api)
        {
            switch (api.AuthType)
            {
                case AuthenticationType.ApiKey:
                    if (api.AuthParameters.TryGetValue("HeaderName", out string headerName))
                        ((TextBox)authPanel.Controls["HeaderName"]).Text = headerName;

                    if (api.AuthParameters.TryGetValue("Value", out string value))
                        ((TextBox)authPanel.Controls["Value"]).Text = value;
                    break;

                case AuthenticationType.OAuth2ClientCredentials:
                    if (api.AuthParameters.TryGetValue("TokenUrl", out string tokenUrl))
                        ((TextBox)authPanel.Controls["TokenUrl"]).Text = tokenUrl;

                    if (api.AuthParameters.TryGetValue("ClientId", out string clientId))
                        ((TextBox)authPanel.Controls["ClientId"]).Text = clientId;

                    if (api.AuthParameters.TryGetValue("ClientSecret", out string clientSecret))
                        ((TextBox)authPanel.Controls["ClientSecret"]).Text = clientSecret;

                    if (api.AuthParameters.TryGetValue("Resource", out string resource))
                        ((TextBox)authPanel.Controls["Resource"]).Text = resource;
                    break;

                case AuthenticationType.Basic:
                    if (api.AuthParameters.TryGetValue("Username", out string username))
                        ((TextBox)authPanel.Controls["Username"]).Text = username;

                    if (api.AuthParameters.TryGetValue("Password", out string password))
                        ((TextBox)authPanel.Controls["Password"]).Text = password;
                    break;
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
        }
    }
}