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
  assert.match(html, /v1\.1\.1/);
  assert.match(html, /三章完整，终章已抵达/);
  assert.match(html, /七日回声/);
  assert.match(html, /终末之书/);
  assert.match(html, /现已完成 · 多重选择等待书写/);
  assert.match(html, /Windows 安装包已就绪/);
  assert.match(html, /class="hero-canvas"/);
  assert.match(html, /class="particle-reveal"/);
  assert.match(html, /class="particle-caption"/);
  assert.match(html, /<button class="text-link gameplay-jump" type="button">查看玩法/);
  assert.doesNotMatch(html, /<a class="text-link" href="#gameplay">/);
  assert.doesNotMatch(html, /gpt[- ]?image/i);
  assert.doesNotMatch(html, /codex-preview|Your site is taking shape|SkeletonPreview/);
});

test("ships the final-chapter poster and v1.1.1 release page", async () => {
  const [page, css, packageJson, webReadme, exportedHtml, poster, villagePoster, battlePoster, prologuePoster, chapterTwoPoster, chapterTwoInstrumentPoster, mirrorMazePoster, chapterThreeHomestayPoster, chapterThreeArchivePoster, finalChapterPoster, installer] = await Promise.all([
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
    stat(new URL("../public/media/poster-chapter-three-homestay.png", import.meta.url)),
    stat(new URL("../public/media/poster-chapter-three-archive.png", import.meta.url)),
    stat(new URL("../public/media/meixi-echo-chapter-three-final-poster.png", import.meta.url)),
    stat(new URL("../out/downloads/MeixiEcho_v1.1.1_Windows_x64_Setup.exe", import.meta.url)),
  ]);

  assert.match(page, /ORIGINAL KEY ART/);
  assert.match(css, /hero-poster-key-art\.png/);
  assert.doesNotMatch(page, /gpt[- ]?image/i);
  assert.match(css, /meixi-echo-chapter-three-final-poster\.png/);
  assert.match(css, /poster-chapter-two-instruments\.png/);
  assert.match(css, /poster-chapter-two-mirror-maze\.png/);
  assert.match(css, /poster-chapter-three-homestay\.png/);
  assert.match(css, /poster-chapter-three-archive\.png/);
  assert.doesNotMatch(css, /scroll-behavior:\s*smooth/);
  const mediaUrls = [...css.matchAll(/url\('\/media\/([^']+)'\)/g)].map((match) => match[1]);
  assert.equal(new Set(mediaUrls).size, mediaUrls.length, "each visual should be used only once");
  assert.doesNotMatch(packageJson, /react-loading-skeleton/);
  assert.match(webReadme, /GitHub Releases/);
  assert.match(exportedHtml, /Windows 安装包已就绪/);
  assert.match(exportedHtml, /三章完整，终章已抵达/);
  assert.match(exportedHtml, /七日回声/);
  assert.doesNotMatch(exportedHtml, /gpt[- ]?image|MeixiEcho_v0\.9\.3/i);
  assert.ok(poster.size > 1_000_000, "generated poster should be a production asset");
  assert.ok(villagePoster.size > 1_000_000, "village poster should be present");
  assert.ok(battlePoster.size > 1_000_000, "battle poster should be present");
  assert.ok(prologuePoster.size > 1_000_000, "prologue poster should be present");
  assert.ok(chapterTwoPoster.size > 1_000_000, "chapter two poster should be present");
  assert.ok(chapterTwoInstrumentPoster.size > 1_000_000, "chapter two instrument poster should be present");
  assert.ok(mirrorMazePoster.size > 1_000_000, "mirror maze poster should be present");
  assert.ok(chapterThreeHomestayPoster.size > 1_000_000, "chapter three homestay poster should be present");
  assert.ok(chapterThreeArchivePoster.size > 1_000_000, "chapter three archive poster should be present");
  assert.ok(finalChapterPoster.size > 1_000_000, "final-chapter poster should be present");
  assert.ok(installer.size > 100_000_000, "v1.1.1 installer should be exported to out/downloads");
  assert.match(exportedHtml, /downloads\/MeixiEcho_v1\.1\.1_Windows_x64_Setup\.exe/);
  assert.match(page, /MeixiEcho_v1\.1\.1_Windows_x64_Setup\.exe/);
  assert.match(exportedHtml, /class="download-card"/);
});
