using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Application.Interfaces
{
    public interface IDocumentSummaryService
    {
        /// <summary>
        /// Kivonja a szöveges tartalmat a megadott fájlból.
        /// </summary>
        /// <param name="file">Feltöltött fájl</param>
        /// <returns>A fájl szöveges tartalma</returns>
        Task<string> ExtractTextAsync(IFormFile file);
    }
}
