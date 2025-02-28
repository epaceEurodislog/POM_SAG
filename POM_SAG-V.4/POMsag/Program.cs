using POMsag.Services;
using System.Net.Http;

namespace POMsag;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Configuration standard de l'application
        ApplicationConfiguration.Initialize();

        try
        {
            // Initialiser la configuration
            var configuration = new AppConfiguration();

            // Créer les services
            var dynamicsApiService = new DynamicsApiService(
                configuration.TokenUrl,
                configuration.ClientId,
                configuration.ClientSecret,
                configuration.Resource,
                configuration.DynamicsApiUrl,
                configuration.MaxRecords);

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(configuration.ApiUrl)
            };
            if (!string.IsNullOrEmpty(configuration.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", configuration.ApiKey);
            }

            var schemaAnalysisService = new SchemaAnalysisService(
                dynamicsApiService,
                httpClient,
                configuration);

            // Lancer l'application avec les services
            Application.Run(new Form1(configuration, dynamicsApiService, httpClient, schemaAnalysisService));
        }
        catch (Exception ex)
        {
            // Journal de l'erreur
            if (LoggerService.IsInitialized)
            {
                LoggerService.LogException(ex, "Démarrage de l'application");
            }

            // Afficher un message d'erreur
            MessageBox.Show(
                $"Une erreur inattendue s'est produite lors du démarrage de l'application :\n{ex.Message}\n\nConsultez les logs pour plus de détails.",
                "Erreur de démarrage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}