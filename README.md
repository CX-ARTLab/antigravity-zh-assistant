# Antigravity 中文助手

一个面向 Windows 的 Google Antigravity 非官方中文界面助手。它通过本地调试接口在运行时翻译系统界面，不修改 Antigravity 官方程序文件，也不读取对话、代码或项目文件。

## 下载

[下载最新版 AntigravityZhAssistant.exe](https://github.com/maclive400-design/antigravity-zh-assistant/releases/latest/download/AntigravityZhAssistant.exe)

## 功能

- 一键切换中文与英文界面
- 启动时仅检查一次远程汉化包
- Antigravity 更新后记录新增且尚未确认的系统词条
- 汉化包独立更新，无需重新安装助手
- 可选随 Windows 启动
- 手动切换成功时播放轻提示音，不发送系统通知

## 汉化包更新

远程汉化包位于 [`translation/translation-pack.json`](translation/translation-pack.json)。助手读取 [`translation/manifest.json`](translation/manifest.json) 判断是否有新版；离线或更新服务不可用时会继续使用本地词典。

## 本地构建

在 Windows PowerShell 中运行：

```powershell
.\build.ps1
```

生成文件位于 `dist/Antigravity中文助手.exe`。项目使用 Windows 自带的 .NET Framework C# 编译器，无需额外安装 SDK。

## 隐私

助手只扫描 Antigravity 的系统界面文本。输入框、代码块、可编辑区域、文件名和用户产物标题会被排除。待适配记录仅保存在本机，除非用户主动提交。

## 说明

本项目并非 Google 官方产品，与 Google 没有隶属或授权关系。“Google”和“Antigravity”及相关品牌资源归其各自权利人所有。MIT 许可证仅适用于本仓库中的源代码，不授予任何品牌或商标使用权。
