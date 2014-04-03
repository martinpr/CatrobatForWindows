﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Catrobat.IDE.Core.CatrobatObjects.Formulas.FormulaToken;
using Catrobat.IDE.Core.CatrobatObjects.Formulas.FormulaTree;
using Catrobat.IDE.Core.ExtensionMethods;

namespace Catrobat.IDE.Core.FormulaEditor
{
    internal class FormulaInterpreter
    {
        public IFormulaTree Interpret(IEnumerable<IFormulaToken> tokens, out ParsingError parsingError)
        {
            if (tokens == null)
            {
                parsingError = null;
                return null;
            }

            var tokens2 = tokens.ToList();

            // clear previously set children
            foreach (var token in tokens2.OfType<IFormulaTree>())
            {
                token.ClearChildren();
            }

            if (!InterpretSyntax(tokens2, out parsingError)) return null;

            IFormulaTree formula;
            if (!InterpretSemantic(tokens2, out formula, out parsingError)) return null;

            return formula;
        }

        #region Syntax

        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretSyntax(List<IFormulaToken> tokens, out ParsingError parsingError)
        {
            return InterpretBrackets(tokens, out parsingError) &&
                   InterpretNumbers(tokens, out parsingError) &&
                   InterpretMinus(tokens, out parsingError) &&
                   InterpretSeparators(tokens, out parsingError) &&
                   InterpretChildren(tokens, out parsingError);
        }

        /// <summary>
        /// Maps all opening and closing parentheses and packs them withh their children into <see cref="FormulaTokenParentheses"/>. 
        /// </summary>
        /// <remarks>Running time O(n). </remarks>
        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretBrackets(List<IFormulaToken> tokens, out ParsingError parsingError)
        {
            var openingIndices = new Stack<int>();
            for (var index = 0; index < tokens.Count; index++)
            {
                var element = tokens[index];
                var parenthesis = element as FormulaTokenParenthesis;
                if (parenthesis == null) continue;
                if (parenthesis.IsOpening)
                {
                    var openingIndex = index;
                    openingIndices.Push(openingIndex);
                }
                else
                {
                    if (openingIndices.Count == 0)
                    {
                        parsingError = new ParsingError("Remove unmatched closing bracket. ");
                        return false;
                    }
                    var openingIndex = openingIndices.Pop();
                    var closingIndex = index;
                    var children = tokens.GetRange(openingIndex + 1, closingIndex - openingIndex - 1);
                    tokens.ReplaceRange(openingIndex, closingIndex - openingIndex + 1, FormulaTokenFactory.CreateParenthesesToken(children));
                    index -= closingIndex - openingIndex;
                }
            }
            if (openingIndices.Count != 0)
            {
                parsingError = new ParsingError("Remove unmatched opening bracket. ");
                return false;
            }
            parsingError = null;
            return true;
        }

        /// <summary>
        /// Resolves the ambiguity between <see cref="FormulaTokenDecimalSeparator"/> and <seealso cref="FormulaTokenParameterSeparator"/>. 
        /// </summary>
        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretSeparators(List<IFormulaToken> tokens, out ParsingError parsingError)
        {
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                // TODO
            }
            parsingError = null;
            return true;
        }

        /// <summary>
        /// Resolves the ambiguity between <see cref="FormulaTokenDecimalSeparator"/> and <seealso cref="FormulaTokenParameterSeparator"/>. 
        /// </summary>
        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretNumbers(List<IFormulaToken> tokens, out ParsingError parsingError)
        {
            for (var index = 0; index < tokens.Count; index++)
            {
                var numberTokens = tokens.Skip(index)
                    .TakeWhile(token =>
                        token.GetType() == typeof (FormulaNodeNumber) ||
                        token.GetType() == typeof(FormulaTokenDecimalSeparator))
                    .ToList();
                if (numberTokens.Count == 0) continue;
                if (numberTokens.OfType<FormulaTokenDecimalSeparator>().Skip(1).Any())
                {
                    parsingError = new ParsingError("Remove duplicate decimal separator. ");
                    return false;
                }
                if (!numberTokens.OfType<FormulaNodeNumber>().Any())
                {
                    parsingError = new ParsingError("Remove decimal separator. ");
                    return false;
                }
                var valueString = numberTokens.Aggregate(string.Empty,
                    (accumulate, token) => accumulate + (token.GetType() == typeof (FormulaNodeNumber)
                        ? ((FormulaNodeNumber) token).Value.ToString()
                        : CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));

                double value;
                if (!double.TryParse(valueString, out value))
                {
                    parsingError = new ParsingError("Overflow error. ");
                    return false;
                }
                tokens.ReplaceRange(index, numberTokens.Count, FormulaTokenFactory.CreateNumberToken(value));
            }
            parsingError = null;
            return true;
        }

