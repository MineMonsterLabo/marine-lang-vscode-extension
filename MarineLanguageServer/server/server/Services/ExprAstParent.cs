using MarineLang.Models.Asts;

namespace MarineLang.LanguageServerImpl.Services
{
    public class ExprAstParent
    {
        public ExprAstParent Parent { get; }
        public ExprAst Current { get; }

        public ExprAstParent(ExprAstParent parent, ExprAst current)
        {
            Parent = parent;
            Current = current;
        }
    }
}