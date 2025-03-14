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
using POMsag.Styles;
using POMsag.Controls;

namespace POMsag
{
    public partial class Form1 : Form
    {
        private HttpClient _httpClient;
        private readonly AppConfiguration _configuration;
        private string _destinationConnectionString;
        private DynamicsApiService _dynamicsApiService;
        private GenericApiService _genericApiService;
        private SchemaAnalysisService _schemaAnalysisService;
        private bool _isTransferInProgress = false;
        private Panel progressPanel;

        // Composants d'UI modernisés
        private NotificationPanel _notificationPanel;
        private List<DashboardPanel> _dashboardWidgets = new List<DashboardPanel>();

        // Exposer le service d'analyse de schéma pour être réutilisé
        public SchemaAnalysisService SchemaAnalysisService => _schemaAnalysisService;

        public Form1()
        {
            InitializeComponent();

            // Initialiser la configuration
            _configuration = new AppConfiguration();

            // Initialiser le client HTTP standard et le service D365
            InitializeHttpClient();

            // Initialiser le service API générique
            _genericApiService = new GenericApiService(_configuration, _httpClient, _dynamicsApiService);

            // Initialiser le service d'analyse de schéma
            _schemaAnalysisService = new SchemaAnalysisService(_dynamicsApiService, _httpClient, _configuration);

            // Initialiser les contrôles
            InitializeControls();

            // Ajouter le tableau de bord
            //AddDashboard();

            // Initialiser le panel de notification
            _notificationPanel = new NotificationPanel(this);
            this.Controls.Add(_notificationPanel);

            // Afficher un message de bienvenue
            _notificationPanel.ShowNotification("Bienvenue dans l'application de transfert de données POM",
                NotificationPanel.NotificationType.Info);
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
            // Mise à jour des choix pour inclure les tables D365
            comboBoxTables.Items.Clear();
            comboBoxTables.Items.AddRange(new string[]
            {
                "Clients",
                "Commandes",
                "Produits",
                "LignesCommandes",
                // Nouvelles entités D365
                "ReleasedProductsV2"
            });

            // Valeurs par défaut
            dateTimePickerStart.Value = DateTime.Now.AddMonths(-1);
            dateTimePickerEnd.Value = DateTime.Now;

            // État initial
            buttonTransfer.Enabled = false;
            dateTimePickerStart.Enabled = false;
            dateTimePickerEnd.Enabled = false;
        }

        private void AddDashboard()
        {
            // Conteneur pour le tableau de bord
            var dashboardPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                Padding = new Padding(20),
                BackColor = ThemeColors.PrimaryBackground
            };

            // Widgets du tableau de bord
            var transfersWidget = new DashboardPanel
            {
                Title = "Transferts totaux",
                Value = "0",
                InfoText = "Transferts effectués aujourd'hui"
            };
            _dashboardWidgets.Add(transfersWidget);

            var recordsWidget = new DashboardPanel
            {
                Title = "Enregistrements",
                Value = "0",
                InfoText = "Données transférées aujourd'hui"
            };
            _dashboardWidgets.Add(recordsWidget);

            var statusWidget = new DashboardPanel
            {
                Title = "État du système",
                Value = "En ligne",
                InfoText = "Tous les services sont actifs"
            };
            _dashboardWidgets.Add(statusWidget);

            // Disposition des widgets
            transfersWidget.Location = new Point(20, 20);
            recordsWidget.Location = new Point(290, 20);
            statusWidget.Location = new Point(560, 20);

            // Ajouter les widgets au tableau de bord
            dashboardPanel.Controls.Add(transfersWidget);
            dashboardPanel.Controls.Add(recordsWidget);
            dashboardPanel.Controls.Add(statusWidget);

            // Ajouter le tableau de bord au formulaire principal
            this.Controls.Add(dashboardPanel);

            // Mettre à jour les stats initiales
            UpdateDashboardStats();
        }

        private void UpdateDashboardStats()
        {
            try
            {
                // On récupère les statistiques
                int transferCount = GetTodayTransferCount();
                int recordCount = GetTodayRecordsCount();
                bool servicesOnline = CheckServicesStatus();

                // Mise à jour des widgets
                if (_dashboardWidgets.Count >= 3)
                {
                    _dashboardWidgets[0].Value = transferCount.ToString();
                    _dashboardWidgets[1].Value = recordCount.ToString("N0"); // Format avec séparateurs de milliers

                    _dashboardWidgets[2].Value = servicesOnline ? "En ligne" : "Hors ligne";
                    _dashboardWidgets[2].InfoText = servicesOnline
                        ? "Tous les services sont actifs"
                        : "Un ou plusieurs services sont indisponibles";
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, "Mise à jour du tableau de bord");
            }
        }

