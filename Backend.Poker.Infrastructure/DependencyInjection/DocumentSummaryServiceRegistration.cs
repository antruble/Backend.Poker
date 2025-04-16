using Backend.Poker.Application.Interfaces;
using Backend.Poker.Application.Services;
using Backend.Poker.Domain.IRepositories;
using Backend.Poker.Domain.Services;
using Backend.Poker.Infrastructure.Data;
using Backend.Poker.Infrastructure.Repositories;
using Backend.Poker.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.DependencyInjection
{
    public static class DocumentSummaryServiceRegistration
    {
        public static void AddDocumentSummaryServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IOpenAiClient, OpenAiClient>();
            services.AddScoped<IDocumentSummaryService, DocumentSummaryAppService>();

        }
    }
}
