using C_to_NASM_Simulator_2._0.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_to_NASM_Simulator_2._0.Core
{
    class EmuCoreHandler
    {
        public static KeyValuePair<string, int> GetOperands(MoveType moveType, string op1, string op2)
        {
            string varName = string.Empty;
            switch (moveType)
            {
                case MoveType.RegisterToVar:
                    varName = op1.Replace("[", "").Replace("]", "");
                    var registerValue = op2.Equals("AX") || op2.Equals("AL") ? 
                        Emulator.UI.AX : Emulator.UI.BX;
                    return new KeyValuePair<string, int>(varName, registerValue);
                case MoveType.NumValueToVar:
                    varName = op1.Replace("[", "").Replace("]", "");
                    if (float.TryParse(op2.Replace(".", ","), out float result1))
                        return new KeyValuePair<string, int>(varName, Convert.ToInt32(result1));
                    return new KeyValuePair<string, int>(varName, int.Parse(op2));
                case MoveType.CharValueToVar:
                    varName = op1.Replace("[", "").Replace("]", "");
                    return new KeyValuePair<string, int>(varName, op2.Replace("'", "").ElementAt(0));
                case MoveType.VarToRegister:
                    varName = op2.Replace("[", "").Replace("]", "");
                    if (Emulator.Ints.FirstOrDefault(Int => Int.Name == varName) != null)
                        return new KeyValuePair<string, int>(op1, 
                            Emulator.Ints.First(Int => Int.Name == varName).Value);
                    if(Emulator.Floats.FirstOrDefault(Float => Float.Name == varName) != null)
                        return new KeyValuePair<string, int>(op1, Convert.ToInt32
                            (Emulator.Floats.First(Float => Float.Name == varName).Value));
                    return new KeyValuePair<string, int>(op1, Emulator.Chars.First(Char => Char.Name ==
                    varName).Value);
                case MoveType.NumValueToRegister:
                    if(float.TryParse(op2.Replace(".", ","), out float result))
                        return new KeyValuePair<string, int>(op1, Convert.ToInt32(result));
                    return new KeyValuePair<string, int>( op1, Convert.ToInt32(op2));
                case MoveType.CharValueToRegister:
                    return new KeyValuePair<string, int>(op1, op2.Replace("'", "").ElementAt(0));


            }
            return new KeyValuePair<string, int>();
        }
    }
}
