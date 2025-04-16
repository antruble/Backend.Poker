using Backend.Poker.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using Spire.Doc;

namespace Backend.Poker.Application.Services
{
    public class DocumentSummaryAppService : IDocumentSummaryService
    {
        // <summary>
        /// Kinyeri a feltöltött fájl szöveges tartalmát.
        /// Támogatott kiterjesztések: .txt, .pdf és .doc.
        /// </summary>
        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension == ".txt")
            {
                // TXT esetén egyszerű szövegfájl beolvasás
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            else if (extension == ".pdf")
            {
                // PDF esetén a PdfPig könyvtár segítségével nyerjük ki a szöveget
                using (var stream = file.OpenReadStream())
                {
                    using (var pdf = PdfDocument.Open(stream))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var page in pdf.GetPages())
                        {
                            sb.AppendLine(page.Text);
                        }
                        return sb.ToString();
                    }
                }
            }
            else if (extension == ".doc" || extension == ".docx")
            {
                using (var stream = file.OpenReadStream())
                {
                    var doc = new Document(stream);
                    return doc.GetText();
                }
            }

            return string.Empty;
        }
    }
}
