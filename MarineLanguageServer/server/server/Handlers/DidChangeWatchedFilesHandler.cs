using System.Threading;
using System.Threading.Tasks;
using MarineLang.LanguageServerImpl.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace MarineLang.LanguageServerImpl.Handlers
{
    public class DidChangeWatchedFilesHandler : IDidChangeWatchedFilesHandler
    {
        private readonly ILanguageServerFacade _languageServerFacade;
        private readonly WorkspaceService _workspaceService;

        public DidChangeWatchedFilesHandler(ILogger<TextDocumentHandler> logger,
            ILanguageServerConfiguration configuration,
            ILanguageServerFacade languageServerFacade, WorkspaceService workspaceService)
        {
            _languageServerFacade = languageServerFacade;
            _workspaceService = workspaceService;
        }

        public DidChangeWatchedFilesRegistrationOptions GetRegistrationOptions(
            DidChangeWatchedFilesCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new DidChangeWatchedFilesRegistrationOptions();
        }

        public Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken)
        {
            var configUrl = DocumentUri.File($"{_workspaceService.RootPath}\\{WorkspaceService.MarineLangConfigFile}");
            foreach (var change in request.Changes)
            {
                if (change.Type == FileChangeType.Changed && change.Uri.Path == configUrl.Path)
                {
                    _workspaceService.LoadConfiguration();
                }
            }

            return Unit.Task;
        }
    }
}