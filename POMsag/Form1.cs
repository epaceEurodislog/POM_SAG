using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Data.SqlClient;

namespace POMsag
{
    public partial class Form1 : Form
    {
        private HttpClient _httpClient;
        private readonly AppConfiguration _configuration;
        private string _destinationConnectionString;

        public Form1()
        {
            InitializeComponent();

            // Initialiser la configuration
            _configuration = new AppConfiguration();

            // Initialiser le client HTTP
            InitializeHttpClient();

            // Initialiser les contrôles
            InitializeControls();
        }

        private void InitializeHttpClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_configuration.ApiUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _configuration.ApiKey);
            _destinationConnectionString = _configuration.DatabaseConnectionString;
        }

        private void InitializeControls()
        {
            // Réintégration de tous les choix
            comboBoxTables.Items.Clear(); // Nettoyer d'abord les éléments existants
            comboBoxTables.Items.AddRange(new string[]
            {
        "Clients",
        "Commandes",
        "Produits",
        "LignesCommandes"
            });

            // Le reste du code reste le même
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
            try
            {
                string selectedTable = comboBoxTables.SelectedItem.ToString().ToLower();
                buttonTransfer.Enabled = false;
                progressBar.Visible = true;

                List<Dictionary<string, object>> data;
                if (checkBoxDateFilter.Checked)
                {
                    var startDate = dateTimePickerStart.Value.ToString("yyyyMMdd");
                    var endDate = dateTimePickerEnd.Value.ToString("yyyyMMdd");
                    data = await FetchDataFromApiAsync($"{selectedTable}?startDate={startDate}&endDate={endDate}");
                }
                else
                {
                    data = await FetchDataFromApiAsync(selectedTable);
                }

                await SaveToDestinationDbAsync(data, selectedTable);

                ShowSuccessMessage($"Transfert réussi ! {data.Count} enregistrements transférés.");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Erreur : {ex.Message}");
            }
            finally
            {
                buttonTransfer.Enabled = true;
                progressBar.Visible = false;
            }
        }

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

        private async Task SaveToDestinationDbAsync(List<Dictionary<string, object>> data, string tableName)
        {
            try
            {
                string jsonData = JsonSerializer.Serialize(data);
                string formattedDate = DateTime.Now.ToString("yyyyMMdd");

                using var connection = new SqlConnection(_destinationConnectionString);
                await connection.OpenAsync();

                var checkTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JSON_DAT')
                    CREATE TABLE JSON_DAT (
                        JsonContent NVARCHAR(MAX) NOT NULL,
                        CreatedAt VARCHAR(8) NOT NULL,
                        SourceTable VARCHAR(50) NOT NULL
                    )";

                using (var command = new SqlCommand(checkTableQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                var query = @"
                    INSERT INTO JSON_DAT (JsonContent, CreatedAt, SourceTable) 
                    VALUES (@JsonContent, @CreatedAt, @SourceTable)";

                using var insertCommand = new SqlCommand(query, connection);
                insertCommand.Parameters.AddWithValue("@JsonContent", jsonData);
                insertCommand.Parameters.AddWithValue("@CreatedAt", formattedDate);
                insertCommand.Parameters.AddWithValue("@SourceTable", tableName);

                await insertCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Erreur lors de la sauvegarde en base de données : {ex.Message}");
                throw;
            }
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
    }
}