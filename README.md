<p align="center">
  <img src="PromoWeb/public/media/hero-poster-key-art.png" alt="《梅溪回响》主视觉" width="900" />
</p>

# 《梅溪回响》Meixi Echo

一款以乡村声音记忆为主题的第三人称剧情动作音游。玩家将跟随林默进入梅溪村，通过探索、对话、魂音采集和节拍战斗，让逐渐沉默的村庄重新响起。

> 当前版本：**v1.1.1** · Windows x64 · 三章主线与终章结局流程已完整贯通。

## 游戏内容

- 第三人称村庄探索、NPC 对话、魂音采集与章节推进。
- 近战突进、远程音符波、瞬身闪避和双手声部节奏战斗。
- 第一章三阶段溪灵战斗、慢动作终结技与魂音拾取流程。
- 序章日记、第一章梅溪村、第二章古戏台、镜像迷宫与终末之书回流。
- 第三章“七日回声”：第一人称民宿探索、九日实践循环、四类实践任务与多分支终章影片。
- 本地离线存档、章节选择、成就和设置界面。

## 环境要求

| 工具 | 版本或说明 |
| --- | --- |
| Unity Editor | `6000.5.3f1`，建议通过 Unity Hub 安装 |
| Git LFS | 克隆完整美术、模型和音频资源所必需 |
| 目标平台 | Windows 10 / 11 x64 |
| Node.js | `>=22.13.0`，仅宣发网站开发需要 |

## 获取项目

```bash
git lfs install
git clone <repository-url>
cd Music_game_MinQing
git lfs pull
```

若模型、贴图或音频文件只有几百字节，请先确认 Git LFS 已安装，再执行 `git lfs pull`。

## 在 Unity 中运行

1. 使用 Unity Hub 添加仓库根目录。
2. 使用 Unity `6000.5.3f1` 打开项目，等待首次资源导入完成。
3. 打开 `Assets/Scenes/MeixiEditable.unity`。
4. 点击 Play。

第二章舞台场景位于 `Assets/Scenes/MeixiTheatre.unity`，正常游戏会通过章节入口切换场景。

## 操作

| 按键 | 功能 |
| --- | --- |
| W / A / S / D | 移动 |
| 鼠标 | 环绕镜头 |
| Space | 跳跃；战斗中按提示闪避 |
| E | 对话、调查或采集 |
| J | 近战声纹攻击 |
| K | 远程音符波 |
| Q / E | 第三阶段持续声部 |
| Esc | 暂停与操作说明 |

## 构建 Windows 版本

在 Unity 菜单中选择：

```text
梅溪回响 > 构建 Windows Demo
```

输出目录为 `Build/Windows/`。该目录属于本地构建产物，已从 Git 仓库排除。

安装程序脚本位于 `Tools/Installer/MeixiEcho_v1.1.1.iss`。发布包应上传到 GitHub Releases，不应直接提交到源码仓库。完整步骤见 [Docs/RELEASING.md](Docs/RELEASING.md)。

## 宣发网站

网站源码位于 `PromoWeb/`：

```bash
cd PromoWeb
npm ci
npm run dev
npm test
```

生产环境通过 `NEXT_PUBLIC_DOWNLOAD_URL` 指向 GitHub Release 或 CDN 下载地址；本地也可将安装包放入 `PromoWeb/public/downloads/`。安装包、依赖目录和网站构建结果均不会进入 Git。

## 仓库结构

```text
Assets/             Unity 源码、场景、模型、音频与美术资源
Packages/           Unity Package Manager 依赖清单
ProjectSettings/    Unity 项目设置
PromoWeb/           游戏宣发网站源码
Tools/Installer/    Windows 安装程序脚本
Docs/               发布和维护文档
.github/            Issue 与 Pull Request 模板
```

## 版本与贡献

- 版本变化见 [CHANGELOG.md](CHANGELOG.md)。
- 提交规范见 [CONTRIBUTING.md](CONTRIBUTING.md)。
- 安全问题报告方式见 [SECURITY.md](SECURITY.md)。
- 本项目原创部分采用 [MIT License](LICENSE) 发布。第三方项目、开放素材和单独授权内容仍遵循其各自许可证或授权条件，不因本项目采用 MIT 而改变。
- 第三方项目、开放素材及授权说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) 与 [开源项目引用声明.md](开源项目引用声明.md)。
