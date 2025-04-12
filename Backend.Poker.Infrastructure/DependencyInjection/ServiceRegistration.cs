using Backend.Poker.Application.Interfaces;
using Backend.Poker.Domain.IRepositories;
using Backend.Poker.Domain.Services;
using Backend.Poker.Infrastructure.Data;
using Backend.Poker.Infrastructure.Repositories;
using Backend.Poker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Poker.Infrastructure.DependencyInjection
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Adatbázis konfiguráció (EF Core)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
           
            services.AddScoped<IUnitOfWork, UnitOfWork>();



            services.AddScoped<IBotService, BotService>();
            services.AddScoped<IPlayerService, PlayerService>();
            services.AddScoped<IPokerHandEvaluator, PokerHandEvaluator>();
            services.AddScoped<IGameService, GameService>();

        }
    }
}
