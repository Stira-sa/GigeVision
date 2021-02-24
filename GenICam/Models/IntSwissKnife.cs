﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GenICam
{
    /// <summary>
    /// this is a mathematical class for register parameter computations
    /// </summary>
    public class IntSwissKnife : IMathematical
    {
        /// <summary>
        /// Math Variable Parameter
        /// </summary>
        private Dictionary<string, object> PVariables { get; set; }

        /// <summary>
        /// Formula Expression
        /// </summary>
        private string Formula { get; set; }

        /// <summary>
        /// Formula Result
        /// </summary>
        public Task<double> Value { get; private set; }

        /// <summary>
        /// Main Method that calculate the given formula
        /// </summary>
        /// <param name="gvcp"></param>
        /// <param name="formula"></param>
        /// <param name="pVarible"></param>
        /// <param name="value"></param>
        public IntSwissKnife(string formula, Dictionary<string, object> pVaribles)
        {
            PVariables = pVaribles;
            Formula = formula;

            //Prepare Expression
            Formula = Formula.Replace(" ", "");
            List<char> opreations = new List<char> { '(', '+', '-', '/', '*', '=', '?', ':', ')', '>', '<', '&', '|', '^', '~', '%' };

            foreach (var character in opreations)
                if (opreations.Where(x => x == character).Count() > 0)
                    Formula = Formula.Replace($"{character}", $" {character} ");

            Value = ExecuteFormula();
        }

        /// <summary>
        /// this method calculates the formula and returns the result
        /// </summary>
        /// <param name="intSwissKnife"></param>
        /// <returns></returns>
        private async Task<double> ExecuteFormula()
        {
            foreach (var word in Formula.Split())
            {
                if (PVariables != null)
                {
                    foreach (var pVariable in PVariables)
                    {
                        if (pVariable.Key.Equals(word))
                        {
                            string value = "";
                            //ToDo : Cover all cases
                            if (pVariable.Value is GenInteger integer)
                                value = (await integer.GetValue()).ToString();
                            else if (pVariable.Value is GenIntReg intReg)
                                value = (await intReg.GetValue()).ToString();
                            else if (pVariable.Value is GenMaskedIntReg genMaskedIntReg)
                                value = (await genMaskedIntReg.GetValue()).ToString();
                            else if (pVariable.Value is IntSwissKnife intSwissKnife1)
                                value = (await intSwissKnife1.GetValue()).ToString();
                            else if (pVariable.Value is GenFloat genFloat)
                                value = (await genFloat.GetValue()).ToString();

                            if (value == "")
                                throw new Exception("Failed to read register value", new InvalidDataException());

                            Formula = Formula.Replace(word, value);
                            break;
                        }
                    }
                }
            }

            if (Formula != string.Empty)
                return Evaluate(Formula);

            return 0;
        }

        /// <summary>
        /// this method evaluate the formula expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private double Evaluate(string expression)
        {
            expression = "( " + expression + " )";
            Stack<string> opreators = new Stack<string>();
            Stack<double> values = new Stack<double>();
            bool tempBoolean = false;
            bool isPower = false;
            bool isEqual = false;
            foreach (var word in expression.Split())
            {
                if (word.StartsWith("0x"))
                    values.Push(Int64.Parse(word.Substring(2), System.Globalization.NumberStyles.HexNumber));
                else if (double.TryParse(word, out double tempNumber))
                    values.Push(tempNumber);
                else
                {
                    switch (word)
                    {
                        case "*":
                            if (isPower)
                            {
                                opreators.Pop();
                                opreators.Push("**");
                                isPower = false;
                            }
                            else
                            {
                                isPower = true;
                                opreators.Push(word);
                            }
                            isEqual = false;
                            break;

                        case ">":
                            if (isEqual)
                            {
                                opreators.Pop();
                                opreators.Push(">=");
                            }
                            else
                            {
                                opreators.Push(word);
                            }
                            isEqual = false;
                            isPower = false;
                            break;

                        case "<":
                            if (isEqual)
                            {
                                opreators.Pop();
                                opreators.Push("<=");
                            }
                            else
                            {
                                opreators.Push(word);
                            }
                            isEqual = false;
                            isPower = false;
                            break;

                        case "=":
                            isEqual = true;
                            isPower = false;
                            opreators.Push(word);
                            break;

                        case "(":
                        case "+":
                        case "-":
                        case "/":
                        case "?":
                        case ":":
                        case "&":
                        case "|":
                        case "%":
                        case "^":
                        case "~":
                        case "ATAN":
                        case "COS":
                        case "SIN":
                        case "TAN":
                        case "ABS":
                        case "EXP":
                        case "LN":
                        case "LG":
                        case "SQRT":
                        case "TRUNC":
                        case "FLOOR":
                        case "CELL":
                        case "ROUND":
                        case "ASIN":
                        case "ACOS":
                        case "SGN":
                        case "NEG":
                        case "E":
                        case "PI":
                            opreators.Push(word);
                            isPower = false;
                            isEqual = false;
                            break;

                        case ")":
                            while (values.Count > 0 && opreators.Count > 0)
                            {
                                string opreator = opreators.Pop();

                                tempBoolean = DoMathOpreation(opreator, opreators, values);

                                if (opreator.Equals("?"))
                                {
                                    if (tempBoolean)
                                    {
                                        if (values.Count > 0)
                                            return values.Pop();
                                    }
                                }
                                if (opreators.Count > 0)
                                {
                                    if (opreators.Peek().Equals("("))
                                    {
                                        opreators.Pop();
                                        break;
                                    }
                                }
                            }
                            isPower = false;
                            isEqual = false;
                            break;

                        case "":

                            break;

                        default:
                            isPower = false;
                            isEqual = false;
                            break;
                    }
                }
            }

            if (values.Count > 0)
                return values.Pop();

            if (tempBoolean)
                return 1;
            else
                return 0;

            throw new InvalidDataException("Failed to read the formula");
        }

        private bool DoMathOpreation(string opreator, Stack<string> opreators, Stack<double> values)
        {
            bool tempBoolean = false;
            double value = 0;
            int integerValue = 0;
            //ToDo: Implement (&&) , (||) Operators

            if (opreator.Equals("+"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() + value;
                values.Push(value);
            }
            else if (opreator.Equals("-"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() - value;
                values.Push(value);
            }
            else if (opreator.Equals("*"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() * value;
                values.Push(value);
            }
            else if (opreator.Equals("**"))
            {
                value = (double)values.Pop();
                value = Math.Pow(values.Pop(), value);
                values.Push(value);
            }
            else if (opreator.Equals("/"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() / value;
                values.Push(value);
            }
            else if (opreator.Equals("="))
            {
                var firstValue = (int)GetLongValueFromString(values.Pop().ToString());
                var secondValue = (int)GetLongValueFromString(values.Pop().ToString());

                if (secondValue == firstValue)
                    tempBoolean = true;
            }
            else if (opreator.Equals(">="))
            {
                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                if (GetLongValueFromString(values.Pop().ToString()) >= integerValue)
                    tempBoolean = true;
            }
            else if (opreator.Equals("<="))
            {
                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                if (GetLongValueFromString(values.Pop().ToString()) <= integerValue)
                    tempBoolean = true;
            }
            else if (opreator.Equals("&"))
            {
                if (values.Count > 1)
                {
                    var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                    var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (byte1 & byte2);
                    values.Push(integerValue);
                }
            }
            else if (opreator.Equals("|"))
            {
                if (values.Count > 1)
                {
                    var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                    var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (byte1 | byte2);
                    values.Push(integerValue);
                }
            }
            else if (opreator.Equals("^"))
            {
                if (values.Count > 2)
                {
                    var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                    var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (byte1 ^ byte2);
                    values.Push(integerValue);
                }
            }
            else if (opreator.Equals("~"))
            {
                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                integerValue = ~integerValue;
                values.Push(integerValue);
            }
            else if (opreator.Equals(">"))
            {
                if (opreators.Count > 0)
                {
                    switch (opreators.Peek())
                    {
                        case ">":
                            opreators.Pop();
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) >> integerValue);
                            values.Push(integerValue);
                            break;

                        default:
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                                tempBoolean = true;
                            break;
                    }
                }
                else
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                        tempBoolean = true;
                }
            }
            else if (opreator.Equals("<"))
            {
                if (opreators.Count > 0)
                {
                    switch (opreators.Peek())
                    {
                        case "<":
                            opreators.Pop();
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) << integerValue);
                            values.Push(integerValue);
                            break;

                        default:
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                                tempBoolean = true;
                            break;
                    }
                }
                else
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                        tempBoolean = true;
                }
            }
            else if (opreator.Equals(":"))
            {
            }
            else if (opreator.Equals("ATAN"))
            {
                values.Push(Math.Atan(values.Pop()));
            }
            else if (opreator.Equals("COS"))
            {
                values.Push(Math.Cos(values.Pop()));
            }
            else if (opreator.Equals("SIN"))
            {
                values.Push(Math.Sin(values.Pop()));
            }
            else if (opreator.Equals("TAN"))
            {
                values.Push(Math.Tan(values.Pop()));
            }
            else if (opreator.Equals("ABS"))
            {
                values.Push(Math.Abs(values.Pop()));
            }
            else if (opreator.Equals("EXP"))
            {
                values.Push(Math.Exp(values.Pop()));
            }
            else if (opreator.Equals("LN"))
            {
                values.Push(Math.Log(values.Pop()));
            }
            else if (opreator.Equals("LG"))
            {
                values.Push(Math.Log10(values.Pop()));
            }
            else if (opreator.Equals("SQRT"))
            {
                values.Push(Math.Sqrt(values.Pop()));
            }
            else if (opreator.Equals("TRUNC"))
            {
                values.Push(Math.Truncate(values.Pop()));
            }
            else if (opreator.Equals("FLOOR"))
            {
                values.Push(Math.Floor(values.Pop()));
            }
            else if (opreator.Equals("CELL"))
            {
                values.Push(Math.Ceiling(values.Pop()));
            }
            else if (opreator.Equals("ROUND"))
            {
                values.Push(Math.Round(values.Pop()));
            }
            else if (opreator.Equals("ASIN"))
            {
                values.Push(Math.Asin(values.Pop()));
            }
            else if (opreator.Equals("ACOS"))
            {
                values.Push(Math.Acos(values.Pop()));
            }
            else if (opreator.Equals("TAN"))
            {
                values.Push(Math.Tan(values.Pop()));
            }

            return tempBoolean;
        }

        private long GetLongValueFromString(string value)
        {
            if (value.StartsWith("0x"))
            {
                value = value.Replace("0x", "");
                return long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }

            try
            {
                return long.Parse(value); ;
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        public async Task<Int64> GetValue()
        {
            return (Int64)await ExecuteFormula();
        }
    }
}