using System;
using System.Threading.Tasks;
using DataTransferWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataTransferWeb.Controllers
{
    public class TransferController : Controller
    {
        private readonly ITransferService _transferService;

        public TransferController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Preview(string tableName, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var data = await _transferService.FetchDataFromApiAsync(tableName);
                
                // Si des dates sont spécifiées, filtrer les données
                if (startDate.HasValue && endDate.HasValue)
                {
                    // Note: La logique de filtrage dépendra de la structure de vos données
                    // Implémentez selon vos besoins
                }

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> StartTransfer(string tableName, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // Récupération des données depuis l'API
                var data = await _transferService.FetchDataFromApiAsync(tableName);

                // Filtrage par date si nécessaire
                if (startDate.HasValue && endDate.HasValue)
                {
                    // Note: La logique de filtrage dépendra de la structure de vos données
                    // Implémentez selon vos besoins
                }

                // Sauvegarde dans la base de destination
                await _transferService.SaveToDestinationDbAsync(data, tableName);

                // Notification à l'API du succès
                await _transferService.NotifyApiSuccessAsync();

                return Json(new 
                { 
                    success = true, 
                    message = $"Transfert réussi pour {tableName}. {data.Count} enregistrements transférés." 
                });
            }
            catch (Exception ex)
            {
                return Json(new 
                { 
                    success = false, 
                    message = $"Erreur lors du transfert : {ex.Message}" 
                });
            }
        }

        [HttpGet]
        public IActionResult GetTransferHistory()
        {
            try
            {
                // Cette méthode devra être implémentée si vous souhaitez
                // garder un historique des transferts
                return Json(new { success = true, history = Array.Empty<object>() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidateConnection()
        {
            try
            {
                // Test de connexion à l'API
                await _transferService.FetchDataFromApiAsync("test");
                return Json(new { success = true, message = "Connexion à l'API réussie" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur de connexion : {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelTransfer(string transferId)
        {
            try
            {
                // Implémentez la logique d'annulation si nécessaire
                return Json(new { success = true, message = "Transfert annulé" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}