using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WechatPCMsgBakTool.Helpers
{
    public class ProcessHelper
    {
        private const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        private const int DUPLICATE_CLOSE_SOURCE = 0x1;
        private const int DUPLICATE_SAME_ACCESS = 0x2;

        private const int CNST_SYSTEM_HANDLE_INFORMATION = 0x10;
        private const int OBJECT_TYPE_MUTANT = 17;

        public static Process GetProcess(string ProcessName)
        {
            Process[] processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length == 0)
                return null;
            else if(processes.Length > 1) {
                SelectWechat selectWechat = new SelectWechat();
                MessageBox.Show("检测到有多个微信，请选择本工作区对应的微信");
                selectWechat.ShowDialog();
                if (selectWechat.SelectProcess == null)
                    return null;

                Process? p = processes.ToList().Find(x => x.Id.ToString() == selectWechat.SelectProcess.ProcessId);
                if (p == null)
                    return null;
                return p;
            }
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

        public static List<SYSTEM_HANDLE_INFORMATION> GetHandles(Process process)
        {
            List<SYSTEM_HANDLE_INFORMATION> aHandles = new List<SYSTEM_HANDLE_INFORMATION>();
            int handle_info_size = Marshal.SizeOf(new SYSTEM_HANDLE_INFORMATION()) * 20000;
            IntPtr ptrHandleData = IntPtr.Zero;
            try
            {
                ptrHandleData = Marshal.AllocHGlobal(handle_info_size);
                int nLength = 0;

                while (NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ptrHandleData, handle_info_size, ref nLength) == STATUS_INFO_LENGTH_MISMATCH)
                {
                    handle_info_size = nLength;
                    Marshal.FreeHGlobal(ptrHandleData);
                    ptrHandleData = Marshal.AllocHGlobal(nLength);
                }

                long handle_count = Marshal.ReadIntPtr(ptrHandleData).ToInt64();
                IntPtr ptrHandleItem = ptrHandleData + Marshal.SizeOf(ptrHandleData);

                for (long lIndex = 0; lIndex < handle_count; lIndex++)
                {
                    SYSTEM_HANDLE_INFORMATION? oSystemHandleInfo = new SYSTEM_HANDLE_INFORMATION();
                    oSystemHandleInfo = Marshal.PtrToStructure(ptrHandleItem, oSystemHandleInfo.GetType()) as SYSTEM_HANDLE_INFORMATION?;
                    if (oSystemHandleInfo == null)
                        throw new Exception("获取SYSTEM_HANDLE_INFORMATION失败");
                    ptrHandleItem += Marshal.SizeOf(new SYSTEM_HANDLE_INFORMATION());
                    if (oSystemHandleInfo.Value.ProcessID != process.Id) { continue; }
                    aHandles.Add(oSystemHandleInfo.Value);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(ptrHandleData);
            }
            return aHandles;
        }
        public static string FindHandleName(SYSTEM_HANDLE_INFORMATION systemHandleInformation, Process process)
        {
            IntPtr ipHandle = IntPtr.Zero;
            IntPtr openProcessHandle = IntPtr.Zero;
            IntPtr hObjectName = IntPtr.Zero;
            try
            {
                PROCESS_ACCESS_FLAGS flags = PROCESS_ACCESS_FLAGS.DupHandle | PROCESS_ACCESS_FLAGS.VMRead;
                openProcessHandle = OpenProcess(flags, false, process.Id);
                // 通过 DuplicateHandle 访问句柄
                if (!DuplicateHandle(openProcessHandle, new IntPtr(systemHandleInformation.Handle), GetCurrentProcess(), out ipHandle, 0, false, DUPLICATE_SAME_ACCESS))
                {
                    return "";
                }

                int nLength = 0;
                hObjectName = Marshal.AllocHGlobal(256 * 1024);

                // 查询句柄名称
                while ((uint)(NtQueryObject(ipHandle, (int)OBJECT_INFORMATION_CLASS.ObjectNameInformation, hObjectName, nLength, ref nLength)) == STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(hObjectName);
                    if (nLength == 0)
                    {
                        Console.WriteLine("Length returned at zero!");
                        return "";
                    }
                    hObjectName = Marshal.AllocHGlobal(nLength);
                }
                OBJECT_NAME_INFORMATION? objObjectName = new OBJECT_NAME_INFORMATION();
                objObjectName = Marshal.PtrToStructure(hObjectName, objObjectName.GetType()) as OBJECT_NAME_INFORMATION?;
                if (objObjectName == null)
                    return "";
                if (objObjectName.Value.Name.Buffer != IntPtr.Zero)
                {
                    string? strObjectName = Marshal.PtrToStringUni(objObjectName.Value.Name.Buffer);
                    if (strObjectName != null)
                        return strObjectName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(hObjectName);
                CloseHandle(ipHandle);
                CloseHandle(openProcessHandle);
            }
            return "";
        }
        // 这里开始下面是对Windows API引用声明
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
        [DllImport("ntdll.dll")]
        private static extern uint NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, ref int returnLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(PROCESS_ACCESS_FLAGS dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("ntdll.dll")]
        private static extern int NtQueryObject(IntPtr ObjectHandle, int ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, ref int returnLength);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool GetHandleInformation(IntPtr hObject, out uint lpdwFlags);


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SYSTEM_HANDLE_INFORMATION
        { // Information Class 16
            public ushort ProcessID;
            public ushort CreatorBackTrackIndex;
            public byte ObjectType;
            public byte HandleAttribute;
            public ushort Handle;
            public IntPtr Object_Pointer;
            public IntPtr AccessMask;
        }
        private enum OBJECT_INFORMATION_CLASS : int
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2,
            ObjectAllTypesInformation = 3,
            ObjectHandleInformation = 4
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct OBJECT_NAME_INFORMATION
        {
            public UNICODE_STRING Name;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }
        [Flags]
        private enum PROCESS_ACCESS_FLAGS : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }
    }

}
