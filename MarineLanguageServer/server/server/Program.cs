using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MarineLang.LanguageServerImpl.Handlers;
using MarineLang.LanguageServerImpl.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

namespace MarineLang.LanguageServerImpl
{
    internal class Program
    {
        private static async Task Main(string[] args) => await MainAsync(args);

        private static async Task MainAsync(string[] args)
        {
            var executeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File($"{executeFolder}/server-logs/log.txt", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Verbose()
                .CreateLogger();

            var server = await LanguageServer.From(
                options =>
                    options
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .ConfigureLogging(
                            x => x
                                .AddSerilog(Log.Logger)
                                .AddLanguageProtocolLogging()
                                .SetMinimumLevel(LogLevel.Debug)
                        )
                        .WithHandler<CompletionHandler>()
                        .WithHandler<DidChangeWatchedFilesHandler>()
                        .WithHandler<TextDocumentHandler>()
                        .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
                        .WithServices(
                            services =>
                            {
                                services.AddSingleton<WorkspaceService>();
                                services.AddSingleton<CompletionService>();
                            }
                        )
                        .OnInitialize((server, param, token) =>
                        {
                            WorkspaceService workspaceService = server.Services.GetService<WorkspaceService>();
                            if (workspaceService != null)
                            {
                                workspaceService.SetRootPath(param.RootPath);
                                workspaceService.LoadConfiguration();
                            }

                            return Unit.Task;
                        })
            );

            await server.WaitForExit;
        }
    }
}