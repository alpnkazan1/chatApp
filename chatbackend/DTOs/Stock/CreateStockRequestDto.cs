using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Stock
{
    public class CreateStockRequestDto
    {
        [Required]
        [MaxLength(10, ErrorMessage = "No stock symbol over 10 characters")]
        public string Symbol { get; set; } = string.Empty;
        [Required]
        [MaxLength(100, ErrorMessage = "Company name cannot be over 100 characters")]
        public string CompanyName { get; set; } = string.Empty;
        [Required]
        [Range(1,1000000000)]
        public decimal Purchase { get; set; }
        [Required]
        [Range(0.001,100)]
        public decimal LastDiv { get; set; }
        [Required]
        [MaxLength(100,ErrorMessage = "Industry name cannot exceed 100 characters")]
        public string Industry { get; set; } = string.Empty;
        public long MarketCap { get; set; }
    }
}