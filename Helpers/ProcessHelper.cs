using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WechatBakTool.Helpers
{
    public class ProcessHelper
    {
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

        public static List<int> FindProcessMemory(IntPtr processHandle, ProcessModule module, string content)
        {
            byte[] buffer = new byte[module.ModuleMemorySize];
            byte[] search = Encoding.ASCII.GetBytes(content);
            // 逐页读取数据

            List<int> offset = new List<int>();
            int readBytes;
            bool success = NativeAPI.ReadProcessMemory(processHandle, module.BaseAddress, buffer, buffer.Length,out readBytes);

            if (!success || readBytes == 0)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"ReadProcessMemory failed. GetLastError: {error}");
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == search[0])
                    {
                        for (int s = 1; s < search.Length; s++)
                        {
                            if (buffer[i + s] != search[s])
                                break;
                            if (s == search.Length - 1)
                                offset.Add(i);
                        }

                    }
                }
            }
            return offset;
        }

        // 这里开始下面是对Windows API引用声明
        public static byte[]? ReadMemoryDate(IntPtr hProcess, IntPtr lpBaseAddress, int nSize = 100)
        {
            byte[] array = new byte[nSize];
            int readByte;
            if (!NativeAPI.ReadProcessMemory(hProcess, lpBaseAddress, array, nSize, out readByte))
                return null;
            else
                return array;
        }

        
    }

}
