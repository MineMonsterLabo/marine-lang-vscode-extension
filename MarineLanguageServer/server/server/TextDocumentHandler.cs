using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;

namespace SampleServer
{
    public class TextDocumentHandler : ITextDocumentSyncHandler
    {
        ILanguageServerFacade LanguageServerFacade { get; }

        public TextDocumentHandler(ILogger<TextDocumentHandler> logger, ILanguageServerConfiguration configuration, ILanguageServerFacade languageServerFacade)
        {
            LanguageServerFacade = languageServerFacade;
        }

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions();
        }

        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "marinescript");
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken token)
        {
            return Unit.Task;
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            PublishValidateDiagnostics(request.TextDocument.Text, request.TextDocument.Uri);
            return Unit.Task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            PublishValidateDiagnostics(request.Text, request.TextDocument.Uri);
            return Unit.Task;
        }

        public void SetCapability(SynchronizationCapability capability)
        {
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions() =>
         new TextDocumentSaveRegistrationOptions
         {
             IncludeText = true
         };

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions();
        }

        private void PublishValidateDiagnostics(string text, DocumentUri uri)
        {
            LanguageServerFacade.TextDocument.PublishDiagnostics(
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
                    new Container<Diagnostic>(
                        new Diagnostic
                        {
                            Range = ToRange(result.Error.ErrorRangePosition),
                            Message = result.Error.FullErrorMessage,
                        }
                    );
            }
            return new Container<Diagnostic>();
        }

        private Range ToRange(MarineLang.Models.RangePosition range)
        {
            return new Range(ToPosition(range.Start), ToPosition(range.End));
        }

        private Position ToPosition(MarineLang.Models.Position position)
        {
            return new Position(position.line - 1, position.column-1);
        }
    }
}
