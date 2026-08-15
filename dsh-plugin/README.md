# dsh-plugin — DeepSeek Harness 配套插件

桌宠的聊天/任务能力依赖 DSH 侧的三个部件:

```
whale-girl-preset/  鲸鱼娘人格 preset(温柔黏人女仆,会干活)
whale-bridge/       静态桥接插件(HTTP API:聊天/活动流/主动问候)
whale-pet-ui/       网页版小桌宠(可选,挂在 DSH Web 界面里)
```

## 安装

### 1. 鲸鱼娘人格 preset

将 `whale-girl-preset/` 复制到:

```
%USERPROFILE%\.dsh\.agent-presets\whale-girl\
```

(agent.cordis.yml + preset.yml)

### 2. 桥接插件(必须,聊天/任务依赖)

```powershell
# 复制包到 profile 的共享 node_modules
Copy-Item -Recurse whale-bridge C:\Users\<you>\.dsh\profiles\node_modules\@dsh-external\dsh-plugin-whale-bridge
```

并在 `C:\Users\<you>\.dsh\profiles\web\cordis.patch.yml` 追加:

```yaml
- insert:
    - id: ui-whale-bridge
      name: '@dsh-external/dsh-plugin-whale-bridge'
```

重启 `dsh web`(或桌宠会自动拉起),验证:

```
GET http://127.0.0.1:3080/api/whale/status   → {"ok":true,...}
```

### 3. 网页版桌宠(可选)

同法安装 `whale-pet-ui` 到 node_modules 并在 patch 中登记 `ui-whale-girl-pet`,
刷新页面后右下角出现网页小桌宠。

## API

| 端点 | 说明 |
|---|---|
| `POST /api/whale/chat` | `{text}` → 聊天/派任务,返回鲸鱼娘回复 |
| `GET /api/whale/status` | 桥接与代理状态 |
| `GET /api/whale/poll` | 拉取鲸鱼娘主动消息 |
| `POST /api/whale/act` | 触发主动问候(`{kind:"greet"}`) |
| `GET /api/whale/activity` | 工作台活动流(最近 40 条用户/回复/工具记录) |

## 许可

插件代码 MIT;角色素材 CC BY-NC-SA 4.0(见仓库根 NOTICE.md)。
