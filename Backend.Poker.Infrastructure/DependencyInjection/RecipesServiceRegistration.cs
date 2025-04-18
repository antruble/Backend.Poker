using Backend.Poker.Application.Interfaces;
using Backend.Poker.Application.Services;
using Backend.Poker.Infrastructure.Services;
using Backend.Poker.Infrastructure.Services.Recipes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.DependencyInjection
{
    public static class RecipesServiceRegistration
    {
        public static void AddRecipesServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IOpenAiRecipesClient, OpenAiRecipesClient>();
            //services.AddScoped<IDocumentSummaryService, DocumentSummaryAppService>();

        }
    }
}
