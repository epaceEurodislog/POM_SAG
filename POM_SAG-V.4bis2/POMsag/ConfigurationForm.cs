using POMsag;
using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using POMsag.Services;
using POMsag.Models;

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
    private SchemaAnalysisService _schemaAnalysisService;
    private TabControl tabControl;
    private TextBox textBoxTokenUrl;
    private TextBox textBoxClientId;
    private TextBox textBoxClientSecret;
    private TextBox textBoxResource;
    private TextBox textBoxDynamicsApiUrl;
    private NumericUpDown numericMaxRecords;

    // Composants pour l'onglet sélection de champs
    private ComboBox comboSourceType;
    private ComboBox comboEntity;
    private Button buttonSelectFields;

    // Pour initialiser un nouveau HttpClient si nécessaire
    private HttpClient _httpClient;
    private IDynamicsApiService _dynamicsApiService;

    public ConfigurationForm(AppConfiguration configuration, SchemaAnalysisService schemaAnalysisService = null)
    {
        InitializeComponent();
        _configuration = configuration;

        // Si un service d'analyse de schéma est fourni, l'utiliser. Sinon, en créer un nouveau lorsque nécessaire.
        _schemaAnalysisService = schemaAnalysisService;

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
        this.Size = new Size(800, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 10);
        this.BackColor = ColorPalette.PrimaryBackground;

        // Création du TabControl avec style moderne
        tabControl = new TabControl
        {
            Location = new Point(10, 10),
            Size = new Size(775, 600),
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

        // Onglet Sélection des champs
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

        // Contenu de l'onglet Sélection des champs
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
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true,
            Location = new Point(10, 10)
        };

        // Panel principal contenant tous les contrôles
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = ColorPalette.WhiteBackground
        };

        // ComboBox pour la source de données
        var labelSourceType = new Label
        {
            Text = "Source de données :",
            ForeColor = ColorPalette.PrimaryText,
            AutoSize = true,
            Location = new Point(10, 50)
        };

        comboSourceType = new ComboBox
        {
            Location = new Point(10, 75),
            Size = new Size(520, 30),
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
            Location = new Point(10, 115)
        };

        comboEntity = new ComboBox
        {
            Location = new Point(10, 140),
            Size = new Size(520, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ColorPalette.SecondaryBackground,
            ForeColor = ColorPalette.PrimaryText
        };

        // Bouton pour ouvrir le formulaire de sélection
        buttonSelectFields = new Button
        {
            Text = "Configurer les champs",
            Location = new Point(10, 180),
            Size = new Size(520, 40),
            BackColor = ColorPalette.AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        buttonSelectFields.FlatAppearance.BorderSize = 0;
        buttonSelectFields.Click += ButtonSelectFields_Click;

        // Explications
        var labelExplanation = new Label
        {
            Text = "Ce module vous permet de sélectionner les champs que vous souhaitez inclure lors des transferts de données.\n\n" +
                  "1. Sélectionnez d'abord le type de source de données (API POM ou Dynamics 365)\n" +
                  "2. Choisissez l'entité (table) dont vous souhaitez configurer les champs\n" +
                  "3. Cliquez sur \"Configurer les champs\" pour ouvrir la fenêtre de sélection",
            Location = new Point(10, 230),
            Size = new Size(520, 150),
            ForeColor = ColorPalette.PrimaryText
        };

        // Info importante
        var labelWarning = new Label
        {
            Text = "Note : Cette analyse peut prendre un peu de temps en fonction de la taille des données et de la vitesse de réponse de l'API.",
            Location = new Point(10, 390),
            Size = new Size(520, 40),
            ForeColor = ColorPalette.SecondaryText,
            Font = new Font("Segoe UI", 9, FontStyle.Italic)
        };

        // Ajouter tous les contrôles au panel principal
        mainPanel.Controls.Add(labelFieldsSection);
        mainPanel.Controls.Add(labelSourceType);
        mainPanel.Controls.Add(comboSourceType);
        mainPanel.Controls.Add(labelEntity);
        mainPanel.Controls.Add(comboEntity);
        mainPanel.Controls.Add(buttonSelectFields);
        mainPanel.Controls.Add(labelExplanation);
        mainPanel.Controls.Add(labelWarning);

        // Événements
        comboSourceType.SelectedIndexChanged += (s, e) =>
        {
            UpdateEntityList();
        };

        // Déclencher l'événement pour remplir le combobox entité
        UpdateEntityList();

        return new Control[] { mainPanel };
    }

    private void UpdateEntityList()
    {
        comboEntity.Items.Clear();

        if (comboSourceType.SelectedIndex == 0) // API POM
        {
            comboEntity.Items.AddRange(new string[] { "Clients", "Commandes", "Produits", "LignesCommandes" });
        }
        else // Dynamics 365
        {
            comboEntity.Items.AddRange(new string[] { "ReleasedProductsV2" });
        }

        if (comboEntity.Items.Count > 0)
            comboEntity.SelectedIndex = 0;
    }

    private void ButtonSelectFields_Click(object sender, EventArgs e)
    {
        if (comboEntity.SelectedItem == null)
        {
            MessageBox.Show("Veuillez sélectionner une entité d'abord.", "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string entity = comboEntity.SelectedItem.ToString();
        string sourceType = comboSourceType.SelectedIndex == 0 ? "pom" : "dynamics";

        try
        {
            // Assurez-vous que le service d'analyse de schéma est disponible
            if (_schemaAnalysisService == null)
            {
                InitializeServices();
            }

            using (var fieldSelectionForm = new FieldSelectionForm(_configuration, _schemaAnalysisService, entity, sourceType))
            {
                fieldSelectionForm.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'ouverture du formulaire de sélection des champs: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LoggerService.LogException(ex, "ButtonSelectFields_Click");
        }
    }

    private void InitializeServices()
    {
        // Initialiser le HttpClient pour l'API POM
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_configuration.ApiUrl)
        };
        if (!string.IsNullOrEmpty(_configuration.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _configuration.ApiKey);
        }

        // Créer la définition de l'API Dynamics 365
        var dynamicsApi = new ApiDefinition
        {
            Name = "Dynamics365",
            BaseUrl = _configuration.DynamicsApiUrl,
            AuthType = ApiAuthType.OAuth2,
            AuthProperties = new Dictionary<string, string>
        {
            { "TokenUrl", _configuration.TokenUrl },
            { "ClientId", _configuration.ClientId },
            { "ClientSecret", _configuration.ClientSecret },
            { "Resource", _configuration.Resource }
        },
            Endpoints = new List<ApiEndpoint>
        {
            new ApiEndpoint
            {
                Name = "ReleasedProductsV2",
                Path = "ReleasedProductsV2",
                Method = HttpMethod.Get,
                SupportsDateFiltering = true,
                StartDateParamName = "$filter=PurchasePriceDate ge @startDate",
                EndDateParamName = "and PurchasePriceDate le @endDate",
                DateFormat = "yyyy-MM-ddT00:00:00Z",
                ResponseRootPath = "value",
                Parameters = new Dictionary<string, string>
                {
                    { "cross-company", "true" },
                    { "$top", _configuration.MaxRecords.ToString() }
                }
            }
        }
        };

        // Ajouter ou mettre à jour l'API dans le gestionnaire d'API
        _apiManager.AddOrUpdateApi(dynamicsApi);

        // Initialiser le service Dynamics
        _dynamicsApiService = new DynamicsApiService(_apiManager);

        // Initialiser le service d'analyse de schéma
        _schemaAnalysisService = new SchemaAnalysisService(
            _dynamicsApiService,
            _httpClient,
            _configuration
        );
    }

    private void ButtonSave_Click(object sender, EventArgs e)
    {
        try
        {
            // Sauvegarder les configurations
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