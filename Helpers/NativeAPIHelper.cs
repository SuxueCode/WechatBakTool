using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static WechatBakTool.Helpers.NativeAPI;

namespace WechatBakTool.Helpers
{
    public class NativeAPIHelper
    {
        // Managed native buffer
        internal static IntPtr AllocManagedMemory(uint iSize)
        {
            IntPtr pAlloc = Marshal.AllocHGlobal((int)iSize);
            RtlZeroMemory(pAlloc, iSize);

            return pAlloc;
        }

        // Free managed buffer
        internal static bool FreeManagedMemory(IntPtr pAlloc)
        {
            Marshal.FreeHGlobal(pAlloc);

            return true;
        }

        // Get an array of OBJECT_ALL_TYPES_INFORMATION, describing all object types
        // Win8+ only
        internal static string FindHandleName(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX systemHandleInformation, Process process)
        {
            IntPtr ipHandle = IntPtr.Zero;
            IntPtr openProcessHandle = IntPtr.Zero;
            IntPtr hObjectName = IntPtr.Zero;
            try
            {
                PROCESS_ACCESS_FLAGS flags = PROCESS_ACCESS_FLAGS.DupHandle | PROCESS_ACCESS_FLAGS.VMRead;
                openProcessHandle = OpenProcess(flags, false, process.Id);
                // 通过 DuplicateHandle 访问句柄
                if (!DuplicateHandle(openProcessHandle, systemHandleInformation.HandleValue, GetCurrentProcess(), out ipHandle, 0, false, DUPLICATE_SAME_ACCESS))
                {
                    return "";
                }

                uint nLength = 0;
                hObjectName = AllocManagedMemory(256 * 1024);

                Task.Run(() =>
                {
                    // 查询句柄名称
                    while (NtQueryObject(ipHandle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, hObjectName, nLength, ref nLength) == NTSTATUS_STATUS_INFO_LENGTH_MISMATCH)
                    {
                        FreeManagedMemory(hObjectName);
                        if (nLength == 0)
                        {
                            Console.WriteLine("Length returned at zero!");
                        }
                        hObjectName = AllocManagedMemory(nLength);
                    }
                }).Wait(100);
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
                FreeManagedMemory(hObjectName);
                CloseHandle(ipHandle);
                CloseHandle(openProcessHandle);
            }
            return "";
        }

        internal static List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> GetHandleInfoForPID(uint ProcId)
        {
            // Create return object
            List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> ltei = new List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();

            // Create Buffer variable
            IntPtr BuffPtr = IntPtr.Zero;

            // Loop till success
            uint LoopSize = 0;
            while (true)
            {
                BuffPtr = AllocManagedMemory(LoopSize);
                uint SystemInformationLength = 0;
                uint CallRes = NtQuerySystemInformation(SystemExtendedHandleInformation, BuffPtr, LoopSize, ref SystemInformationLength);
                if (CallRes == NTSTATUS_STATUS_INFO_LENGTH_MISMATCH)
                {
                    FreeManagedMemory(BuffPtr);
                    LoopSize = Math.Max(LoopSize, SystemInformationLength);
                }
                else if (CallRes == NTSTATUS_STATUS_SUCCESS)
                {
                    break;
                }
                else if (CallRes == NTSTATUS_STATUS_ACCESS_DENIED)
                {
                    FreeManagedMemory(BuffPtr);
                    throw new AccessViolationException("[!] Failed to query SystemExtendedHandleInformation: Access Denied");
                }
                else
                {
                    FreeManagedMemory(BuffPtr);
                    throw new InvalidOperationException("[!] Failed to query SystemExtendedHandleInformation.");
                }
            }

            // Read handle count
            Int32 HandleCount = Marshal.ReadInt32(BuffPtr);

            // Move Buff ptr
            BuffPtr = (IntPtr)(BuffPtr.ToInt64() + (IntPtr.Size * 2));

            // Loop handles
            for (int i = 0; i < HandleCount; i++)
            {
                ulong iCurrProcId = (ulong)Marshal.ReadIntPtr((IntPtr)(BuffPtr.ToInt64() + IntPtr.Size));
                if (ProcId == iCurrProcId)
                {
                    // Ptr -> SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
                    SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX? tei = (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX?)Marshal.PtrToStructure(BuffPtr, typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

                    if (tei == null)
                        continue;
                    else
                        ltei.Add(tei.Value);
                }

                // Move Buffptr
                BuffPtr = (IntPtr)(BuffPtr.ToInt64() + Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)));
            }

            // Return list
            return ltei;
        }

        public static List<long> SearchProcessAllMemory(Process process, string searchString)
        {
            IntPtr minAddress = IntPtr.Zero;
            IntPtr maxAddress = IntPtr.MaxValue;
            List<long> addrList = new List<long>();

            while (minAddress.ToInt64() < maxAddress.ToInt64())
            {
                MEMORY_BASIC_INFORMATION64 memInfo;
                int result = VirtualQueryEx(process.Handle, minAddress, out memInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64)));

                if (result == 0)
                {
                    break;
                }

                if (memInfo.State == MEM_COMMIT && (memInfo.Protect == PAGE_EXECUTE || memInfo.Protect == PAGE_EXECUTE_READ || memInfo.Protect == PAGE_EXECUTE_READ || memInfo.Protect == PAGE_READWRITE || memInfo.Protect == PAGE_READONLY))
                {
                    byte[] buffer = new byte[(long)memInfo.RegionSize];
                    bool success = ReadProcessMemory(process.Handle, memInfo.BaseAddress, buffer, buffer.Length, out _);

                    if (success)
                    {
                        byte[] search = Encoding.ASCII.GetBytes(searchString);
                        for (int i = 0; i < buffer.Length - 8; i++)
                        {
                            if (buffer[i] == search[0])
                            {
                                for (int s = 1; s < search.Length; s++)
                                {
                                    if (buffer[i + s] != search[s])
                                        break;
                                    if (s == search.Length - 1)
                                    {
                                        addrList.Add((long)memInfo.BaseAddress + i);
                                    }
                                }
                            }
                        }
                    }
                }
                minAddress = new IntPtr(memInfo.BaseAddress.ToInt64() + (long)memInfo.RegionSize);
            }
            return addrList;
        }

    }
}
