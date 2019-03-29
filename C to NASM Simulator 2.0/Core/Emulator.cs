using C_to_NASM_Simulator_2._0.Types;
using C_to_NASM_Simulator_2._0.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C_to_NASM_Simulator_2._0.Core
{
    class Emulator
    {
        public static List<EmuVar<float>> Floats = new List<EmuVar<float>>();
        public static List<EmuVar<int>> Ints = new List<EmuVar<int>>();
        public static List<EmuVar<char>> Chars = new List<EmuVar<char>>();
        public static List<EmuVar<int>> VarsInMemory = new List<EmuVar<int>>();
        public static int AvailableMemBlock = 1;

        public static UserInterface UI = new UserInterface();

        private MainWindow View;       
        private string CurrentCommand;

        private static KeyValuePair<string, int> CurrentArithmeticCmd;
        private MoveType currentMoveType;
        //private ObjectType currentObjectType;
        private static string CurrentJump;
        private static string CallingFunction;
        private static bool equalsFlag = false;
        private static bool greaterFlag = false;
        private static bool lessFlag = false;
        private static bool notEqualsFlag = false;
        private MoveType toVar = MoveType.Null;

        public Emulator(MainWindow view)
        {
            View = view;
        }
        public void NextStep()
        {
            var map = new Dictionary<NasmCommand, Action>
            {
                { NasmCommand.NotHandled, VarHandler },
                { NasmCommand.Copy, MovHandler },
                { NasmCommand.Addition, AddHandler },
                { NasmCommand.Subtraction, SubHandler },
                { NasmCommand.Multiplication, MulHandler },
                { NasmCommand.Division, DivHandler },
                { NasmCommand.Jump, JumpHandler },
                { NasmCommand.Comparison, CmpHandler },
                { NasmCommand.Push, () => StackHandler(NasmCommand.Push) },
                { NasmCommand.Pop, () => StackHandler(NasmCommand.Pop) },
                { NasmCommand.Return, RetHandler },
                { NasmCommand.Caller, CallHandler }
            };
            string cmdWithIndex = View.OutputList.SelectedValue.ToString();
            CurrentCommand = cmdWithIndex.InnerString(cmdWithIndex.IndexOf(" ") + 1, cmdWithIndex.Length);
            map.TryGetValue(CmdCheck(CurrentCommand), out Action ProperHandler);
            ProperHandler();
            UI.IP = View.OutputList.SelectedIndex;
        }
        private void MemoryMapHandler(int memoryIndex, int value)
        {
            switch(memoryIndex)
            {
                    case 1:
                        UI.MemoryMap.M01 = value;
                        break;
                    case 2:
                        UI.MemoryMap.M02 = value;
                        break;
                    case 3:
                        UI.MemoryMap.M03 = value;
                        break;
                    case 4:
                        UI.MemoryMap.M04 = value;
                        break;
                    case 5:
                        UI.MemoryMap.M05 = value;
                        break;
                    case 6:
                        UI.MemoryMap.M06 = value;
                        break;
                    case 7:
                        UI.MemoryMap.M07 = value;
                        break;
                    case 8:
                        UI.MemoryMap.M08 = value;
                        break;
            }
        
        }

        private void VarHandler()
        {            
            if (CurrentCommand.Contains("DW") || CurrentCommand.Contains("RESW"))
            {
                string varName = CurrentCommand.InnerString(0, CurrentCommand.IndexOf(" ")).Trim();
                var _int = Ints.FirstOrDefault(Int => Int.Name == varName);
                _int.MemoryIndex = AvailableMemBlock;
                VarsInMemory.Add(_int);
                MemoryMapHandler(AvailableMemBlock, _int.Value);
                AvailableMemBlock++;
                View.MemoryStructure.Items.Refresh();
            }
            IncreaseIndex();
        }

        private void MovHandler()
        {
            PrepareOperands(CurrentCommand.Replace("MOV", ""));
            var operandsPair = CurrentArithmeticCmd;
            if (toVar.Equals(MoveType.Null))
                switch (currentMoveType)
                {
                    case MoveType.VarToRegister:
                    case MoveType.NumValueToRegister:
                    case MoveType.CharValueToRegister:
                        UI.AX = operandsPair.Key.Equals("AX") ? operandsPair.Value : UI.AX;
                        UI.BX = operandsPair.Key.Equals("AX") ? UI.BX : operandsPair.Value;
                        break;

                }
            else switch (toVar)
                {
                    case MoveType.RegisterToVar:
                    case MoveType.NumValueToVar:
                        if (Ints.FirstOrDefault(Int => Int.Name == operandsPair.Key) != null)
                        {
                            var temp = Ints.First(Int => Int.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = 0;
                                temp.Type = "DW";
                            }
                            temp.Value = operandsPair.Value;
                            EmuVar<int> mem = VarsInMemory.First(var => var.Name == temp.Name);
                            MemoryMapHandler(mem.MemoryIndex, temp.Value);
                            View.MemoryStructure.Items.Refresh();
                            Ints.Remove(Ints.First(Int => Int.Name == operandsPair.Key));
                            Ints.Add(temp);
                        }
                        if (Floats.FirstOrDefault(Float => Float.Name == operandsPair.Key) != null)
                        {
                            var temp = Floats.First(Float => Float.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = 0;
                                temp.Type = "DD";
                            }
                            temp.Value = operandsPair.Value;
                            Floats.Remove(Floats.First(Float => Float.Name == operandsPair.Key));
                            Floats.Add(temp);
                        }
                        break;
                    case MoveType.CharValueToVar:
                        if (Chars.FirstOrDefault(Char => Char.Name == operandsPair.Key) != null)
                        {
                            var temp = Chars.First(Chars => Chars.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = (char)0;
                                temp.Type = "DB";
                            }
                            temp.Value = (char) operandsPair.Value;
                            Chars.Remove(Chars.First(Char => Char.Name == operandsPair.Key));
                            Chars.Add(temp);
                        }
                        break;
                }
            IncreaseIndex();

        }
        private void AddHandler()
        {
            PrepareOperands(CurrentCommand.Replace("ADD", ""));
            var operandsPair = CurrentArithmeticCmd;
            if (toVar.Equals(MoveType.Null))
                switch (currentMoveType)
                {
                    case MoveType.VarToRegister:
                    case MoveType.NumValueToRegister:
                    case MoveType.CharValueToRegister:
                        UI.AX = operandsPair.Key.Equals("AX") ? UI.AX + operandsPair.Value : UI.AX;
                        UI.BX = operandsPair.Key.Equals("AX") ? UI.BX : UI.BX + operandsPair.Value;
                        break;

                }
            else switch (toVar)
                {
                    case MoveType.RegisterToVar:
                    case MoveType.NumValueToVar:
                        if (Ints.FirstOrDefault(Int => Int.Name == operandsPair.Key) != null)
                        {
                            var temp = Ints.First(Int => Int.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = 0;
                                temp.Type = "DW";
                            }
                            temp.Value += operandsPair.Value;
                            EmuVar<int> mem = VarsInMemory.First(var => var.Name == temp.Name);
                            MemoryMapHandler(mem.MemoryIndex, temp.Value);
                            View.MemoryStructure.Items.Refresh();
                            Ints.Remove(Ints.First(Int => Int.Name == operandsPair.Key));
                            Ints.Add(temp);
                        }
                        if (Floats.FirstOrDefault(Float => Float.Name == operandsPair.Key) != null)
                        {
                            var temp = Floats.First(Float => Float.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = 0;
                                temp.Type = "DD";
                            }
                            temp.Value += operandsPair.Value;
                            Floats.Remove(Floats.First(Float => Float.Name == operandsPair.Key));
                            Floats.Add(temp);
                        }
                        break;
                    case MoveType.CharValueToVar:
                        if (Chars.FirstOrDefault(Char => Char.Name == operandsPair.Key) != null)
                        {
                            var temp = Chars.First(Chars => Chars.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = (char)0;
                                temp.Type = "DB";
                            }
                            temp.Value += (char)operandsPair.Value;
                            Chars.Remove(Chars.First(Char => Char.Name == operandsPair.Key));
                            Chars.Add(temp);
                        }
                        break;
                }
            IncreaseIndex();
        }
        private void SubHandler()
        {
            PrepareOperands(CurrentCommand.Replace("SUB", ""));
            var operandsPair = CurrentArithmeticCmd;
            if (toVar.Equals(MoveType.Null))
                switch (currentMoveType)
                {
                    case MoveType.VarToRegister:
                    case MoveType.NumValueToRegister:
                    case MoveType.CharValueToRegister:
                        UI.AX = operandsPair.Key.Equals("AX") ? UI.AX - operandsPair.Value : UI.AX;
                        UI.BX = operandsPair.Key.Equals("AX") ? UI.BX : UI.BX - operandsPair.Value;
                        break;

                }
            else switch (toVar)
                {
                    case MoveType.RegisterToVar:
                    case MoveType.NumValueToVar:
                        if (Ints.FirstOrDefault(Int => Int.Name == operandsPair.Key) != null)
                        {
                            var temp = Ints.First(Int => Int.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = 0;
                                temp.Type = "DW";
                            }
                            temp.Value -= operandsPair.Value;
                            EmuVar<int> mem = VarsInMemory.First(var => var.Name == temp.Name);
                            MemoryMapHandler(mem.MemoryIndex, temp.Value);
                            View.MemoryStructure.Items.Refresh();
                            Ints.Remove(Ints.First(Int => Int.Name == operandsPair.Key));
                            Ints.Add(temp);
                        }
                        if (Floats.FirstOrDefault(Float => Float.Name == operandsPair.Key) != null)
                        {
                            var temp = Floats.First(Float => Float.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = 0;
                                temp.Type = "DD";
                            }
                            temp.Value -= operandsPair.Value;
                            Floats.Remove(Floats.First(Float => Float.Name == operandsPair.Key));
                            Floats.Add(temp);
                        }
                        break;
                    case MoveType.CharValueToVar:
                        if (Chars.FirstOrDefault(Char => Char.Name == operandsPair.Key) != null)
                        {
                            var temp = Chars.First(Chars => Chars.Name == operandsPair.Key);
                            if (temp.Type.Contains("RES"))
                            {
                                temp.Value = (char)0;
                                temp.Type = "DB";
                            }
                            temp.Value -= (char)operandsPair.Value;
                            Chars.Remove(Chars.First(Char => Char.Name == operandsPair.Key));
                            Chars.Add(temp);
                        }
                        break;
                }
            IncreaseIndex();
        }
        private void MulHandler()
        {
            UI.AX *= UI.BX;
            IncreaseIndex();
        }
        private void DivHandler()
        {
            UI.AX /= UI.BX;
            IncreaseIndex();
        }
        private void JumpHandler()
        {
            var map = new Dictionary<string, bool>
            {
                { "JE", equalsFlag },
                { "JG", greaterFlag},
                { "JL", lessFlag },
                { "JNE", notEqualsFlag },
                { "JGE", greaterFlag || equalsFlag },
                { "JLE", lessFlag || equalsFlag },
                { "JMP", true }
            };
            string label = CurrentCommand
                .InnerString(CurrentCommand.IndexOf(" "), CurrentCommand.Length)
                .Trim();
            map.TryGetValue(CurrentJump, out bool isConditionMet);
            View.OutputList.SelectedIndex = isConditionMet ? 
                UI.ObservableLines.IndexOf(Utils.GetLineWithId($"{label}:")) :
                    View.OutputList.SelectedIndex + 1;
            equalsFlag = false;
            greaterFlag = false;
            lessFlag = false;
            notEqualsFlag = false;              
        }
        private void CmpHandler()
        {
            PrepareOperands(CurrentCommand.Replace("CMP", ""));
            var operandsPair = CurrentArithmeticCmd;
                switch (currentMoveType)
                {
                    case MoveType.VarToRegister:
                    case MoveType.NumValueToRegister:
                    case MoveType.CharValueToRegister:
                    if (UI.BX == operandsPair.Value)
                        equalsFlag = true;
                    if (UI.BX != operandsPair.Value)
                        notEqualsFlag = true;
                    if (UI.BX > operandsPair.Value)
                        greaterFlag = true;
                    if (UI.BX < operandsPair.Value)
                        lessFlag = true;
                        break;
                }
            IncreaseIndex();
        }
        private void StackHandler(NasmCommand stackCmdType)
        {
            switch (stackCmdType)
            {
                case NasmCommand.Push:
                    UI.Stack.Insert(0, UI.AX);
                    break;
                case NasmCommand.Pop:
                    UI.AX = UI.Stack.First();
                    UI.Stack.RemoveAt(0);
                    break;
            }
            IncreaseIndex();
        }
        private void RetHandler()
        {
            View.OutputList.SelectedIndex = 
                UI.ObservableLines.IndexOf(Utils.GetLineWithId($"CALL { CallingFunction }"));
            IncreaseIndex();
        }
        private void CallHandler()
        {
            CallingFunction = CurrentCommand.Replace("CALL ", "");
            View.OutputList.SelectedIndex = 
                UI.ObservableLines.IndexOf(Utils.GetLineWithId($"{ CallingFunction }:"));
        }
        private void IncreaseIndex()
        {
            View.OutputList.SelectedIndex += 1;
        }
        private void PrepareOperands(string unhandledOperands)
        {
                var list = new List<char>(unhandledOperands);
                string temp = string.Empty, operand1 = string.Empty, operand2 = string.Empty;
                list.ForEach(ch =>
                {
                    if (!ch.Equals(','))
                        temp += ch;
                    else
                    {
                        operand1 = temp.Trim();
                        temp = string.Empty;
                    }
                });
                operand2 = temp.Trim();
            if (operand1.Equals("AX") || operand1.Equals("BX") 
                || operand1.Equals("AL") || operand1.Equals("BL"))
                if (operand2.ElementAt(0).Equals('['))
                {
                    CurrentArithmeticCmd = EmuCoreHandler.GetOperands(
                        MoveType.VarToRegister,
                        operand1, operand2);
                    currentMoveType = MoveType.VarToRegister;
                }
                else if (char.IsNumber(operand2.ElementAt(0)))
                {
                    CurrentArithmeticCmd = EmuCoreHandler.GetOperands(
                        MoveType.NumValueToRegister,
                        operand1, operand2);
                    currentMoveType = MoveType.NumValueToRegister;
                }
                else if (operand2.ElementAt(0).Equals('\''))
                {
                    CurrentArithmeticCmd = EmuCoreHandler.GetOperands(
                        MoveType.CharValueToRegister,
                        operand1, operand2);
                    currentMoveType = MoveType.CharValueToRegister;
                }
            if (operand1.ElementAt(0).Equals('['))
                if (operand2.Equals("AX") || operand2.Equals("BX"))
                {
                    CurrentArithmeticCmd = EmuCoreHandler.GetOperands(
                        MoveType.RegisterToVar,
                        operand1, operand2);
                    toVar = MoveType.RegisterToVar;
                }
                else if (char.IsNumber(operand2.ElementAt(0)))
                {
                    CurrentArithmeticCmd = EmuCoreHandler.GetOperands(
                        MoveType.NumValueToVar,
                        operand1, operand2);
                    toVar = MoveType.NumValueToVar;
                }
                else if (operand2.ElementAt(0).Equals('\''))
                {
                    CurrentArithmeticCmd = EmuCoreHandler.GetOperands(
                        MoveType.CharValueToVar,
                        operand1, operand2);
                    toVar = MoveType.CharValueToVar;
                }           
        }

        private NasmCommand CmdCheck(string cmd)
        {
            var map = new Dictionary<string, NasmCommand>
            {
                { "MOV", NasmCommand.Copy},
                { "ADD", NasmCommand.Addition },
                { "SUB", NasmCommand.Subtraction },
                { "MUL", NasmCommand.Multiplication },
                { "DIV", NasmCommand.Division },
                { "JE", NasmCommand.Jump },
                { "JG", NasmCommand.Jump},
                { "JL", NasmCommand.Jump },
                { "JNE", NasmCommand.Jump },
                { "JGE", NasmCommand.Jump },
                { "JLE", NasmCommand.Jump },
                { "JMP", NasmCommand.Jump },
                { "CMP", NasmCommand.Comparison },
                { "PUSH", NasmCommand.Push },
                { "POP", NasmCommand.Pop },
                { "RET", NasmCommand.Return },
                { "CALL", NasmCommand.Caller }
            };

            if (cmd.Length >= 4)
                if (map.TryGetValue(cmd.Substring(0, 4), out NasmCommand type))
                    return type;
            if (cmd.Length >= 3)
                if (map.TryGetValue(cmd.Substring(0, 3), out NasmCommand type))
                {
                    if (type.Equals(NasmCommand.Jump))
                        CurrentJump = cmd.Substring(0, 3);
                    return type;
                }
            if (cmd.Length >= 2)
                if (map.TryGetValue(cmd.Substring(0, 2), out NasmCommand type))
                {
                    if (type.Equals(NasmCommand.Jump))
                        CurrentJump = cmd.Substring(0, 2);
                    return type;
                }
            return NasmCommand.NotHandled;
        }       
    }
}
