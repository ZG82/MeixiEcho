# 《梅溪回响》宣发网站

游戏 v0.9.3 的宣传与下载页面，使用 vinext、React 和 Cloudflare Vite 插件构建。

## 环境

- Node.js `>=22.13.0`
- npm

## 本地开发

```bash
npm ci
npm run dev
```

默认下载链接为：

```text
/downloads/MeixiEcho_v0.9.3_Windows_x64_Setup.exe
```

本地需要测试下载时，可自行把安装包复制到 `public/downloads/`。安装包已被 Git 忽略，不会提交到 GitHub。

## 生产下载地址

正式部署时使用环境变量指向 GitHub Releases 或 CDN：

```text
NEXT_PUBLIC_DOWNLOAD_URL=https://github.com/<owner>/<repository>/releases/download/v0.9.3/MeixiEcho_v0.9.3_Windows_x64_Setup.exe
```

可复制 `.env.example` 为本地环境文件后修改。不要把真实部署密钥写入仓库。

## 验证

```bash
npm test
```

测试会构建页面、检查主要文案和海报资源，并验证下载地址支持通过环境变量配置；测试不要求仓库内存在 Windows 安装包。

Windows 下 vinext 可能在已经输出 `Build complete` 后触发 libuv 退出断言。`scripts/build.mjs` 仅在构建产物存在且错误精确匹配该断言时兼容处理，其他构建错误仍会返回失败。
