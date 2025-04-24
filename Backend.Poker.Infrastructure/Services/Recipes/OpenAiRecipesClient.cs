using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Backend.Poker.Infrastructure.Services.Recipes
{
    public interface IOpenAiRecipesClient
    {
        /// <summary>
        /// AI segítségével recepteket generál a megadott prompt alapján.
        /// </summary>
        /// <param name="prompt">A felhasználói kérésből összeállított prompt</param>
        /// <returns>Az AI által generált válasz, JSON formátumban</returns>
        Task<string> GenerateRecipesAsync(string prompt);
    }

    public class OpenAiRecipesClient : IOpenAiRecipesClient
    {
        private readonly ChatClient _client;
        private readonly string _baseSystemMessage;

        public OpenAiRecipesClient(IConfiguration configuration)
        {
            // Az API kulcs és modell beállítása
            // Az apiKey-t a konfigurációból vesszük, itt egy előre megadott érték vagy üres string lehet
            var apiKey = configuration["OpenAI:ApiKey"] ?? "";
            var model = "gpt-4.1-nano";

            _client = new ChatClient(model: model, apiKey: apiKey);

            // Az alap rendszerüzenet, mely meghatározza az AI szerepét és a válasz formátumát.
            _baseSystemMessage = "You are an AI chef specialized in generating creative and detailed recipes. " +
                "Based on the user's request, provide 10 recipe suggestions along with their ingredients and preparation steps. " +
                "Output should be formatted as a JSON array of objects, where each object contains the properties: 'Title', 'ShortDescription', and 'DetailedRecipe'. " +
                "Ensure that the entire answer is written in Hungarian. " +
                "Incorporate the user's provided ingredients into the recipes when applicable.";
        }

        public async Task<string> GenerateRecipesAsync(string prompt)
        {
            // Felépítjük az üzenetsorozatot, mely tartalmazza a rendszerüzenetet és a felhasználói promptot.
            var messages = new ChatMessage[]
            {
                new SystemChatMessage(_baseSystemMessage),
                new UserChatMessage(prompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.5f,
            };

            // Meghívjuk az AI API-t a ChatClient segítségével.
            var result = await _client.CompleteChatAsync(messages, options);
            string jsonOutput = result.Value.Content[0].Text;

            return jsonOutput;
        }
    }

}
