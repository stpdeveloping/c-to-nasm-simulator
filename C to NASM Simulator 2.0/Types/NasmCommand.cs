using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_to_NASM_Simulator_2._0.Types
{
    public enum NasmCommand
    {
        NotHandled, 
        Copy,
        Addition,
        Subtraction,
        Multiply,
        Multiplication,
        Division,
        Jump,
        Comparison,
        Return,
        Caller,
        Push,
        Pop
    }
}
