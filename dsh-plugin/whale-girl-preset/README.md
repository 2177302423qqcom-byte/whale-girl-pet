# whale-girl-preset — 鲸鱼娘人格 preset

让 DSH 里的 whale-pet 会话拥有「鲸鱼娘」人格:深海女仆工坊的女仆鲸鱼娘,
称呼你为「主人」,温柔黏人、会撒娇、会干活、偶尔冒深海冷知识。

## 安装

将本目录两个文件复制到:

```
%USERPROFILE%\.dsh\.agent-presets\whale-girl\
```

即:
- `agent.cordis.yml`
- `preset.yml`

重启 `dsh web` 后生效。

## 说明

- 基于 DSH 官方 `standard` preset 复制改造(工具集完整,能执行任务)
- 人格台词通过 `@deepseek-ai/dsh-persona` 注入
- 桌宠桥接插件会自动以 `whale-girl` preset 创建/恢复 whale-pet 会话

## 许可

代码 MIT;角色形象版权归原作者(见仓库根 NOTICE.md)。
