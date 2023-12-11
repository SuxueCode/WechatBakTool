using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WechatBakTool.Model
{
    public class YearReport
    {
        public List<ReportItem>? List { get; set; }
        public int Version { get; set; }
    }

    public class ReportItem
    {
        public string ImgName { get; set; } = "";
        public string Type { get; set; } = "";
        public List<TextPostion>? TextPostions { get; set; }
    }

    public class TextPostion
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string TextTemplate { get; set; } = "";
    }
}
