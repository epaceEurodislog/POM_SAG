using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataTransferWeb.Services
{
    public interface ITransferService
    {
        Task<List<Dictionary<string, object>>> FetchDataFromApiAsync(string endpoint);
        Task SaveToDestinationDbAsync(List<Dictionary<string, object>> data, string tableName);
        Task NotifyApiSuccessAsync();
    }
}