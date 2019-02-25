using C_to_NASM_Simulator_2._0.Core;
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
            int conditionCounter = Compiler.conditionEndCount;
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
        public static bool VarExists(string varName)
        {
            if (Compiler.initVars.SingleOrDefault(s => s.Substring(0, varName.Length).Equals(varName)) == null)
                return false;
            return true;
        }
        public static void RefreshCompiler()
        {
            Compiler.initVars.Clear();
            Compiler.initProcedures.Clear();
            Compiler.labelsCount = -1;
            Compiler.labelsOutCount = 0;
            Compiler.conditionEndCount = 0;
            Compiler.bracketBalance = 0;
        }
    }
}
