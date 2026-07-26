# 安全策略

## 支持范围

当前维护版本为 `v0.9.x`。游戏为本地离线程序，宣发网站使用静态导出，不应部署项目中的开发服务器作为公开生产服务。

## 报告安全问题

请优先使用 GitHub 的 Private vulnerability reporting / Security Advisory 私下报告安全问题，不要先创建公开 Issue。

报告建议包含：

- 受影响版本与平台。
- 可复现步骤及影响范围。
- 必要的日志或最小化证明。

请移除个人存档、用户名、绝对路径、访问令牌和受授权限制的素材。

## 依赖维护

- 宣发网站依赖由 `PromoWeb/package-lock.json` 锁定。
- GitHub Dependabot 每周检查 npm 更新。
- 上游安全修复合并后必须重新执行 `npm test`。
- 不使用未经验证的 `npm audit fix --force` 直接修改发布分支。

