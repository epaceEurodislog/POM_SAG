using System;
using System.Net.Http;
using Microsoft.Data.SqlClient; // Corriger l'import ici
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace POMsag.Services
{
    public static class ErrorHandlingService
    {
        private static class ColorPalette
        {
            public static Color ErrorColor = Color.FromArgb(229, 62, 62); // Rouge pour erreurs
            public static Color WarningColor = Color.FromArgb(237, 137, 54); // Orange pour avertissements
            public static Color InfoColor = Color.FromArgb(49, 130, 206); // Bleu pour informations
        }

        public static void HandleError(Exception ex, string context, Form? parentForm = null)
        {
            // Journaliser l'erreur
            LoggerService.LogException(ex, context);

            // Déterminer le type d'erreur
            string title = "Erreur";
            string message = ex.Message;
            MessageBoxIcon icon = MessageBoxIcon.Error;

            // Personnaliser le message selon le type d'erreur
            if (ex is HttpRequestException httpEx)
            {
                title = "Erreur de communication";
                message = $"Une erreur de communication s'est produite lors de l'appel API:\n\n{httpEx.Message}";

                // Ajouter des conseils selon le code HTTP si disponible
                if (httpEx.StatusCode.HasValue)
                {
                    int statusCode = (int)httpEx.StatusCode.Value;
                    message += $"\n\nCode d'erreur HTTP: {statusCode}";

                    switch (statusCode)
                    {
                        case 401:
                            message += "\n\nConseil: Vérifiez vos identifiants d'authentification dans la configuration.";
                            break;
                        case 403:
                            message += "\n\nConseil: Vous n'avez pas les autorisations nécessaires pour accéder à cette ressource.";
                            break;
                        case 404:
                            message += "\n\nConseil: L'URL de l'API ou l'endpoint demandé n'existe pas. Vérifiez la configuration.";
                            break;
                        case 500:
                            message += "\n\nConseil: Une erreur interne s'est produite sur le serveur. Contactez l'administrateur de l'API.";
                            break;
                        case 503:
                            message += "\n\nConseil: Le service est temporairement indisponible. Réessayez plus tard.";
                            break;
                    }
                }
            }
            else if (ex is SqlException sqlEx)
            {
                title = "Erreur de base de données";
                message = $"Une erreur de base de données s'est produite:\n\n{sqlEx.Message}";

                // Ajouter des conseils selon le code d'erreur SQL
                switch (sqlEx.Number)
                {
                    case 4060:
                        message += "\n\nConseil: La base de données spécifiée n'existe pas. Vérifiez le nom de la base de données.";
                        break;
                    case 18456:
                        message += "\n\nConseil: Échec de connexion pour l'utilisateur. Vérifiez le nom d'utilisateur et le mot de passe.";
                        break;
                    case 2627:
                        message += "\n\nConseil: Violation de contrainte d'unicité. Un enregistrement avec la même clé existe déjà.";
                        break;
                    case 547:
                        message += "\n\nConseil: Violation de contrainte d'intégrité référentielle.";
                        break;
                    case 53:
                        message += "\n\nConseil: Impossible de se connecter au serveur. Vérifiez le nom du serveur et la connectivité réseau.";
                        break;
                }
            }
            else if (ex is JsonException jsonEx)
            {
                title = "Erreur de format de données";
                message = $"Une erreur s'est produite lors du traitement des données JSON:\n\n{jsonEx.Message}\n\nConseil: Le format des données reçues est peut-être incorrect ou inattendu.";
                icon = MessageBoxIcon.Warning;
            }

            // Si une exception interne existe, l'ajouter au message
            if (ex.InnerException != null)
            {
                message += $"\n\nDétails supplémentaires:\n{ex.InnerException.Message}";
            }

            // Afficher la boîte de dialogue d'erreur
            if (parentForm != null && !parentForm.IsDisposed && parentForm.InvokeRequired)
            {
                parentForm.Invoke(new Action(() =>
                {
                    MessageBox.Show(parentForm, message, title, MessageBoxButtons.OK, icon);
                }));
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
            }
        }

        // Méthode pour afficher une erreur dans un label (utile pour les formulaires)
        public static void ShowErrorInLabel(Label label, string errorMessage)
        {
            if (label == null) return;

            if (label.InvokeRequired)
            {
                label.Invoke(new Action(() =>
                {
                    label.Text = errorMessage;
                    label.ForeColor = ColorPalette.ErrorColor;
                    label.Visible = true;
                }));
            }
            else
            {
                label.Text = errorMessage;
                label.ForeColor = ColorPalette.ErrorColor;
                label.Visible = true;
            }
        }

        // Méthode pour effacer un message d'erreur d'un label
        public static void ClearErrorInLabel(Label label)
        {
            if (label == null) return;

            if (label.InvokeRequired)
            {
                label.Invoke(new Action(() =>
                {
                    label.Text = "";
                    label.Visible = false;
                }));
            }
            else
            {
                label.Text = "";
                label.Visible = false;
            }
        }
    }
}