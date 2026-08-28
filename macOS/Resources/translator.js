(() => {
  const VERSION = "0.6.6";
  const DISABLED_KEY = "__antigravityZhAssistantDisabled";
  const AUTO_ADAPT = __AUTO_ADAPT__;
  const extraDictionary = Object.freeze(__EXTRA_TRANSLATIONS__);
  const previousAssistant = window.__antigravityZhAssistant;
  if (previousAssistant) {
    if (previousAssistant.version === VERSION && localStorage.getItem(DISABLED_KEY) !== "1") {
      window.__antigravityZhAssistant.scan(document);
      return { ok: true, version: window.__antigravityZhAssistant.version, reused: true };
    }
    try { previousAssistant.observer.disconnect(); } catch (_) {}
  }

  // Exact-match, offline UI dictionary. Deliberately avoids translating
  // arbitrary prose so conversations, code and project content remain intact.
  const dictionary = Object.freeze({
    "File": "文件",
    "View": "视图",
    "Window": "窗口",
    "Help": "帮助",
    "New Conversation": "新建对话",
    "Conversation History": "对话历史",
    "Scheduled Tasks": "定时任务",
    "Projects": "项目",
    "Project": "项目",
    "CLI Project": "CLI 项目",
    "Infinite-Canvas": "无限画布",
    "Local": "本地",
    "Conversations": "对话",
    "Display Options": "显示选项",
    "Create New Project": "新建项目",
    "Create Project": "创建项目",
    "Project options": "项目选项",
    "New Conversation in Project": "在项目中新建对话",
    "No conversations yet": "暂无对话",
    "Settings": "设置",
    "Open IDE": "打开 IDE",
    "Message input": "消息输入框",
    "Ask anything, @ to mention, / for actions": "输入问题，@ 添加引用，/ 选择操作",
    "Add context": "添加上下文",
    "Send message": "发送消息",
    "Select model": "选择模型",
    "Toggle Sidebar": "切换侧边栏",
    "Go Back": "后退",
    "Go Forward": "前进",
    "Notifications": "通知",
    "Loading Antigravity": "正在加载 Antigravity",
    "New Project": "新建项目",
    "Open Project": "打开项目",
    "Add Folder": "添加文件夹",
    "Add folders": "添加文件夹",
    "Remove Folder": "移除文件夹",
    "Choose Folder": "选择文件夹",
    "Choose a folder": "选择一个文件夹",
    "Select Folder": "选择文件夹",
    "Browse": "浏览",
    "Search": "搜索",
    "Search conversations": "搜索对话",
    "Search projects": "搜索项目",
    "General": "常规",
    "Account": "账号",
    "Accounts": "账号",
    "Models": "模型",
    "Model": "模型",
    "Appearance": "外观",
    "Theme": "主题",
    "System": "跟随系统",
    "Light": "浅色",
    "Dark": "深色",
    "Browser": "浏览器",
    "Skills": "技能",
    "Prompts": "提示词",
    "MCP Servers": "MCP 服务器",
    "MCP Server": "MCP 服务器",
    "MCP Error": "MCP 错误",
    "View MCP settings": "查看 MCP 设置",
    "MCP Tools": "MCP 工具",
    "Configure external tools via Model Context Protocol.": "通过模型上下文协议配置外部工具。",
    "Advanced": "高级",
    "Advanced Settings": "高级设置",
    "About": "关于",
    "Privacy": "隐私",
    "Security": "安全",
    "Permissions": "权限",
    "Keyboard Shortcuts": "键盘快捷键",
    "Version": "版本",
    "Updates": "更新",
    "Check for Updates": "检查更新",
    "Up to date": "已是最新版本",
    "Save": "保存",
    "Save changes": "保存更改",
    "Cancel": "取消",
    "Close": "关闭",
    "Delete": "删除",
    "Remove": "移除",
    "Add": "添加",
    "Edit": "编辑",
    "Create": "创建",
    "Continue": "继续",
    "Confirm": "确认",
    "Done": "完成",
    "Back": "返回",
    "Next": "下一步",
    "Previous": "上一步",
    "Retry": "重试",
    "Refresh": "刷新",
    "Clear": "清除",
    "Copy": "复制",
    "Copied": "已复制",
    "Download": "下载",
    "Upload": "上传",
    "Import": "导入",
    "Export": "导出",
    "Enable": "启用",
    "Disable": "停用",
    "Enabled": "已启用",
    "Disabled": "已停用",
    "On": "开",
    "Off": "关",
    "Default": "默认",
    "Medium": "中等",
    "High": "高",
    "Low": "低",
    "Always Ask": "始终询问",
    "Request Review": "请求审查",
    "Recommended": "推荐",
    "Learn more": "了解更多",
    "Dismiss": "知道了",
    "More options": "更多选项",
    "Pin conversation": "置顶对话",
    "Unpin conversation": "取消置顶",
    "Archive conversation": "归档对话",
    "Unarchive conversation": "取消归档",
    "Rename": "重命名",
    "Open": "打开",
    "Open in IDE": "在 IDE 中打开",
    "Open in File Explorer": "在文件资源管理器中打开",
    "Show in File Explorer": "在文件资源管理器中显示",
    "New Task": "新建任务",
    "Create Task": "创建任务",
    "Task": "任务",
    "Tasks": "任务",
    "Schedule": "计划",
    "Run now": "立即运行",
    "Pause": "暂停",
    "Resume": "继续",
    "Stop": "停止",
    "Start": "开始",
    "Running": "运行中",
    "Completed": "已完成",
    "Failed": "失败",
    "Pending": "等待中",
    "In progress": "进行中",
    "New": "新建",
    "Recent": "最近",
    "All": "全部",
    "None": "无",
    "Name": "名称",
    "Description": "说明",
    "Status": "状态",
    "Type": "类型",
    "Date": "日期",
    "Time": "时间",
    "Today": "今天",
    "Yesterday": "昨天",
    "Show more": "显示更多",
    "Show less": "收起",
    "Expand": "展开",
    "Collapse": "折叠",
    "Details": "详情",
    "Activity": "活动",
    "History": "历史记录",
    "Workspace": "工作区",
    "Workspaces": "工作区",
    "Files": "文件",
    "Folder": "文件夹",
    "Folders": "文件夹",
    "Terminal": "终端",
    "Artifact": "产物",
    "Artifacts": "产物",
    "Preview": "预览",
    "Proceed": "继续",
    "Walkthrough": "操作说明",
    "No more older messages": "没有更早的消息",
    "Plan": "计划",
    "Review": "审查",
    "Accept": "接受",
    "Reject": "拒绝",
    "Waiting for user input...": "等待用户确认……",
    "Yes, allow this time": "是，仅本次允许",
    "No (tell the agent what to do instead)": "否（改为告诉智能体应该怎么做）",
    "Skip": "跳过",
    "Allow checking C# compilers and build tools?": "允许检查 C# 编译器和构建工具吗？",
    "Apply": "应用",
    "Apply all": "全部应用",
    "Keep": "保留",
    "Discard": "放弃",
    "Allow": "允许",
    "Deny": "拒绝",
    "Always allow": "始终允许",
    "Ask every time": "每次询问",
    "Full machine": "整台电脑",
    "Turbo mode": "极速模式",
    "Custom": "自定义",
    "Unrestricted": "不受限制",
    "Sign in": "登录",
    "Sign out": "退出登录",
    "Switch account": "切换账号",
    "Continue with different account": "使用其他账号继续",
    "Having trouble? Let us know": "遇到问题？请告诉我们",
    "There was an unexpected issue setting up your account.": "设置账号时出现意外问题。",
    "Something went wrong": "出现问题",
    "Try again": "重试",
    "No results": "没有结果",
    "No results found": "未找到结果",
    "Get started": "开始使用",
    "Welcome to Antigravity": "欢迎使用 Antigravity",
    "What's new": "新增功能",
    "Release notes": "发行说明"
    ,"Customizations": "自定义",
    "App": "应用",
    "App Settings": "应用设置",
    "Manage application settings.": "管理应用程序设置。",
    "Not in Project": "不属于项目",
    "Shortcuts": "快捷键",
    "Provide Feedback": "提供反馈",
    "Execution": "执行方式",
    "Queued Messages": "排队消息",
    "Configure agent execution, queued message delivery, and permissions.": "配置智能体执行、排队消息发送和权限。",
    "Configure when follow-up messages are sent.": "配置后续消息的发送时机。",
    "Queue until after the current turn.": "当前轮次结束后再发送。",
    "Interrupt the agent and send immediately.": "中断智能体并立即发送。",
    "Queue": "排队发送",
    "Send Immediately": "立即发送",
    "Keyboard shortcuts": "键盘快捷键",
    "Agent Settings": "智能体设置",
    "Security Preset": "安全预设",
    "Choose a predefined security preset for the agent. This controls terminal auto-execution policy, and file access policy.": "为智能体选择预设的安全方案，用于控制终端自动执行和文件访问策略。",
    "Learn more about": "了解更多：",
    "Inherit General": "继承常规设置",
    "Inherits your General settings when working in this project.": "继承常规页面中的设置。",
    "Requires manual review for all terminal commands and file accesses outside of the working folders.": "所有终端命令及工作文件夹以外的文件访问都需要手动确认。",
    "All terminal commands require review. The agent can read or write to any file in the machine.": "所有终端命令都需要确认；智能体可以读写电脑上的任意文件。",
    "Disables all safety barriers for maximal iteration velocity.": "关闭所有安全限制，以获得最快的迭代速度。",
    "Manually customize individual settings.": "手动逐项配置权限。",
    "Agent settings and permissions for conversations outside of projects.": "为项目外的对话配置智能体设置和权限。",
    "Agent settings and permissions for this project.": "配置此项目的智能体设置和权限。",
    "Agent Behavior": "智能体行为",
    "Artifact Review Policy": "产物审查策略",
    "Specifies Agent's behavior when asking for review on artifacts, which are documents it creates to enable a richer conversation experience.": "指定智能体在请求审查产物时的行为；产物是它为丰富对话体验而创建的文档。",
    "File Permissions": "文件权限",
    "File Access Rules": "文件访问规则",
    "Configure allowed and denied paths for file reads and writes.": "配置允许或禁止读写的文件路径。",
    "Network Permissions": "网络权限",
    "Network Access Rules": "网络访问规则",
    "Configure allowed and denied URLs for reading.": "配置允许或禁止读取的网址。",
    "Terminal & Tooling Permissions": "终端与工具权限",
    "Terminal Commands": "终端命令",
    "Configure allowed terminal commands.": "配置允许执行的终端命令。",
    "Commands Outside Sandbox": "沙箱外命令",
    "Configure allowed commands outside the sandbox.": "配置允许在沙箱外执行的命令。",
    "Local Permissions": "本地权限",
    "Also includes": "还会应用",
    "global settings": "全局设置",
    "when working in this project.": "。",
    "Chat Settings": "对话设置",
    "Verbose Agent Chat": "显示智能体详细过程",
    "Display and preserve intermediate thinking steps.": "显示并保留中间思考步骤。",
    "Conversation Width": "对话宽度",
    "Configure the maximum width of the conversation panel.": "配置对话面板的最大宽度。",
    "Narrow": "窄",
    "Wide": "宽",
    "Select light, dark, or inherit system settings.": "选择浅色、深色或跟随系统设置。",
    "Light Theme": "浅色主题",
    "Dark Theme": "深色主题",
    "Preset": "预设",
    "Default Light": "默认浅色",
    "Default Dark": "默认深色",
    "Background": "背景",
    "Foreground": "前景",
    "Accent": "强调色",
    "Models & Usage": "模型与用量",
    "Refresh quota and credits data": "刷新额度和点数数据",
    "Manage your model quota and credits.": "管理模型额度和 AI 点数。",
    "Your Plan:": "当前方案：",
    "You can upgrade to a Google AI Ultra plan to receive higher rate limits.": "可以升级到 Google AI Ultra 方案以获得更高的速率限制。",
    "Upgrade": "升级",
    "Model Credits": "模型点数",
    "Enable AI Credit Overages": "额度用尽后使用 AI 点数",
    "When toggled on, Antigravity will use your AI credits to fulfill model requests once you're out of model quota. Antigravity will always use your model quota first before using AI credits.": "开启后，模型额度用尽时 Antigravity 将使用 AI 点数完成请求；始终优先使用模型额度。",
    "Model Quota": "模型额度",
    "Gemini Models": "Gemini 模型",
    "Claude and GPT models": "Claude 和 GPT 模型",
    "Weekly Limit Remaining": "每周额度剩余",
    "Five Hour Limit Remaining": "五小时额度剩余",
    "Manage your plan, credentials, and general preferences.": "管理方案、登录凭据和常规偏好。",
    "Enable Telemetry": "启用使用情况数据",
    "When toggled on, Antigravity collects usage data to help Google enhance performance and features.": "开启后，Antigravity 会收集使用数据，帮助 Google 改进性能和功能。",
    "Marketing Emails": "产品推广邮件",
    "Receive product updates, tips, and promotions from Google Antigravity via email.": "通过电子邮件接收 Google Antigravity 的产品更新、技巧和推广信息。",
    "Email": "电子邮箱",
    "Sign Out": "退出登录",
    "By using this app, you agree to its": "使用本应用即表示你同意其",
    "Terms of Service": "服务条款",
    "Browser Settings": "浏览器设置",
    "Configure the browser subagent. It requires": "配置浏览器子智能体。需要安装",
    "to be installed.": "。",
    "The browser subagent can be invoked by typing /browser in the conversation input box.": "在对话输入框中输入 /browser 即可调用浏览器子智能体。",
    "Browser Javascript Execution Policy": "浏览器 JavaScript 执行策略",
    "Controls whether the agent can run custom JavaScript to automate complex browser actions.": "控制智能体能否运行自定义 JavaScript 来自动完成复杂的浏览器操作。",
    "Actuation Permissions": "浏览器操作权限",
    "Browser Actuation Rules": "浏览器操作规则",
    "Configure allowed and denied URLs for browser actuation.": "配置允许或禁止执行浏览器操作的网址。",
    "Configure default behaviors, skills, and MCP servers.": "配置默认行为、技能和 MCP 服务器。",
    "Token Usage": "Token 用量",
    "The breakdown below shows token usage from customizations like skills, rules, and MCP. If the budget is exceeded, large customizations will be truncated automatically.": "以下明细显示技能、规则和 MCP 等自定义内容占用的 Token；超出预算时，较大的自定义内容将被自动截断。",
    "Global": "全局",
    "Copy path": "复制路径",
    "Prevent Sleep": "阻止系统休眠",
    "Prevent the computer from sleeping while the app is running.": "应用运行时阻止电脑进入睡眠。",
    "Keep In Menu Bar": "在菜单栏中保留",
    "Keep the app accessible from the menu bar and running in the background when all windows are closed.": "关闭所有窗口后仍让应用在后台运行，并可从菜单栏访问。",
    "Notification Settings": "通知设置",
    "To modify notification settings, open your operating system's system preferences.": "若要修改通知设置，请打开操作系统的系统设置。",
    "Open System Preferences": "打开系统设置"
    ,"Keyboard shortcuts for quick navigation and control.": "用于快速导航和控制的键盘快捷键。",
    "Open Conversation Picker": "打开对话选择器",
    "Open File Search": "打开文件搜索",
    "Focus Input": "聚焦输入框",
    "NAVIGATION": "导航",
    "Navigation": "导航",
    "File Picker": "文件选择器",
    "Select Previous Conversation": "选择上一个对话",
    "Select Next Conversation": "选择下一个对话",
    "Previous Pane Tab": "上一个面板标签页",
    "Next Pane Tab": "下一个面板标签页",
    "Open Settings": "打开设置",
    "CONVERSATION": "对话",
    "Conversation": "对话",
    "Toggle Model Selector": "打开或关闭模型选择器",
    "Toggle Voice Recording": "打开或关闭语音录制",
    "Find in Pane": "在面板中查找",
    "Add to Chat/Quote": "添加到对话或引用",
    "LAYOUT CONTROLS": "布局控制",
    "Layout Controls": "布局控制",
    "Toggle Auxiliary Pane": "打开或关闭辅助面板",
    "Commands": "命令",
    "Type to search...": "输入内容以搜索……",
    "Collapse All Folders": "折叠所有文件夹",
    "Expand All Folders": "展开所有文件夹",
    "Open Keyboard Shortcuts": "打开键盘快捷键",
    "Provide feedback": "提供反馈",
    "to navigate": "导航",
    "to select": "选择",
    "Installed MCP Servers": "已安装的 MCP 服务器",
    "Add MCP": "添加 MCP",
    "Open MCP Config": "打开 MCP 配置",
    "Build With Google Plugins": "使用 Google 插件构建",
    "Customize": "自定义",
    "Show": "显示",
    "more...": "项……",
    "Error:": "错误：",
    "[MCP Proxy] Socket connection error:": "[MCP 代理] 套接字连接错误：",
    "connection closed: calling": "连接已关闭；调用",
    "client is closing": "客户端正在关闭",
    "% of the customization budget is available.": "% 的自定义内容预算可用。",
    "**STOP AND VERIFY**: Before running any command or tool that results in irreversible data loss, you MUST obtain explicit user consent. When in doubt, ask. It is better to wait for confirmation than to accidentally delete production data or critical project assets. Use this for: - SQL: DROP TABLE/VIEW/SCHEMA/DATABASE, TRUNCATE, or broad DELETE (missing WHERE or using 1=1). - Cloud Storage: gsutil rm or gcloud storage rm targeting production data or critical buckets. - Infrastructure: gcloud projects delete, deleting Spanner/BigQuery/Dataproc resources, deleting secrets, or KMS key destruction.": "**停止并确认**：运行任何可能造成不可逆数据丢失的命令或工具前，必须先获得用户明确同意。如有疑问，应先询问，避免误删生产数据或关键项目资产。适用于：SQL 删除或清空操作；删除生产环境云存储数据；删除云项目、数据库资源、密钥或 KMS 密钥等基础设施操作。",
    "Comprehensive guide and reference for the Antigravity Customization System. Use to explain how customizations work, their loading priority, discovery mechanisms, and to guide the creation of skills, rules, plugins, hooks, and MCP servers.": "Antigravity 自定义系统的完整指南与参考，用于说明自定义内容的工作方式、加载优先级和发现机制，并指导创建技能、规则、插件、钩子及 MCP 服务器。",
    "Use these skills when you need to manage user roles, inspect permissions, and verify security-related configuration parameters.": "需要管理用户角色、检查权限或核验安全相关配置参数时使用这些技能。",
    "You're an expert in AlloyDB Omni running in a container. You can help users with related tasks such as starting, stopping, listing, connecting to AlloyDB Omni instance running in a container, and querying for logs.": "用于管理容器中运行的 AlloyDB Omni，包括启动、停止、列出和连接实例，以及查询日志。",
    "Use these skills when you need to explore the database structure, identify schema objects like views and triggers, and execute SQL queries to interact with your data.": "需要探索数据库结构、识别视图和触发器等架构对象，或执行 SQL 查询处理数据时使用这些技能。",
    "Use these skills when you need to audit database health, identify storage bloat, find broken indexes, and verify tablespace or maintenance configurations.": "需要审计数据库健康状况、识别存储膨胀、查找损坏索引，或核验表空间和维护配置时使用这些技能。",
    "You're an expert in AlloyDB Omni Operator running in Kubernetes. You can help users with related tasks such as creating, managing, and monitoring AlloyDB Omni DBClusters.": "用于在 Kubernetes 中通过 AlloyDB Omni Operator 创建、管理和监控 AlloyDB Omni 数据库集群。",
    "Use these skills when you need to troubleshoot production issues by identifying locks, tracking long-running transactions, and getting a high-level view of server state.": "需要通过识别锁、跟踪长时间运行的事务和查看服务器整体状态来排查生产问题时使用这些技能。"
  });

  const skippedSelector = [
    "textarea",
    "input",
    "pre",
    "code",
    "[contenteditable='true']",
    "[role='textbox']"
  ].join(",");

  // Skill descriptions are located by their stable technical identifiers.
  // This keeps the Chinese UI useful even when upstream English copy changes.
  const skillSummaries = Object.freeze({
    "accidental-data-loss-prevention": "执行可能造成不可逆数据丢失的操作前，强制停止并取得用户明确同意，涵盖数据库、云存储和基础设施删除操作。",
    "agy-customizations": "Antigravity 自定义系统指南，说明加载优先级和发现机制，并指导创建技能、规则、插件、钩子及 MCP 服务器。",
    "alloydb-omni-access-control": "管理 AlloyDB Omni 用户角色、检查权限，并核验安全相关配置参数。",
    "alloydb-omni-container": "管理容器中的 AlloyDB Omni，包括启动、停止、列出、连接实例和查询日志。",
    "alloydb-omni-data": "探索 AlloyDB Omni 数据库结构、识别视图和触发器等对象，并执行 SQL 查询。",
    "alloydb-omni-health": "审计 AlloyDB Omni 健康状况，检查存储膨胀、损坏索引、表空间和维护配置。",
    "alloydb-omni-kubernetes": "通过 Kubernetes 中的 AlloyDB Omni Operator 创建、管理和监控数据库集群。",
    "alloydb-omni-monitor": "通过锁、长事务和服务器状态排查 AlloyDB Omni 生产问题。",
    "alloydb-omni-optimize": "调整 AlloyDB Omni 数据库引擎设置、管理扩展，并优化列式引擎的分析性能。",
    "alloydb-omni-performance": "分析查询性能、生成执行计划、检查表和列统计信息，并监控数据库活动。",
    "alloydb-omni-replication": "监控 AlloyDB Omni 复制健康状况、节点同步状态和发布表。",
    "alloydb-postgres-access-management": "管理 AlloyDB for PostgreSQL 用户、角色和权限，并核验安全配置。",
    "alloydb-postgres-admin": "创建 AlloyDB 集群和实例、监控创建进度，并查看环境配置与健康信息。",
    "alloydb-postgres-data": "探索 AlloyDB for PostgreSQL 架构、识别视图和触发器，并执行自定义 SQL 查询。",
    "alloydb-postgres-health": "优化存储、检查索引和表统计信息，并管理自动清理与表空间配置。",
    "alloydb-postgres-monitor": "排查慢查询、分析执行计划、识别高资源进程，并监控系统指标。",
    "alloydb-postgres-optimize": "发现和管理 PostgreSQL 扩展，并调整内存与服务器配置等引擎设置。",
    "alloydb-postgres-replication": "监控复制和节点同步状态，保障 AlloyDB 集群的高可用与数据分布。",
    "antigravity-guide": "Google Antigravity、Antigravity IDE、CLI、SDK、快捷命令及自定义功能的完整使用指南。",
    "bigquery": "提供 BigQuery 专用知识与规范，涵盖 SQL 优化、BigFrames、BigQuery ML/AI 和图分析。",
    "bigquery-data-transfer-service": "发现并检查 BigQuery 数据传输服务配置，识别现有数据导入管道和数据源元数据。",
    "building-data-apps": "使用 React、Vite 或 Streamlit 构建连接 GCP 数据源的现代数据应用、仪表板和交互式报告。",
    "cloud-sql-mysql-admin": "创建 Cloud SQL for MySQL 实例、数据库和用户，克隆环境并监控基础设施操作。",
    "cloud-sql-mysql-data": "探索 MySQL 架构、执行 SQL 查询，并检查查询执行计划。",
    "cloud-sql-mysql-lifecycle": "管理 MySQL 备份、恢复和实例克隆，保障数据持久性并支持恢复测试。",
    "cloud-sql-mysql-monitor": "排查 MySQL 慢查询、分析系统指标，并识别表碎片和唯一索引缺失等问题。",
    "cloud-sql-postgres-admin": "创建 Cloud SQL for PostgreSQL 实例、数据库和用户，克隆环境并监控操作进度。",
    "cloud-sql-postgres-data": "探索 PostgreSQL 数据库结构、发现视图和存储过程，并执行自定义 SQL 查询。",
    "cloud-sql-postgres-health": "审计 PostgreSQL 健康状况，检查存储膨胀、无效索引、表统计和自动清理配置。",
    "cloud-sql-postgres-lifecycle": "管理 PostgreSQL 备份、恢复、主版本升级兼容性和实例生命周期。",
    "cloud-sql-postgres-monitor": "排查 PostgreSQL 性能瓶颈、分析执行计划和高资源进程，并监控系统指标。",
    "cloud-sql-postgres-replication": "监控 PostgreSQL 复制和同步状态，并审计数据库角色与安全设置。",
    "cloud-sql-postgres-vectorassist": "根据性能需求设置并优化可用于生产环境的 PostgreSQL 向量工作负载。",
    "cloud-sql-postgres-view-config": "发现和管理 PostgreSQL 扩展，并调整内存及服务器配置等引擎参数。",
    "cloud-sql-sqlserver-admin": "创建 Cloud SQL for SQL Server 实例、数据库和用户，克隆环境并监控操作。",
    "cloud-sql-sqlserver-data": "探索 SQL Server 架构、执行 SQL 查询，并通过系统指标监控性能。",
    "cloud-sql-sqlserver-lifecycle": "管理 SQL Server 备份、恢复和实例克隆，保障数据生命周期与可恢复性。",
    "cloud-sql-sqlserver-monitor": "排查 SQL Server 慢查询并分析系统级监控指标。",
    "data-autocleaning": "为 Dataform、dbt 和 BigQuery 管道自动执行数据导入、转换、架构映射及质量清理。",
    "dataform-bigquery": "为 BigQuery ELT 创建、修改和优化 Dataform 项目、SQLX 操作、数据源声明及工作流配置。",
    "dbt-bigquery": "创建、修改和优化面向 BigQuery 的 dbt 模型、项目与数据转换管道。",
    "discovering-gcp-data-assets": "在 Google Cloud 中查找和检查 BigQuery、BigLake、Spanner 等数据资产及其架构和治理元数据。",
    "federate-lakehouse-catalog": "将 Google Cloud Lakehouse 目录连接到 Databricks Unity 或 AWS Glue 等远程 Iceberg REST 目录。",
    "firestore-data": "执行 Firestore 文档增删改查、集合层级探索和结构化查询。",
    "gcloud-auth-verification": "诊断并解决 gcloud、bq、Dataform 或 Python 库的 Google Cloud 身份验证和 ADC 问题。",
    "gcp-composer-troubleshooting": "排查 Cloud Composer 和 Airflow 管道或 DAG 失败，并生成根因分析报告。",
    "gcp-data-pipelines": "构建和管理 Google Cloud 数据管道，并将任务分派给 dbt、Dataflow、Dataform、Spark、DTS 或 Composer。",
    "gcp-dataflow": "编写、打包、运行和排查 Apache Beam Dataflow 管道，包括 Flex Template、Cloud Build 和性能诊断。",
    "gcp-managed-airflow-migrations": "迁移托管 Airflow 或 Cloud Composer DAG，检查兼容性并处理 Airflow 2 和 3 的破坏性变更。",
    "gcp-pipeline-orchestration": "使用 Cloud Composer 编排 dbt、Notebook、Spark、Dataform、Python 脚本和 BigQuery SQL 管道。",
    "gcp-pipeline-resource-provisioning": "通过 deployment.yaml 声明并部署 BigQuery、Dataform、Dataproc、DTS 等数据管道资源。",
    "gcp-spark": "在 Dataproc 集群或 Serverless 上开发和运行 Spark，连接 BigLake、BigQuery 和 Spanner 并排查故障。",
    "gcs-security-assessment": "评估 Google Cloud Storage 存储桶或项目的安全状态、风险和 SAIF 合规性。",
    "managing-python-dependencies": "正确管理 Python 依赖，避免全局安装；适用于项目、Notebook、第三方库和虚拟环境配置。",
    "ml-best-practices": "机器学习和数据分析最佳实践，涵盖聚类、分类、回归、预测、统计检验、模型比较和 BigQuery ML。",
    "notebook-guidance": "使用 Jupyter Notebook 进行数据分析、探索和可视化，涵盖执行验证、依赖、绘图及 BigQuery 工作流。",
    "skill-repair": "修复并重新安装失败的智能体技能，并在修复后精确更新 manifest.json。",
    "spanner-data": "探索 Spanner 数据库结构、发现表和图等对象，并执行自定义 SQL 查询。"
  });

  const originalTextNodes = new Map();
  const originalAttributes = new Map();

  function setTranslatedText(node, value) {
    if (!originalTextNodes.has(node)) originalTextNodes.set(node, node.nodeValue);
    node.nodeValue = value;
  }

  function rememberAttribute(element, attribute, value) {
    let values = originalAttributes.get(element);
    if (!values) {
      values = new Map();
      originalAttributes.set(element, values);
    }
    if (!values.has(attribute)) values.set(attribute, value);
  }

  function translateSkillCard(nameNode, summary) {
    const dialog = nameNode.parentElement && nameNode.parentElement.closest("[role='dialog']");
    if (!dialog) return;
    const walker = document.createTreeWalker(dialog, NodeFilter.SHOW_TEXT);
    const nodes = [];
    let node;
    while ((node = walker.nextNode())) nodes.push(node);
    const start = nodes.indexOf(nameNode);
    if (start < 0) return;
    for (let index = start + 1; index < Math.min(nodes.length, start + 30); index++) {
      const candidate = nodes[index];
      if (shouldSkip(candidate)) continue;
      const text = (candidate.nodeValue || "").trim();
      if (!text || text === "Global" || text === "全局") continue;
      if (Object.prototype.hasOwnProperty.call(skillSummaries, text)) return;
      if (text.length < 20) continue;
      setTranslatedText(candidate, candidate.nodeValue.replace(text, summary));
      return;
    }
  }

  function translateSkillCards(root) {
    const scope = root && root.nodeType === Node.TEXT_NODE ? root.parentElement : root;
    if (!scope || !scope.ownerDocument && scope !== document) return;
    const names = [];
    if (scope.nodeType === Node.TEXT_NODE && Object.prototype.hasOwnProperty.call(skillSummaries, scope.nodeValue.trim())) names.push(scope);
    const walker = document.createTreeWalker(scope, NodeFilter.SHOW_TEXT);
    let node;
    while ((node = walker.nextNode())) {
      const name = (node.nodeValue || "").trim();
      if (Object.prototype.hasOwnProperty.call(skillSummaries, name)) names.push(node);
    }
    for (const nameNode of names) translateSkillCard(nameNode, skillSummaries[nameNode.nodeValue.trim()]);
  }

  function translateExact(value) {
    if (!value) return null;
    const trimmed = value.trim();
    if (!trimmed) return null;
    if (Object.prototype.hasOwnProperty.call(dictionary, trimmed)) {
      return value.replace(trimmed, dictionary[trimmed]);
    }
    if (Object.prototype.hasOwnProperty.call(extraDictionary, trimmed)) {
      return value.replace(trimmed, extraDictionary[trimmed]);
    }
    let approvalMatch = trimmed.match(/^Thought for (\d+)s$/);
    if (approvalMatch) return value.replace(trimmed, `思考了 ${approvalMatch[1]} 秒`);
    approvalMatch = trimmed.match(/^Run (.+)$/s);
    if (approvalMatch) return value.replace(trimmed, `运行 ${approvalMatch[1]}`);
    approvalMatch = trimmed.match(/^Yes, and always allow '(.+)' in this conversation$/s);
    if (approvalMatch) return value.replace(trimmed, `是，并在本次对话中始终允许“${approvalMatch[1]}”`);
    approvalMatch = trimmed.match(/^Yes, and always allow '(.+)' when not in a project$/s);
    if (approvalMatch) return value.replace(trimmed, `是，并在不属于项目时始终允许“${approvalMatch[1]}”`);
    approvalMatch = trimmed.match(/^Yes, and always allow '(.+)'$/s);
    if (approvalMatch) return value.replace(trimmed, `是，始终允许“${approvalMatch[1]}”`);
    approvalMatch = trimmed.match(/^(\d+) files? changed$/);
    if (approvalMatch) return value.replace(trimmed, `${approvalMatch[1]} 个文件已更改`);
    approvalMatch = trimmed.match(/^Worked for (\d+)m$/);
    if (approvalMatch) return value.replace(trimmed, `工作了 ${approvalMatch[1]} 分钟`);
    approvalMatch = trimmed.match(/^Media \(Today (.+)\)$/);
    if (approvalMatch) return value.replace(trimmed, `媒体（今天 ${approvalMatch[1]}）`);
    if (trimmed === "**STOP AND VERIFY**:") {
      return value.replace(trimmed, "**停止并确认**：");
    }
    if (trimmed.startsWith("**STOP AND VERIFY**:")) {
      return value.replace(trimmed, "**停止并确认**：运行任何可能造成不可逆数据丢失的命令或工具前，必须先获得用户明确同意。如有疑问，应先询问，避免误删生产数据或关键项目资产。适用于 SQL 删除或清空操作、生产环境云存储删除，以及云项目、数据库资源、密钥或 KMS 密钥等基础设施删除操作。");
    }
    if (trimmed.startsWith("Before running any command or tool that results in irreversible data loss")) {
      return value.replace(trimmed, "运行任何可能造成不可逆数据丢失的命令或工具前，必须先获得用户明确同意。如有疑问，应先询问，避免误删生产数据或关键项目资产。适用于 SQL 删除或清空操作、生产环境云存储删除，以及云项目、数据库资源、密钥或 KMS 密钥等基础设施删除操作。");
    }
    if (trimmed.startsWith("Ensures proper Python dependency management")) {
      return value.replace(trimmed, "确保正确管理 Python 依赖，避免全局运行 `pip install`，并遵循项目专用工具。适用于安装或修改 Python 包、创建 Python 项目或 Notebook、使用第三方库，以及运行 Python 脚本前确认正确虚拟环境等场景。");
    }
    if (trimmed.startsWith("CRITICAL RULE: You MUST use this skill whenever the task involves any machine learning tasks or data analysis")) {
      return value.replace(trimmed, "重要规则：任务涉及机器学习或数据分析时必须使用此技能，包括聚类、分类、回归、时间序列预测、统计检验、模型比较及 SQL/BigQuery ML 数据分析。若需要 SQL 方案，本技能负责分析步骤，SQL 语法交由 `bigquery` 技能处理。");
    }
    if (trimmed.startsWith("This skill guides the use of Jupyter notebooks for data analysis")) {
      return value.replace(trimmed, "指导使用 Jupyter Notebook 进行数据分析、探索和可视化，尤其适用于 BigQuery。内容包括单元格执行与验证、依赖安装、Notebook 结构、数据清理、绘图、BigQuery SQL 和机器学习工作流；创建或编辑 `.ipynb`、执行 Notebook 或在其中查询 BigQuery 时使用。");
    }
    if (trimmed.startsWith("Use this to fix and re-install agent skills that have failed installation")) {
      return value.replace(trimmed, "用于修复并重新安装失败的智能体技能；修复完成后，可在必要上下文和权限下精确更新 `manifest.json`。");
    }
    if (trimmed.startsWith("Use these skills when you need to explore the database structure, discover schema objects like tables and graphs")) {
      return value.replace(trimmed, "需要探索数据库结构、发现表和图等架构对象，或执行自定义 SQL 查询处理数据时使用这些技能。");
    }
    if (trimmed.includes("[MCP Proxy] Socket connection error:")) {
      const translatedError = trimmed
        .replace("Error: [MCP Proxy] Socket connection error:", "错误：[MCP 代理] 套接字连接错误：")
        .replace("[MCP Proxy] Socket connection error:", "[MCP 代理] 套接字连接错误：")
        .replace("connection closed: calling", "连接已关闭；调用")
        .replace("client is closing", "客户端正在关闭");
      return value.replace(trimmed, translatedError);
    }
    let match = trimmed.match(/^(\d+)mo$/);
    if (match) return value.replace(trimmed, `${match[1]}个月`);
    match = trimmed.match(/^(\d+)d$/);
    if (match) return value.replace(trimmed, `${match[1]}天`);
    match = trimmed.match(/^(\d+)h$/);
    if (match) return value.replace(trimmed, `${match[1]}小时`);
    match = trimmed.match(/^(\d+)min$/);
    if (match) return value.replace(trimmed, `${match[1]}分钟`);
    match = trimmed.match(/^Select model, current: (.+)$/);
    if (match) return value.replace(trimmed, `选择模型，当前：${match[1]}`);
    match = trimmed.match(/^Show (\d+) breakdowns$/);
    if (match) return value.replace(trimmed, `显示 ${match[1]} 项明细`);
    match = trimmed.match(/^Show (\d+) more\.\.\.$/);
    if (match) return value.replace(trimmed, `再显示 ${match[1]} 项……`);
    match = trimmed.match(/^(\d+(?:\.\d+)?)% of the customization budget is available\.$/);
    if (match) return value.replace(trimmed, `自定义内容预算还剩 ${match[1]}%。`);
    if (AUTO_ADAPT) {
      const composed = translateComposed(trimmed);
      if (composed) return value.replace(trimmed, composed);
    }
    return null;
  }

  const adaptivePhrases = Object.freeze({
    "Open": "打开", "Close": "关闭", "Show": "显示", "Hide": "隐藏",
    "Enable": "启用", "Disable": "停用", "Enabled": "已启用", "Disabled": "已停用",
    "Add": "添加", "Remove": "移除", "Create": "创建", "Delete": "删除",
    "Edit": "编辑", "Save": "保存", "Cancel": "取消", "Apply": "应用",
    "Settings": "设置", "Preferences": "偏好设置", "General": "常规",
    "Account": "账号", "Appearance": "外观", "Model": "模型", "Models": "模型",
    "Conversation": "对话", "Conversations": "对话", "Project": "项目", "Projects": "项目",
    "File": "文件", "Files": "文件", "Folder": "文件夹", "Folders": "文件夹",
    "Search": "搜索", "Select": "选择", "Previous": "上一个", "Next": "下一个",
    "New": "新建", "Current": "当前", "Default": "默认", "Custom": "自定义",
    "Update": "更新", "Updates": "更新", "Refresh": "刷新", "Check": "检查",
    "Keyboard": "键盘", "Shortcuts": "快捷键", "Shortcut": "快捷键",
    "Command": "命令", "Commands": "命令", "Terminal": "终端",
    "Permission": "权限", "Permissions": "权限", "Security": "安全",
    "Network": "网络", "Browser": "浏览器", "Tools": "工具", "Tool": "工具",
    "Installed": "已安装", "Available": "可用", "Loading": "正在加载",
    "Retry": "重试", "Error": "错误", "Warning": "警告", "Success": "成功",
    "Details": "详情", "More": "更多", "Less": "更少", "All": "全部",
    "Collapse": "折叠", "Expand": "展开", "Copy": "复制", "Path": "路径",
    "Rules": "规则", "Usage": "用量", "Limit": "额度", "Remaining": "剩余"
  });

  function translateComposed(text) {
    if (text.length > 72 || !/^[A-Za-z][A-Za-z0-9 &/+_.:()'-]*$/.test(text)) return null;
    const tokens = text.split(/(\s+|[&/+():.-])/);
    let translated = 0;
    let words = 0;
    const output = tokens.map((token) => {
      if (!/[A-Za-z]/.test(token)) return token;
      words++;
      const replacement = adaptivePhrases[token];
      if (replacement) { translated++; return replacement; }
      return token;
    });
    if (!words || translated / words < 0.75) return null;
    return output.join("").replace(/\s+/g, " ").trim();
  }

  function isSafeUiTextNode(node) {
    if (shouldSkip(node)) return false;
    const element = node.parentElement;
    if (!element) return false;
    if (element.closest("nav,a[href]")) return false;
    return Boolean(element.closest("[role='dialog'],[role='menu'],[role='listbox'],[role='option'],button,[aria-label],[data-tooltip-content]"));
  }

  function collectUnknown() {
    const values = new Set();
    const walker = document.createTreeWalker(document.body || document.documentElement, NodeFilter.SHOW_TEXT);
    let node;
    while ((node = walker.nextNode())) {
      if (!isSafeUiTextNode(node)) continue;
      const text = (node.nodeValue || "").trim();
      if (text.length < 2 || text.length > 180 || !/[A-Za-z]{2}/.test(text)) continue;
      if (/[\u3400-\u9fff]/.test(text)) continue;
      if (/https?:|\\|\/Users\/|[{}<>]|^[A-Za-z]:/.test(text)) continue;
      if (/\.(?:cs|csproj|sln|json|js|ts|tsx|jsx|zip|png|jpg|jpeg|gif|svg|md|txt|log)$/i.test(text)) continue;
      if (/^(?:Implementation Plan|Google Antigravity Current UI Design)$/i.test(text)) continue;
      if (/^[a-z0-9]+(?:-[a-z0-9]+)+$/.test(text)) continue;
      if (/^[A-Z][A-Za-z0-9_.-]*\s?\d+(?:\.\d+)+/.test(text)) continue;
      if (/^[\w.+-]+@[\w.-]+$/.test(text)) continue;
      if (/^Send feedback as /i.test(text)) continue;
      if (/^Workspace_\d+$/i.test(text) || /^go\//i.test(text)) continue;
      if (Object.prototype.hasOwnProperty.call(skillSummaries, text)) continue;
      if (["Antigravity", "Alt", "Ctrl", "Shift", "Tab", "Google AI Pro", "Google Chrome", "Google3", "notebooks", "visualization", "Previewing Local Project", "Running Application Locally", "Setting Language to Chinese"].includes(text)) continue;
      if (Object.prototype.hasOwnProperty.call(dictionary, text) || Object.prototype.hasOwnProperty.call(extraDictionary, text)) continue;
      if (translateExact(text)) continue;
      values.add(text);
    }
    return Array.from(values).sort();
  }

  function shouldSkip(node) {
    const element = node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement;
    return !element || Boolean(element.closest(skippedSelector));
  }

  function translateTextNode(node) {
    if (shouldSkip(node)) return;
    const translated = translateExact(node.nodeValue);
    if (translated && translated !== node.nodeValue) setTranslatedText(node, translated);
  }

  const translatedAttributes = ["aria-label", "title", "placeholder", "data-tooltip-content"];
  function translateElement(element) {
    if (!(element instanceof Element)) return;
    for (const attribute of translatedAttributes) {
      if (!element.hasAttribute(attribute)) continue;
      const original = element.getAttribute(attribute);
      const translated = translateExact(original);
      if (translated && translated !== original) {
        rememberAttribute(element, attribute, original);
        element.setAttribute(attribute, translated);
      }
    }
  }

  function buildReverseDictionary() {
    const reverse = new Map();
    const add = (source) => {
      for (const [english, chinese] of Object.entries(source)) {
        if (chinese && !reverse.has(chinese)) reverse.set(chinese, english);
      }
    };
    add(dictionary);
    add(extraDictionary);
    add(skillSummaries);
    return reverse;
  }

  function restoreValue(value, reverse) {
    if (!value) return null;
    const trimmed = value.trim();
    if (!trimmed) return null;
    if (reverse.has(trimmed)) return value.replace(trimmed, reverse.get(trimmed));
    let match = trimmed.match(/^思考了 (\d+) 秒$/);
    if (match) return value.replace(trimmed, `Thought for ${match[1]}s`);
    match = trimmed.match(/^显示 (\d+) 项明细$/);
    if (match) return value.replace(trimmed, `Show ${match[1]} breakdowns`);
    match = trimmed.match(/^再显示 (\d+) 项……$/);
    if (match) return value.replace(trimmed, `Show ${match[1]} more...`);
    match = trimmed.match(/^自定义内容预算还剩 (\d+(?:\.\d+)?)%。$/);
    if (match) return value.replace(trimmed, `${match[1]}% of the customization budget is available.`);
    return null;
  }

  function restoreLegacy(root) {
    const reverse = buildReverseDictionary();
    const textWalker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    let textNode;
    while ((textNode = textWalker.nextNode())) {
      if (shouldSkip(textNode)) continue;
      const restored = restoreValue(textNode.nodeValue, reverse);
      if (restored && restored !== textNode.nodeValue) textNode.nodeValue = restored;
    }
    if (root.querySelectorAll) {
      root.querySelectorAll("[aria-label],[title],[placeholder],[data-tooltip-content]").forEach((element) => {
        for (const attribute of translatedAttributes) {
          if (!element.hasAttribute(attribute)) continue;
          const value = element.getAttribute(attribute);
          const restored = restoreValue(value, reverse);
          if (restored && restored !== value) element.setAttribute(attribute, restored);
        }
      });
    }
  }

  function scan(root) {
    if (!root) return;
    translateSkillCards(root);
    if (root.nodeType === Node.TEXT_NODE) translateTextNode(root);
    if (root.nodeType === Node.ELEMENT_NODE) translateElement(root);
    const textWalker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    let textNode;
    while ((textNode = textWalker.nextNode())) translateTextNode(textNode);
    if (root.querySelectorAll) {
      root.querySelectorAll("[aria-label],[title],[placeholder],[data-tooltip-content]").forEach(translateElement);
    }
  }

  let scheduled = false;
  const pendingRoots = new Set();
  function scheduleScan(root) {
    pendingRoots.add(root && root.nodeType ? root : document);
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(() => {
      scheduled = false;
      for (const pending of pendingRoots) scan(pending);
      pendingRoots.clear();
    });
  }

  const observer = new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      if (mutation.type === "characterData") scheduleScan(mutation.target);
      else if (mutation.type === "attributes") scheduleScan(mutation.target);
      else for (const node of mutation.addedNodes) scheduleScan(node);
    }
  });

  function restore() {
    try { observer.disconnect(); } catch (_) {}
    for (const [node, value] of originalTextNodes) {
      try { node.nodeValue = value; } catch (_) {}
    }
    for (const [element, values] of originalAttributes) {
      for (const [attribute, value] of values) {
        try { element.setAttribute(attribute, value); } catch (_) {}
      }
    }
    originalTextNodes.clear();
    originalAttributes.clear();
    restoreLegacy(document);
    localStorage.setItem(DISABLED_KEY, "1");
    delete window.__antigravityZhAssistant;
    return { ok: true, version: VERSION, restored: true };
  }

  if (previousAssistant && previousAssistant.version !== VERSION) restoreLegacy(document);
  delete window.__antigravityZhAssistant;
  if (localStorage.getItem(DISABLED_KEY) === "1") {
    restoreLegacy(document);
    return { ok: true, version: VERSION, disabled: true };
  }

  scan(document);
  observer.observe(document.documentElement || document, {
    subtree: true,
    childList: true,
    characterData: true,
    attributes: true,
    attributeFilter: translatedAttributes
  });

  window.__antigravityZhAssistant = { version: VERSION, scan, observer, collectUnknown, restore };
  return { ok: true, version: VERSION, reused: false };
})();
