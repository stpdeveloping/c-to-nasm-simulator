using C_to_NASM_Simulator_2._0.Core;
using C_to_NASM_Simulator_2._0.Types;
using System.Collections.Generic;
using System.Linq;

namespace C_to_NASM_Simulator_2._0.Utility
{
    class Utils
    {
        public static List<string> StartLabelFix(List<string> stdOutput)
        {
            if (stdOutput.FirstOrDefault(line => line.Equals("RET")) != null)
            {
                int lastRetIndex = stdOutput.FindLastIndex(line => line.Equals("RET"));
                stdOutput.Insert(lastRetIndex + 1, "START:");
            }
            else stdOutput.Insert(0, "START:");
            return stdOutput;
        }

        public static List<string> IfElseFix(List<string> stdOutput)
        {
            int conditionCounter = Compiler.conditionEndsCount;
            var conditions = new List<int>();
            int i = 0;
            while (i <= conditionCounter)
            {
                conditions.Add(i);
                i++;
            }
            conditions.ForEach(condition => {
                if(stdOutput.FindLastIndex(line => line.Equals($"JMP conditionEnd{condition}")) != -1)
                {
                    int foundLastOccur = stdOutput.FindLastIndex(line => line.Equals($"JMP conditionEnd{condition}"));
                    if (stdOutput.ElementAt(foundLastOccur + 1).Contains("labelOut")){
                        string thisLabelOut = stdOutput.ElementAt(foundLastOccur + 1).Remove(
                            stdOutput.ElementAt(foundLastOccur + 1).Length-1);
                        while (stdOutput.FindIndex(l => l.Equals($"JMP {thisLabelOut}")) != -1)
                        {
                            int lastJmpLblOutIndex = stdOutput.FindIndex(l => l.Equals($"JMP {thisLabelOut}"));
                            stdOutput.RemoveAt(lastJmpLblOutIndex);
                            stdOutput.Insert(lastJmpLblOutIndex, $"JMP conditionEnd{condition}");
                        }
                        stdOutput.RemoveAt(foundLastOccur+1);
                        stdOutput.Insert(foundLastOccur+1, $"conditionEnd{condition}:");
                    }                   
                }
                });
            return stdOutput;
        }
        public static void LoadVarsToEmu()
        {
            Compiler.initVars.ForEach(var =>
            {
                string type = var.InnerString(var.IndexOf(" "), var.LastIndexOf(" ")).Trim();
                string name = var.InnerString(0, var.IndexOf(" ")).Trim();
                string value = var.InnerString(var.LastIndexOf(" "), var.Length).Trim();
                switch (type)
                {
                    case "RESB":
                    case "DB":
                        if (value.ElementAt(0).Equals('\''))
                            Emulator.Chars.Add(new EmuVar<char>
                            {
                                Type = type,
                                Name = name,
                                Value = value.Replace("'", "").ElementAt(0)
                            });
                        else if (char.IsNumber(value.ElementAt(0)))
                        {
                            Emulator.Ints.Add(new EmuVar<int>
                            {
                                Type = type,
                                Name = name,
                                Value = string.IsNullOrWhiteSpace(value) ?
                                0 : int.Parse(value)
                            });
                            Emulator.Chars.Add(new EmuVar<char>
                            {
                                Type = type,
                                Name = name,
                                Value = char.MinValue
                            });
                        }
                        break;
                    case "RESD":
                    case "DD":
                        Emulator.Floats.Add(new EmuVar<float>
                        {
                            Type = type,
                            Name = name,
                            Value = float.Parse(value.Replace(".", ","))
                        });
                        break;
                    default:
                        Emulator.Ints.Add(new EmuVar<int>
                        {
                            Type = type,
                            Name = name,
                            Value = int.TryParse(value, out int val) ? val : 0
                        });
                        break;
                }
            });
        }
        public static bool VarExists(string varName)
        {
            if (Compiler.initVars.FirstOrDefault(s => s.Substring(0, varName.Length)
            .Equals(varName)) == null)
                return false;
            return true;
        }
        public static void RefreshSimulator()
        {
            Compiler.initVars.Clear();
            Compiler.initProcedures.Clear();
            Compiler.labelsCount = -1;
            Compiler.labelsOutCount = 0;
            Compiler.conditionEndsCount = 0;
            Compiler.bracketBalance = 0;
            Emulator.Ints.Clear();
            Emulator.Floats.Clear();
            Emulator.Chars.Clear();
        }
    }
}
