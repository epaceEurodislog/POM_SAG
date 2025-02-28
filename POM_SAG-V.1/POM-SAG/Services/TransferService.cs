using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataTransferWeb.Services
{
    public class TransferService : ITransferService
    {
        private readonly HttpClient _httpClient;
        private readonly string _destinationConnectionString;

        public TransferService(IConfiguration configuration)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(configuration["TransferSettings:ApiUrl"])
            };
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", configuration["TransferSettings:ApiKey"]);
            _destinationConnectionString = configuration["TransferSettings:DestinationConnectionString"];
        }

        public async Task<List<Dictionary<string, object>>> FetchDataFromApiAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync($"api/{endpoint}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
        }

        public async Task SaveToDestinationDbAsync(List<Dictionary<string, object>> data, string tableName)
        {
            // Convertir les données en JSON
            string jsonData = JsonSerializer.Serialize(data);
            string formattedDate = DateTime.Now.ToString("yyyyMMdd");

            using var connection = new SqlConnection(_destinationConnectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO JsonData (JsonContent, CreatedAt, SourceTable) 
                VALUES (@JsonContent, @CreatedAt, @SourceTable)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@JsonContent", jsonData);
            command.Parameters.AddWithValue("@CreatedAt", formattedDate);
            command.Parameters.AddWithValue("@SourceTable", tableName);

            await command.ExecuteNonQueryAsync();
        }

        public async Task NotifyApiSuccessAsync()
        {
            await _httpClient.PostAsync("api/transferstatus", new StringContent(
                JsonSerializer.Serialize(new { Status = "Success" }),
                Encoding.UTF8,
                "application/json"));
        }
    }
}