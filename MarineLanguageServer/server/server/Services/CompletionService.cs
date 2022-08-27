using System.Collections.Generic;
using System.Linq;
using MarineLang.Inputs;
using MarineLang.LexicalAnalysis;
using MarineLang.Models.Asts;
using MarineLang.SyntaxAnalysis;
using MarineLang.Utils;
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

        public IEnumerable<CompletionItem> GetCompletions(ProgramAst programAst, string filePath,
            string triggerCharacter,
            CompletionTriggerKind triggerKind, Position position)
        {
            var list = new List<CompletionItem>();
            var funcDefinition = HittingFuncDefinition(programAst, position);
            if (funcDefinition != null)
            {
                if (triggerCharacter == ".")
                {
                    if (_workspaceService.DumpModel == null)
                        return list;

                    var lineBuffer = _workspaceService.GetMarineFileBufferForStrings(filePath)[position.Line];
                    var tokens = new LexicalAnalyzer().GetTokens(lineBuffer.Remove(position.Character - 1, 1) + "{}");
                    var result = new SyntaxAnalyzer().marineParser.ParseStatement(TokenInput.Create(tokens.ToArray()))
                        .Result;
                    ExprAst exprAst;
                    if (result.IsOk)
                    {
                        exprAst = HittingExpr(result.RawValue, new Position(0, position.Character - 1));
                        while (true)
                        {
                            if (exprAst is VariableAst or InstanceFieldAst or FuncCallAst or InstanceFuncCallAst)
                            {
                                break;
                            }

                            var expr = HittingNextExpr(exprAst, new Position(0, position.Character - 1));
                            if (expr == null)
                            {
                                break;
                            }

                            exprAst = expr;
                        }
                    }
                    else
                    {
                        return list;
                    }

                    var currentExpr = CreateAstParent(exprAst, new Position(0, position.Character - 1));
                    TypeDumpModel currentType = null;
                    while (currentExpr != null)
                    {
                        var name = GetNameExprAst(currentExpr.Current) ?? string.Empty;
                        var upperName = NameUtil.GetUpperCamelName(name);
                        var lowerName = NameUtil.ConvertCameName(name);
                        if (currentExpr.Current is FuncCallAst)
                        {
                            if (currentType == null)
                            {
                                var methods = _workspaceService.DumpModel.GlobalMethods;
                                if (methods.TryGetValue(upperName, out MethodDumpModel methodModel))
                                {
                                    currentType =
                                        methodModel.TypeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                    currentExpr = currentExpr.Parent;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (currentType.Members.TryGetValue(upperName,
                                        out List<MemberDumpModel> memberDumpModel))
                                {
                                    currentType =
                                        ((MethodDumpModel)memberDumpModel.First()).TypeName.GetTypeDumpModel(
                                            _workspaceService.DumpModel);
                                    currentExpr = currentExpr.Parent;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else if (currentExpr.Current is VariableAst)
                        {
                            if (currentType == null)
                            {
                                var variables = _workspaceService.DumpModel.GlobalVariables;
                                if (variables.TryGetValue(name, out TypeNameDumpModel typeName))
                                {
                                    currentType = typeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                    currentExpr = currentExpr.Parent;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (currentType.Members.TryGetValue(lowerName,
                                        out List<MemberDumpModel> memberDumpModel))
                                {
                                    var first = memberDumpModel.First();
                                    if (first is FieldDumpModel fieldDumpModel)
                                    {
                                        currentType =
                                            fieldDumpModel.TypeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                }
                                else if (currentType.Members.TryGetValue(upperName,
                                             out List<MemberDumpModel> memberDumpModel2))
                                {
                                    var first = memberDumpModel2.First();
                                    if (first is PropertyDumpModel propertyDumpModel)
                                    {
                                        currentType =
                                            propertyDumpModel.TypeName.GetTypeDumpModel(_workspaceService
                                                .DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else if (currentExpr.Current is InstanceFuncCallAst)
                        {
                            if (currentType == null)
                            {
                                var methods = _workspaceService.DumpModel.GlobalMethods;
                                var variables = _workspaceService.DumpModel.GlobalVariables;
                                if (methods.TryGetValue(upperName, out MethodDumpModel methodModel))
                                {
                                    currentType =
                                        methodModel.TypeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                    currentExpr = currentExpr.Parent;
                                }
                                else if (variables.TryGetValue(name, out TypeNameDumpModel typeName))
                                {
                                    currentType = typeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                    currentExpr = currentExpr.Parent;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (currentType.Members.TryGetValue(lowerName,
                                        out List<MemberDumpModel> memberDumpModel))
                                {
                                    var first = memberDumpModel.First();
                                    if (first is FieldDumpModel fieldDumpModel)
                                    {
                                        currentType =
                                            fieldDumpModel.TypeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else if (currentType.Members.TryGetValue(upperName,
                                             out List<MemberDumpModel> memberDumpModel2))
                                {
                                    var first = memberDumpModel2.First();
                                    if (first is PropertyDumpModel propertyDumpModel)
                                    {
                                        currentType =
                                            propertyDumpModel.TypeName.GetTypeDumpModel(_workspaceService
                                                .DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                    else if (first is MethodDumpModel memberDumpModel3)
                                    {
                                        currentType =
                                            memberDumpModel3.TypeName.GetTypeDumpModel(_workspaceService
                                                .DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else if (currentExpr.Current is InstanceFieldAst)
                        {
                            if (currentType == null)
                            {
                                var methods = _workspaceService.DumpModel.GlobalMethods;
                                var variables = _workspaceService.DumpModel.GlobalVariables;
                                if (methods.TryGetValue(upperName, out MethodDumpModel methodModel))
                                {
                                    currentType =
                                        methodModel.TypeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                    currentExpr = currentExpr.Parent;
                                }
                                else if (variables.TryGetValue(name, out TypeNameDumpModel typeName))
                                {
                                    currentType = typeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                    currentExpr = currentExpr.Parent;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (currentType.Members.TryGetValue(lowerName,
                                        out List<MemberDumpModel> memberDumpModel))
                                {
                                    var first = memberDumpModel.First();
                                    if (first is FieldDumpModel fieldDumpModel)
                                    {
                                        currentType =
                                            fieldDumpModel.TypeName.GetTypeDumpModel(_workspaceService.DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else if (currentType.Members.TryGetValue(upperName,
                                             out List<MemberDumpModel> memberDumpModel2))
                                {
                                    var first = memberDumpModel2.First();
                                    if (first is PropertyDumpModel propertyDumpModel)
                                    {
                                        currentType =
                                            propertyDumpModel.TypeName.GetTypeDumpModel(_workspaceService
                                                .DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                    else if (first is MethodDumpModel memberDumpModel3)
                                    {
                                        currentType =
                                            memberDumpModel3.TypeName.GetTypeDumpModel(_workspaceService
                                                .DumpModel);
                                        currentExpr = currentExpr.Parent;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }

                    if (currentType != null)
                        list.AddRange(CreateTypeCompletionItems(currentType));
                }
                else
                {
                    list.AddRange(programAst.funcDefinitionAsts.Select(e => CreateCompletionItem(e.funcName, 2,
                        string.Empty, CompletionItemKind.Function,
                        $"function {e.funcName}({string.Join(", ", e.args.Select(a => a.VarName))})")));

                    if (_workspaceService.DumpModel != null)
                    {
                        list.AddRange(_workspaceService.DumpModel.StaticTypes.Select(e =>
                            CreateCompletionItem(e.Key, 5, e.Value.FullName, CompletionItemKind.Class)));
                        list.AddRange(_workspaceService.DumpModel.GlobalMethods.Select(e =>
                            CreateCompletionItem(ToSnakeCase(e.Key), 4, BuildDoc(e.Key, e.Value),
                                CompletionItemKind.Method)));
                        list.AddRange(_workspaceService.DumpModel.GlobalVariables.Select(e =>
                            CreateCompletionItem(ToSnakeCase(e.Key), 1, e.Value.Name,
                                CompletionItemKind.Variable)));
                    }

                    list.AddRange(funcDefinition.args.Select(e =>
                        CreateCompletionItem(e.VarName, 0, string.Empty, CompletionItemKind.Variable,
                            $"paramater {e.VarName}")));

                    list.AddRange(GetKeywordAndSnippetItems());
                }
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

        private ExprAst HittingNextExpr(ExprAst exprAst, Position position)
        {
            return exprAst.LookUp<ExprAst>().FirstOrDefault(e => e != exprAst && Contains(e, position));
        }

        private string GetNameExprAst(ExprAst exprAst)
        {
            if (exprAst is FuncCallAst funcCallAst)
                return funcCallAst.FuncName;
            if (exprAst is VariableAst variableAst)
                return variableAst.VarName;
            if (exprAst is InstanceFuncCallAst instanceFuncCallAst)
                return GetNameExprAst(instanceFuncCallAst.instancefuncCallAst);
            if (exprAst is InstanceFieldAst instanceFieldAst)
                return GetNameExprAst(instanceFieldAst.variableAst);

            return null;
        }

        private ExprAstParent CreateAstParent(ExprAst exprAst, Position position)
        {
            ExprAstParent root = new ExprAstParent(null, exprAst);
            ExprAstParent current = root;
            while (exprAst != null)
            {
                if (exprAst is InstanceFuncCallAst instanceFuncCallAst)
                {
                    var intersect =
                        instanceFuncCallAst.instancefuncCallAst.args.FirstOrDefault(e => Contains(e, position));
                    if (intersect != null)
                    {
                        return CreateAstParent(intersect, position);
                    }

                    current = new ExprAstParent(current, instanceFuncCallAst.instanceExpr);
                    exprAst = instanceFuncCallAst.instanceExpr;
                }
                else if (exprAst is InstanceFieldAst fieldAst)
                {
                    current = new ExprAstParent(current, fieldAst.instanceExpr);
                    exprAst = fieldAst.instanceExpr;
                }
                else
                {
                    return current;
                }
            }

            return current;
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

        private IEnumerable<CompletionItem> CreateTypeCompletionItems(TypeDumpModel typeDumpModel)
        {
            foreach (var pair in typeDumpModel.Members)
            {
                var first = pair.Value.First();
                switch (first)
                {
                    case FieldDumpModel fieldDumpModel:
                        yield return CreateCompletionItem(pair.Key, 0,
                            fieldDumpModel.TypeName.Name, CompletionItemKind.Field);
                        break;

                    case PropertyDumpModel propertyDumpModel:
                        yield return CreateCompletionItem(pair.Key, 1,
                            propertyDumpModel.TypeName.Name, CompletionItemKind.Property);
                        break;

                    case MethodDumpModel memberDumpModel:
                        yield return CreateCompletionItem(ToSnakeCase(pair.Key), 2,
                            BuildDoc(pair.Key, memberDumpModel), CompletionItemKind.Method);
                        break;
                }
            }
        }

        private string BuildDoc(string methodName, MethodDumpModel model)
        {
            return
                $"{model.TypeName.Name} {methodName}({string.Join(',', model.Parameters.Select(e => $"{e.Value.TypeName.Name} {e.Key}"))})";
        }

        private IEnumerable<CompletionItem> GetKeywordAndSnippetItems()
        {
            yield return CreateCompletionItem("let", 3, string.Empty, CompletionItemKind.Keyword);
            yield return CreateCompletionItem("ret", 3, string.Empty, CompletionItemKind.Keyword);
            yield return CreateCompletionItem("yield", 3, string.Empty, CompletionItemKind.Keyword);
            yield return CreateCompletionItem("true", 3, string.Empty, CompletionItemKind.Keyword);
            yield return CreateCompletionItem("false", 3, string.Empty, CompletionItemKind.Keyword);

            yield return CreateSnippetItem("letc", "let ${1:foo} = $2");
            yield return CreateSnippetItem("if", "if ($1) {\n\t$2\n}");
            yield return CreateSnippetItem("else", "else {\n\t$1\n}");
            yield return CreateSnippetItem("elseif", "else if ($1) {\n\t$2\n}");
            yield return CreateSnippetItem("while", "while ($1) {\n\t$2\n}");
            yield return CreateSnippetItem("for", "for ${1:i} = $2, $3, $4 {\n\t$5\n}");
            yield return CreateSnippetItem("foreach", "foreach ${1:val} in $2 {\n\t$3\n}");
            yield return CreateSnippetItem("letac", "let ${1:foo} = {|$2| $3}");
        }

        private bool Contains(IAst ast, Position position)
        {
            return ast.Range.Contain(new Models.Position(position.Line + 1, position.Character + 1));
        }

        private string ToSnakeCase(string str)
        {
            var i = 0;
            return string.Join("",
                str.Select(c => char.IsUpper(c) ? (i++ > 0 ? "_" : "") + char.ToLower(c) : c.ToString()));
        }
    }
}