using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Poker.Shared.Models;
using Backend.Poker.Shared.Models.DocumentSummary;

namespace Backend.Poker.Infrastructure.Services
{
    public class OpenAiClient : IOpenAiClient
    {
        private readonly ChatClient _client;
        private readonly string _baseSystemMessage;

        public OpenAiClient(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            var model = "gpt-4.1-nano";

            _client = new(
                model: model,
                apiKey: apiKey
            );

            _baseSystemMessage = "You are an AI assistant specialized in summarizing documents. " +
               "Your task is to first provide a concise, 1-2 sentence summary outlining the main topic and thematic focus of the provided document, " +
               "and then provide a detailed summary. " +
               "Format your answer as a JSON object with two properties: 'ShortSummary' and 'DetailedSummary'. " +
               "Ensure that your entire response is written in Hungarian, regardless of the language or content of the input. " +
               "The summaries must be clear, logically structured, and accurate.";
        }

        public async Task<DocumentSummaryApiResult> GetSummaryAsync(string text, string style)
        {
            // Alkalmazzuk a stílus leírását az alap rendszerüzenetünkre
            var styleDescription = GetStyleDescription(style);
            var systemMessageWithStyle = _baseSystemMessage + styleDescription;

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemMessageWithStyle),
                new UserChatMessage(text)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.5f,
            };

            var result = await _client.CompleteChatAsync(messages, options);
            string jsonOutput = result.Value.Content[0].Text;

            try
            {
                // Deszerializáljuk a JSON kimenetet a DocumentSummaryResult modellbe
                var summary = JsonSerializer.Deserialize<DocumentSummaryApiResult>(
                    jsonOutput, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                if (summary == null)
                {
                    throw new Exception("A JSON deserialization returned null.");
                }
                return summary;
            }
            catch (Exception ex)
            {
                // Ha a JSON struktúra nem felel meg az elvártnak, kezelheted az esetet itt.
                // Esetleg visszaadhatod az egész kimenetet részletes összegzésként, vagy más fallback megoldást alkalmazhatsz.
                throw new Exception("Error deserializing the OpenAI API output into DocumentSummaryResult.", ex);
            }
        }

        /// <summary>
        /// Segédmetódus, amely visszaadja a választott stílus részletes leírását.
        /// </summary>
        private string GetStyleDescription(string style)
        {
            // A leírások itt az AI számára adott utasítások, hogy a stílus személyisége megvalósuljon.
            return style.ToLower() switch
            {
                "academic" => " Please provide the summary using a scientific and academic tone, employing technical terms and evidence-based language. ",
                "practical" => " Please provide the summary in a practical and action-oriented style, using clear and direct language along with real-world examples. ",
                "simple" => " Please provide the summary in a simple, clear, and accessible tone, avoiding technical jargon and complex language. ",
                _ => string.Empty,
            };
        }
    }
}
