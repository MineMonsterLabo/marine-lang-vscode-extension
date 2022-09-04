using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarineLang.LanguageServerImpl.Services;
using MarineLang.LexicalAnalysis;
using MarineLang.Models;
using MarineLang.SyntaxAnalysis;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace MarineLang.LanguageServerImpl.Handlers
{
    public class TextDocumentHandler : ITextDocumentSyncHandler
    {
        private const int CompletionTimeout = 5000;

        private readonly ILanguageServerFacade _languageServerFacade;
        private readonly WorkspaceService _workspaceService;

        public TextDocumentHandler(ILogger<TextDocumentHandler> logger, ILanguageServerConfiguration configuration,
            ILanguageServerFacade languageServerFacade, WorkspaceService workspaceService)
        {
            _languageServerFacade = languageServerFacade;
            _workspaceService = workspaceService;
        }

        TextDocumentChangeRegistrationOptions
            IRegistration<TextDocumentChangeRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(
                SynchronizationCapability capability,
                ClientCapabilities clientCapabilities)
        {
            return new TextDocumentChangeRegistrationOptions
            {
                SyncKind = TextDocumentSyncKind.Full
            };
        }

        TextDocumentOpenRegistrationOptions
            IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(
                SynchronizationCapability capability,
                ClientCapabilities clientCapabilities)
        {
            return new TextDocumentOpenRegistrationOptions();
        }

        TextDocumentCloseRegistrationOptions
            IRegistration<TextDocumentCloseRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(
                SynchronizationCapability capability,
                ClientCapabilities clientCapabilities)
        {
            return new TextDocumentCloseRegistrationOptions();
        }

        TextDocumentSaveRegistrationOptions
            IRegistration<TextDocumentSaveRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(
                SynchronizationCapability capability,
                ClientCapabilities clientCapabilities)
        {
            return new TextDocumentSaveRegistrationOptions
            {
                IncludeText = true
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "marinescript");
        }

        public async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken token)
        {
            await PublishValidateDiagnostics(request.ContentChanges.First().Text, request.TextDocument.Uri, token);
            _workspaceService.UpdateMarineFileBuffer(request.TextDocument.Uri.Path,
                request.ContentChanges.First().Text);

            return Unit.Value;
        }

        public async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            await PublishValidateDiagnostics(request.TextDocument.Text, request.TextDocument.Uri, cancellationToken);
            _workspaceService.UpdateMarineFileBuffer(request.TextDocument.Uri.Path, request.TextDocument.Text);

            return Unit.Value;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public async Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            await PublishValidateDiagnostics(request.Text, request.TextDocument.Uri, cancellationToken);
            _workspaceService.UpdateMarineFileBuffer(request.TextDocument.Uri.Path,
                request.Text);

            return Unit.Value;
        }

        private async Task PublishValidateDiagnostics(string text, DocumentUri uri, CancellationToken cancellationToken)
        {
            _languageServerFacade.TextDocument.PublishDiagnostics(
                new PublishDiagnosticsParams { Uri = uri, Diagnostics = await Validate(text, cancellationToken) }
            );
        }

        private async Task<Container<Diagnostic>> Validate(string text, CancellationToken cancellationToken)
        {
            var tokens = new LexicalAnalyzer().GetTokens(text);
            var result = new SyntaxAnalyzer().Parse(tokens);

            if (result.IsError)
            {
                var container = Task.Run(() => new Container<Diagnostic>(
                    result.parseErrorInfos.Select(e => new Diagnostic()
                    {
                        Range = ToRange(e.ErrorRangePosition),
                        Message = e.FullErrorMessage,
                    })), cancellationToken);
                var cancelTask = Task.Run(async () =>
                {
                    await Task.Delay(CompletionTimeout, cancellationToken);
                    return new Container<Diagnostic>();
                }, cancellationToken);

                var task = await Task.WhenAny(container, cancelTask);
                if (task.Status != TaskStatus.Canceled && task != container)
                {
                    _languageServerFacade.Window.ShowError("Parse timed out.");
                }

                return await task;
            }

            return new Container<Diagnostic>();
        }

        private Range ToRange(RangePosition range)
        {
            return new Range(ToPosition(range.Start), ToPosition(range.End));
        }

        private Position ToPosition(Models.Position position)
        {
            return new Position(position.line - 1, position.column - 1);
        }
    }
}