using POMsag;
using POMsag.Services;
using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;

public partial class ConfigurationForm : Form
{
    // Ajout de la même palette de couleurs que dans Form1
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

    private AppConfiguration _configuration;
    private TabControl tabControl;
    private TextBox textBoxTokenUrl;
    private TextBox textBoxClientId;
    private TextBox textBoxClientSecret;
    private TextBox textBoxResource;
    private TextBox textBoxDynamicsApiUrl;
    private NumericUpDown numericMaxRecords;

    public ConfigurationForm(AppConfiguration configuration)
    {
        InitializeComponent();
        _configuration = configuration;

        // Décomposer et remplir les champs de connexion
        ParseAndFillConnectionString(_configuration.DatabaseConnectionString);
        textBoxApiUrl.Text = _configuration.ApiUrl;
        textBoxApiKey.Text = _configuration.ApiKey;

        // Remplir les champs D365
        textBoxTokenUrl.Text = _configuration.TokenUrl;
        textBoxClientId.Text = _configuration.ClientId;
        textBoxClientSecret.Text = _configuration.ClientSecret;
        textBoxResource.Text = _configuration.Resource;
        textBoxDynamicsApiUrl.Text = _configuration.DynamicsApiUrl;

        // Initialiser la valeur du NumericUpDown avec la configuration
        if (numericMaxRecords != null && _configuration != null)
            numericMaxRecords.Value = Math.Max(numericMaxRecords.Minimum, Math.Min(numericMaxRecords.Maximum, _configuration.MaxRecords));
    }

    private void InitializeComponent()
    {
        this.Text = "Configuration de l'Application";
        this.Size = new Size(600, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 10);
        this.BackColor = ColorPalette.PrimaryBackground;

        // Création du TabControl avec style moderne
        tabControl = new TabControl
        {
            Location = new Point(10, 10),
            Size = new Size(575, 600),
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 10),
            Padding = new Point(10, 3)
        };

        // Onglet Général
        var tabGeneral = new TabPage
        {
            Text = "Général",
            Padding = new Padding(10),
            BackColor = ColorPalette.WhiteBackground
        };

        // Onglet D365
        var tabD365 = new TabPage
        {
            Text = "Dynamics 365",
            Padding = new Padding(10),
            BackColor = ColorPalette.WhiteBackground
        };

        // Nouvel onglet Champs
        var tabFields = new TabPage
        {
            Text = "Sélection des champs",
            Padding = new Padding(10),
            BackColor = ColorPalette.WhiteBackground
        };

        tabControl.Controls.Add(tabGeneral);
        tabControl.Controls.Add(tabD365);
        tabControl.Controls.Add(tabFields);

        // Contenu de l'onglet Général (ancien contenu)
        var generalControls = CreateGeneralTabControls();
        tabGeneral.Controls.AddRange(generalControls);

        // Contenu de l'onglet D365
        var d365Controls = CreateD365TabControls();
        tabD365.Controls.AddRange(d365Controls);

        // Contenu de l'onglet Champs
        var fieldsControls = CreateFieldsTabControls();
        tabFields.Controls.AddRange(fieldsControls);

        // Bouton Enregistrer commun
        var buttonSave = new Button
        {
            Text = "Enregistrer",
            Location = new Point(20, 620),
            Size = new Size(540, 40),
            BackColor = ColorPalette.AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Bottom,
            Font = new Font("Segoe UI", 12, FontStyle.Regular)
        };

