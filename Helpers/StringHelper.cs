using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WechatBakTool.Helpers
{
  public static class StringHelper
    {
        /// <summary>
        /// 清理XML中的非法字符
        /// </summary>
        /// <param name="input">需要清理的字符串</param>
        /// <returns>清理后的字符串</returns>
        public static string CleanInvalidXmlChars(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
            // 这里使用正则表达式匹配非法字符并替换
            return Regex.Replace(input, @"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]", "");
        }

        /// <summary>
        /// 替换文件名中的非法字符为指定字符
        /// </summary>
        /// <param name="fileName">原始文件名</param>
        /// <param name="replacement">用于替换非法字符的字符，默认为 "-"</param>
        /// <returns>清理后的文件名</returns>
        public static string SanitizeFileName(string fileName, char replacement = '-')
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            // 处理Windows系统中文件名不允许的特殊字符
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

            foreach (char invalidChar in invalidFileNameChars)
            {
                fileName = fileName.Replace(invalidChar, '-');
            }

            return fileName;
        }
    }
}