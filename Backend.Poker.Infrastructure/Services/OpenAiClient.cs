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
        private readonly string _systemMessage;

        public OpenAiClient(IConfiguration configuration)
        {
            //var apiKey = configuration["OpenAI:ApiKey"];
            var apiKey = "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";
            var model = "gpt-4.1-nano";

            _client = new(
                model: model,
                apiKey: apiKey
            );

            _systemMessage = "You are an AI assistant specialized in summarizing documents. " +
              "Your task is to first provide a concise, 1-2 sentence summary that outlines the main topic and thematic focus of the provided document, " +
              "and then to provide a more detailed summary. " +
              "Format your entire response as a JSON object with two properties: 'ShortSummary' and 'DetailedSummary'. " +
              "Ensure that your entire response is written in Hungarian, regardless of the language or content of the input. " +
              "The summaries must be clear, logically structured, and accurate.";
        }

        public async Task<DocumentSummaryApiResult> GetSummaryAsync(string text)
        {
            var messages = new ChatMessage[]
            {
                new SystemChatMessage(_systemMessage),
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
                var summary = JsonSerializer.Deserialize<DocumentSummaryApiResult>(jsonOutput, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
    }
}
