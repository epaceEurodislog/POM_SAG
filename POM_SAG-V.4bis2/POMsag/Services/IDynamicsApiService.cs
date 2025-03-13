using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POMsag.Models;

namespace POMsag.Services
{
    public interface IDynamicsApiService
    {
        Task<string> GetTokenAsync();
        Task<List<ReleasedProduct>> GetReleasedProductsAsync(DateTime? startDate = null, DateTime? endDate = null, string itemNumber = null);
    }
}