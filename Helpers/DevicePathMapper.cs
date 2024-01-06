using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WechatBakTool.Helpers
{
    public static class DevicePathMapper
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint QueryDosDevice([In] string lpDeviceName, [Out] StringBuilder lpTargetPath, [In] int ucchMax);

        public static string FromDevicePath(string devicePath)
        {
            var drive = Array.Find(
                DriveInfo.GetDrives(), d =>
                devicePath.StartsWith(d.GetDevicePath() + "\\", StringComparison.InvariantCultureIgnoreCase)
            );
            return drive != null ?
                devicePath.ReplaceFirst(drive.GetDevicePath(), drive.GetDriveLetter()) :
                null;
        }

        private static string GetDevicePath(this DriveInfo driveInfo)
        {
            var devicePathBuilder = new StringBuilder(128);
            return QueryDosDevice(driveInfo.GetDriveLetter(), devicePathBuilder, devicePathBuilder.Capacity + 1) != 0 ?
                devicePathBuilder.ToString() :
                null;
        }

        private static string GetDriveLetter(this DriveInfo driveInfo)
        {
            return driveInfo.Name.Substring(0, 2);
        }

        private static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
