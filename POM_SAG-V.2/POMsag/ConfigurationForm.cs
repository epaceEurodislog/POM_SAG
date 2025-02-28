using POMsag;

public partial class ConfigurationForm : Form
{
    private AppConfiguration _configuration;

    public ConfigurationForm(AppConfiguration configuration)
    {
        InitializeComponent();
        _configuration = configuration;

        // Décomposer et remplir les champs de connexion
        ParseAndFillConnectionString(_configuration.DatabaseConnectionString);
        textBoxApiUrl.Text = _configuration.ApiUrl;
        textBoxApiKey.Text = _configuration.ApiKey;
    }

    private void InitializeComponent()
    {
        this.Text = "Configuration de l'Application";
        this.Size = new Size(600, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 10);

        // Section API
        var labelApiSection = new Label
        {
            Text = "Configuration API",
            Location = new Point(20, 20),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true
        };

        var labelApiUrl = new Label
        {
            Text = "URL de l'API :",
            Location = new Point(20, 50),
            AutoSize = true
        };

        var labelApiKey = new Label
        {
            Text = "Clé API :",
            Location = new Point(20, 100),
            AutoSize = true
        };

        textBoxApiUrl = new TextBox
        {
            Location = new Point(20, 70),
            Size = new Size(540, 30)
        };

        textBoxApiKey = new TextBox
        {
            Location = new Point(20, 120),
            Size = new Size(540, 30)
        };

        // Section Base de données
        var labelDbSection = new Label
        {
            Text = "Configuration Base de Données",
            Location = new Point(20, 170),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true
        };

        var labelServer = new Label
        {
            Text = "Serveur :",
            Location = new Point(20, 200),
            AutoSize = true
        };

        textBoxServer = new TextBox
        {
            Location = new Point(20, 220),
            Size = new Size(540, 30)
        };

        var labelDatabase = new Label
        {
            Text = "Base de données :",
            Location = new Point(20, 250),
            AutoSize = true
        };

        textBoxDatabase = new TextBox
        {
            Location = new Point(20, 270),
            Size = new Size(540, 30)
        };

        var labelUser = new Label
        {
            Text = "Utilisateur :",
            Location = new Point(20, 300),
            AutoSize = true
        };

        textBoxUser = new TextBox
        {
            Location = new Point(20, 320),
            Size = new Size(260, 30)
        };

        var labelPassword = new Label
        {
            Text = "Mot de passe :",
            Location = new Point(300, 300),
            AutoSize = true
        };

        textBoxPassword = new TextBox
        {
            Location = new Point(300, 320),
            Size = new Size(260, 30),
            PasswordChar = '•'
        };

        var buttonSave = new Button
        {
            Text = "Enregistrer",
            Location = new Point(20, 380),
            Size = new Size(540, 40),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        buttonSave.Click += ButtonSave_Click;

        this.Controls.AddRange(new Control[]
        {
            labelApiSection,
            labelApiUrl,
            labelApiKey,
            textBoxApiUrl,
            textBoxApiKey,
            labelDbSection,
            labelServer,
            textBoxServer,
            labelDatabase,
            textBoxDatabase,
            labelUser,
            textBoxUser,
            labelPassword,
            textBoxPassword,
            buttonSave
        });
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

    private void ButtonSave_Click(object sender, EventArgs e)
    {
        try
        {
            _configuration.SaveConfiguration(
                textBoxApiUrl.Text,
                textBoxApiKey.Text,
                BuildConnectionString()
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

    private TextBox textBoxApiUrl;
    private TextBox textBoxApiKey;
    private TextBox textBoxServer;
    private TextBox textBoxDatabase;
    private TextBox textBoxUser;
    private TextBox textBoxPassword;
}