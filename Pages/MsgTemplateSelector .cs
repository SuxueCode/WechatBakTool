using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WechatBakTool.Model;

namespace WechatBakTool.Pages
{
    public class MsgTemplateSelector : DataTemplateSelector
    {

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement? element = container as FrameworkElement;

            if (element != null && item != null && item is WXMsg)
            {
                WXMsg? wxmsg = item as WXMsg;

                if (wxmsg == null)
                    return null;

                if (wxmsg.Type == 1)
                    return
                        element.FindResource("MsgText") as DataTemplate;
                else if (wxmsg.Type == 3)
                    return
                        element.FindResource("MsgImage") as DataTemplate;
                else
                    return
                        element.FindResource("MsgText") as DataTemplate;
            }
            return null;
        }

    }
}