        /// <summary>
        /// Resolves the ambiguity between <see cref="FormulaNodeSubtract"/> and <seealso cref="FormulaNodeNegativeSign"/>. 
        /// </summary>
        /// <remarks>Running time O(n). </remarks>
        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretMinus(List<IFormulaToken> tokens, out ParsingError parsingError)
        {
            IFormulaToken previousToken = null;
            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                var minusToken = token as FormulaNodeSubtract;
                if (minusToken != null &&
                    (previousToken == null ||
                     previousToken is IFormulaFunction ||
                     previousToken is IFormulaOperator))
                {
                    tokens[index] = FormulaTokenFactory.CreateNegativeSignToken();
                }
                previousToken = token;
            }
            parsingError = null;
            return true;
        }

        /// <summary>
        /// Assigns children to all tokens considering operator order like * before +. 
        /// <remarks>Running time O(n). </remarks>
        /// </summary>
        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretChildren(List<IFormulaToken> tokens, out ParsingError parsingError)
        {
            // number of elements waiting for children
            var pending = 0;

            for (var index = 0; index < tokens.Count; index++)
            {
                var element = tokens[index];

                // process current token
                var processed = false;

                var constantTree = element as ConstantFormulaTree;
                if (constantTree != null)
                {
                    // constants are already finished (see InterpretNumbers)
                    processed = true;
                }
                
                var parenthesesToken = element as FormulaTokenParentheses;
                if (parenthesesToken != null)
                {
                    if (!InterpretParentheses(tokens, index, parenthesesToken, out parsingError)) return false;
                    processed = true;
                }

                if (processed)
                {
                    // add processed token to pending nodes
                    while (pending > 0)
                    {
                        element = tokens[index];
                        processed = false;

                        var pendingElement = tokens[index - 1];
                        parenthesesToken = element as FormulaTokenParentheses; // function parameters
                        var parenthesesNode = element as FormulaNodeParentheses;

                        // consider operator order
                        var operatorNode = pendingElement as IFormulaOperator;
                        if (operatorNode != null)
                        {
                            if (index + 1 < tokens.Count)
                            {
                                var nextOperator = tokens[index + 1] as IFormulaOperator;
                                if (nextOperator != null)
                                {
                                    if (operatorNode.Order < nextOperator.Order) break;
                                }
                            }
                        }

                        var infixOperator = pendingElement as FormulaNodeInfixOperator;
                        if (infixOperator != null)
                        {
                            if (parenthesesToken != null)
                            {
                                // TODO: add translated error like 'Remove parameters'
                                parsingError = new ParsingError("An error occured. ");
                                return false;
                            }
                            if (index - 2 < 0)
                            {
                                parsingError = new ParsingError("Missing left value of infix operator. ");
                                return false;
                            }

                            var previousElement = tokens[index - 2];
                            if (previousElement is FormulaTokenParameterSeparator)
                            {
                                parsingError = new ParsingError("Remove parameter separator. ");
                                return false;
                            }
                            tokens.RemoveAt(index - 2);
                            index--;
                            pending--;
                            infixOperator.LeftChild = (IFormulaTree)previousElement;
                            infixOperator.RightChild = (IFormulaTree)element;
                            processed = true;
                        }

                        var prefixOperator = pendingElement as FormulaNodePrefixOperator;
                        if (prefixOperator != null)
                        {
                            if (parenthesesToken != null)
                            {
                                // TODO: add translated error like 'Remove parameters'
                                parsingError = new ParsingError("An error occured. ");
                                return false;
                            }
                            if (prefixOperator is FormulaNodeNegativeSign && element is FormulaNodeNumber)
                            {
                                // merge negative sign and number
                                tokens[index - 1] = element;
                                ((FormulaNodeNumber) element).Value *= -1;

                            }
                            else
                            {
                                prefixOperator.Child = (IFormulaTree)element;
                            }
                            processed = true;
                        }

                        var binaryFunction = pendingElement as FormulaNodeBinaryFunction;
                        if (binaryFunction != null)
                        {
                            if (parenthesesToken == null)
                            {
                                parsingError = new ParsingError("Too few parameters. ");
                                return false;
                            }
                            if (parenthesesToken.Children.Count != 2)
                            {
                                parsingError = new ParsingError("Too many parameters. ");
                                return false;
                            }
                            binaryFunction.FirstChild = (IFormulaTree) parenthesesToken.Children[0];
                            binaryFunction.SecondChild = (IFormulaTree) parenthesesToken.Children[1];
                            processed = true;
                        }

                        var unaryFunction = pendingElement as FormulaNodeUnaryFunction;
                        if (unaryFunction != null)
                        {
                            if (parenthesesToken != null)
                            {
                                parsingError = new ParsingError("Too many parameters. ");
                                return false;
                            }
                            if (parenthesesNode != null)
                            {
                                unaryFunction.Child = parenthesesNode.Child;
                            }
                            else
                            {
                                unaryFunction.Child = (IFormulaTree)element;
                            }
                            processed = true;
                        }

                        if (!processed) break;

                        tokens.RemoveAt(index);
                        index--;
                        pending--;
                    }
                }
                else
                {
                    pending++;
                }
            }

            if (tokens.OfType<FormulaTokenParentheses>().Any())
            {
                // TODO: add translated error like 'Missing binary function name'
                parsingError = new ParsingError("An error occured. ");
                return false;
            }

            parsingError = null;
            return true;
        }


        /// <summary>
        /// Interprets the children of <see cref="FormulaTokenParentheses"/> and when possible converts to <see cref="FormulaNodeParentheses"/>. 
        /// </summary>
        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretParentheses(List<IFormulaToken> tokens, int index, FormulaTokenParentheses parentheses, out ParsingError parsingError)
        {
            if (!(InterpretSyntax(parentheses.Children, out parsingError) &&
                  InterpretParameters(parentheses.Children, out parsingError))) return false;

            switch (parentheses.Children.Count)
            {
                case 0:
                    parsingError = new ParsingError("Missing parentheses' content. ");
                    return false;
                case 1:
                    tokens[index] = FormulaTreeFactory.CreateParenthesesNode(
                        child: (IFormulaTree) parentheses.Children.FirstOrDefault());
                    break;
            }
            return true;
        }

        /// <summary>
        /// Ensures all tokens are separated by exactly one <see cref="FormulaTokenParameterSeparator"/> and removes them. 
        /// </summary>
        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretParameters(List<IFormulaToken> tokens, out ParsingError parsingError)
        {
            var expectSeparator = false;
            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                if (token is FormulaTokenParameterSeparator)
                {
                    if (index == 0)
                    {
                        parsingError = new ParsingError("Delete leading parameter separator. ");
                        return false;  
                    }
                    if (index == tokens.Count - 1)
                    {
                        parsingError = new ParsingError("Delete trailing parameter separator. ");
                        return false;
                    }
                    if (!expectSeparator)
                    {
                        parsingError = new ParsingError("Delete duplicate parameter separator. ");
                        return false;
                    }
                    tokens.RemoveAt(index);
                }
                else
                {
                    if (expectSeparator)
                    {
                        parsingError = new ParsingError("Missing parameter separator. ");
                        return false;
                    }
                }
                expectSeparator = !expectSeparator;
            }
            parsingError = null;
            return true;
        }

        #endregion

        #region Semantic

        /// <returns><paramref name="parsingError"/> is not <c>null</c></returns>
        private bool InterpretSemantic(List<IFormulaToken> tokens, out IFormulaTree formula, out ParsingError parsingError)
        {


            switch (tokens.Count)
            {
                case 0:
                    // TODO: add translated error like 'Type something'
                    parsingError = new ParsingError("An error occured. ");
                    formula = null;
                    return false;
                case 1:
                    if (tokens.OfType<FormulaTokenParameterSeparator>().Any())
                    {
                        parsingError = new ParsingError("Remove parameter separator. ");
                        formula = null;
                        return false;
                    }
                    formula = (IFormulaTree) tokens.Single();
                    break;
                default:
                    // TODO: add translated error like 'Missing value between operators'
                    // TODO: add translated error like 'Missing operator between values'
                    parsingError = new ParsingError("An error occured. ");
                    formula = null;
                    return false;
            }

            // validate evaluation constraints like sin(True + 4)
            try
            {
                var isNumber = formula.IsNumber();
            }
            catch (Exception ex)
            {
                // TODO: translate
                parsingError = new ParsingError(ex.Message);
                return false;
            }
            parsingError = null;
            return true;
        }

        #endregion
    }
}
