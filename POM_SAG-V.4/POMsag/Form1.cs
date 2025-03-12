// Fichier Form1.cs (version modifiée)
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Data.SqlClient;
using POMsag.Services;
using POMsag.Models;
using System.IO;
using System.Linq;

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

        private Panel FindDataSelectionPanel()
        {
            foreach (Control control in this.Controls)
            {
                if (control is Panel mainPnl)
                {
                    foreach (Control subControl in mainPnl.Controls)
                    {
                        if (subControl is Panel contentPnl && contentPnl.BackColor == ColorPalette.WhiteBackground)
                        {
                            foreach (Control contentElement in contentPnl.Controls)
                            {
                                if (contentElement is Panel panel && panel.Dock == DockStyle.Top)
                                {
                                    return panel;
                                }
                            }
                        }
                    }
                }
            }
            return null;
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

            // Trouver le panneau de sélection de données
            var dataSelectionPanel = FindDataSelectionPanel();
            if (dataSelectionPanel != null)
            {
                dataSelectionPanel.Controls.Clear();
                dataSelectionPanel.Padding = new Padding(20);
                dataSelectionPanel.Height = 400; // Augmenter la hauteur pour éviter les superpositions

                // Créer un TableLayoutPanel pour organiser tous les contrôles
                var mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 5, // Titre + API/Endpoint + DateFilter + DatePicker + Espacement
                    AutoSize = true
                };

                // Définir les hauteurs des lignes
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Titre
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // API/Endpoint
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // DateFilter
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // DatePicker
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Espace restant

                // Titre de section
                var labelSelect = new Label
                {
                    Text = "Sélectionnez les données à transférer",
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 14, FontStyle.Regular),
                    ForeColor = ColorPalette.PrimaryText,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // Créer un TableLayoutPanel pour organiser les contrôles d'API et d'endpoints
                var apiEndpointLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 2,
                    Padding = new Padding(0, 10, 0, 10)
                };

                apiEndpointLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // Labels
                apiEndpointLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Comboboxes
                apiEndpointLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                apiEndpointLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

                // Label API Source
                var labelApi = new Label
                {
                    Text = "API Source :",
                    Anchor = AnchorStyles.Left,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 12, FontStyle.Regular),
                    ForeColor = ColorPalette.PrimaryText
                };

                // Configuration de comboBoxApi s'il n'existe pas déjà
                if (comboBoxApi == null)
                {
                    comboBoxApi = new ComboBox
                    {
                        Name = "comboBoxApi",
                        Dock = DockStyle.Fill,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        BackColor = ColorPalette.SecondaryBackground,
                        ForeColor = ColorPalette.PrimaryText,
                        Font = new Font("Segoe UI", 12, FontStyle.Regular)
                    };
                }
                else
                {
                    // Reconfigurer comboBoxApi existant
                    comboBoxApi.Dock = DockStyle.Fill;
                    comboBoxApi.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBoxApi.BackColor = ColorPalette.SecondaryBackground;
                    comboBoxApi.ForeColor = ColorPalette.PrimaryText;
                    comboBoxApi.Font = new Font("Segoe UI", 12, FontStyle.Regular);
                }

                // Label Endpoint
                var labelEndpoint = new Label
                {
                    Text = "Endpoint :",
                    Anchor = AnchorStyles.Left,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 12, FontStyle.Regular),
                    ForeColor = ColorPalette.PrimaryText
                };

                // Configuration des contrôles existants
                comboBoxTables.Dock = DockStyle.Fill;
                comboBoxTables.DropDownStyle = ComboBoxStyle.DropDownList;
                comboBoxTables.BackColor = ColorPalette.SecondaryBackground;
                comboBoxTables.ForeColor = ColorPalette.PrimaryText;
                comboBoxTables.Font = new Font("Segoe UI", 12, FontStyle.Regular);

                // Ajouter les contrôles au TableLayoutPanel pour API/Endpoint
                apiEndpointLayout.Controls.Add(labelApi, 0, 0);
                apiEndpointLayout.Controls.Add(comboBoxApi, 1, 0);
                apiEndpointLayout.Controls.Add(labelEndpoint, 0, 1);
                apiEndpointLayout.Controls.Add(comboBoxTables, 1, 1);

                // Panel pour le filtre de date avec CheckBox
                var dateFilterPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0, 5, 0, 5)
                };

                // Amélioration des contrôles de filtre de date
                checkBoxDateFilter.Text = "Filtrer par date";
                checkBoxDateFilter.AutoSize = true;
                checkBoxDateFilter.Font = new Font("Segoe UI", 12);
                checkBoxDateFilter.ForeColor = ColorPalette.PrimaryText;
                checkBoxDateFilter.Margin = new Padding(0, 5, 0, 5);
                checkBoxDateFilter.Location = new Point(0, 5);
                dateFilterPanel.Controls.Add(checkBoxDateFilter);

                // Panel pour les DateTimePickers
                var datePickerPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3, // Date début + label + Date fin
                    RowCount = 1,
                    BackColor = ColorPalette.SecondaryBackground
                };

                datePickerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); // Date début
                datePickerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); // Label "au"
                datePickerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); // Date fin

                // Amélioration des DateTimePickers
                dateTimePickerStart.Dock = DockStyle.Fill;
                dateTimePickerStart.Format = DateTimePickerFormat.Short;
                dateTimePickerStart.Font = new Font("Segoe UI", 12);

                var labelTo = new Label
                {
                    Text = "au",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 12),
                    ForeColor = ColorPalette.PrimaryText
                };

                dateTimePickerEnd.Dock = DockStyle.Fill;
                dateTimePickerEnd.Format = DateTimePickerFormat.Short;
                dateTimePickerEnd.Font = new Font("Segoe UI", 12);

                datePickerPanel.Controls.Add(dateTimePickerStart, 0, 0);
                datePickerPanel.Controls.Add(labelTo, 1, 0);
                datePickerPanel.Controls.Add(dateTimePickerEnd, 2, 0);

                // Ajouter tous les panels au layout principal
                mainLayout.Controls.Add(labelSelect, 0, 0);
                mainLayout.Controls.Add(apiEndpointLayout, 0, 1);
                mainLayout.Controls.Add(dateFilterPanel, 0, 2);
                mainLayout.Controls.Add(datePickerPanel, 0, 3);

                // Ajouter le layout principal au panel de sélection de données
                dataSelectionPanel.Controls.Add(mainLayout);

                // Amélioration du bouton de transfert
                buttonTransfer.Text = "Démarrer le transfert";
                buttonTransfer.Dock = DockStyle.Bottom;
                buttonTransfer.Height = 60;
                buttonTransfer.Font = new Font("Segoe UI", 16, FontStyle.Bold);
                buttonTransfer.BackColor = ColorPalette.AccentColor;
                buttonTransfer.ForeColor = Color.White;
                buttonTransfer.FlatStyle = FlatStyle.Flat;
                buttonTransfer.FlatAppearance.BorderSize = 0;
                buttonTransfer.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 142, 208);
                buttonTransfer.Margin = new Padding(20);
            }

            // Remplir le combobox API
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
            comboBoxTables.SelectedIndexChanged += ComboBoxTables_SelectedIndexChanged;
            checkBoxDateFilter.CheckedChanged += CheckBoxDateFilter_CheckedChanged;
            buttonTransfer.Click += ButtonTransfer_Click;

            // Valeurs par défaut
            dateTimePickerStart.Value = DateTime.Now.AddMonths(-1);
            dateTimePickerEnd.Value = DateTime.Now;

            // État initial
            dateTimePickerStart.Enabled = checkBoxDateFilter.Checked;
            dateTimePickerEnd.Enabled = checkBoxDateFilter.Checked;
            buttonTransfer.Enabled = comboBoxTables.SelectedItem != null && !_isTransferInProgress;
        }

        // Méthode pour mettre à jour la liste des endpoints
        // Méthode pour mettre à jour la liste des endpoints
        public void UpdateEndpointsList()
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
                comboBoxTables.SelectedIndex = 0;

            // Mettre à jour l'état du bouton de transfert
            buttonTransfer.Enabled = comboBoxTables.Items.Count > 0 && !_isTransferInProgress;
        }

        // Ajoutez cette méthode si elle n'existe pas déjà
        private void ApiManager_Closed(object sender, EventArgs e)
        {
            // Mettre à jour la liste des API dans le combobox
            comboBoxApi.Items.Clear();
            foreach (var api in _configuration.ConfiguredApis)
            {
                comboBoxApi.Items.Add(api.Name);
            }

            if (comboBoxApi.Items.Count > 0)
            {
                // Conserver la sélection actuelle si possible
                string currentApiName = comboBoxApi.SelectedItem?.ToString();
                int newIndex = currentApiName != null ? comboBoxApi.Items.IndexOf(currentApiName) : 0;
                comboBoxApi.SelectedIndex = newIndex >= 0 ? newIndex : 0;
            }

            // Mettre à jour la liste des endpoints
            UpdateEndpointsList();
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
                    RefreshAfterApiManagerClosed();
                }
            };

            contextMenu.Items.Add(configItem);
            contextMenu.Items.Add(apiManagerItem);

            // Afficher le menu contextuel près du bouton de configuration
            var menuLocation = this.PointToClient(
                this.mainMenu.PointToScreen(
                    new Point(configMenuItem.Bounds.Left, configMenuItem.Bounds.Bottom)
                )
            );
            contextMenu.Show(this, menuLocation);
        }

        private void RefreshAfterApiManagerClosed()
        {
            // Mettre à jour le service générique
            _genericApiService = new GenericApiService(_configuration);

            // Mettre à jour les listes
            comboBoxApi.Items.Clear();
            foreach (var api in _configuration.ConfiguredApis)
            {
                comboBoxApi.Items.Add(api.Name);
            }

            if (comboBoxApi.Items.Count > 0)
            {
                // Conserver la sélection actuelle si possible
                string currentApiName = comboBoxApi.SelectedItem?.ToString();
                int newIndex = currentApiName != null ? comboBoxApi.Items.IndexOf(currentApiName) : 0;
                comboBoxApi.SelectedIndex = newIndex >= 0 ? newIndex : 0;
            }

            // Mettre à jour la liste des endpoints
            UpdateEndpointsList();
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
            statusPanel.Visible = true;

            LoggerService.Log($"Début du transfert pour: {api.Name} / {endpointName}");
            ShowStatus($"Transfert en cours pour {api.Name} / {endpointName}...");

            try
            {
                List<Dictionary<string, object>> data;

                // Récupérer les données via le service générique
                DateTime? startDate = checkBoxDateFilter.Checked ? dateTimePickerStart.Value : null;
                DateTime? endDate = checkBoxDateFilter.Checked ? dateTimePickerEnd.Value : null;

                ShowStatus($"Récupération des données depuis {api.Name} ({endpointName})...");

                // Ajout de gestion d'erreur supplémentaire
                try
                {
                    data = await _genericApiService.FetchDataAsync(apiId, endpointName, startDate, endDate);
                }
                catch (Exception fetchEx)
                {
                    ShowStatus($"Erreur lors de la récupération des données : {fetchEx.Message}");
                    LoggerService.LogException(fetchEx, $"Récupération des données {endpointName}");
                    throw;
                }

                // Vérification des données brutes
                LoggerService.Log($"Données brutes récupérées : {data.Count} enregistrements");
                if (data.Count > 0)
                {
                    var keys = string.Join(", ", data[0].Keys.Take(10)); // Afficher les 10 premiers champs
                    LoggerService.Log($"Champs disponibles : {keys}");

                    // Vérification des contenus non-null
                    int nonNullCount = data.Count(d => d.Values.Any(v => v != null));
                    LoggerService.Log($"Enregistrements avec au moins une valeur non-null : {nonNullCount}");
                }

                ShowStatus($"Récupération terminée. {data.Count} enregistrements trouvés.");

                // Filtrer les champs selon les préférences
                var filteredData = new List<Dictionary<string, object>>();
                foreach (var item in data)
                {
                    // Utiliser apiId_endpointName comme clé pour les préférences de champs
                    var filteredItem = FilterFieldsBasedOnPreferences(item, $"{apiId}_{endpointName}");
                    filteredData.Add(filteredItem);
                }

                // Vérification après filtrage
                LoggerService.Log($"Après filtrage : {filteredData.Count} enregistrements, " +
                                 $"Premier élément contient {(filteredData.Count > 0 ? filteredData[0].Count : 0)} champs");

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
            {
                LoggerService.Log($"Aucune préférence de champ pour {entityName}, utilisation de tous les champs");
                return original;
            }

            var preferences = _configuration.FieldSelections[entityName];
            var filtered = new Dictionary<string, object>();
            int includedFields = 0;
            int excludedFields = 0;

            foreach (var key in original.Keys)
            {
                // Si la préférence existe et est activée, ou si elle n'existe pas du tout (par défaut inclure)
                bool shouldInclude = !preferences.Fields.ContainsKey(key) || preferences.Fields[key];

                if (shouldInclude)
                {
                    filtered[key] = original[key];
                    includedFields++;
                }
                else
                {
                    excludedFields++;
                }
            }

            LoggerService.Log($"Filtrage : {includedFields} champs conservés, {excludedFields} champs exclus");

            if (includedFields == 0)
            {
                LoggerService.Log("ATTENTION : Tous les champs ont été filtrés ! Utilisation des données originales");
                // Retourner l'original si tous les champs sont exclus
                return original;
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
                // Vérification des données avant sérialisation
                if (data == null || data.Count == 0)
                {
                    LoggerService.Log("ERREUR : Pas de données à sauvegarder");
                    throw new ArgumentException("Pas de données à sauvegarder");
                }

                using var connection = new SqlConnection(_destinationConnectionString);
                await connection.OpenAsync();

                // Vérifier la connexion à la base de données
                LoggerService.Log($"Connexion à la base de données : {connection.State}");

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
                var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
                var allDataJson = JsonSerializer.Serialize(data, jsonOptions);

                // Vérifier la taille du JSON
                LoggerService.Log($"Taille du JSON à insérer : {allDataJson.Length} caractères");

                // Vérification rapide du contenu JSON
                if (allDataJson.Length <= 2) // "{}" ou "[]"
                {
                    LoggerService.Log("ERREUR : JSON sérialisé vide ou invalide");
                    throw new Exception("JSON sérialisé vide ou invalide");
                }

                // Insérer toutes les données en un seul enregistrement
                var query = @"
            INSERT INTO JSON_DAT (JsonContent, CreatedAt, SourceTable) 
            VALUES (@JsonContent, @CreatedAt, @SourceTable);
            
            SELECT SCOPE_IDENTITY() AS JSON_KEYU;";

                using var insertCommand = new SqlCommand(query, connection);
                insertCommand.Parameters.AddWithValue("@JsonContent", allDataJson);
                insertCommand.Parameters.AddWithValue("@CreatedAt", formattedDate);
                insertCommand.Parameters.AddWithValue("@SourceTable", tableName);

                // Définir la taille du paramètre pour les grands volumes de données
                insertCommand.Parameters["@JsonContent"].SqlDbType = System.Data.SqlDbType.NVarChar;
                insertCommand.Parameters["@JsonContent"].Size = -1; // -1 = max

                var jsonKeyu = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());
                LoggerService.Log($"Données insérées avec JSON_KEYU: {jsonKeyu}, {data.Count} éléments");

                // Vérification après insertion
                var verifyQuery = "SELECT LEN(JsonContent) FROM JSON_DAT WHERE JSON_KEYU = @Id";
                using var verifyCommand = new SqlCommand(verifyQuery, connection);
                verifyCommand.Parameters.AddWithValue("@Id", jsonKeyu);
                var jsonLength = await verifyCommand.ExecuteScalarAsync();
                LoggerService.Log($"Vérification : longueur du JSON enregistré = {jsonLength}");
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Sauvegarde en base de données");
                throw;
            }
        }

        private async void TestApiConnectionButton_Click(object sender, EventArgs e)
        {
            try
            {
                var apiId = comboBoxApi.SelectedItem.ToString();
                var endpointName = comboBoxTables.SelectedItem.ToString();

                string result = await _genericApiService.TestApiConnectionAsync(apiId, endpointName);
                MessageBox.Show($"Réponse de l'API :\n{result}", "Test de connexion");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur de connexion");
            }
        }
    }
}