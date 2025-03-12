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

        public Form1()
        {
            InitializeComponent();

            // Initialiser la configuration
            _configuration = new AppConfiguration();

            // Initialiser le client HTTP standard et le service D365
            InitializeHttpClient();

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

            comboBoxTables.SelectedIndexChanged += ComboBoxTables_SelectedIndexChanged;
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

        private void ConfigButton_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConfigurationForm(_configuration))
            {
                configForm.ShowDialog();

                // Mettre à jour la configuration
                InitializeHttpClient();
            }
        }

        private void CheckBoxDateFilter_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePickerStart.Enabled = checkBoxDateFilter.Checked;
            dateTimePickerEnd.Enabled = checkBoxDateFilter.Checked;
        }

        private void ComboBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonTransfer.Enabled = comboBoxTables.SelectedItem != null;
        }

        private async void ButtonTransfer_Click(object sender, EventArgs e)
        {
            if (comboBoxTables.SelectedItem == null)
                return;

            string selectedTable = comboBoxTables.SelectedItem.ToString();
            buttonTransfer.Enabled = false;
            progressBar.Visible = true;
            statusPanel.Visible = false;

            LoggerService.Log($"Début du transfert pour: {selectedTable}");
            ShowStatus($"Transfert en cours pour {selectedTable}...");

            try
            {
                List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();

                if (selectedTable == "ReleasedProductsV2")
                {
                    ShowStatus("Récupération des produits depuis Dynamics 365...");

                    DateTime? startDate = checkBoxDateFilter.Checked ? dateTimePickerStart.Value : (DateTime?)null;
                    DateTime? endDate = checkBoxDateFilter.Checked ? dateTimePickerEnd.Value : (DateTime?)null;

                    var products = await _dynamicsApiService.GetReleasedProductsAsync(
                        startPurchaseDate: startDate,
                        endPurchaseDate: endDate
                    );
                    ShowStatus($"Récupération terminée. {products.Count} produits trouvés.");

                    // Conversion avec toutes les propriétés
                    foreach (var product in products)
                    {
                        var dict = product.ToDictionary();
                        data.Add(dict);
                    }
                }
                else
                {
                    // Appel à l'API standard
                    string endpoint = selectedTable.ToLower();
                    ShowStatus($"Récupération des données depuis l'API standard ({endpoint})...");

                    if (checkBoxDateFilter.Checked)
                    {
                        var startDate = dateTimePickerStart.Value.ToString("yyyyMMdd");
                        var endDate = dateTimePickerEnd.Value.ToString("yyyyMMdd");
                        data = await FetchDataFromApiAsync($"{endpoint}?startDate={startDate}&endDate={endDate}");
                    }
                    else
                    {
                        data = await FetchDataFromApiAsync(endpoint);
                    }

                    ShowStatus($"Récupération terminée. {data.Count} enregistrements trouvés.");
                }

                ShowStatus("Enregistrement des données dans la base de destination...");
                await SaveToDestinationDbAsync(data, selectedTable);

                // Modification ici - N'afficher le message de succès qu'une seule fois
                ShowStatus("Enregistrement terminé avec succès.");
                ShowSuccessMessage($"Transfert réussi ! {data.Count} enregistrements transférés.");

                LoggerService.Log($"Transfert terminé avec succès pour {selectedTable}. {data.Count} enregistrements.");
            }
            catch (Exception ex)
            {
                LoggerService.LogException(ex, $"Transfert {selectedTable}");
                ShowErrorDetail($"Erreur lors du transfert de {selectedTable}", ex);
            }
            finally
            {
                buttonTransfer.Enabled = true;
                progressBar.Visible = false;
            }
        }

        // Les autres méthodes restent identiques (FetchDataFromApiAsync, SaveToDestinationDbAsync, etc.)
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

        // Les autres méthodes existantes (ShowErrorDetail, ShowSuccessMessage, etc.)
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

        // Les autres méthodes comme FetchDataFromApiAsync, SaveToDestinationDbAsync restent identiques
        private async Task<List<Dictionary<string, object>>> FetchDataFromApiAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/{endpoint}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Données reçues : {content}");

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

                Console.WriteLine($"Nombre d'éléments désérialisés : {result.Count}");
                return result;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Erreur lors de la récupération des données : {ex.Message}");
                throw;
            }
        }

        // Méthodes restantes : ViewLogs_Click, AboutMenuItem_Click, etc.
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
                "Version 2.0\n\n" +
                "Application permettant de transférer des données depuis:\n" +
                "- L'API POM\n" +
                "- Dynamics 365 Finance & Operations\n\n",
                "À propos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // Méthodes privées restantes
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