using Algora.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IAiProductDescriptionService
    {
        Task<ProductDescriptionDto> GenerateDescriptionAsync(string title, string category, string color, string material, string features);
    }
}
