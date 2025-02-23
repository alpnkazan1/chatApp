using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.DTOs.Stock;
using chatbackend.Helpers;
using chatbackend.Models;

namespace chatbackend.Interfaces
{
    public interface IStockRepository
    {
        Task<List<Stock>> GetAllAsync(QueryObject query);
        Task<Stock?> GetByIdAsync(int id);
        Task<Stock> CreateAsync(Stock stockModel);
        Task<Stock?> UpdateAsync(int id, UpdateStockRequestDto stockDto);
        Task<Stock?> DeleteByAsync(int id);
        Task<bool> StockExists(int id);
    }
}