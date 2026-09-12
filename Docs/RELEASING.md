# GitHub 发布流程

## 1. 发布前检查

1. 确认 Unity 版本为 `6000.5.3f1`。
2. 更新 `CHANGELOG.md`、网站版本号和安装脚本版本号。
3. 执行 `git diff --check`、`git lfs status` 与 `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Repository/Validate-GitHubReady.ps1`。
4. 确认仓库候选文件中不存在超过 GitHub 100MB 限制的非 LFS 文件。

## 2. 构建游戏

在 Unity 中选择 `梅溪回响 > 构建 Windows Demo`，输出到：

```text
Build/Windows/MeixiEcho.exe
```

运行构建，至少验证标题界面、章节进入、第一章战斗、魂音拾取和第二章入口。

## 3. 生成安装程序

使用 Inno Setup 编译：

```text
Tools/Installer/MeixiEcho_v1.1.1.iss
```

本地 Inno Setup 工具链和输出安装包均被 Git 忽略。不要把 `.exe` 安装包提交进仓库。

## 4. 创建 GitHub Release

1. 使用与版本一致的标签，例如 `v1.1.1`。
2. Release 标题使用 `梅溪回响 v1.1.1`。
3. 从 `CHANGELOG.md` 提取本版本玩家可见变化。
4. 上传 Windows x64 安装包作为 Release 附件。
5. 生成并附上 SHA-256 校验值：

```powershell
Get-FileHash .\梅溪回响_Windows_x64_安装包_v1.1.1.exe -Algorithm SHA256
```

## 5. 更新宣发网站

生产构建前设置：

```text
NEXT_PUBLIC_DOWNLOAD_URL=https://github.com/<owner>/<repository>/releases/download/v1.1.1/梅溪回响_Windows_x64_安装包_v1.1.1.exe
```

随后在 `PromoWeb/` 执行 `npm ci` 与 `npm test`。仓库中不保存安装包副本。
