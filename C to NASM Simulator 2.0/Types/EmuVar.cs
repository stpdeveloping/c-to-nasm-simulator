using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_to_NASM_Simulator_2._0.Types
{
    class EmuVar<T> : ProcVar
    {
        public int MemoryIndex { get; set; }
        public T Value { get; set; }
    }
}
