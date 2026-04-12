using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AvaBot.Infra.Context;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.AppServices;
using AvaBot.Infra.Repository;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;

namespace AvaBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAvaBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AvaBotContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AvaBotContext")));

        // Repositories
        services.AddScoped<IAgentRepository<Agent>, AgentRepository>();
        services.AddScoped<IKnowledgeFileRepository<KnowledgeFile>, KnowledgeFileRepository>();
        services.AddScoped<IChatSessionRepository<ChatSession>, ChatSessionRepository>();
        services.AddScoped<IChatMessageRepository<ChatMessage>, ChatMessageRepository>();
        services.AddScoped<ITelegramChatRepository<TelegramChat>, TelegramChatRepository>();

        // Domain Services
        services.AddScoped<AgentService>();
        services.AddScoped<IngestionService>();
        services.AddScoped<SearchService>();
        services.AddScoped<ChatService>();
        services.AddScoped<TelegramService>();
        services.AddScoped<WhatsappService>();

        // WPP Connect HttpClient
        services.AddHttpClient("WppConnect", (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(config["WppConnect:BaseUrl"] ?? "http://localhost:21465");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IWppConnectService, WppConnectService>();

        // AutoMapper
        services.AddSingleton<AutoMapper.IMapper>(sp =>
        {
            var expression = new AutoMapper.MapperConfigurationExpression();
            expression.AddMaps(typeof(AgentProfile).Assembly);
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var config = new AutoMapper.MapperConfiguration(expression, loggerFactory);
            return config.CreateMapper();
        });

        // App Services
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
        services.AddSingleton<IOpenAIService, OpenAIService>();

        return services;
    }
}
