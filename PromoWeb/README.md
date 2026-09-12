# 《梅溪回响》宣发网站

游戏 v1.1.1 的宣传页面，使用 vinext、React 和 Cloudflare Vite 插件构建。当前页面展示《梅溪回响》三章完整内容，并以第三章“七日回声”的终章海报与玩法信息收束全篇。

## 环境

- Node.js `>=22.13.0`
- npm

## 本地开发

```bash
npm ci
npm run dev
```

Windows 安装包（v1.1.1）随静态产物一并编译到 `out/downloads/`，页面上的下载按钮默认指向该本地文件；也可通过环境变量改为 GitHub Releases 或 CDN 地址。

## 生产下载地址

正式部署时使用环境变量指向 GitHub Releases 或 CDN：

```text
NEXT_PUBLIC_DOWNLOAD_URL=https://github.com/<owner>/<repository>/releases/download/v1.1.1/MeixiEcho_v1.1.1_Windows_x64_Setup.exe
```

未设置时页面回退到本站 `/downloads/MeixiEcho_v1.1.1_Windows_x64_Setup.exe`。可复制 `.env.example` 为本地环境文件后修改。不要把真实部署密钥写入仓库。

## 验证

```bash
npm test
```

测试会构建页面、检查主要文案和海报资源，验证下载地址可通过环境变量配置，并确认 v1.1.1 安装包已随构建产物导出到 `out/downloads/`。构建与测试要求 `public/downloads/` 中存在 v1.1.1 安装包。

Windows 下 vinext 可能在已经输出 `Build complete` 后触发 libuv 退出断言。`scripts/build.mjs` 仅在构建产物存在且错误精确匹配该断言时兼容处理，其他构建错误仍会返回失败。
