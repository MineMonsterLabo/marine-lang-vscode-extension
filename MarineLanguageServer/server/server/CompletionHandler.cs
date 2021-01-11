using MarineLang.LexicalAnalysis;
using MarineLang.Models.Asts;
using MarineLang.SyntaxAnalysis;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public class CompletionHandler : ICompletionHandler
    {
        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions();
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var text = File.ReadAllText(request.TextDocument.Uri.Path.Remove(0, 1));
            var tokens = new LexicalAnalyzer().GetTokens(text);
            var result = new SyntaxAnalyzer().Parse(tokens);

            var tasks = new List<Task<IEnumerable<CompletionItem>>>();
            FuncDefinitionAst currentFuncDefAst = null;
            if (!result.IsError)
            {
                currentFuncDefAst = await Task.Run(() => GetCurrentFunctionAst(result.Value, request.Position));

                if (currentFuncDefAst != null)
                {
                    tasks.Add(Task.Run(() => CreateFunctions(result.Value)));

                    tasks.Add(Task.Run(() => CreateFunctionParamaters(currentFuncDefAst)));

                    tasks.Add(Task.Run(() => CreateKeywords()));
                }
            }

            tasks.Add(Task.Run(() => CreateSnippets(currentFuncDefAst)));

            return new CompletionList((await Task.WhenAll(tasks)).SelectMany(e => e));
        }

        public void SetCapability(CompletionCapability capability)
        {
        }

        private IEnumerable<CompletionItem> CreateKeywords()
        {
            return new[]
            {
                CreateCompletionItem("let", CompletionItemKind.Keyword),
                CreateCompletionItem("ret", CompletionItemKind.Keyword),
                CreateCompletionItem("yield", CompletionItemKind.Keyword),
            };
        }

        private IEnumerable<CompletionItem> CreateSnippets(FuncDefinitionAst ast)
        {
            List<CompletionItem> items = new List<CompletionItem>();
            if (ast != null)
            {
                items.Add(CreateSnippetItem("letc", "let ${1:foo} = $2"));
                items.Add(CreateSnippetItem("if", "if ($1) {\n\t$2\n}"));
                items.Add(CreateSnippetItem("else", "else {\n\t$1\n}"));
                items.Add(CreateSnippetItem("elseif", "else if ($1) {\n\t$2\n}"));
                items.Add(CreateSnippetItem("while", "while ($1) {\n\t$2\n}"));
                items.Add(CreateSnippetItem("for", "for ${1:i} = $2, $3, $4 {\n\t$5\n}"));
            }
            else
            {
                items.Add(CreateSnippetItem("fun", "fun ${1:foo}()\n\t$2\nend"));
                items.Add(CreateSnippetItem("fmain", "fun main()\n\t$2\nend"));
            }

            return items;
        }

        private IEnumerable<CompletionItem> CreateFunctions(ProgramAst ast)
        {
            return ast.funcDefinitionAsts.Select(e => CreateCompletionItem(e.funcName, CompletionItemKind.Function, $"function {e.funcName}({string.Join(", ", e.args.Select(a => a.VarName))})"));
        }

        private IEnumerable<CompletionItem> CreateFunctionParamaters(FuncDefinitionAst ast)
        {
            return ast.args.Select(e => CreateCompletionItem(e.VarName, CompletionItemKind.Field, $"paramater {e.VarName}"));
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
            return ast.Start.line <= position.Line + 1
                && ast.End.line >= position.Line + 1;
        }
    }
}