        buttonSave.FlatAppearance.BorderSize = 0;
        buttonSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 142, 208);
        buttonSave.Click += ButtonSave_Click;

        this.Controls.AddRange(new Control[] { tabControl, buttonSave });
    }

    private Control[] CreateGeneralTabControls()
    {
        // Section API
        var labelApiSection = new Label
        {
            Text = "Configuration API",
            Location = new Point(10, 10),
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        var labelApiUrl = new Label
        {
            Text = "URL de l'API :",
            Location = new Point(10, 40),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        var labelApiKey = new Label
        {
            Text = "Clé API :",
            Location = new Point(10, 90),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxApiUrl = new TextBox
        {
            Location = new Point(10, 60),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        textBoxApiKey = new TextBox
        {
            Location = new Point(10, 110),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        // Section Base de données
        var labelDbSection = new Label
        {
            Text = "Configuration Base de Données",
            Location = new Point(10, 160),
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        var labelServer = new Label
        {
            Text = "Serveur :",
            Location = new Point(10, 190),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxServer = new TextBox
        {
            Location = new Point(10, 210),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelDatabase = new Label
        {
            Text = "Base de données :",
            Location = new Point(10, 240),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxDatabase = new TextBox
        {
            Location = new Point(10, 260),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelUser = new Label
        {
            Text = "Utilisateur :",
            Location = new Point(10, 290),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxUser = new TextBox
        {
            Location = new Point(10, 310),
            Size = new Size(250, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelPassword = new Label
        {
            Text = "Mot de passe :",
            Location = new Point(280, 290),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxPassword = new TextBox
        {
            Location = new Point(280, 310),
            Size = new Size(250, 30),
            PasswordChar = '•',
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        return new Control[]
        {
            labelApiSection, labelApiUrl, labelApiKey, textBoxApiUrl, textBoxApiKey,
            labelDbSection, labelServer, textBoxServer, labelDatabase, textBoxDatabase,
            labelUser, textBoxUser, labelPassword, textBoxPassword
        };
    }

    private Control[] CreateD365TabControls()
    {
        // Section D365
        var labelD365Section = new Label
        {
            Text = "Configuration Dynamics 365",
            Location = new Point(10, 10),
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        var labelTokenUrl = new Label
        {
            Text = "URL du Token :",
            Location = new Point(10, 40),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxTokenUrl = new TextBox
        {
            Location = new Point(10, 60),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelClientId = new Label
        {
            Text = "Client ID :",
            Location = new Point(10, 90),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxClientId = new TextBox
        {
            Location = new Point(10, 110),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelClientSecret = new Label
        {
            Text = "Client Secret :",
            Location = new Point(10, 140),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxClientSecret = new TextBox
        {
            Location = new Point(10, 160),
            Size = new Size(520, 30),
            PasswordChar = '•',
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelResource = new Label
        {
            Text = "Resource :",
            Location = new Point(10, 190),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxResource = new TextBox
        {
            Location = new Point(10, 210),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelDynamicsApiUrl = new Label
        {
            Text = "URL de l'API Dynamics :",
            Location = new Point(10, 240),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        textBoxDynamicsApiUrl = new TextBox
        {
            Location = new Point(10, 260),
            Size = new Size(520, 30),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        var labelMaxRecords = new Label
        {
            Text = "Nombre d'articles à récupérer (0 pour tout récupérer) :",
            Location = new Point(10, 290),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        numericMaxRecords = new NumericUpDown
        {
            Location = new Point(10, 310),
            Size = new Size(520, 30),
            Minimum = 0,        // Minimum à 0 
            Maximum = 5000,
            Increment = 1,      // Incrément de 1
            Value = 0,          // Valeur par défaut à 0
            ThousandsSeparator = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        // Info importante
        var labelRecordsWarning = new Label
        {
            Text = "Attention : Récupérer tous les articles peut prendre du temps et causer des problèmes de performance.",
            Location = new Point(10, 350),
            Size = new Size(520, 50),
            ForeColor = ColorPalette.ErrorColor,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        return new Control[]
        {
            labelD365Section,
            labelTokenUrl, textBoxTokenUrl,
            labelClientId, textBoxClientId,
            labelClientSecret, textBoxClientSecret,
            labelResource, textBoxResource,
            labelDynamicsApiUrl, textBoxDynamicsApiUrl,
            labelMaxRecords, numericMaxRecords,
            labelRecordsWarning
        };
    }

    private Control[] CreateFieldsTabControls()
    {
        // Section titre
        var labelFieldsSection = new Label
        {
            Text = "Configuration des champs à transférer",
            Location = new Point(10, 10),
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        // ComboBox pour la source de données
        var labelSourceType = new Label
        {
            Text = "Source de données :",
            Location = new Point(10, 50),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        var comboSourceType = new ComboBox
        {
            Location = new Point(10, 70),
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
            Location = new Point(270, 50),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true
        };

        var comboEntity = new ComboBox
        {
            Location = new Point(270, 70),
            Size = new Size(250, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        // Bouton pour ouvrir le formulaire de sélection
        var buttonOpenFieldSelection = new Button
        {
            Text = "Configurer les champs",
            Location = new Point(10, 110),
            Size = new Size(510, 40),
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
            Location = new Point(10, 170),
            Size = new Size(520, 180),
            ForeColor = ColorPalette.PrimaryText
        };

        // Info importante
        var labelWarning = new Label
        {
            Text = "Note : Cette analyse peut prendre un peu de temps en fonction de la taille des données et de la vitesse de réponse de l'API.",
            Location = new Point(10, 350),
            Size = new Size(520, 50),
            ForeColor = ColorPalette.SecondaryText,
            Font = new Font("Segoe UI", 9, FontStyle.Italic)
        };

        // Événements
        comboSourceType.SelectedIndexChanged += (s, e) =>
        {
            comboEntity.Items.Clear();
            if (comboSourceType.SelectedIndex == 0) // API POM
            {
                comboEntity.Items.AddRange(new string[] { "clients", "commandes", "produits", "lignescommandes" });
            }
            else // Dynamics 365
            {
                comboEntity.Items.AddRange(new string[] { "ReleasedProductsV2" });
            }
            comboEntity.SelectedIndex = 0;
        };

        buttonOpenFieldSelection.Click += (s, e) =>
        {
            string sourceType = comboSourceType.SelectedIndex == 0 ? "pom" : "dynamics";
            string entity = comboEntity.SelectedItem.ToString();

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

        return new Control[]
        {
            labelFieldsSection, labelSourceType, comboSourceType,
            labelEntity, comboEntity, buttonOpenFieldSelection,
            labelExplanation, labelWarning
        };
    }

    private void ButtonSave_Click(object sender, EventArgs e)
    {
        try
        {
            _configuration.SaveConfiguration(
                textBoxApiUrl.Text,
                textBoxApiKey.Text,
                BuildConnectionString(),
                textBoxTokenUrl.Text,
                textBoxClientId.Text,
                textBoxClientSecret.Text,
                textBoxResource.Text,
                textBoxDynamicsApiUrl.Text,
                (int)numericMaxRecords.Value,
                "" // Chaîne vide pour le paramètre spécifique
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

    private void ParseAndFillConnectionString(string connectionString)
    {
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

    // Contrôles pour les champs de base de données et API
    private TextBox textBoxApiUrl;
    private TextBox textBoxApiKey;
    private TextBox textBoxServer;
    private TextBox textBoxDatabase;
    private TextBox textBoxUser;
    private TextBox textBoxPassword;
}
