using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using server;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace SampleServer
{
    public class TextDocumentHandler : ITextDocumentSyncHandler
    {
        private readonly ILanguageServerFacade _languageServerFacade;
        private readonly MarineLangWorkspaceService _workspaceService;

        public TextDocumentHandler(ILogger<TextDocumentHandler> logger, ILanguageServerConfiguration configuration,
            ILanguageServerFacade languageServerFacade, MarineLangWorkspaceService workspaceService)
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

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken token)
        {
            PublishValidateDiagnostics(request.ContentChanges.First().Text, request.TextDocument.Uri);
            _workspaceService.UpdateMarineFileBuffer(request.TextDocument.Uri.Path,
                request.ContentChanges.First().Text);

            return Unit.Task;
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            PublishValidateDiagnostics(request.TextDocument.Text, request.TextDocument.Uri);
            _workspaceService.UpdateMarineFileBuffer(request.TextDocument.Uri.Path, request.TextDocument.Text);

            return Unit.Task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            PublishValidateDiagnostics(request.Text, request.TextDocument.Uri);
            _workspaceService.UpdateMarineFileBuffer(request.TextDocument.Uri.Path,
                request.Text);

            return Unit.Task;
        }

        private void PublishValidateDiagnostics(string text, DocumentUri uri)
        {
            _languageServerFacade.TextDocument.PublishDiagnostics(
                new PublishDiagnosticsParams { Uri = uri, Diagnostics = Validate(text) }
            );
        }

        private Container<Diagnostic> Validate(string text)
        {
            var tokens = new LexicalAnalyzer().GetTokens(text);
            var result = new SyntaxAnalyzer().Parse(tokens);

            if (result.IsError)
            {
                return
                    new Container<Diagnostic>(result.parseErrorInfos.Select(e => new Diagnostic()
                    {
                        Range = ToRange(e.ErrorRangePosition),
                        Message = e.FullErrorMessage,
                    }));
            }

            return new Container<Diagnostic>();
        }

        private Range ToRange(RangePosition range)
        {
            return new Range(ToPosition(range.Start), ToPosition(range.End));
        }

        private Position ToPosition(MarineLang.Models.Position position)
        {
            return new Position(position.line - 1, position.column - 1);
        }
    }
}