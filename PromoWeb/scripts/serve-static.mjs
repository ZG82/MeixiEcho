// Minimal zero-dependency static server used only for local verification.
import { createServer } from "node:http";
import { readFile, stat } from "node:fs/promises";
import { extname, join, normalize } from "node:path";
import { fileURLToPath } from "node:url";

const root = fileURLToPath(new URL("../out/", import.meta.url));
const port = Number(process.env.PORT || 8080);

const MIME = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".mp4": "video/mp4",
  ".exe": "application/octet-stream",
  ".json": "application/json; charset=utf-8",
  ".md": "text/markdown; charset=utf-8",
};

createServer(async (req, res) => {
  try {
    const url = new URL(req.url ?? "/", "http://localhost");
    let pathname = decodeURIComponent(url.pathname);
    if (pathname === "/") pathname = "/index.html";
    const filePath = normalize(join(root, pathname));
    if (!filePath.startsWith(normalize(root))) {
      res.writeHead(403).end("Forbidden");
      return;
    }
    const info = await stat(filePath);
    if (info.isDirectory()) {
      res.writeHead(404).end("Not found");
      return;
    }
    const body = await readFile(filePath);
    res.writeHead(200, {
      "Content-Type": MIME[extname(filePath)] ?? "application/octet-stream",
      "Content-Length": info.size,
      "Cache-Control": "no-store",
    });
    res.end(body);
  } catch {
    res.writeHead(404).end("Not found");
  }
}).listen(port, () => {
  process.stdout.write(`[serve-static] http://localhost:${port}\n`);
});
