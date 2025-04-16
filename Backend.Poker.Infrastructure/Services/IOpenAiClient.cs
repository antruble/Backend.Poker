using Backend.Poker.Shared.Models.DocumentSummary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.Services
{
    public interface IOpenAiClient
    {
        /// <summary>
        /// Aszinkron módon elküldi a bemeneti szöveget az OpenAI API-nak, és visszaadja az AI által generált választ.
        /// </summary>
        /// <param name="text">A dokumentum tartalma, amit feldolgozni szeretnél</param>
        /// <returns>Az AI válasza</returns>
        Task<DocumentSummaryApiResult> GetSummaryAsync(string text);
    }
}
