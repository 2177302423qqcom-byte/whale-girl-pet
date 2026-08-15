# 🐳 鲸鱼娘小助理 (Whale Girl Pet)

> 一只住在你 Windows 桌面上、会撒娇会害羞会干活的女仆鲸鱼娘。

「鲸鱼娘小助理」是一个 **WPF 桌面陪护桌宠 + DeepSeek Harness 智能体** 的完整组合:
桌面上的她负责陪伴与互动(摸头、亲亲、喂小鱼干、溜角落偷看你),聊天与任务的大脑则来自 DSH 里的专属鲸鱼娘会话——她会用工具、会查文件、会汇报工作,还会在你想她的时候主动来找你。

---

## ✨ 特性

### 🐾 陪伴与互动
- **单击 = 摸头**:害羞脸红、舒服眯眼、开心蹦跳、闹小情绪,连续摸头有彩蛋
- **双击 = 亲亲**:当场脸红发颤:"呜哇!!被亲亲了…(〃////〃)"
- **🐟 喂小鱼干**:摸头有几率让她翻出小鱼干,喂给她会开心得跳起来
- **📝 记住你的称呼**:让她用你喜欢的称呼叫你,永久记住
- **🕐 定时问候**:早安 / 午饭 / 晚饭 / 晚安,每天准时惦记你
- **🙈 藏起来**:她会溜到屏幕随机角落,探出半个脑袋偷看你;被发现了会害羞地探出来

### 🎐 活着的她
- 完整立绘(可选 3 套姿势)在水下光斑与呼吸动画中缓缓浮动
- 6+2 种休闲小动作:伸懒腰、转圈圈、打瞌睡、蹦蹦跳、左右张望、冒泡泡、跳舞、打喷嚏
- 会在屏幕工作区内自由漫游,被你拖走就乖乖待着

### 💬 聊天与任务
- **双击/长按/右键菜单** 打开聊天室:纯本地 WPF 窗口,与鲸鱼娘会话实时对话
- **🛠️ 工作台**:下达任务,实时查看她的工作流(工具调用、结果、汇报)
- **主动问候**:你冷落她超过 25 分钟,她会主动游回来找你说话
- **服务自愈**:DSH 服务掉线后,她会自己把服务拉起来——PowerShell 可以退休了

### 🎨 个性化
- 聊天室双主题:**深海蓝 / 纯黑**,一键切换并记住
- 称呼、姿势、行为节奏都可以调

---

## 🏗️ 系统架构

```
┌──────────────────────────────┐        HTTP (127.0.0.1:3080)
│   WhalePet.exe (WPF 桌宠)     │◄─────────────────────┐
│   ─ 形象/动画/互动             │                      │
│   ─ 服务守护(断线自动拉起)     │                      │
└──────────────────────────────┘                      │
        │ 双击 / 长按 / 菜单                          │
        ▼                                             │
┌──────────────────────────────┐                      │
│   ChatWindow (聊天室 + 工作台) │──── /api/whale/* ────┤
└──────────────────────────────┘                      │
                                                      ▼
                                          ┌──────────────────────────┐
                                          │  dsh-plugin-whale-bridge   │
                                          │  (DSH 静态桥接插件)          │
                                          │  /chat /activity /poll      │
                                          └────────────┬───────────────┘
                                                       ▼
                                          ┌──────────────────────────┐
                                          │  whale-pet 会话            │
                                          │  (whale-girl 人格 preset)  │
                                          │  有工具,会干活             │
                                          └──────────────────────────┘
```

- **桌面端**:`WhalePet/` — WPF + .NET 9,零外部运行时依赖(仅 WebView2 时代已移除,纯 WPF)
- **DSH 端**:`dsh-plugin/whale-bridge` — 静态桥接插件,提供聊天/工作台 API 与主动问候
- **人格**:`dsh-plugin/whale-girl-preset` — 鲸鱼娘 agent preset(温柔黏人女仆,会干活)
- **网页版桌宠(可选)**:`dsh-plugin/whale-pet-ui` — 挂在 DSH Web 界面里的同款小桌宠

---

## 🚀 快速开始

### 环境要求
- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- (可选)DeepSeek Harness 用于聊天/任务能力

### 1. 获取立绘素材
角色素材遵循 CC BY-NC-SA 4.0,由仓库脚本下载(不入库):

```powershell
cd scripts
.\fetch-assets.ps1        # 下载立绘并转换为 PNG(需要 node + sharp,或手动放置)
```

或将 `WhalePet/assets/` 下放置以下 PNG(透明背景):
`maid-right.png` / `maid-left.png`(可选)/ `maid-extra-trim.png`(可选)/ `avatar.png`(可选)

### 2. 构建

```powershell
cd WhalePet
dotnet build -c Release
# 输出: bin/Release/net9.0-windows/WhalePet.exe
```

### 3. 安装 DSH 配套(启用聊天/任务)

```powershell
# 1) 安装鲸鱼娘人格 preset(复制到 %USERPROFILE%\.dsh\.agent-presets\whale-girl\)
# 2) 安装桥接插件(复制到 profile 的 node_modules,并在 cordis.patch.yml 加一行)
# 3) 安装网页桌宠(可选)
# 详细步骤见 dsh-plugin/README.md 及各插件目录 README
```

### 4. 运行

双击 `WhalePet.exe`。她会出现在桌面右下角;DSH 服务不在时她会自动拉起。

---

## 🎮 交互手册

| 操作 | 效果 |
|---|---|
| 单击她 | 摸头(随机反应) |
| 双击她 | 亲亲(脸红) |
| 长按 0.65s | 打开聊天室 |
| 右键她 | 菜单:换姿势 / 抱抱 / 晚安 / 聊天室 / 喂小鱼干 / 称呼 / 藏起来 / 退出 |
| 按住拖动 | 把她带到任何位置 |
| 藏起来后 | 她溜到随机角落探出半个脑袋偷看你;点她 = 被抓包(害羞);右键可以"出来吧" |

## 🔧 个性化

- **称呼**:右键 → "📝 怎么称呼你"
- **主题**:聊天室右上角 🎨 深海蓝/纯黑
- **姿势**:右键 → "🔄 换个姿势"

---

## 📜 许可

- **程序代码**:MIT License(见 `LICENSE`)
- **角色素材**:CC BY-NC-SA 4.0(非商业用途),署名链见 `NOTICE.md`
  - 角色原作:上善 · 女仆鲸鱼娘二创:zipzip · 皮肤:Small-tailqwq(dsh-deep-whale)
  - 额外立绘:萌娘百科共享(commons.moegirl.org.cn)

## 💙 致谢

- [Small-tailqwq/dsh-deep-whale](https://github.com/Small-tailqwq/dsh-deep-whale) — 深海女仆工坊皮肤包(角色素材来源)
- [DeepSeek Harness](https://github.com/deepseek-ai/deepseek-harness) — 鲸鱼娘的大脑
- 上善 / ZipZipPipe — 鲸鱼娘角色形象与二创设计
