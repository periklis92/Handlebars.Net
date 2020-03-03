using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace HandlebarsDotNet.Compiler
{
    internal class FunctionBuilder
    {
        private readonly HandlebarsConfiguration _configuration;
        private static readonly Expression<Action<TextWriter, object>> _emptyLambda =
            Expression.Lambda<Action<TextWriter, object>>(
                Expression.Empty(),
                Expression.Parameter(typeof(TextWriter)),
                Expression.Parameter(typeof(object)));

        public FunctionBuilder(HandlebarsConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static Expression Reduce(Expression expression, CompilationContext compilationContext)
        {
            expression = CommentVisitor.Visit(expression, compilationContext);
            expression = UnencodedStatementVisitor.Visit(expression, compilationContext);
            expression = PartialBinder.Bind(expression, compilationContext);
            expression = StaticReplacer.Replace(expression, compilationContext);
            expression = IteratorBinder.Bind(expression, compilationContext);
            expression = BlockHelperFunctionBinder.Bind(expression, compilationContext);
            expression = DeferredSectionVisitor.Bind(expression, compilationContext);
            expression = HelperFunctionBinder.Bind(expression, compilationContext);
            expression = BoolishConverter.Convert(expression, compilationContext);
            expression = PathBinder.Bind(expression, compilationContext);
            expression = SubExpressionVisitor.Visit(expression, compilationContext);
            expression = HashParameterBinder.Bind(expression, compilationContext);
            
            return expression;
        }

        public Expression<Action<TextWriter, object>> Compile(IEnumerable<Expression> expressions, Expression parentContext, string templatePath = null)
        {
            try
            {
                if (expressions.Any() == false)
                {
                    return _emptyLambda;
                }
                if (expressions.IsOneOf<Expression, DefaultExpression>() == true)
                {
                    return _emptyLambda;
                }
                var compilationContext = new CompilationContext(_configuration);
                var expression = CreateExpressionBlock(expressions);
                expression = Reduce(expression, compilationContext);
                
                return ContextBinder.Bind(expression, compilationContext, parentContext, templatePath);
            }
            catch (Exception ex)
            {
                throw new HandlebarsCompilerException("An unhandled exception occurred while trying to compile the template", ex);
            }
        }

        public Action<TextWriter, object> Compile(IEnumerable<Expression> expressions, string templatePath = null)
        {
            try
            {

                var expression = Compile(expressions, null, templatePath);
                return expression.Compile();
            }
            catch (Exception ex)
            {
                throw new HandlebarsCompilerException("An unhandled exception occurred while trying to compile the template", ex);
            }
        }
        
        private static Expression CreateExpressionBlock(IEnumerable<Expression> expressions)
        {
            return Expression.Block(expressions);
        }
    }
}

