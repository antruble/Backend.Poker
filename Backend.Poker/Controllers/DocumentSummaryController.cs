using Backend.Poker.Application.Interfaces;
using Backend.Poker.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Poker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentSummaryController : Controller
    {
        private readonly IOpenAiClient _openAiClient;
        private readonly IDocumentSummaryService _documentSummaryService;

        public DocumentSummaryController(IDocumentSummaryService documentSummaryService, IOpenAiClient openAiClient)
        {
            _documentSummaryService = documentSummaryService;
            _openAiClient = openAiClient;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string style)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Fájl nem lett feltöltve.");
            }

            // Ellenőrizzük a fájl típusát
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".txt" && extension != ".pdf" && extension != ".doc" && extension != ".docx")
            {
                return BadRequest("Csak txt és pdf és doc fájlok támogatottak.");
            }

            // A fájl szöveges tartalmát a service segítségével nyerjük ki
            var extractedText = await _documentSummaryService.ExtractTextAsync(file);
            var apiResponse = await _openAiClient.GetSummaryAsync(extractedText, style);


            return Ok(apiResponse);
        }

        // Ha szükséges, hagyhatja meg az eredeti Index metódust is
        public IActionResult Index()
        {
            return View();
        }
    }
}
