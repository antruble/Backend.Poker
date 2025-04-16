using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Shared.Models.DocumentSummary
{
    public class DocumentSummaryApiResult
    {
        public required string ShortSummary { get; set; }
        public required string DetailedSummary { get; set; }
    }
}
