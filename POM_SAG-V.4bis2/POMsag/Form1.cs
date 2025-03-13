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
using System.Linq;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag
{
    public partial class Form1 : Form
    {
        private HttpClient _httpClient;
        private readonly AppConfiguration _configuration;
        private string _destinationConnectionString;
        private GenericApiService _genericApiService;
        private SchemaAnalysisService _schemaAnalysisService;
        private bool _isTransferInProgress = false;
        private Panel progressPanel;
        private ApiManager _apiManager;
        private DynamicsApiService _dynamicsApiService;
        private ToolStripMenuItem _apiManagerMenuItem;

        // Exposer le service d'analyse de schéma pour être réutilisé
        public SchemaAnalysisService SchemaAnalysisService => _schemaAnalysisService;

        public Form1()
        {
            InitializeComponent();

            // Initialiser la configuration
            _configuration = new AppConfiguration();

            // Créer le répertoire des APIs s'il n'existe pas
            if (!Directory.Exists("APIs"))
            {
                Directory.CreateDirectory("APIs");
            }

            // Initialiser le gestionnaire d'API
            _apiManager = new ApiManager();
            _dynamicsApiService = new DynamicsApiService(_apiManager);
            // Initialiser le client HTTP standard et le service D365
            InitializeHttpClient();

            // Initialiser le service API générique
            _genericApiService = new GenericApiService(_configuration, _httpClient, _dynamicsApiService);

            // Initialiser le service d'analyse de schéma
            _schemaAnalysisService = new SchemaAnalysisService(_dynamicsApiService, _httpClient, _configuration);

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
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _configuration.ApiKey);
            _destinationConnectionString = _configuration.DatabaseConnectionString;

            _dynamicsApiService = new DynamicsApiService(_apiManager);
        }

        private void ApiManagerMenuItem_Click(object sender, EventArgs e)
        {
            using (var apiManagerForm = new ApiManagerForm(_apiManager))
            {
                apiManagerForm.ShowDialog();

                // Mettre à jour les sources si nécessaire
                LoadApiSourcesInComboBox();
            }
        }

        private void InitializeControls()
        {
            // Ajoutez ceci dans le menu Fichier
            _apiManagerMenuItem = new ToolStripMenuItem("Gestionnaire d'API");
            _apiManagerMenuItem.Click += ApiManagerMenuItem_Click;
            fileMenuItem.DropDownItems.Insert(1, _apiManagerMenuItem);

            // Mise à jour des choix pour inclure les tables D365
            comboBoxTables.Items.Clear();
            LoadApiSourcesInComboBox();

            // Bouton de test de connexion API
            var buttonTestConnection = new Button
            {
                Text = "Tester la connexion",
                Size = new Size(150, 35),
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.PrimaryText,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 390) // Ajustez la position selon votre layout
            };
            buttonTestConnection.FlatAppearance.BorderSize = 1;
            buttonTestConnection.FlatAppearance.BorderColor = ColorPalette.BorderColor;
            buttonTestConnection.Click += ButtonTestConnection_Click;

            // Amélioration du panneau de statut
            statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                BackColor = ColorPalette.SecondaryBackground,
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Titre du panneau de statut
            var statusTitlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = ColorPalette.AccentColor
            };

            var statusTitle = new Label
            {
                Text = "Journal d'exécution",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(5, 0, 0, 0)
            };

            // Bouton de fermeture du panneau
            var closeStatusButton = new Button
            {
                Text = "×",
                Dock = DockStyle.Right,
                Width = 30,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            closeStatusButton.Click += (s, e) => statusPanel.Visible = false;

            // Bouton pour effacer le journal
            var clearStatusButton = new Button
            {
                Text = "Effacer",
                Dock = DockStyle.Right,
                Width = 80,
                BackColor = ColorPalette.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            clearStatusButton.Click += (s, e) => statusTextBox.Clear();

            statusTitlePanel.Controls.Add(statusTitle);
            statusTitlePanel.Controls.Add(clearStatusButton);
            statusTitlePanel.Controls.Add(closeStatusButton);

            // Zone de texte de statut améliorée
            statusTextBox = new RichTextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BackColor = ColorPalette.WhiteBackground,
                ForeColor = ColorPalette.PrimaryText,
                Font = new Font("Consolas", 10),
                ScrollBars = RichTextBoxScrollBars.Vertical,
            };

            // Assembler le panneau de statut
            statusPanel.Controls.Add(statusTextBox);
            statusPanel.Controls.Add(statusTitlePanel);
            statusPanel.Visible = false;

            // Barre de progression améliorée
            progressPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(10, 8, 10, 8)
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Maximum = 100,
                BackColor = ColorPalette.SecondaryBackground,
                ForeColor = ColorPalette.AccentColor
            };

            var progressLabel = new Label
            {
                Dock = DockStyle.Right,
                Width = 60,
                Text = "0%",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = ColorPalette.PrimaryText
            };

            progressPanel.Controls.Add(progressBar);
            progressPanel.Controls.Add(progressLabel);
            progressPanel.Visible = false;

            // Attacher les gestionnaires d'événements
            comboBoxTables.SelectedIndexChanged += ComboBoxTables_SelectedIndexChanged;
            checkBoxDateFilter.CheckedChanged += CheckBoxDateFilter_CheckedChanged;
            buttonTransfer.Click += ButtonTransfer_Click;

            // Ajouter les contrôles au formulaire
            this.Controls.Add(buttonTestConnection);
            this.Controls.Add(statusPanel);
            this.Controls.Add(progressPanel);

            // Valeurs par défaut
            dateTimePickerStart.Value = DateTime.Now.AddMonths(-1);
            dateTimePickerEnd.Value = DateTime.Now;

            // État initial
            buttonTransfer.Enabled = false;
            dateTimePickerStart.Enabled = false;
            dateTimePickerEnd.Enabled = false;
        }

        private void LoadApiSourcesInComboBox()
        {
            comboBoxTables.Items.Clear();

            // Ajouter les sources traditionnelles (pour la rétrocompatibilité)
            comboBoxTables.Items.Add("Clients");
            comboBoxTables.Items.Add("Commandes");
            comboBoxTables.Items.Add("Produits");
            comboBoxTables.Items.Add("LignesCommandes");
            comboBoxTables.Items.Add("ReleasedProductsV2");

            // Ajouter les endpoints des API dynamiques
            var apis = _apiManager.GetAllApis();
            foreach (var api in apis)
            {
                foreach (var endpoint in api.Endpoints)
                {
                    comboBoxTables.Items.Add($"{api.Name}:{endpoint.Name}");
                }
            }
        }

        private void ConfigButton_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConfigurationForm(_configuration, _schemaAnalysisService))
            {
                configForm.ShowDialog();

                // Mettre à jour la configuration
                InitializeHttpClient();

                // Recréer le service générique avec la nouvelle configuration
                _genericApiService = new GenericApiService(_configuration, _httpClient, _dynamicsApiService);
            }
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

        private async void ButtonTestConnection_Click(object sender, EventArgs e)
        {
            if (comboBoxTables.SelectedItem == null)
            {
                MessageBox.Show("Veuillez d'abord sélectionner une table.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedTable = comboBoxTables.SelectedItem.ToString();
            string source = selectedTable == "ReleasedProductsV2" ? "dynamics" : "pom";

            // Désactiver le bouton pendant le test
            var button = (Button)sender;
            button.Enabled = false;
            button.Text = "Test en cours...";

            try
            {
                ShowStatus($"Test de connexion à {source} ({selectedTable})...", StatusType.Info);

                if (source == "dynamics")
                {
                    // Test de connexion Dynamics 365
                    await _dynamicsApiService.GetTokenAsync(); // Test d'authentification
                    var testResult = await _dynamicsApiService.GetReleasedProductsAsync(null, null);

                    MessageBox.Show(
                        $"Connexion à Dynamics 365 réussie!\n\nServeur: {_configuration.DynamicsApiUrl}\nEndpoint: {selectedTable}\nTest: {testResult.Count} enregistrement(s) récupéré(s)",
                        "Test de connexion",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    // Test de connexion API POM
                    var response = await _httpClient.GetAsync($"api/{selectedTable.ToLower()}?limit=1");
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    MessageBox.Show(
                        $"Connexion à l'API POM réussie!\n\nServeur: {_configuration.ApiUrl}\nEndpoint: {selectedTable}\nRéponse: {content.Length} caractères",
                        "Test de connexion",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                ShowStatus($"Test de connexion réussi!", StatusType.Success);
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Test de connexion {source}/{selectedTable}");
                ShowStatus($"Erreur lors du test de connexion: {ex.Message}", StatusType.Error);
                ShowErrorDetail($"Test de connexion {source}/{selectedTable}", ex);
            }
            finally
            {
                // Réactiver le bouton
                button.Enabled = true;
                button.Text = "Tester la connexion";
            }
        }

        private async void ButtonTransfer_Click(object sender, EventArgs e)
        {
            if (comboBoxTables.SelectedItem == null || _isTransferInProgress)
                return;

            // Marquer le début du transfert
            _isTransferInProgress = true;
            string selectedTable = comboBoxTables.SelectedItem.ToString();
            buttonTransfer.Enabled = false;

            // Afficher la barre de progression et le statut
            progressBar.Value = 0;
            progressPanel.Visible = true;
            statusPanel.Visible = true;

            LoggerService.Log($"Début du transfert pour: {selectedTable}");
            ShowStatus($"Transfert en cours pour {selectedTable}...", StatusType.Info);

            try
            {
                DateTime? startDate = checkBoxDateFilter.Checked ? dateTimePickerStart.Value : (DateTime?)null;
                DateTime? endDate = checkBoxDateFilter.Checked ? dateTimePickerEnd.Value : (DateTime?)null;

                List<Dictionary<string, object>> data;

                // Vérifier si c'est une source API dynamique (format: ApiName:EndpointName)
                if (selectedTable.Contains(":"))
                {
                    var parts = selectedTable.Split(':');
                    string apiName = parts[0];
                    string endpointName = parts[1];

                    ShowStatus($"Récupération des données depuis l'API dynamique {apiName} (endpoint: {endpointName})...", StatusType.Info);

                    // Mettre à jour la barre de progression
                    progressBar.Value = 10;
                    UpdateProgressLabel();

                    var parts = selectedTable.Split(':');
                    string apiName = parts[0];
                    string endpoint = parts[1];
                    data = await _dynamicsApiService.FetchDataAsync(apiName, endpoint, startDate, endDate);
                }
                else
                {
                    // Déterminer la source des données (Dynamics ou API POM)
                    string source = selectedTable == "ReleasedProductsV2" ? "dynamics" : "pom";

                    ShowStatus($"Récupération des données depuis {source} ({selectedTable})...", StatusType.Info);

                    // Mettre à jour la barre de progression
                    progressBar.Value = 10;
                    UpdateProgressLabel();

                    // Récupérer les données avec le service générique
                    data = await _genericApiService.FetchDataAsync("dynamics", endpointName, startDate, endDate);
                }

                // Mise à jour du progrès
                progressBar.Value = 50;
                UpdateProgressLabel();

                ShowStatus($"Récupération terminée. {data.Count} enregistrements trouvés.", StatusType.Success);

                // Filtrer les champs selon les préférences
                ShowStatus("Filtrage des champs selon vos préférences...", StatusType.Info);
                var filteredData = new List<Dictionary<string, object>>();

                // Traitement par lot avec mise à jour de la progression
                int totalItems = data.Count;
                int processedItems = 0;
                int progressStep = Math.Max(1, totalItems / 20); // Mise à jour toutes les 5%

                foreach (var item in data)
                {
                    var filteredItem = FilterFieldsBasedOnPreferences(item, selectedTable);
                    filteredData.Add(filteredItem);

                    // Mise à jour de la progression toutes les 5%
                    processedItems++;
                    if (processedItems % progressStep == 0 || processedItems == totalItems)
                    {
                        int newProgress = 50 + (processedItems * 20 / totalItems);
                        progressBar.Value = Math.Min(70, newProgress);
                        UpdateProgressLabel();

                        if (processedItems % (progressStep * 5) == 0)
                        {
                            ShowStatus($"Filtrage en cours: {processedItems}/{totalItems} enregistrements traités.", StatusType.Info);
                        }
                    }
                }

                ShowStatus("Enregistrement des données dans la base de destination...", StatusType.Info);
                await SaveToDestinationDbAsync(filteredData, selectedTable,
                    progress =>
                    {
                        progressBar.Value = 70 + (int)(progress * 30); // de 70% à 100%
                        UpdateProgressLabel();
                    });

                // Affichage terminé
                progressBar.Value = 100;
                UpdateProgressLabel();

                // Modification ici - N'afficher le message de succès qu'une seule fois
                ShowStatus("Enregistrement terminé avec succès.", StatusType.Success);
                ShowSuccessMessage($"Transfert réussi ! {filteredData.Count} enregistrements transférés.");

                LoggerService.Log($"Transfert terminé avec succès pour {selectedTable}. {filteredData.Count} enregistrements.");
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Transfert {selectedTable}");
                ShowStatus($"Erreur lors du transfert: {ex.Message}", StatusType.Error);
                ShowErrorDetail($"Transfert {selectedTable}", ex);
            }
            finally
            {
                // Marquer la fin du transfert
                _isTransferInProgress = false;
                buttonTransfer.Enabled = comboBoxTables.SelectedItem != null;

                // Cacher la barre de progression après quelques secondes
                await Task.Delay(3000);
                progressPanel.Visible = false;
            }
        }

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

        // Méthode pour mettre à jour le label de progression
        private void UpdateProgressLabel()
        {
            var progressPanel = Controls.OfType<Panel>().FirstOrDefault(p => p.Contains(progressBar));
            if (progressPanel != null)
            {
                var label = progressPanel.Controls.OfType<Label>().FirstOrDefault();
                if (label != null)
                {
                    label.Text = $"{progressBar.Value}%";
                }
            }
        }

        private void ShowStatus(string message, StatusType type = StatusType.Info)
        {
            Color textColor;
            string prefix;

            switch (type)
            {
                case StatusType.Error:
                    textColor = Color.Red;
                    prefix = "ERREUR: ";
                    break;
                case StatusType.Warning:
                    textColor = Color.DarkOrange;
                    prefix = "ATTENTION: ";
                    break;
                case StatusType.Success:
                    textColor = Color.Green;
                    prefix = "SUCCÈS: ";
                    break;
                default:
                    textColor = ColorPalette.PrimaryText;
                    prefix = "";
                    break;
            }

            // Mise à jour du panneau d'état
            if (statusTextBox.InvokeRequired)
            {
                statusTextBox.Invoke(new Action(() =>
                {
                    int currentPosition = statusTextBox.TextLength;

                    // Ajouter le nouveau texte
                    statusTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {prefix}{message}{Environment.NewLine}");

                    // Sélectionner le texte ajouté pour changer sa couleur
                    statusTextBox.Select(currentPosition, statusTextBox.TextLength - currentPosition);
                    statusTextBox.SelectionColor = textColor;

                    // Désélectionner et défiler vers le bas
                    statusTextBox.SelectionStart = statusTextBox.TextLength;
                    statusTextBox.ScrollToCaret();
                    statusPanel.Visible = true;
                }));
            }
            else
            {
                int currentPosition = statusTextBox.TextLength;
                statusTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {prefix}{message}{Environment.NewLine}");
                statusTextBox.Select(currentPosition, statusTextBox.TextLength - currentPosition);
                statusTextBox.SelectionColor = textColor;
                statusTextBox.SelectionStart = statusTextBox.TextLength;
                statusTextBox.ScrollToCaret();
                statusPanel.Visible = true;
            }

            LoggerService.Log(message);
        }

        // Énumération pour les types de messages de statut
        public enum StatusType
        {
            Info,
            Success,
            Warning,
            Error
        }

        private void ShowErrorDetail(string context, Exception ex)
        {
            ShowStatus($"ERREUR: {ex.Message}", StatusType.Error);
            ErrorHandlingService.HandleError(ex, context, this);
        }

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

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "POM SAG - Outil de Transfert de Données\n" +
                "Version 2.5\n\n" +
                "Application permettant de transférer des données depuis:\n" +
                "- L'API POM\n" +
                "- Dynamics 365 Finance & Operations\n\n" +
                "Améliorations v2.5:\n" +
                "- Sélection des champs à transférer\n" +
                "- Service API générique\n" +
                "- Test de connexion\n" +
                "- Traitement des erreurs amélioré\n" +
                "- Interface utilisateur améliorée",
                "À propos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(
                message,
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(
                message,
                "Succès",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private async Task SaveToDestinationDbAsync(List<Dictionary<string, object>> data, string tableName, Action<double> progressCallback = null)
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

                // Créer une transaction pour assurer la cohérence
                using var transaction = connection.BeginTransaction();
                try
                {
                    int insertedCount = 0;
                    int totalCount = data.Count;

                    // Insérer chaque élément comme une ligne séparée
                    foreach (var item in data)
                    {
                        // Sérialiser l'élément individuel en JSON
                        var itemJson = JsonSerializer.Serialize(item, new JsonSerializerOptions { WriteIndented = false });

                        // Query d'insertion pour un élément
                        var query = @"
                        INSERT INTO JSON_DAT (JsonContent, CreatedAt, SourceTable) 
                        VALUES (@JsonContent, @CreatedAt, @SourceTable);";

                        using var insertCommand = new SqlCommand(query, connection, transaction);
                        insertCommand.Parameters.AddWithValue("@JsonContent", itemJson);
                        insertCommand.Parameters.AddWithValue("@CreatedAt", formattedDate);
                        insertCommand.Parameters.AddWithValue("@SourceTable", tableName);

                        await insertCommand.ExecuteNonQueryAsync();
                        insertedCount++;

                        // Mettre à jour la progression
                        if (insertedCount % 10 == 0 || insertedCount == totalCount)
                        {
                            double progress = (double)insertedCount / totalCount;
                            progressCallback?.Invoke(progress);

                            // Afficher la progression tous les 100 éléments
                            if (insertedCount % 100 == 0)
                            {
                                ShowStatus($"Insertion en cours... {insertedCount}/{totalCount} éléments traités", StatusType.Info);
                            }
                        }
                    }

                    // Valider la transaction après toutes les insertions
                    transaction.Commit();
                    LoggerService.Log($"Données insérées avec succès, {data.Count} éléments dans la table JSON_DAT");
                }
                catch (Exception ex)
                {
                    // En cas d'erreur, annuler la transaction
                    transaction.Rollback();
                    LoggerService.LogException(ex, "Erreur lors de l'insertion des données");
                    throw;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Erreur lors de la sauvegarde en base de données : {ex.Message}", StatusType.Error);
                throw;
            }
        }
    }
}