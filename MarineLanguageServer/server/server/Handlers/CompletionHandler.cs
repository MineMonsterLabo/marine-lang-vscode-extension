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

namespace MarineLang.LanguageServerImpl.Handlers
{
    public class CompletionHandler : ICompletionHandler
    {
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
            return new CompletionRegistrationOptions();
        }

        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var text = _workspaceService.GetMarineFileBuffer(request.TextDocument.Uri.Path);
            var tokens = new LexicalAnalyzer().GetTokens(text);
            var result = new SyntaxAnalyzer().Parse(tokens);

            var list = new CompletionList();
            if (result.programAst != null)
            {
                list = new CompletionList(_completionService.GetCompletions(result.programAst, request.Position));
            }

            return Task.FromResult(list);
        }

        /*private IEnumerable<CompletionItem> CreateVariables(FuncDefinitionAst ast, Position position)
        {
            return ast.LookUp<AssignmentVariableAst>().Select(e => CreateCompletionItem(e.variableAst.VarName,
                CompletionItemKind.Variable, $"local variable {e.variableAst.VarName}"));
        }*/
    }
}