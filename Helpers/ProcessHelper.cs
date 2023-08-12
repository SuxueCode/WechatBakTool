using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WechatPCMsgBakTool.Helpers
{
    public class ProcessHelper
    {
        public static Process? GetProcess(string ProcessName)
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length == 0)
                return null;
            else
                return processes[0];
        }

        public static ProcessModule? FindProcessModule(int ProcessId, string ModuleName)
        {
            Process process = Process.GetProcessById(ProcessId);
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName == ModuleName)
                    return module;
            }
            return null;
        }

        public static byte[]? ReadMemoryDate(IntPtr hProcess, IntPtr lpBaseAddress, int nSize = 100)
        {
            byte[] array = new byte[nSize];
            if (ReadProcessMemory(hProcess, lpBaseAddress, array, nSize, 0) == 0)
                return null;
            else
                return array;
        }

        [DllImport("kernel32.dll")]
        public static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, int lpNumberOfBytesRead);
    }
}
