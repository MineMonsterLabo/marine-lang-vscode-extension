using System.Collections.Generic;
using System.Linq;
using MarineLang.Models.Asts;
using MarineLang.VirtualMachines.Dumps.Models;
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

        public IEnumerable<CompletionItem> GetCompletions(ProgramAst programAst, string triggerCharacter,
            CompletionTriggerKind triggerKind, Position position)
        {
            var list = new List<CompletionItem>();
            list.AddRange(programAst.funcDefinitionAsts.Select(e => CreateCompletionItem(e.funcName, 2, string.Empty,
                CompletionItemKind.Function,
                $"function {e.funcName}({string.Join(", ", e.args.Select(a => a.VarName))})")));
            var funcDefinition = HittingFuncDefinition(programAst, position);
            if (funcDefinition != null)
            {
                StatementAst statementAst = HittingStatement(funcDefinition, position);
                if (statementAst != null)
                {
                    ExprAst exprAst = HittingExpr(statementAst, position);
                }

                /*if (triggerCharacter == ".")
                {
                }
                else
                {*/
                    list.AddRange(_workspaceService.DumpModel.StaticTypes.Select(e =>
                        CreateCompletionItem(e.Key, 4, e.Value.FullName, CompletionItemKind.Class)));
                    list.AddRange(_workspaceService.DumpModel.GlobalMethods.Select(e =>
                        CreateCompletionItem(e.Key, 3, BuildDoc(e.Key, e.Value), CompletionItemKind.Method)));
                    list.AddRange(_workspaceService.DumpModel.GlobalVariables.Select(e =>
                        CreateCompletionItem(e.Key, 1, e.Value.Name, CompletionItemKind.Variable)));

                    list.AddRange(funcDefinition.args.Select(e =>
                        CreateCompletionItem(e.VarName, 0, string.Empty, CompletionItemKind.Variable,
                            $"paramater {e.VarName}")));
                //}
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

        private ExprAst HittingExpr(StatementAst statementAst, Position position)
        {
            return statementAst.LookUp<ExprAst>().FirstOrDefault(e => Contains(e, position));
        }

        private CompletionItem CreateCompletionItem(string label, int order, string document,
            CompletionItemKind kind, string detail = null)
        {
            return new CompletionItem()
            {
                Label = label,
                SortText = order + label,
                Documentation = document,
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

        private string BuildDoc(string methodName, MethodDumpModel model)
        {
            return
                $"{model.TypeRef.Name} {methodName}({string.Join(',', model.Parameters.Select(e => $"{e.Value.TypeRef.Name} {e.Key}"))})";
        }

        private bool Contains(IAst ast, Position position)
        {
            return ast.Range.Contain(new Models.Position(position.Line + 1, position.Character + 1));
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