using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POMsag.Models;
#pragma warning disable CS8618, CS8625, CS8600, CS8602, CS8603, CS8604, CS8601

namespace POMsag.Services
{
    public interface IDynamicsApiService
    {
        Task<string> GetTokenAsync();
        Task<List<ReleasedProduct>> GetReleasedProductsAsync(DateTime? startDate = null, DateTime? endDate = null, string itemNumber = null);
    }
}