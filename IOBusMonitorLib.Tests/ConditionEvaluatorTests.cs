using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IOBusMonitorLib.Tests
{
    [TestClass]
    public class ConditionEvaluatorTests
    {
        [TestMethod]
        public void Evaluate_IdentityFormula_ReturnsOriginalValue()
        {
            float result = ConditionEvaluator.Evaluate("value", 12.5f);
            Assert.AreEqual(12.5f, result, 0.0001f);
        }

        [TestMethod]
        public void Evaluate_MultipliesValue_ReturnsExpectedResult()
        {
            float result = ConditionEvaluator.Evaluate("value * 2", 6f);
            Assert.AreEqual(12f, result, 0.0001f);
        }

        [TestMethod]
        public void Evaluate_UsesParentheses_ReturnsExpectedResult()
        {
            float result = ConditionEvaluator.Evaluate("(value + 10) / 2", 14f);
            Assert.AreEqual(12f, result, 0.0001f);
        }

        [TestMethod]
        public void Evaluate_AllowsNegativeResults()
        {
            float result = ConditionEvaluator.Evaluate("value - 10", 3f);
            Assert.AreEqual(-7f, result, 0.0001f);
        }

        [TestMethod]
        public void Evaluate_InvalidFormula_ThrowsInvalidOperationException()
        {
            Assert.ThrowsException<InvalidOperationException>(
                () => ConditionEvaluator.Evaluate("value ** 2", 4f));
        }

        [TestMethod]
        public void Evaluate_EmptyFormula_ThrowsInvalidOperationException()
        {
            Assert.ThrowsException<InvalidOperationException>(
                () => ConditionEvaluator.Evaluate(" ", 4f));
        }
    }
}
