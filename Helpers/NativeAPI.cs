using System;
using System.Runtime.InteropServices;

namespace WechatBakTool.Helpers
{
    public class NativeAPI
    {
        // Constants
        //=================================================

        internal static uint NTSTATUS_STATUS_SUCCESS = 0x0;
        internal static uint NTSTATUS_STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        internal static uint NTSTATUS_STATUS_ACCESS_DENIED = 0xC0000022;

        internal static uint MEM_COMMIT = 0x1000;
        internal static uint PAGE_READONLY = 0x02;
        internal static uint PAGE_READWRITE = 0x04;
        internal static uint PAGE_EXECUTE = 0x10;
        internal static uint PAGE_EXECUTE_READ = 0x20;

        // API Constants
        internal static uint SystemExtendedHandleInformation = 0x40;
        internal static uint DUPLICATE_SAME_ACCESS = 0x2;


        // Structs
        //=================================================

        [StructLayout(LayoutKind.Sequential)]
        internal struct OBJECT_NAME_INFORMATION
        {
            public UNICODE_STRING Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct OSVERSIONINFOEX
        {
            public uint OSVersionInfoSize;
            public uint MajorVersion;
            public uint MinorVersion;
            public uint BuildNumber;
            public uint PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;
            public ushort ServicePackMajor;
            public ushort ServicePackMinor;
            public ushort SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GENERIC_MAPPING
        {
            public uint GenericRead;
            public uint GenericWrite;
            public uint GenericExecute;
            public uint GenericAll;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct OBJECT_TYPE_INFORMATION
        {
            public UNICODE_STRING TypeName;
            public uint TotalNumberOfObjects;
            public uint TotalNumberOfHandles;
            public uint TotalPagedPoolUsage;
            public uint TotalNonPagedPoolUsage;
            public uint TotalNamePoolUsage;
            public uint TotalHandleTableUsage;
            public uint HighWaterNumberOfObjects;
            public uint HighWaterNumberOfHandles;
            public uint HighWaterPagedPoolUsage;
            public uint HighWaterNonPagedPoolUsage;
            public uint HighWaterNamePoolUsage;
            public uint HighWaterHandleTableUsage;
            public uint InvalidAttributes;
            public GENERIC_MAPPING GenericMapping;
            public uint ValidAccessMask;
            public byte SecurityRequired;
            public byte MaintainHandleCount;
            public byte TypeIndex;
            public byte ReservedByte;
            public uint PoolType;
            public uint DefaultPagedPoolCharge;
            public uint DefaultNonPagedPoolCharge;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct OBJECT_ALL_TYPES_INFORMATION
        {
            public uint NumberOfObjectTypes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_HANDLE_INFORMATION_EX
        {
            public IntPtr NumberOfHandles;
            public IntPtr Reserved;
            public SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX[] Handles;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            public IntPtr Object;
            public IntPtr UniqueProcessId;
            public IntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;
        }


        public struct MEMORY_BASIC_INFORMATION64
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        // Enums
        //=================================================

        internal enum OBJECT_INFORMATION_CLASS
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2,
            ObjectAllTypesInformation = 3,
            ObjectHandleInformation = 4
        }

        internal enum POOL_TYPE
        {
            NonPagedPool,
            NonPagedPoolExecute = NonPagedPool,
            PagedPool,
            NonPagedPoolMustSucceed = NonPagedPool + 2,
            DontUseThisType,
            NonPagedPoolCacheAligned = NonPagedPool + 4,
            PagedPoolCacheAligned,
            NonPagedPoolCacheAlignedMustS = NonPagedPool + 6,
            MaxPoolType,
            NonPagedPoolBase = 0,
            NonPagedPoolBaseMustSucceed = NonPagedPoolBase + 2,
            NonPagedPoolBaseCacheAligned = NonPagedPoolBase + 4,
            NonPagedPoolBaseCacheAlignedMustS = NonPagedPoolBase + 6,
            NonPagedPoolSession = 32,
            PagedPoolSession = NonPagedPoolSession + 1,
            NonPagedPoolMustSucceedSession = PagedPoolSession + 1,
            DontUseThisTypeSession = NonPagedPoolMustSucceedSession + 1,
            NonPagedPoolCacheAlignedSession = DontUseThisTypeSession + 1,
            PagedPoolCacheAlignedSession = NonPagedPoolCacheAlignedSession + 1,
            NonPagedPoolCacheAlignedMustSSession = PagedPoolCacheAlignedSession + 1,
            NonPagedPoolNx = 512,
            NonPagedPoolNxCacheAligned = NonPagedPoolNx + 4,
            NonPagedPoolSessionNx = NonPagedPoolNx + 32,
        }

        internal enum PROCESS_ACCESS_FLAGS : uint
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

        // API
        //=================================================

        [DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("ntdll.dll")]
        internal static extern uint RtlGetVersion(
            ref OSVERSIONINFOEX VersionInformation);

        [DllImport("ntdll.dll")]
        internal static extern void RtlZeroMemory(
            IntPtr Destination,
            uint length);

        [DllImport("ntdll.dll")]
        internal static extern uint NtQueryObject(
            IntPtr objectHandle,
            OBJECT_INFORMATION_CLASS informationClass,
            IntPtr informationPtr,
            uint informationLength,
            ref uint returnLength);

        [DllImport("ntdll.dll")]
        internal static extern uint NtQuerySystemInformation(
            uint SystemInformationClass,
            IntPtr SystemInformation,
            uint SystemInformationLength,
            ref uint ReturnLength);


        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(PROCESS_ACCESS_FLAGS dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        internal static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

    }
}