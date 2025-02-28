using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Data.SqlClient;
using POMsag.Services;
using POMsag.Models;
using System.IO;

namespace POMsag
{
    public partial class Form1 : Form
    {
        private HttpClient _httpClient;
        private readonly AppConfiguration _configuration;
        private string _destinationConnectionString;
        private DynamicsApiService _dynamicsApiService;
        private SchemaAnalysisService _schemaAnalysisService;
        private GenericApiService _genericApiService;
        private bool _isTransferInProgress = false;
        private Panel mainPanel;

        // Exposer le service d'analyse de schéma pour être réutilisé
        public SchemaAnalysisService SchemaAnalysisService => _schemaAnalysisService;

        // ComboBox pour la sélection d'API
        private ComboBox comboBoxApi;

        public Form1(AppConfiguration configuration = null, DynamicsApiService dynamicsApiService = null,
                     HttpClient httpClient = null, SchemaAnalysisService schemaAnalysisService = null)
        {
            InitializeComponent();

            // Utiliser les services fournis ou créer de nouveaux
            _configuration = configuration ?? new AppConfiguration();

            // Initialiser le client HTTP standard et le service D365
            if (httpClient != null && dynamicsApiService != null)
            {
                // Utiliser les services fournis
                _httpClient = httpClient;
                _dynamicsApiService = dynamicsApiService;
                _destinationConnectionString = _configuration.DatabaseConnectionString;
            }
            else
            {
                // Créer tous les services
                InitializeHttpClient();
            }

            _schemaAnalysisService = schemaAnalysisService;
            _genericApiService = new GenericApiService(_configuration);

            // Initialiser les contrôles
            InitializeControls();
        }

        private void InitializeHttpClient()
        {
            // Client HTTP standard
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_configuration.ApiUrl)
            };

            if (!string.IsNullOrEmpty(_configuration.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _configuration.ApiKey);
            }

            _destinationConnectionString = _configuration.DatabaseConnectionString;