        // Méthodes pour les statistiques du tableau de bord
        private int GetTodayTransferCount()
        {
            // Dans une implémentation complète, vous récupéreriez ces données depuis la base
            try
            {
                using var connection = new SqlConnection(_destinationConnectionString);
                connection.Open();

                string query = @"
                    SELECT COUNT(DISTINCT SourceTable) 
                    FROM JSON_DAT 
                    WHERE CreatedAt = @today";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@today", DateTime.Now.ToString("yyyyMMdd"));

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }
            catch
            {
                // Silencieusement ignorer les erreurs pour ne pas bloquer l'UI
            }

            return 0;
        }

        private int GetTodayRecordsCount()
        {
            // Récupérer le nombre d'enregistrements pour aujourd'hui
            try
            {
                using var connection = new SqlConnection(_destinationConnectionString);
                connection.Open();

                string query = @"
                    SELECT COUNT(*) 
                    FROM JSON_DAT 
                    WHERE CreatedAt = @today";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@today", DateTime.Now.ToString("yyyyMMdd"));

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }
            catch
            {
                // Silencieusement ignorer les erreurs pour ne pas bloquer l'UI
            }

            return 0;
        }

        private bool CheckServicesStatus()
        {
            // Vérifier si les services sont disponibles
            try
            {
                // Vérifier si la base de données est accessible
                using var connection = new SqlConnection(_destinationConnectionString);
                connection.Open();

                // Si on arrive ici, la connexion est OK
                return true;
            }
            catch
            {
                return false;
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

                // Réinitialiser le statut de connexion
                if (connectionStatusLabel != null)
                    connectionStatusLabel.Text = "État: Non testé";

                // Afficher une notification
                _notificationPanel.ShowNotification("Configuration mise à jour",
                    NotificationPanel.NotificationType.Info);
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
                _notificationPanel.ShowNotification("Veuillez d'abord sélectionner une table.",
                    NotificationPanel.NotificationType.Warning);
                return;
            }

            string selectedTable = comboBoxTables.SelectedItem.ToString();
            string source = selectedTable == "ReleasedProductsV2" ? "dynamics" : "pom";

            // Désactiver le bouton pendant le test
            var button = (RoundedButton)sender;
            button.Enabled = false;
            button.Text = "Test en cours...";

            // Mettre à jour le libellé d'état
            if (connectionStatusLabel != null)
                connectionStatusLabel.Text = "État: Test en cours...";

            try
            {
                ShowStatus($"Test de connexion à {source} ({selectedTable})...", StatusType.Info);

                if (source == "dynamics")
                {
                    // Test de connexion Dynamics 365
                    await _dynamicsApiService.GetTokenAsync(); // Test d'authentification
                    var testResult = await _dynamicsApiService.GetReleasedProductsAsync(null, null);

                    if (connectionStatusLabel != null)
                    {
                        connectionStatusLabel.Text = $"État: Connecté à Dynamics 365 • {testResult.Count} enreg.";
                        connectionStatusLabel.ForeColor = ThemeColors.SuccessColor;
                    }

                    _notificationPanel.ShowNotification(
                        $"Connexion à Dynamics 365, {testResult.Count} enregistrement(s) récupéré(s)",
                        NotificationPanel.NotificationType.Success
                    );
                }
                else
                {
                    // Test de connexion API POM
                    var response = await _httpClient.GetAsync($"api/{selectedTable.ToLower()}?limit=1");
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    if (connectionStatusLabel != null)
                    {
                        connectionStatusLabel.Text = $"État: Connecté à l'API POM • {content.Length} car.";
                        connectionStatusLabel.ForeColor = ThemeColors.SuccessColor;
                    }

                    _notificationPanel.ShowNotification(
                        $"Connexion à l'API POM réussie, {content.Length} caractères reçus",
                        NotificationPanel.NotificationType.Success
                    );
                }

                ShowStatus($"Test de connexion réussi!", StatusType.Success);
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Test de connexion {source}/{selectedTable}");

                if (connectionStatusLabel != null)
                {
                    connectionStatusLabel.Text = $"État: Échec de connexion";
                    connectionStatusLabel.ForeColor = ThemeColors.ErrorColor;
                }

                ShowStatus($"Erreur lors du test de connexion: {ex.Message}", StatusType.Error);
                _notificationPanel.ShowNotification(
                    $"Échec de connexion: {ex.Message.Substring(0, Math.Min(100, ex.Message.Length))}",
                    NotificationPanel.NotificationType.Error,
                    false
                );
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
            progressBar.Visible = true;
            AnimationEffects.FadeIn(statusPanel);

            LoggerService.Log($"Début du transfert pour: {selectedTable}");
            ShowStatus($"Transfert en cours pour {selectedTable}...", StatusType.Info);

            try
            {
                // Déterminer la source des données (Dynamics ou API POM)
                string source = selectedTable == "ReleasedProductsV2" ? "dynamics" : "pom";

                DateTime? startDate = checkBoxDateFilter.Checked ? dateTimePickerStart.Value : (DateTime?)null;
                DateTime? endDate = checkBoxDateFilter.Checked ? dateTimePickerEnd.Value : (DateTime?)null;

                ShowStatus($"Récupération des données depuis {source} ({selectedTable})...", StatusType.Info);

                // Mettre à jour la barre de progression
                progressBar.Value = 10;

                // Notification de démarrage
                _notificationPanel.ShowNotification(
                    $"Transfert démarré pour {selectedTable}",
                    NotificationPanel.NotificationType.Info
                );

                // Récupérer les données avec le service générique
                List<Dictionary<string, object>> data = await _genericApiService.FetchDataAsync(source, selectedTable, startDate, endDate);

                // Mise à jour du progrès
                progressBar.Value = 50;

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
                    });

                // Affichage terminé
                progressBar.Value = 100;

                // Modification ici - N'afficher le message de succès qu'une seule fois
                ShowStatus("Enregistrement terminé avec succès.", StatusType.Success);

                // Notification de succès avec animation
                _notificationPanel.ShowNotification(
                    $"Transfert réussi : {filteredData.Count} enregistrements transférés",
                    NotificationPanel.NotificationType.Success
                );

                // Mettre à jour les statistiques du tableau de bord
                UpdateDashboardStats();

                LoggerService.Log($"Transfert terminé avec succès pour {selectedTable}. {filteredData.Count} enregistrements.");
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Transfert {selectedTable}");
                ShowStatus($"Erreur lors du transfert: {ex.Message}", StatusType.Error);

                // Notification d'erreur
                _notificationPanel.ShowNotification(
                    $"Erreur lors du transfert: {ex.Message.Substring(0, Math.Min(80, ex.Message.Length))}...",
                    NotificationPanel.NotificationType.Error,
                    false
                );
            }
            finally
            {
                // Marquer la fin du transfert
                _isTransferInProgress = false;
                buttonTransfer.Enabled = comboBoxTables.SelectedItem != null;

                // Cacher la barre de progression après quelques secondes
                await Task.Delay(3000);
                AnimationEffects.FadeOut(progressBar);
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

        private void ShowStatus(string message, StatusType type = StatusType.Info)
        {
            Color textColor;
            string prefix;

            switch (type)
            {
                case StatusType.Error:
                    textColor = ThemeColors.ErrorColor;
                    prefix = "ERREUR: ";
                    break;
                case StatusType.Warning:
                    textColor = ThemeColors.WarningColor;
                    prefix = "ATTENTION: ";
                    break;
                case StatusType.Success:
                    textColor = ThemeColors.SuccessColor;
                    prefix = "SUCCÈS: ";
                    break;
                default:
                    textColor = ThemeColors.PrimaryText;
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
                _notificationPanel.ShowNotification(
                    "Aucun fichier de log n'existe encore.",
                    NotificationPanel.NotificationType.Warning
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
                _notificationPanel.ShowNotification(
                    $"Impossible d'ouvrir le fichier de log: {ex.Message}",
                    NotificationPanel.NotificationType.Error
                );
            }
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            _notificationPanel.ShowNotification(
                "POM SAG - Outil de Transfert de Données v2.5",
                NotificationPanel.NotificationType.Info
            );

            // Créer une form "À propos" plus jolie
            var aboutForm = new Form
            {
                Text = "À propos",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = ThemeColors.PrimaryBackground,
                Font = new Font("Segoe UI", 10)
            };

            var titleLabel = new Label
            {
                Text = "POM SAG - Outil de Transfert de Données",
                Font = new Font("Segoe UI Light", 16, FontStyle.Regular),
                ForeColor = ThemeColors.PrimaryText,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var versionLabel = new Label
            {
                Text = "Version 2.5",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeColors.AccentColor,
                AutoSize = true,
                Location = new Point(20, 50)
            };

            var descriptionLabel = new Label
            {
                Text = "Application permettant de transférer des données depuis:\n" +
                    "- L'API POM\n" +
                    "- Dynamics 365 Finance & Operations\n\n" +
                    "Améliorations v2.5:\n" +
                    "- Sélection des champs à transférer\n" +
                    "- Service API générique\n" +
                    "- Test de connexion\n" +
                    "- Traitement des erreurs amélioré\n" +
                    "- Interface utilisateur améliorée",
                Location = new Point(20, 90),
                Size = new Size(440, 200),
                ForeColor = ThemeColors.PrimaryText
            };

            var closeButton = new RoundedButton
            {
                Text = "Fermer",
                Location = new Point(200, 300),
                Size = new Size(100, 40),
                IsPrimary = true,
                BorderRadius = 8
            };
            closeButton.Click += (s, ev) => aboutForm.Close();

            aboutForm.Controls.Add(titleLabel);
            aboutForm.Controls.Add(versionLabel);
            aboutForm.Controls.Add(descriptionLabel);
            aboutForm.Controls.Add(closeButton);

            aboutForm.ShowDialog(this);
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