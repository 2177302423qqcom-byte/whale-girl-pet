# whale-bridge — 鲸鱼娘桥接插件(静态)

桌宠与 DSH 之间的桥梁:为 whale-pet 会话提供 HTTP API,并驱动主动问候。

## 安装

```powershell
# 1. 复制包到 profile 的共享 node_modules
Copy-Item -Recurse whale-bridge `
  "$env:USERPROFILE\.dsh\profiles\node_modules\@dsh-external\dsh-plugin-whale-bridge"
```

# 2. 在 profile 的补丁文件中登记
# 编辑 %USERPROFILE%\.dsh\profiles\web\cordis.patch.yml,追加:

```yaml
- insert:
    - id: ui-whale-bridge
      name: '@dsh-external/dsh-plugin-whale-bridge'
```

重启 `dsh web`(或让桌宠自动拉起)。

## 验证

```
GET http://127.0.0.1:3080/api/whale/status
→ {"ok":true,"server":true,"rev":3,...}
```

## API

| 端点 | 说明 |
|---|---|
| `POST /api/whale/chat` | `{text}` → 聊天/派任务,返回鲸鱼娘回复 |
| `GET /api/whale/status` | 桥接与代理状态 |
| `GET /api/whale/poll` | 拉取鲸鱼娘主动消息 |
| `POST /api/whale/act` | `{kind:"greet"}` 触发主动问候 |
| `GET /api/whale/activity` | 工作台活动流(最近 40 条) |

## 许可

MIT。
