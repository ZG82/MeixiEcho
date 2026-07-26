# 贡献指南

感谢关注《梅溪回响》。提交修改前，请先确认变更不会破坏当前第一章完整流程和第二章入口。

## 开发准备

1. 安装 Unity `6000.5.3f1` 和 Git LFS。
2. 克隆仓库后执行 `git lfs pull`。
3. 从 `Assets/Scenes/MeixiEditable.unity` 启动游戏。

## 分支与提交

- 从最新主分支创建功能分支，名称建议使用 `feature/`、`fix/` 或 `docs/` 前缀。
- 一次提交只处理一个明确主题。
- 不要提交 `Library/`、`Build/`、`Logs/`、`Release/`、本地安装包或编辑器用户设置。
- 新增 Unity 资源时必须同时提交对应 `.meta` 文件。
- 大型模型、贴图、音频和二进制插件必须由 Git LFS 管理。

## 提交前检查

```bash
git status --short
git diff --check
git lfs status
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Repository/Validate-GitHubReady.ps1
```

涉及游戏逻辑时，至少验证：

- 标题界面可以进入游戏。
- 第一章移动、对话、战斗和魂音拾取可正常完成。
- 第一章出口能进入第二章。
- 最大化、窗口切换和暂停恢复正常。

涉及宣发网站时运行：

```bash
cd PromoWeb
npm ci
npm test
```

## Pull Request

PR 描述应写明修改范围、验证方式和可能影响的章节；视觉变化建议附截图或短视频。不要在 PR 中附带 Windows 安装包，发布二进制应上传到 GitHub Releases。
