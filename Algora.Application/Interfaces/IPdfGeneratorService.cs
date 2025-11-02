using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algora.Application.Interfaces
{
    public interface IPdfGeneratorService
    {
        Task<byte[]> GeneratePdfAsync(string html);
    }
}
