using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
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
            var keywords = Task.Run(() => CreateKeywords());
            var snippets = Task.Run(() => CreateSnippets());

            return new CompletionList((await Task.WhenAll(keywords, snippets)).SelectMany(e => e));
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

        private IEnumerable<CompletionItem> CreateSnippets()
        {
            return new[]
            {
                CreateSnippetItem("fun", "fun ${1:foo}()\n\t$2\nend"),
                CreateSnippetItem("fmain", "fun main()\n\t$2\nend"),
                CreateSnippetItem("letc", "let ${1:foo} = $2"),
                CreateSnippetItem("if", "if ($1) {\n\t$2\n}"),
                CreateSnippetItem("else", "else {\n\t$1\n}"),
                CreateSnippetItem("elseif", "else if ($1) {\n\t$2\n}"),
                CreateSnippetItem("while", "while ($1) {\n\t$2\n}"),
                CreateSnippetItem("for", "for ${1:i} = $2, $3, $4 {\n\t$5\n}"),
            };
        }

        private CompletionItem CreateCompletionItem(string label, CompletionItemKind kind, StringOrMarkupContent document = null)
        {
            return new CompletionItem()
            {
                Label = label,
                Kind = kind,
                Documentation = document
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
    }
}
