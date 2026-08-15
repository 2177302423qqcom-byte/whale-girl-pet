# whale-pet-ui — 网页版鲸鱼娘桌宠(可选)

挂在 DSH Web 界面右下角的小桌宠:点击说话、右键换姿势/抱抱/藏起来、
定时自言自语。是桌面版桌宠的轻量替代或补充。

## 安装

```powershell
# 1. 复制包到 profile 的共享 node_modules
Copy-Item -Recurse whale-pet-ui `
  "$env:USERPROFILE\.dsh\profiles\node_modules\@dsh-external\dsh-client-ui-whale-girl-pet"
```

# 2. 登记插件(%USERPROFILE%\.dsh\profiles\web\cordis.patch.yml):

```yaml
- insert:
    - id: ui-whale-girl-pet
      name: '@dsh-external/dsh-client-ui-whale-girl-pet'
```

刷新页面,右下角出现网页小桌宠。

## 说明

- 立绘通过 jsDelivr CDN 引用皮肤包素材
- 纯前端,无后端依赖
- 与桌面版桌宠二选一或共存(桌面版更完整)

## 许可

代码 MIT;角色素材 CC BY-NC-SA 4.0(见仓库根 NOTICE.md)。
