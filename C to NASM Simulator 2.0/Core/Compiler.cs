using C_to_NASM_Simulator_2._0.Types;
using C_to_NASM_Simulator_2._0.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace C_to_NASM_Simulator_2._0.Core
{
    class Compiler
    {
        public static List<string> initVars = new List<string>();
        public static Dictionary<string, int> initProcedures = new Dictionary<string, int>();
        public static int labelsCount = -1;
        public static int labelsOutCount = 0;
        public static int conditionEndsCount = 0;
        public static int bracketBalance = 0;

        private static List<ProcVar> tempArgs = new List<ProcVar>();
        private static bool procedureFlag = false;
        private static bool ifBlock = false;
        private static bool elseFlag = false;

        public string Output = string.Empty;
        
        public Compiler(string codeLine)
        {
            switch (VerbCheck(codeLine))
            {
                case Verb.ProcDeclaration:
                    Output = ProcInit(codeLine);
                    break;
                case Verb.ProcCall:
                    Output = ProcCall(codeLine);
                    break;
                case Verb.BlockClosing:
                    if (!procedureFlag)
                    {
                        bracketBalance--;
                        if (bracketBalance == 0)
                            if (elseFlag)
                            {
                                Output = $"JMP conditionEnd{conditionEndsCount}{Environment.NewLine}conditionEnd{conditionEndsCount}:{Environment.NewLine}";
                                elseFlag = false;
                                conditionEndsCount++;
                            }
                            else if (ifBlock && !elseFlag)
                                Output = $"JMP conditionEnd{conditionEndsCount}" +
                                        $"{Environment.NewLine}labelOut{labelsOutCount}" +
                                        $":{Environment.NewLine}";
                    }
                    if (procedureFlag && tempArgs.Any())
                    {
                        tempArgs.Reverse();
                        tempArgs.ForEach(arg => Output += $"MOV AX, " +
                        $"[{NasmVar($"{arg.Type} {arg.Name};", true).InnerString(0, NasmVar($"{arg.Type} {arg.Name};", true).IndexOf(" ")).Trim()}]" +
                            $"{Environment.NewLine}PUSH AX{Environment.NewLine}");
                        Output += $"RET{ Environment.NewLine}";
                        procedureFlag = false;
                        tempArgs.Clear();
                    }
                    else if (procedureFlag)
                    {
                        Output += $"RET{ Environment.NewLine}";
                        procedureFlag = false;
                        tempArgs.Clear();
                    }
                    break;
                case Verb.Variable:
                    Output = NasmVar(codeLine, true);
                    if(!String.IsNullOrEmpty(Output))
                        if (!Output.Substring(0, 3).Equals("MOV"))
                            initVars.Add(Output);                    
                    break;
                case Verb.AssignInstruction:
                    Output = NasmVar(codeLine, false);
                    break;
                case Verb.ArithmeticInstruction:
                    Output = NasmEquation(codeLine);
                    break;
                case Verb.Condition:
                    procedureFlag = false;
                    Output = NasmCondition(codeLine);
                    break;
            }
        }

        private Verb VerbCheck(string verb)
        {
            if (verb.ElementAt(0).Equals('}'))
                return Verb.BlockClosing;
            if (verb.Length >= 4)
            {
                if (verb.Substring(0, 4).Equals("void"))
                    return Verb.ProcDeclaration;
                if (verb.Substring(0, 2).Equals("if") || verb.Substring(0, 4).Equals("else"))
                    return Verb.Condition;
            }
            if (!verb.Contains(';'))
                return Verb.Unidentified;
            if (verb.HasEquatChar())
                return Verb.ArithmeticInstruction;
            if (verb.IsVarDeclaration())
                return Verb.Variable;
            if (verb.Contains(" = "))
                return Verb.AssignInstruction;
            if (verb.Contains("(") && verb.Contains(")"))
                return Verb.ProcCall;
            return Verb.Unidentified;
        }

        private string ProcInit(string procDeclaration)
        {
            string _out = string.Empty;
            procedureFlag = true;
            string procName = procDeclaration.InnerString(procDeclaration.IndexOf("d") + 1, procDeclaration.IndexOf("(")).Trim();
            string _args = procDeclaration.InnerString(procDeclaration.IndexOf("(") + 1, procDeclaration.IndexOf(")") + 1).Replace(" ", "");
            string temp = string.Empty;
            _args.AsEnumerable().ToList().ForEach(ch =>
            {
                if (!ch.Equals(',') && !ch.Equals(')'))
                    temp += ch;
                else
                {
                    if (!string.IsNullOrEmpty(temp)) 
                    tempArgs.Add(new ProcVar
                    {
                        Type = temp.InnerString(0, temp.IndexOf("&")),
                        Name = temp.InnerString(temp.IndexOf("&") + 1, temp.Length)
                    });
                    temp = string.Empty;
                }
            });
            tempArgs.Reverse();
            _out += $"{procName}:{Environment.NewLine}";
            bool validation = true;
            tempArgs.ForEach(arg =>
            {
                if (arg.Type.Equals("int"))
                {
                    _out += $"POP AX{Environment.NewLine}{NasmVar($"{arg.Type} {arg.Name};", true)}{Environment.NewLine}" +
                    $"MOV [{NasmVar($"{arg.Type} {arg.Name};", true).InnerString(0, NasmVar($"{arg.Type} {arg.Name};", true).IndexOf(" ")).Trim()}], AX" +
                    $"{Environment.NewLine}";
                    initVars.Add(NasmVar($"{arg.Type} {arg.Name};", true));
                }
                else
                {
                    _out = string.Empty;
                    validation = false;
                }
            });
            if (!validation) tempArgs.Clear();
            else initProcedures.Add(procName, tempArgs.Count);
            return string.IsNullOrEmpty(_out) || !validation ? null : _out;
        }
        private string ProcCall(string procCalling)
        {
            string _out = string.Empty;
            bool validation = true;
            string procName = procCalling.InnerString(0, procCalling.IndexOf("(")).Trim();
            string _args = procCalling.InnerString(procCalling.IndexOf("(") + 1, procCalling.IndexOf(")") + 1)
                .Replace(" ", "");
            string temp = string.Empty;
            _args.AsEnumerable().ToList().ForEach(ch =>
            {
                string varName = string.Empty;
                string varType = string.Empty;
                temp += ch;
                if (ch.Equals(',') || ch.Equals(')'))
                {
                    varName = temp.InnerString(0, temp.IndexOf(ch)).Trim();
                    if (!_args.Equals(")"))
                    {
                        if (!Utils.VarExists(varName)) validation = false;
                        if (initVars.FirstOrDefault
                        (str => str.InnerString(0, str.IndexOf(" ")).Equals(varName) &&
                        str.InnerString(str.IndexOf(" ") + 1, str.LastIndexOf(" ")).Equals("DW")) == null)
                            validation = false;
                        if (!initProcedures.ContainsKey(procName)) validation = false;
                        tempArgs.Add(new ProcVar
                        {
                            Type = "int",
                            Name = varName
                        });
                    }
                    temp = string.Empty;
                }
            });
            initProcedures.TryGetValue(procName, out int argsCount);
            if (argsCount > tempArgs.Count || argsCount < tempArgs.Count)
                validation = false;
            if(validation)
                tempArgs.ForEach(arg =>
                {
                    _out += $"MOV AX, [{NasmVar($"{arg.Type} {arg.Name};", true).InnerString(0, NasmVar($"{arg.Type} {arg.Name};", true).IndexOf(" ")).Trim()}]" +
                        $"{Environment.NewLine}PUSH AX{Environment.NewLine}";
                });
            else
            return null;
            _out += $"CALL {procName}{Environment.NewLine}";
            tempArgs.Reverse();
            tempArgs.ForEach(arg =>
            {
                _out += $"POP AX{Environment.NewLine}" +
                $"MOV [{NasmVar($"{arg.Type} {arg.Name};", true).InnerString(0, NasmVar($"{arg.Type} {arg.Name};", true).IndexOf(" ")).Trim()}], AX" +
                $"{Environment.NewLine}";
            });
            tempArgs.Clear();
            return _out;
        }

        private string NasmCondition(string condition)
        {
            string _out = string.Empty;
            ifBlock = true;
            if (condition.Contains("else") && !condition.Contains("else if"))
            {
                elseFlag = true;
                bracketBalance++;
            }
            
            if (condition.Contains("if"))
            {
                if (!condition.Contains("else if"))
                    conditionEndsCount++;
                bracketBalance++;
                if (bracketBalance<2)
                labelsOutCount++;
                labelsCount++;         
                var nasmJumps = new Dictionary<string, string>
                {
                    { "==", "JE" },
                    { "!=", "JNE" },
                    { ">", "JG"},
                    { ">=", "JGE" },
                    { "<", "JL" },
                    { "<=", "JLE" }
                };                
                List<string> conditionSigns = new List<string> { "==", "!=", ">=", "<=", ">", "<" };
                List<string> limiters = new List<string> { "&&", "||", ")" };
                string input = condition.InnerString(condition.IndexOf("(") + 1, condition.LastIndexOf(")") + 1);
                string temp = string.Empty;
                string previousBoolSign = string.Empty;
                int andCounter = 0;
                foreach (char c in input)
                {
                    temp += c;
                    string var = string.Empty;
                    string conditionSign = conditionSigns
                        .Where(sign => temp.Contains(sign) && sign.Length == 2)
                        .FirstOrDefault();
                    conditionSign = string.IsNullOrEmpty(conditionSign) ? 
                         conditionSigns.Where(sign => temp.Contains(sign))
                        .FirstOrDefault() : conditionSign;
                    string value = string.Empty;
                    if (!string.IsNullOrEmpty(conditionSign))
                    {
                        var = temp.InnerString(0, temp.IndexOf(conditionSign)).Trim();
                        if (!Utils.VarExists(var))
                            return null;
                        foreach (string limiter in limiters)
                        {
                            if (temp.Contains(limiter))
                            {
                                if (conditionSign.Contains("="))
                                    value = temp.InnerString(temp.LastIndexOf("=") + 1, temp.IndexOf(limiter)).Trim();
                                else if (conditionSign.Contains("<"))
                                    value = temp.InnerString(temp.IndexOf("<") + 1, temp.IndexOf(limiter)).Trim();
                                else if (conditionSign.Contains(">"))
                                    value = temp.InnerString(temp.IndexOf(">") + 1, temp.IndexOf(limiter)).Trim();
                                if (!value.ElementAt(0).Equals('\'') && !Char.IsDigit(value.ElementAt(0)))
                                    if (!Utils.VarExists(value))
                                        return null;
                                    else value = $"[{value}]";                               
                                string nasmJump = string.Empty;
                                nasmJumps.TryGetValue(conditionSign, out nasmJump);
                                _out+= $"MOV BX, [{var}]{Environment.NewLine}CMP BX, {value}{Environment.NewLine}";                               
                                switch (limiter)
                                {
                                    case "||":
                                        if (previousBoolSign.Equals("&&"))
                                            labelsCount++;
                                        _out += $"{nasmJump} label{labelsCount}{Environment.NewLine}";
                                        previousBoolSign = limiter;
                                        break;
                                    case "&&":
                                        if (previousBoolSign.Equals("||"))
                                        {
                                            _out += $"{nasmJump} label{labelsCount}{Environment.NewLine}";
                                            _out += $"JMP labelOut{labelsOutCount}{Environment.NewLine}";
                                            _out += $"label{ labelsCount}:{ Environment.NewLine}";                                      
                                        }
                                        else
                                        {
                                            andCounter++;
                                            if(andCounter!=1)
                                            labelsCount++;
                                            _out += $"{nasmJump} label{labelsCount}{Environment.NewLine}";
                                            _out += $"JMP labelOut{labelsOutCount}{Environment.NewLine}";
                                            _out += $"label{ labelsCount}:{ Environment.NewLine}";
                                        }
                                        previousBoolSign = limiter;
                                        break;
                                    default:
                                        if (previousBoolSign.Equals("&&"))                                        
                                            labelsCount++;
                                        _out += $"{nasmJump} label{labelsCount}{Environment.NewLine}";
                                        _out += $"JMP labelOut{labelsOutCount}{Environment.NewLine}";
                                        _out += $"label{ labelsCount}:{ Environment.NewLine}";                                        
                                        break;
                                }                               
                                temp = string.Empty;
                            }
                        }
                    }
                }
            }          
            return _out;
        }       
        private string VarAssign(string varName, string varValue, bool typeDef)
        {
            if (Char.IsLetter(varValue.ElementAt(0)))
                if (!Utils.VarExists(varValue))
                    return null;
                    if (!typeDef)
                if (!Utils.VarExists(varName))
                    return null;          
            if (Char.IsLetter(varValue.ElementAt(0)))
            {
                string declaredVar = initVars.SingleOrDefault(s => s.Substring(0, varName.Length).Equals(varName));
                string assignedVar = initVars.SingleOrDefault(s => s.Substring(0, varValue.Length).Equals(varValue));
                string assignValStr = assignedVar.InnerString(assignedVar.LastIndexOf(' ') + 1, assignedVar.Length + 1);
                int numbVal = int.MinValue;
                if (assignedVar.ElementAt(assignedVar.LastIndexOf(' ') + 1).Equals('\'')) numbVal = 0;
                else if (!int.TryParse(assignValStr, out numbVal)) return null;
                if ((declaredVar.Contains(" DB ") || declaredVar.Contains(" RESB ")) &&
                    (assignedVar.Contains(" DB ") || assignedVar.Contains(" RESB ")))
                {
                    if (numbVal > 255) return null;
                    return "MOV AL, 0" + Environment.NewLine + "MOV AL, [" + varValue + "]"
                        + Environment.NewLine + "MOV [" + varName + "], AL";
                }
                if ((declaredVar.Contains(" DW ") || declaredVar.Contains(" RESW ")) &&
                    (assignedVar.Contains(" DW ") || assignedVar.Contains(" RESW ")))
                {
                    if (numbVal > 65535) return null;
                    return "MOV AX, 0" + Environment.NewLine + "MOV AX, [" + varValue + "]"
                        + Environment.NewLine + "MOV [" + varName + "], AX";
                }
                if ((declaredVar.Contains(" DB ") || declaredVar.Contains(" RESB ")) &&
                    (assignedVar.Contains(" DW ") || assignedVar.Contains(" RESW ")))
                {
                    if (numbVal > 65535) return null;
                    return "MOV AX, 0" + Environment.NewLine + "MOV AL, [" + varValue + "]"
                        + Environment.NewLine + "MOV [" + varName + "], AX";
                }
                if ((declaredVar.Contains(" DW ") || declaredVar.Contains(" RESW ")) &&
                    (assignedVar.Contains(" DB ") || assignedVar.Contains(" RESB ")))
                    return null;
            }
                else if(varValue.ElementAt(0).Equals('\''))
                return "MOV AL, 0" + Environment.NewLine + "MOV AL, " + varValue
                        + Environment.NewLine + "MOV [" + varName + "], AL";
            else
            {
                int val = int.MinValue;
                if (int.TryParse(varValue, out val))
                {
                    string declaredVar = initVars.SingleOrDefault(s => s.Substring(0, varName.Length).Equals(varName));
                    if (declaredVar.Contains(" DB ") || declaredVar.Contains(" RESB "))
                        return val > 255 ? null : "MOV AL, 0" + Environment.NewLine + "MOV AL, " + varValue
                        + Environment.NewLine + "MOV [" + varName + "], AL";
                    if (declaredVar.Contains(" DW ") || declaredVar.Contains(" RESW "))
                        return val > 65535 || val == int.MinValue ? null : "MOV AX, 0" + Environment.NewLine + "MOV AX, " 
                            + varValue + Environment.NewLine + "MOV [" + varName + "], AX";
                }
            }
            return null;
        }
        private string ArrValAssign(string arrName, string arrValue, int valIndex)
        {
            if (Char.IsLetter(arrValue.ElementAt(0)))
                if (initVars.SingleOrDefault(s => s.Substring(0, arrValue.Length).Equals(arrValue)) == null)
                    return null;
            if (initVars.SingleOrDefault(s => s.Substring(0, arrName.Length).Equals(arrName)) == null)
                return null;
            string declaredArr = initVars.SingleOrDefault(s => s.Contains(arrName + " "));
            int arrLength = declaredArr.Contains("'") ? declaredArr.CharCounter('\'') / 2
                : declaredArr.CharCounter(',') + 1;
            if (valIndex >= arrLength) return null;
            if (declaredArr.Contains('.') && !declaredArr.Contains("'.'")) return null;
            if (Char.IsLetter(arrValue.ElementAt(0)))
            {
                if (declaredArr.Contains(" DB ") || declaredArr.Contains(" RESB "))
                    return "";
                if (declaredArr.Contains(" DW ") || declaredArr.Contains(" RESW "))
                    return "";
            }
            if (arrValue.ElementAt(0).Equals('\''))
                return "MOV AL, 0" + Environment.NewLine + "MOV AL, " + arrValue
                        + Environment.NewLine + "MOV [" + arrName + "+" + valIndex + "], AL";
            else
            {
                int val = int.MinValue;
                if (int.TryParse(arrValue.Replace(";", ""), out val))
                {
                    string declaredVar = initVars.SingleOrDefault(s => s.Substring(0, arrName.Length).Equals(arrName));
                    if (declaredVar.Contains(" DB ") || declaredVar.Contains(" RESB "))
                        return val > 255 ? null : "MOV [" + arrName + "+" + valIndex + "], " + arrValue;
                    if (declaredVar.Contains(" DW ") || declaredVar.Contains(" RESW "))
                        return val > 65535 ? null : "MOV [" + arrName + "+" + valIndex + "*2" + "], " + arrValue;
                }
            }
            return null;
        }
        private string NasmEquation(string equat)
        {
            string _out = String.Empty;
            List<char> calcChars = new List<char> { '+', '-', '*', '/', '(', ')' };
            string val = String.Empty;
            List<string> equatVals = new List<string>();
            string varName = String.Empty;
            string varType = String.Empty;
            if (equat.IsVarDeclaration())
            {
                varName = equat.InnerString(equat.IndexOf(' ') + 1, equat.IndexOf('=')).Trim();
                varType = equat.InnerString(0, equat.IndexOf(' '));
                if (initVars.SingleOrDefault(s => s.Contains(varName + " ")) != null)
                    return null;
                else
                {
                    _out += varName + " RESW 1" + Environment.NewLine;
                    initVars.Add(varName + " RESW 1");
                }
            }
            else
            {
                varName = equat.InnerString(0, equat.IndexOf('=') - 1);
                if (initVars.FirstOrDefault(s => s.Contains(varName + " ")) == null)
                    return null;
            }
            if (!equat.Contains("(") || !equat.Contains(")"))
            {
                equat = equat.InnerString(equat.IndexOf("=") + 1, equat.IndexOf(";"));
                equat = "="
                    + Regex.Replace(equat, @"[^+-]*[^*/ +-][^+-]*", @"($0)") + ";";
            }
            else
            {
                equat = equat.InnerString(equat.IndexOf('=') + 1, equat.IndexOf(";"));
                equat = equat.Replace("(", String.Empty);
                equat = equat.Replace(")", String.Empty);
                equat = "="
                + Regex.Replace(equat, @"[^*/]*[^*/ +-][^*/]*", @"($0)") + ";";
            }
            for (int i = equat.IndexOf('='); i < equat.Length; i++)
            {
                if (Char.IsLetter(equat.ElementAt(i)) || Char.IsDigit(equat.ElementAt(i)))
                    val += equat.ElementAt(i);
                foreach (char calcChar in calcChars)
                    if (equat.ElementAt(i).Equals(calcChar) || equat.ElementAt(i).Equals(';'))
                    {
                        if (val.FirstOrDefault(s => Char.IsLetter(s)) != 0 && val.FirstOrDefault(s => Char.IsDigit(s)) != 0)
                            return null;
                        if (!String.IsNullOrEmpty(val))
                            equatVals.Add(val);
                        if (!equat.ElementAt(i).Equals(';')) equatVals.Add(calcChar.ToString());

                        val = String.Empty;
                    }
            }
            List<string>.Enumerator e = equatVals.GetEnumerator();
            string divOrMul = String.Empty;
            bool isInParenthese = false;
            bool isPlus = true;
            bool isMulOrDiv = false;
            int parentheseCount = 0;
            _out += "MOV AX, 0" + Environment.NewLine;
            while (e.MoveNext())
            {              
                if (e.Current.Equals("("))
                {
                    isInParenthese = true;
                    parentheseCount++;
                    if (isMulOrDiv)
                        continue;
                    else if (isPlus || parentheseCount > 1)
                        _out += "MOV AX, ";
                    else
                        _out += "SUB AX, ";
                    continue;
                }
                else if (e.Current.Equals(")"))
                {
                    isInParenthese = false;
                    if (isMulOrDiv)
                    {
                        _out += "MOV [" + varName + "], AX";
                        continue;
                    }
                    if (isPlus && parentheseCount > 1)
                        _out += "ADD [" + varName + "], AX";
                    else if(!isPlus && parentheseCount > 1)
                    _out += "SUB [" + varName + "], AX";
                    else
                    _out += "MOV [" + varName + "], AX";
                    continue;
                }
                if (e.Current.Equals("-") && isInParenthese)               
                    _out += "SUB AX, ";     
                
                else if (e.Current.Equals("-") && !isInParenthese)
                {
                    if (parentheseCount != 0)
                        _out += Environment.NewLine;
                    isPlus = false;
                }
                else if (e.Current.Equals("+") && isInParenthese)
                    _out += "ADD AX, ";

                else if (e.Current.Equals("+") && !isInParenthese)
                {
                    _out += Environment.NewLine;
                    isPlus = true;
                }
                else if (e.Current.Equals("*"))
                {
                    if(isInParenthese)
                    _out += "MOV BX, ";
                    else
                    {
                        _out += Environment.NewLine + "MOV BX, ";
                        isMulOrDiv = true;
                    }
                    divOrMul = "MUL BX";
                }
                else if (e.Current.Equals("/"))
                {
                    if (isInParenthese)
                        _out += "MOV BX, ";
                    else
                    {
                        _out += Environment.NewLine + "MOV BX, ";
                        isMulOrDiv = true;
                    }
                    divOrMul = "DIV BX";
                }
                else if (String.IsNullOrEmpty(divOrMul))
                {
                    if (Char.IsDigit(e.Current.ElementAt(0)))
                        _out += e.Current + Environment.NewLine;
                    else if (initVars.SingleOrDefault(s => s.Contains(e.Current + " ")) != null)
                        _out += "[" + e.Current + "]" + Environment.NewLine;
                    else return null;
                }
                else
                {
                    if(char.IsLetter(e.Current.ElementAt(0)))
                        _out += $"[{e.Current}]{Environment.NewLine}{divOrMul}{Environment.NewLine}";
                    else 
                        _out += e.Current + Environment.NewLine + divOrMul + Environment.NewLine;
                    divOrMul = String.Empty;
                }            
            }
            return _out;
        }
        private string NasmArr(string arrType, string arrName, 
            Array arrValues, int declaredLght, bool isInitialized)
        {
            string outArr = "";
            if (arrType.Equals("char")) {
                outArr = arrName + " DB ";
                if (arrValues == null && isInitialized && declaredLght != -1)
                    for (int i = 0; i < declaredLght; i++)
                        outArr += i != declaredLght - 1 ? "'0', " : "'0'";
                if (arrValues != null && isInitialized && declaredLght != -1)
                {
                    for (int i = 0; i < arrValues.Length; i++)
                        outArr += i != arrValues.Length - 1 ? "'" + arrValues.GetValue(i) + "', "
                            : "'" + arrValues.GetValue(i) + "'";
                    for (int i = arrValues.Length; i < declaredLght; i++)
                        outArr += ", '0'";
                }
                if (arrValues != null && isInitialized && declaredLght == -1)
                    for (int i = 0; i < arrValues.Length; i++)
                        outArr += i != arrValues.Length - 1 ? "'" + arrValues.GetValue(i) + "', "
                        : "'" + arrValues.GetValue(i) + "'";
                if (arrValues == null && !isInitialized && declaredLght != -1)
                    return arrName + " RESB " + declaredLght;
                return outArr;
            } else
            {
                switch (arrType)
                {
                    case "bool": outArr = arrName + " DB "; break;
                    case "int": outArr = arrName + " DW "; break;
                    case "float": outArr = arrName + " DD "; break;
                    case "double": outArr = arrName + " DQ "; break;
                }
                if (arrValues == null && isInitialized && declaredLght != -1)
                    for (int i = 0; i < declaredLght; i++)
                        outArr += i != declaredLght - 1 ? "0, " : "0";
                if (arrValues != null && isInitialized && declaredLght != -1)
                {
                    for (int i = 0; i < arrValues.Length; i++)
                        outArr += i != arrValues.Length - 1 ? arrValues.GetValue(i).ToString().Replace(",", ".") + ", "
                            : arrValues.GetValue(i).ToString().Replace(",", ".");
                    for (int i = arrValues.Length; i < declaredLght; i++)
                        outArr += ", 0";
                }
                if (arrValues != null && isInitialized && declaredLght == -1)
                    for (int i = 0; i < arrValues.Length; i++)
                        outArr += i != arrValues.Length - 1 ? arrValues.GetValue(i).ToString().Replace(",", ".") + ", "
                        : arrValues.GetValue(i).ToString().Replace(",", ".");
                if (arrValues == null && !isInitialized && declaredLght != -1)
                {
                    if (arrType.Equals("bool")) return arrName + " RESB " + declaredLght;
                    if (arrType.Equals("int")) return arrName + " RESW " + declaredLght;
                    if (arrType.Equals("float")) return arrName + " RESD " + declaredLght;
                }
                return outArr;
            }
        }
        private string NasmVar(string inputVar, bool hasTypeDef)
        {
                int firstSpace = inputVar.IndexOf(' ');
                string varType = inputVar.Substring(0, firstSpace);
                if (inputVar.Contains('[') && inputVar.Contains(']'))
            {
                string _arrName = inputVar.InnerString(firstSpace + 1, inputVar.IndexOf('['));
                if (int.TryParse(inputVar.InnerString(inputVar.IndexOf('[') + 1,
                    inputVar.IndexOf(']')), out int _arrLength))
                {
                    if (inputVar.Contains("{") && inputVar.Contains("}"))
                    {
                        Array _arrValues = !varType.Equals("char") ?
                             inputVar.NumbsInArr(',', '{') : inputVar.CharsInArr(',', '{');
                        if (_arrValues == null) return NasmArr(varType, _arrName, null, _arrLength, true);
                        return _arrLength >= _arrValues.Length ?
                            NasmArr(varType, _arrName, _arrValues, _arrLength, true) : null;
                    }

                    else
                    {
                        if (inputVar.Contains('='))
                        {
                            int firstValChar = inputVar.IndexOf('=') + 1;
                            string arrValue = inputVar.InnerString(firstValChar, inputVar.Length - 1).Trim();
                            int arrValIndex = _arrLength;
                            _arrName = inputVar.InnerString(0, inputVar.IndexOf('['));
                            if (_arrName.Contains(' ')) return null;
                            else return ArrValAssign(_arrName, arrValue, arrValIndex);
                        }
                        return NasmArr(varType, _arrName, null, _arrLength, false);
                    }
                }
                else if (inputVar.Contains("{") && inputVar.Contains("}"))
                {
                    Array _arrValues = !varType.Equals("char") ?
                         inputVar.NumbsInArr(',', '{') : inputVar.CharsInArr(',', '{');
                    return NasmArr(varType, _arrName, _arrValues, -1, true);
                }
            }
            if (inputVar.Contains("="))
            {
                string varName = hasTypeDef==true ? inputVar.InnerString(firstSpace, inputVar.IndexOf('=')).Trim() 
                    : inputVar.InnerString(0, inputVar.IndexOf('=')).Trim();
                int firstValChar = inputVar.IndexOf('=') + 1;
                string varValue = inputVar.InnerString(firstValChar, inputVar.Length - 1).Trim();
                if (Char.IsLetter(varValue.ElementAt(0))) return VarAssign(varName, varValue, hasTypeDef);
                switch (varType)
                    {
                        case "char":
                        if (Char.IsDigit(varValue.ElementAt(0)))
                        {
                            varValue.Replace("'", "");
                            char asciiChar = (char)int.Parse(varValue);
                            varValue = "" + asciiChar;
                        }
                        return varName + " DB " + varValue;
                        case "bool": return varName + " DB " + varValue;
                        case "int": return varName + " DW " + varValue;
                        case "float":
                        varValue = varValue.Contains(",") ? varValue.Replace(',', '.') : varValue;
                        return varName + " DD " + varValue;
                        default:
                        return VarAssign(varName, varValue, hasTypeDef);
                    }
            } else
            {
                string varName = inputVar.InnerString(firstSpace + 1, inputVar.Length - 1);
                    switch (varType)
                    {
                        case "char": return varName + " RESB 1";
                        case "bool": return varName + " RESB 1";
                        case "int": return varName + " RESW 1";
                        case "float": return varName + " RESD 1";
                    }

            }
            return null;           
        }
    }
}
