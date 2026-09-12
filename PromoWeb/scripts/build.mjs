import { cpSync, existsSync, rmSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const projectRoot = fileURLToPath(new URL("../", import.meta.url));
const vinextCli = fileURLToPath(
  new URL("../node_modules/vinext/dist/cli.js", import.meta.url),
);
const staticClient = fileURLToPath(new URL("../dist/client/", import.meta.url));
const staticExport = fileURLToPath(new URL("../out/", import.meta.url));
const deploymentExclusions = [
  "downloads/MeixiEcho_v0.9.3_Windows_x64_Setup.exe",
  "downloads/MeixiEcho_v0.9.5_Windows_x64_Setup.exe",
  "media/meixi-echo-promo-30s.mp4",
  "media/meixi-echo-campaign-poster.png",
  "media/meixi-echo-campaign-poster-v2.png",
  "media/meixi-echo-v095-terminal-book-keyart.png",
].map((fileName) => fileURLToPath(new URL(`../out/${fileName}`, import.meta.url)));

function syncStaticExport() {
  const sourceIndex = fileURLToPath(new URL("../dist/client/index.html", import.meta.url));
  if (!existsSync(sourceIndex)) {
    throw new Error("vinext build completed without dist/client/index.html");
  }

  rmSync(staticExport, { recursive: true, force: true });
  cpSync(staticClient, staticExport, { recursive: true });
  for (const artifact of deploymentExclusions) rmSync(artifact, { force: true });
  process.stdout.write("[build] refreshed static export in out/.\n");
}

const result = spawnSync(
  process.execPath,
  [vinextCli, "build", ...process.argv.slice(2)],
  {
    cwd: projectRoot,
    env: process.env,
    encoding: "utf8",
    maxBuffer: 32 * 1024 * 1024,
  },
);

process.stdout.write(result.stdout ?? "");
process.stderr.write(result.stderr ?? "");

if (result.status === 0) {
  syncStaticExport();
  process.exit(0);
}

const output = `${result.stdout ?? ""}\n${result.stderr ?? ""}`;
const builtServer = fileURLToPath(
  new URL("../dist/server/index.js", import.meta.url),
);
const knownWindowsShutdownAssertion =
  process.platform === "win32" &&
  output.includes("Build complete") &&
  output.includes("UV_HANDLE_CLOSING") &&
  existsSync(builtServer);

if (knownWindowsShutdownAssertion) {
  syncStaticExport();
  process.stderr.write(
    "[build] vinext completed successfully; ignoring its known Windows libuv shutdown assertion.\n",
  );
  process.exit(0);
}

if (result.error) {
  throw result.error;
}

process.exit(result.status ?? 1);
