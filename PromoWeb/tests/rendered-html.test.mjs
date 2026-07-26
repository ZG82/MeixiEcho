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
  assert.match(html, /v0\.9\.3/);
  assert.match(html, /判定时长增加 75%/);
  assert.match(html, /增加终端系统/);
  assert.match(html, /href="\/downloads\/MeixiEcho_v0\.9\.3_Windows_x64_Setup\.exe"/);
  assert.doesNotMatch(html, /codex-preview|Your site is taking shape|SkeletonPreview/);
});

test("ships the generated key art and release download configuration", async () => {
  const [page, css, packageJson, webReadme, poster, villagePoster, battlePoster, prologuePoster, chapterTwoPoster] = await Promise.all([
    readFile(new URL("../app/page.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/globals.css", import.meta.url), "utf8"),
    readFile(new URL("../package.json", import.meta.url), "utf8"),
    readFile(new URL("../README.md", import.meta.url), "utf8"),
    stat(new URL("../public/media/hero-poster-key-art.png", import.meta.url)),
    stat(new URL("../public/media/poster-village-exploration.png", import.meta.url)),
    stat(new URL("../public/media/poster-phase-three.png", import.meta.url)),
    stat(new URL("../public/media/poster-prologue-return.png", import.meta.url)),
    stat(new URL("../public/media/poster-chapter-two-old-song.png", import.meta.url)),
  ]);

  assert.match(page, /ORIGINAL KEY ART/);
  assert.match(css, /hero-poster-key-art\.png/);
  assert.doesNotMatch(page, /gpt[- ]?image/i);
  assert.doesNotMatch(css, /gpt[- ]?image/i);
  assert.doesNotMatch(packageJson, /react-loading-skeleton/);
  assert.match(page, /NEXT_PUBLIC_DOWNLOAD_URL/);
  assert.match(webReadme, /GitHub Releases/);
  assert.ok(poster.size > 1_000_000, "generated poster should be a production asset");
  assert.ok(villagePoster.size > 1_000_000, "village poster should be present");
  assert.ok(battlePoster.size > 1_000_000, "battle poster should be present");
  assert.ok(prologuePoster.size > 1_000_000, "prologue poster should be present");
  assert.ok(chapterTwoPoster.size > 1_000_000, "chapter two poster should be present");
});
