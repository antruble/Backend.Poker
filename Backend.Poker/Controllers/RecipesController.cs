using Backend.Poker.Infrastructure.Services.Recipes;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Backend.Poker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IOpenAiRecipesClient _openAiRecipesClient;

        public RecipesController(IOpenAiRecipesClient openAiRecipesClient)
        {
            _openAiRecipesClient = openAiRecipesClient;
        }

        /// <summary>
        /// Recepteket generáló végpont.
        /// A kérés tartalmazza a recept leírást és a rendelkezésre álló hozzávalókat.
        /// Az AI számára előkészíti a promptot, majd visszaadja a generált recept ajánlásokat.
        /// </summary>
        /// <param name="request">A felhasználó által megadott paraméterek (leírás, hozzávalók)</param>
        /// <returns>A generált receptek listája</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRecipes([FromBody] RecipeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest("A recept leírása kötelező.");
            }

            // Felépítjük az AI számára küldendő promptot.
            string userPrompt = BuildUserPrompt(request.Description, request.Ingredients);

            // Hívjuk az új kliens metódusát, amely elküldi a promptot az OpenAI API-nak.
            var aiResponse = await _openAiRecipesClient.GenerateRecipesAsync(userPrompt) 
                ?? throw new Exception();

            // Az AI válaszának feldolgozása: a JSON formátumú választ a RecipeSuggestion osztállyá alakítjuk.
            List<RecipeSuggestion> recipes;
            try
            {
                recipes = JsonSerializer.Deserialize<List<RecipeSuggestion>>(
                    aiResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                //recipes = JsonConvert.DeserializeObject<List<RecipeSuggestion>>(aiResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Hiba történt az AI válasz feldolgozása során: {ex.Message}");
            }

            return Ok(recipes);
        }

        /// <summary>
        /// A felhasználó által adott leírás és a hozzávalók alapján összerak egy promptot az AI számára.
        /// </summary>
        /// <param name="description">A recept leírása</param>
        /// <param name="ingredients">A rendelkezésre álló hozzávalók</param>
        /// <returns>Egy jól strukturált szöveg, mely az AI kérésként lesz elküldve</returns>
        private string BuildUserPrompt(string description, List<string> ingredients)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Felhasználói recept kérés:");
            sb.AppendLine("Leírás: " + description);
            if (ingredients != null && ingredients.Count > 0)
            {
                sb.Append("Elérhető hozzávalók: ");
                sb.AppendLine(string.Join(", ", ingredients));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// A receptek generálásához szükséges bemeneti modell.
    /// </summary>
    public class RecipeRequest
    {
        /// <summary>
        /// A felhasználó által megadott recept leírása.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A felhasználó által rendelkezésre állónak jelölt hozzávalók listája.
        /// </summary>
        public List<string> Ingredients { get; set; }
    }

    /// <summary>
    /// A recept ajánlásokat reprezentáló modell.
    /// </summary>
    public class RecipeSuggestion
    {
        /// <summary>
        /// A recept címe.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Rövid leírás, amely összegzi a recept lényegét.
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// A recept részletes elkészítési módja.
        /// </summary>
        public string DetailedRecipe { get; set; }
    }
}
