using System;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Evaluates a C# expression string (e.g. <c>"value * 1.8 + 32"</c>)
    /// where the single parameter <c>value</c> is the raw measurement value.
    /// </summary>
    public static class ConditionEvaluator
    {
        /// <summary>
        /// Compiles and executes the <paramref name="condition"/> against
        /// the supplied <paramref name="value"/>.
        /// </summary>
        /// <param name="condition">
        /// C# expression that must evaluate to <see cref="float"/>.
        /// The identifier <c>value</c> represents the input value.
        /// </param>
        /// <param name="value">Raw measurement value.</param>
        /// <returns>Result of the expression as <see cref="float"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the expression is empty or cannot be parsed/compiled.
        /// </exception>
        public static float Evaluate(string condition, float value)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new InvalidOperationException("Condition string is empty.");

            try
            {
                var param = Expression.Parameter(typeof(float), "value");
                var expression = DynamicExpressionParser.ParseLambda(
                                    new[] { param }, typeof(float), condition);

                return (float)expression.Compile().DynamicInvoke(value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid condition expression.", ex);
            }
        }
    }
}
