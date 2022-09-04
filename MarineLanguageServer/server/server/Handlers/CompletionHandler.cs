using System.Threading;
using System.Threading.Tasks;
using MarineLang.LanguageServerImpl.Services;
using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace MarineLang.LanguageServerImpl.Handlers
{
    public class CompletionHandler : ICompletionHandler
    {
        private const int CompletionTimeout = 5000;

        private readonly ILanguageServerFacade _languageServerFacade;
        private readonly WorkspaceService _workspaceService;
        private readonly CompletionService _completionService;

        public CompletionHandler(ILogger<TextDocumentHandler> logger, ILanguageServerConfiguration configuration,
            ILanguageServerFacade languageServerFacade, WorkspaceService workspaceService,
            CompletionService completionService)
        {
            _languageServerFacade = languageServerFacade;
            _workspaceService = workspaceService;
            _completionService = completionService;
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions()
            {
                TriggerCharacters = new Container<string>("."),
                AllCommitCharacters = new Container<string>("//", "/*", "*/")
            };
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var items = Task.Run(() =>
            {
                var text = _workspaceService.GetMarineFileBuffer(request.TextDocument.Uri.Path);
                var tokens = new LexicalAnalyzer().GetTokens(text);
                var result = new SyntaxAnalyzer().Parse(tokens);

                var list = new CompletionList();
                if (result.programAst != null)
                {
                    return new CompletionList(_completionService.GetCompletions(result.programAst,
                        request.TextDocument.Uri.Path, request.Context?.TriggerCharacter,
                        request.Context?.TriggerKind ?? 0,
                        request.Position));
                }

                return list;
            }, cancellationToken);
            var cancelTask = Task.Run(async () =>
            {
                await Task.Delay(CompletionTimeout, cancellationToken);
                return new CompletionList();
            }, cancellationToken);

            var task = await Task.WhenAny(items, cancelTask);
            if (task.Status != TaskStatus.Canceled && task != items)
            {
                _languageServerFacade.Window.ShowError("Candidate selection timed out.");
            }

            return await task;
        }

        /*private IEnumerable<CompletionItem> CreateVariables(FuncDefinitionAst ast, Position position)
        {
            return ast.LookUp<AssignmentVariableAst>().Select(e => CreateCompletionItem(e.variableAst.VarName,
                CompletionItemKind.Variable, $"local variable {e.variableAst.VarName}"));
        }*/
    }
}