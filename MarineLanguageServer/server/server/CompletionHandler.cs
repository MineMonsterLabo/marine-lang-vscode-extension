﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarineLang.LexicalAnalysis;
using MarineLang.Models.Asts;
using MarineLang.SyntaxAnalysis;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using SampleServer;

namespace server
{
    public class CompletionHandler : ICompletionHandler
    {
        private readonly ILanguageServerFacade _languageServerFacade;
        private readonly MarineLangWorkspaceService _workspaceService;

        public CompletionHandler(ILogger<TextDocumentHandler> logger, ILanguageServerConfiguration configuration,
            ILanguageServerFacade languageServerFacade, MarineLangWorkspaceService workspaceService)
        {
            _languageServerFacade = languageServerFacade;
            _workspaceService = workspaceService;
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions();
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var text = _workspaceService.GetMarineFileBuffer(request.TextDocument.Uri.Path);
            var tokens = new LexicalAnalyzer().GetTokens(text);
            var result = new SyntaxAnalyzer().Parse(tokens);

            var tasks = new List<Task<IEnumerable<CompletionItem>>>();
            FuncDefinitionAst currentFuncDefAst = null;
            if (result.programAst != null)
            {
                currentFuncDefAst = await Task.Run(() => GetCurrentFunctionAst(result.programAst, request.Position),
                    cancellationToken);
                if (currentFuncDefAst != null)
                {
                    tasks.Add(Task.Run(() => CreateFunctions(result.programAst), cancellationToken));

                    tasks.Add(Task.Run(() => CreateFunctionParamaters(currentFuncDefAst), cancellationToken));
                    tasks.Add(Task.Run(() => CreateVariables(currentFuncDefAst, request.Position), cancellationToken));

                    tasks.Add(Task.Run(CreateKeywords, cancellationToken));
                }
            }

            tasks.Add(Task.Run(() => CreateSnippets(currentFuncDefAst), cancellationToken));

            return new CompletionList((await Task.WhenAll(tasks)).SelectMany(e => e));
        }

        private IEnumerable<CompletionItem> CreateKeywords()
        {
            yield return CreateCompletionItem("let", CompletionItemKind.Keyword);
            yield return CreateCompletionItem("ret", CompletionItemKind.Keyword);
            yield return CreateCompletionItem("yield", CompletionItemKind.Keyword);
            yield return CreateCompletionItem("true", CompletionItemKind.Keyword);
            yield return CreateCompletionItem("false", CompletionItemKind.Keyword);
        }

        private IEnumerable<CompletionItem> CreateSnippets(FuncDefinitionAst ast)
        {
            if (ast != null)
            {
                yield return CreateSnippetItem("letc", "let ${1:foo} = $2");
                yield return CreateSnippetItem("if", "if ($1) {\n\t$2\n}");
                yield return CreateSnippetItem("else", "else {\n\t$1\n}");
                yield return CreateSnippetItem("elseif", "else if ($1) {\n\t$2\n}");
                yield return CreateSnippetItem("while", "while ($1) {\n\t$2\n}");
                yield return CreateSnippetItem("for", "for ${1:i} = $2, $3, $4 {\n\t$5\n}");
                yield return CreateSnippetItem("foreach", "foreach ${1:val} in $2 {\n\t$3\n}");
                yield return CreateSnippetItem("letac", "let ${1:foo} = {|$2| $3}");
            }
            else
            {
                yield return CreateSnippetItem("fun", "fun ${1:foo}()\n\t$2\nend");
            }
        }

        private IEnumerable<CompletionItem> CreateFunctions(ProgramAst ast)
        {
            return ast.funcDefinitionAsts.Select(e => CreateCompletionItem(e.funcName, CompletionItemKind.Function,
                $"function {e.funcName}({string.Join(", ", e.args.Select(a => a.VarName))})"));
        }

        private IEnumerable<CompletionItem> CreateFunctionParamaters(FuncDefinitionAst ast)
        {
            return ast.args.Select(e =>
                CreateCompletionItem(e.VarName, CompletionItemKind.Variable, $"paramater {e.VarName}"));
        }

        private IEnumerable<CompletionItem> CreateVariables(FuncDefinitionAst ast, Position position)
        {
            return ast.LookUp<AssignmentVariableAst>().Select(e => CreateCompletionItem(e.variableAst.VarName,
                CompletionItemKind.Variable, $"local variable {e.variableAst.VarName}"));
        }

        private CompletionItem CreateCompletionItem(string label, CompletionItemKind kind, string detail = null)
        {
            return new CompletionItem()
            {
                Label = label,
                Kind = kind,
                Detail = detail
            };
        }

        private CompletionItem CreateSnippetItem(string label, string code)
        {
            return new CompletionItem()
            {
                Label = label,
                Kind = CompletionItemKind.Snippet,
                InsertText = code,
                InsertTextFormat = InsertTextFormat.Snippet,
                InsertTextMode = InsertTextMode.AdjustIndentation
            };
        }

        private FuncDefinitionAst GetCurrentFunctionAst(ProgramAst programAst, Position position)
        {
            foreach (FuncDefinitionAst ast in programAst.funcDefinitionAsts)
            {
                if (Contains(ast, position))
                {
                    return ast;
                }
            }

            return null;
        }

        private bool Contains(IAst ast, Position position)
        {
            return ast.Range.Contain(new MarineLang.Models.Position(position.Line + 1, position.Line + 1));
        }
    }
}