# WechatPCMsgBakTool
微信PC聊天记录备份工具，仅支持Windows

- 支持3.9.6.33版本后，若版本更新可在version.json添加版本号和地址即可完成新版本支持
- 导出图片、视频、音频
- 导出Html文件
- 支持聊天频率分析，全消息库内容搜索

本项目仅做学习使用，主要供个人备份自己的微信记录，请勿用于非法用途。
本项目严禁商用。
如果有什么好的建议或意见，或者遇到什么问题，欢迎提issue，看到会回。

#### 使用
<p>1.打开微信，并登录。</p>
<p>2.在工作区上方点击新增，选择要创建的工作区微信</p>
<p>3.点新建会弹出Handle64的协议说明，同意即可。如没有内容显示，重新点一下新建即可</p>
<p>4.选中刚刚创建的工作区，点击解密。（如当前多开微信，请选择对应的微信进行解谜）</p>
<p>5.选中刚刚创建的工作区，点击读取</p>
<p><b>尽情使用吧！</b></p>

#### 注意
<p>本项目基于.NET开发，需要安装.NET Desktop Runtime，如未安装，双击EXE时会提示。</p>
<p>如果使用过程中发生崩溃，请删除工作区试一下，工作区即根据用户名在运行目录下生成的md5文件夹。</p>
<p>已解密的工作区可以直接读取。</p>
<p>再次强调，主要用于个人备份自己微信使用，请勿用于非法用途，严禁商用！</p>

#### 参考/引用
都是站在大佬们的肩膀上完成的项目，本项目 参考/引用 了以下 项目/文章 内代码。
##### [Mr0x01/WXDBDecrypt.NET](https://github.com/Mr0x01/WXDBDecrypt.NET)
##### [AdminTest0/SharpWxDump](https://github.com/AdminTest0/SharpWxDump)
##### [kn007/silk-v3-decoder](https://github.com/kn007/silk-v3-decoder)
##### [吾爱破解chenhahacjl/微信 DAT 图片解密 （C#）](https://www.52pojie.cn/forum.php?mod=viewthread&tid=1507922)
