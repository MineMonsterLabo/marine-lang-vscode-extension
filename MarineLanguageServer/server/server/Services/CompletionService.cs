using System.Collections.Generic;
using System.Linq;
using MarineLang.Models.Asts;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace MarineLang.LanguageServerImpl.Services
{
    public class CompletionService
    {
        private readonly WorkspaceService _workspaceService;

        public CompletionService(WorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        public IEnumerable<CompletionItem> GetCompletions(ProgramAst programAst, Position position)
        {
            var list = new List<CompletionItem>();
            list.AddRange(programAst.funcDefinitionAsts.Select(e => CreateCompletionItem(e.funcName,
                CompletionItemKind.Function,
                $"function {e.funcName}({string.Join(", ", e.args.Select(a => a.VarName))})")));
            var funcDefinition = HittingFuncDefinition(programAst, position);
            if (funcDefinition != null)
            {
                list.AddRange(funcDefinition.args.Select(e =>
                    CreateCompletionItem(e.VarName, CompletionItemKind.Variable, $"paramater {e.VarName}")));
            }
            else
            {
                list.Add(CreateSnippetItem("fun", "fun ${1:foo}()\n\t$2\nend"));
            }

            return list;
        }

        private FuncDefinitionAst HittingFuncDefinition(ProgramAst programAst, Position position)
        {
            return programAst.LookUp<FuncDefinitionAst>().FirstOrDefault(e => Contains(e, position));
        }

        private StatementAst HittingStatement(FuncDefinitionAst funcDefinitionAst, Position position)
        {
            return funcDefinitionAst.LookUp<StatementAst>().FirstOrDefault(e => Contains(e, position));
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

        private bool Contains(IAst ast, Position position)
        {
            return ast.Range.Contain(new Models.Position(position.Line + 1, position.Line + 1));
        }

        /*yield return CreateCompletionItem("let", CompletionItemKind.Keyword);
        yield return CreateCompletionItem("ret", CompletionItemKind.Keyword);
        yield return CreateCompletionItem("yield", CompletionItemKind.Keyword);
        yield return CreateCompletionItem("true", CompletionItemKind.Keyword);
        yield return CreateCompletionItem("false", CompletionItemKind.Keyword);
        yield return CreateSnippetItem("letc", "let ${1:foo} = $2");
        yield return CreateSnippetItem("if", "if ($1) {\n\t$2\n}");
        yield return CreateSnippetItem("else", "else {\n\t$1\n}");
        yield return CreateSnippetItem("elseif", "else if ($1) {\n\t$2\n}");
        yield return CreateSnippetItem("while", "while ($1) {\n\t$2\n}");
        yield return CreateSnippetItem("for", "for ${1:i} = $2, $3, $4 {\n\t$5\n}");
        yield return CreateSnippetItem("foreach", "foreach ${1:val} in $2 {\n\t$3\n}");
        yield return CreateSnippetItem("letac", "let ${1:foo} = {|$2| $3}");*/
    }
}