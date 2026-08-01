import assert from "node:assert/strict";
import { readFile, stat } from "node:fs/promises";
import test from "node:test";

async function render() {
  const workerUrl = new URL("../dist/server/index.js", import.meta.url);
  workerUrl.searchParams.set("test", `${process.pid}-${Date.now()}`);
  const { default: worker } = await import(workerUrl.href);

  return worker.fetch(
    new Request("http://localhost/", { headers: { accept: "text/html" } }),
    { ASSETS: { fetch: async () => new Response("Not found", { status: 404 }) } },
    { waitUntil() {}, passThroughOnException() {} },
  );
}

test("renders the Meixi Echo promotional page", async () => {
  const response = await render();
  assert.equal(response.status, 200);
  assert.match(response.headers.get("content-type") ?? "", /^text\/html\b/i);

  const html = await response.text();
  assert.match(html, /<title>梅溪回响 · 第三人称乡村音乐冒险<\/title>/);
  assert.match(html, /MEIXI ECHO/);
  assert.match(html, /v0\.9\.5/);
  assert.match(html, /第一章、第二章完整版现已完成/);
  assert.match(html, /基础窗口的 2\.10 倍/);
  assert.match(html, /终末之书会在地图中央显现/);
  assert.match(html, /href="\/downloads\/MeixiEcho_v0\.9\.5_Windows_x64_Setup\.exe"/);
  assert.match(html, /下载 0\.9\.5 安装包/);
  assert.match(html, /<button class="text-link gameplay-jump" type="button">查看玩法/);
  assert.doesNotMatch(html, /<a class="text-link" href="#gameplay">/);
  assert.doesNotMatch(html, /gpt[- ]?image/i);
  assert.doesNotMatch(html, /codex-preview|Your site is taking shape|SkeletonPreview/);
});

test("ships the campaign art and v0.9.5 release download", async () => {
  const [page, css, packageJson, webReadme, exportedHtml, poster, villagePoster, battlePoster, prologuePoster, chapterTwoPoster, chapterTwoInstrumentPoster, mirrorMazePoster, campaignPoster, installer, exportedInstaller] = await Promise.all([
    readFile(new URL("../app/page.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/globals.css", import.meta.url), "utf8"),
    readFile(new URL("../package.json", import.meta.url), "utf8"),
    readFile(new URL("../README.md", import.meta.url), "utf8"),
    readFile(new URL("../out/index.html", import.meta.url), "utf8"),
    stat(new URL("../public/media/hero-poster-key-art.png", import.meta.url)),
    stat(new URL("../public/media/poster-village-exploration.png", import.meta.url)),
    stat(new URL("../public/media/poster-phase-three.png", import.meta.url)),
    stat(new URL("../public/media/poster-prologue-return.png", import.meta.url)),
    stat(new URL("../public/media/poster-chapter-two-old-song.png", import.meta.url)),
    stat(new URL("../public/media/poster-chapter-two-instruments.png", import.meta.url)),
    stat(new URL("../public/media/poster-chapter-two-mirror-maze.png", import.meta.url)),
    stat(new URL("../public/media/meixi-echo-v095-terminal-book-keyart.png", import.meta.url)),
    stat(new URL("../public/downloads/MeixiEcho_v0.9.5_Windows_x64_Setup.exe", import.meta.url)),
    stat(new URL("../out/downloads/MeixiEcho_v0.9.5_Windows_x64_Setup.exe", import.meta.url)),
  ]);

  assert.match(page, /ORIGINAL KEY ART/);
  assert.match(css, /hero-poster-key-art\.png/);
  assert.doesNotMatch(page, /gpt[- ]?image/i);
  assert.match(css, /meixi-echo-v095-terminal-book-keyart\.png/);
  assert.match(css, /poster-chapter-two-instruments\.png/);
  assert.match(css, /poster-chapter-two-mirror-maze\.png/);
  assert.doesNotMatch(css, /scroll-behavior:\s*smooth/);
  const mediaUrls = [...css.matchAll(/url\('\/media\/([^']+)'\)/g)].map((match) => match[1]);
  assert.equal(new Set(mediaUrls).size, mediaUrls.length, "each visual should be used only once");
  assert.doesNotMatch(packageJson, /react-loading-skeleton/);
  assert.match(page, /NEXT_PUBLIC_DOWNLOAD_URL/);
  assert.match(webReadme, /GitHub Releases/);
  assert.match(exportedHtml, /下载 0\.9\.5 安装包/);
  assert.doesNotMatch(exportedHtml, /gpt[- ]?image|MeixiEcho_v0\.9\.3/i);
  assert.ok(poster.size > 1_000_000, "generated poster should be a production asset");
  assert.ok(villagePoster.size > 1_000_000, "village poster should be present");
  assert.ok(battlePoster.size > 1_000_000, "battle poster should be present");
  assert.ok(prologuePoster.size > 1_000_000, "prologue poster should be present");
  assert.ok(chapterTwoPoster.size > 1_000_000, "chapter two poster should be present");
  assert.ok(chapterTwoInstrumentPoster.size > 1_000_000, "chapter two instrument poster should be present");
  assert.ok(mirrorMazePoster.size > 1_000_000, "mirror maze poster should be present");
  assert.ok(campaignPoster.size > 1_000_000, "campaign poster should be present");
  assert.equal(installer.size, 158_843_845, "v0.9.5 installer should match the release artifact");
  assert.equal(exportedInstaller.size, installer.size, "out should contain the current installer");
});