            // Service D365 API avec limite d'enregistrements
            _dynamicsApiService = new DynamicsApiService(
                _configuration.TokenUrl,
                _configuration.ClientId,
                _configuration.ClientSecret,
                _configuration.Resource,
                _configuration.DynamicsApiUrl,
                _configuration.MaxRecords
            );
        }

        private void InitializeControls()
        {
            // IMPORTANT: Détacher les gestionnaires d'événements existants pour éviter les doublons
            if (comboBoxTables != null)
            {
                comboBoxTables.SelectedIndexChanged -= ComboBoxTables_SelectedIndexChanged;
            }
            if (checkBoxDateFilter != null)
            {
                checkBoxDateFilter.CheckedChanged -= CheckBoxDateFilter_CheckedChanged;
            }
            if (buttonTransfer != null)
            {
                buttonTransfer.Click -= ButtonTransfer_Click;
            }
            if (comboBoxApi != null)
            {
                comboBoxApi.SelectedIndexChanged -= ComboBoxApi_SelectedIndexChanged;
            }

            // Mise à jour des choix pour inclure les tables D365
            comboBoxTables.Items.Clear();

            // Ajout d'un ComboBox pour choisir l'API
            if (comboBoxApi == null)
            {
                var labelApi = new Label
                {
                    Text = "API Source :",
                    Location = new Point(20, 85),
                    Size = new Size(100, 20),
                    ForeColor = ColorPalette.PrimaryText
                };

                comboBoxApi = new ComboBox
                {
                    Location = new Point(120, 80),
                    Size = new Size(250, 30),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = ColorPalette.SecondaryBackground,
                    ForeColor = ColorPalette.PrimaryText
                };

                // Trouver le panel principal où ajouter ces contrôles
                foreach (Control control in this.Controls)
                {
                    if (control is Panel panel && panel.Controls.Contains(labelTitle) && panel.Controls.Contains(contentPanel))
                    {
                        mainPanel = panel;
                        break;
                    }
                }

                if (mainPanel != null)
                {
                    // Trouver le panel de contenu
                    Panel contentPanel = null;
                    foreach (Control control in mainPanel.Controls)
                    {
                        if (control is Panel && control.BackColor == ColorPalette.WhiteBackground)
                        {
                            contentPanel = (Panel)control;
                            break;
                        }
                    }

                    if (contentPanel != null)
                    {
                        // Trouver le panel de sélection des données
                        Panel dataSelectionPanel = null;
                        foreach (Control control in contentPanel.Controls)
                        {
                            if (control is Panel && control.Dock == DockStyle.Top)
                            {
                                dataSelectionPanel = (Panel)control;
                                break;
                            }
                        }

                        if (dataSelectionPanel != null)
                        {
                            // Ajouter les contrôles au panel de sélection des données
                            dataSelectionPanel.Controls.Add(labelApi);
                            dataSelectionPanel.Controls.Add(comboBoxApi);
                        }
                    }
                }
            }

            comboBoxApi.Items.Clear();
            foreach (var api in _configuration.ConfiguredApis)
            {
                comboBoxApi.Items.Add(api.Name);
            }

            if (comboBoxApi.Items.Count > 0)
                comboBoxApi.SelectedIndex = 0;

            comboBoxApi.SelectedIndexChanged += ComboBoxApi_SelectedIndexChanged;

            // Mettre à jour la liste des endpoints en fonction de l'API sélectionnée
            UpdateEndpointsList();

            // Attacher les gestionnaires d'événements
            checkBoxDateFilter.CheckedChanged += CheckBoxDateFilter_CheckedChanged;
            buttonTransfer.Click += ButtonTransfer_Click;

            // Valeurs par défaut
            dateTimePickerStart.Value = DateTime.Now.AddMonths(-1);
            dateTimePickerEnd.Value = DateTime.Now;

            // État initial
            buttonTransfer.Enabled = false;
            dateTimePickerStart.Enabled = false;
            dateTimePickerEnd.Enabled = false;
        }

        // Méthode pour mettre à jour la liste des endpoints
        private void UpdateEndpointsList()
        {
            comboBoxTables.Items.Clear();

            if (comboBoxApi.SelectedIndex < 0)
                return;

            var api = _configuration.ConfiguredApis[comboBoxApi.SelectedIndex];

            foreach (var endpoint in api.Endpoints)
            {
                comboBoxTables.Items.Add(endpoint.Name);
            }

            if (comboBoxTables.Items.Count > 0)
            {
                comboBoxTables.SelectedIndex = 0;
                comboBoxTables.SelectedIndexChanged += ComboBoxTables_SelectedIndexChanged;
            }
        }

        // Méthode pour réagir au changement d'API
        private void ComboBoxApi_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEndpointsList();
        }

        private void ConfigButton_Click(object sender, EventArgs e)
        {
            // Créer un menu contextuel
            var contextMenu = new ContextMenuStrip();

            var configItem = new ToolStripMenuItem("Configuration générale");
            configItem.Click += (s, ev) =>
            {
                using (var configForm = new ConfigurationForm(_configuration))
                {
                    configForm.ShowDialog();

                    // Mettre à jour la configuration
                    InitializeHttpClient();

                    // Rafraichir les combobox
                    InitializeControls();
                }
            };

            var apiManagerItem = new ToolStripMenuItem("Gestionnaire d'API");
            apiManagerItem.Click += (s, ev) =>
            {
                using (var apiManagerForm = new ApiManagerForm(_configuration))
                {
                    apiManagerForm.ShowDialog();

                    // Mettre à jour le service générique
                    _genericApiService = new GenericApiService(_configuration);

                    // Rafraichir les combobox
                    InitializeControls();
                }
            };

            contextMenu.Items.Add(configItem);
            contextMenu.Items.Add(apiManagerItem);

            // Afficher le menu contextuel près du bouton de configuration
            contextMenu.Show(configMenuItem, new Point(0, configMenuItem.Height));
        }

        private void CheckBoxDateFilter_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePickerStart.Enabled = checkBoxDateFilter.Checked;
            dateTimePickerEnd.Enabled = checkBoxDateFilter.Checked;
        }

        private void ComboBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonTransfer.Enabled = comboBoxTables.SelectedItem != null && !_isTransferInProgress;
        }

        private async void ButtonTransfer_Click(object sender, EventArgs e)
        {
            // Vérification si un transfert est déjà en cours ou si aucune table n'est sélectionnée
            if (_isTransferInProgress || comboBoxTables.SelectedItem == null || comboBoxApi.SelectedItem == null)
                return;

            // Marquer le début du transfert
            _isTransferInProgress = true;

            var apiIndex = comboBoxApi.SelectedIndex;
            var endpointIndex = comboBoxTables.SelectedIndex;

            if (apiIndex < 0 || endpointIndex < 0)
            {
                _isTransferInProgress = false;
                return;
            }

            var api = _configuration.ConfiguredApis[apiIndex];
            string endpointName = comboBoxTables.SelectedItem.ToString();
            var endpoint = api.Endpoints.FirstOrDefault(ep => ep.Name == endpointName);

            if (endpoint == null)
            {
                _isTransferInProgress = false;
                return;
            }

            string apiId = api.ApiId;

            buttonTransfer.Enabled = false;
            progressBar.Visible = true;
            statusPanel.Visible = false;

            LoggerService.Log($"Début du transfert pour: {api.Name} / {endpointName}");
            ShowStatus($"Transfert en cours pour {api.Name} / {endpointName}...");

            try
            {
                List<Dictionary<string, object>> data;

                // Récupérer les données via le service générique
                DateTime? startDate = checkBoxDateFilter.Checked ? dateTimePickerStart.Value : null;
                DateTime? endDate = checkBoxDateFilter.Checked ? dateTimePickerEnd.Value : null;

                ShowStatus($"Récupération des données depuis {api.Name} ({endpointName})...");

                data = await _genericApiService.FetchDataAsync(apiId, endpointName, startDate, endDate);

                ShowStatus($"Récupération terminée. {data.Count} enregistrements trouvés.");

                // Filtrer les champs selon les préférences
                var filteredData = new List<Dictionary<string, object>>();
                foreach (var item in data)
                {
                    // Utiliser apiId_endpointName comme clé pour les préférences de champs
                    var filteredItem = FilterFieldsBasedOnPreferences(item, $"{apiId}_{endpointName}");
                    filteredData.Add(filteredItem);
                }

                // Sauvegarder les données
                ShowStatus("Enregistrement des données dans la base de destination...");
                await SaveToDestinationDbAsync(filteredData, endpointName);

                // Afficher le message de succès
                ShowStatus("Enregistrement terminé avec succès.");
                ShowSuccessMessage($"Transfert réussi ! {filteredData.Count} enregistrements transférés.");

                LoggerService.Log($"Transfert terminé avec succès pour {api.Name} / {endpointName}. {filteredData.Count} enregistrements.");
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Transfert {api.Name} / {endpointName}");
                ShowErrorDetail($"Erreur lors du transfert de {api.Name} / {endpointName}", ex);
            }
            finally
            {
                // Marquer la fin du transfert
                _isTransferInProgress = false;
                buttonTransfer.Enabled = comboBoxTables.SelectedItem != null;
                progressBar.Visible = false;
            }
        }

        // Méthode pour filtrer les champs selon les préférences
        private Dictionary<string, object> FilterFieldsBasedOnPreferences(Dictionary<string, object> original, string entityName)
        {
            // Si aucune préférence n'est définie, renvoyer l'original
            if (!_configuration.FieldSelections.ContainsKey(entityName))
                return original;

            var preferences = _configuration.FieldSelections[entityName];
            var filtered = new Dictionary<string, object>();

            foreach (var key in original.Keys)
            {
                // Si la préférence existe et est activée, ou si elle n'existe pas du tout (par défaut inclure)
                if (!preferences.Fields.ContainsKey(key) || preferences.Fields[key])
                {
                    filtered[key] = original[key];
                }
            }

            return filtered;
        }

        // Méthode pour montrer l'état du transfert
        private void ShowStatus(string message)
        {
            // Mise à jour du panneau d'état
            if (statusTextBox.InvokeRequired)
            {
                statusTextBox.Invoke(new Action(() =>
                {
                    statusTextBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\r\n";
                    statusTextBox.SelectionStart = statusTextBox.Text.Length;
                    statusTextBox.ScrollToCaret();
                    statusPanel.Visible = true;
                }));
            }
            else
            {
                statusTextBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\r\n";
                statusTextBox.SelectionStart = statusTextBox.Text.Length;
                statusTextBox.ScrollToCaret();
                statusPanel.Visible = true;
            }

            LoggerService.Log(message);
        }

        // Méthode pour afficher les détails d'erreur
        private void ShowErrorDetail(string message, Exception ex)
        {
            var detailedMessage = $"{message}\r\n\r\nDétails: {ex.Message}";

            if (ex.InnerException != null)
            {
                detailedMessage += $"\r\n\r\nErreur interne: {ex.InnerException.Message}";
            }

            ShowStatus($"ERREUR: {ex.Message}");

            // Afficher l'erreur à l'utilisateur
            MessageBox.Show(
                detailedMessage,
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        // Méthode pour récupérer les données depuis l'API
        private async Task<List<Dictionary<string, object>>> FetchDataFromApiAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/{endpoint}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                LoggerService.Log($"Données reçues de l'API ({content.Length} caractères)");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                    content,
                    options
                );

                if (result == null)
                {
                    throw new Exception("La désérialisation a retourné null");
                }

                LoggerService.Log($"Nombre d'éléments désérialisés : {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Erreur lors de la récupération des données : {ex.Message}");
                throw;
            }
        }

        // Méthode pour afficher les logs
        private void ViewLogs_Click(object sender, EventArgs e)
        {
            var logFilePath = "pom_api_log.txt";

            if (!File.Exists(logFilePath))
            {
                MessageBox.Show(
                    "Aucun fichier de log n'existe encore.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            try
            {
                // Ouvrir le fichier de log avec l'application par défaut
                System.Diagnostics.Process.Start("notepad.exe", logFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible d'ouvrir le fichier de log: {ex.Message}",
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // Méthode pour afficher les informations sur l'application
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "POM SAG - Outil de Transfert de Données\n" +
                "Version 3.0\n\n" +
                "Application permettant de transférer des données depuis:\n" +
                "- L'API POM\n" +
                "- Dynamics 365 Finance & Operations\n" +
                "- Toute autre API configurable par l'utilisateur\n\n" +
                "Améliorations v3.0:\n" +
                "- Détection et sélection automatique des champs API\n" +
                "- Configuration d'API dynamique sans modifier le code\n" +
                "- Interface de gestion des API et des endpoints",
                "À propos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // Méthode pour afficher un message d'erreur
        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(
                message,
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        // Méthode pour afficher un message de succès
        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(
                message,
                "Succès",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // Méthode pour sauvegarder les données dans la base de destination
        private async Task SaveToDestinationDbAsync(List<Dictionary<string, object>> data, string tableName)
        {
            try
            {
                using var connection = new SqlConnection(_destinationConnectionString);
                await connection.OpenAsync();

                // Vérifier si la table JSON_DAT existe, sinon la créer
                var checkTableQuery = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JSON_DAT')
            CREATE TABLE JSON_DAT (
                JSON_KEYU INT IDENTITY(1,1) PRIMARY KEY,
                JsonContent NVARCHAR(MAX) NOT NULL,
                CreatedAt VARCHAR(8) NOT NULL,
                SourceTable VARCHAR(50) NOT NULL
            )";

                using (var command = new SqlCommand(checkTableQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                string formattedDate = DateTime.Now.ToString("yyyyMMdd");

                // Sérialiser toutes les données en un seul document JSON
                var allDataJson = JsonSerializer.Serialize(data,
                    new JsonSerializerOptions { WriteIndented = false });

                // Insérer toutes les données en un seul enregistrement
                var query = @"
            INSERT INTO JSON_DAT (JsonContent, CreatedAt, SourceTable) 
            VALUES (@JsonContent, @CreatedAt, @SourceTable);
            
            SELECT SCOPE_IDENTITY() AS JSON_KEYU;";

                using var insertCommand = new SqlCommand(query, connection);
                insertCommand.Parameters.AddWithValue("@JsonContent", allDataJson);
                insertCommand.Parameters.AddWithValue("@CreatedAt", formattedDate);
                insertCommand.Parameters.AddWithValue("@SourceTable", tableName);

                var jsonKeyu = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());
                LoggerService.Log($"Données insérées avec JSON_KEYU: {jsonKeyu}, {data.Count} éléments");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Erreur lors de la sauvegarde en base de données : {ex.Message}");
                throw;
            }
        }
    }
}