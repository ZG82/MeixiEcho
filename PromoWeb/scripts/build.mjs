import { existsSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const projectRoot = fileURLToPath(new URL("../", import.meta.url));
const vinextCli = fileURLToPath(
  new URL("../node_modules/vinext/dist/cli.js", import.meta.url),
);

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
  process.stderr.write(
    "[build] vinext completed successfully; ignoring its known Windows libuv shutdown assertion.\n",
  );
  process.exit(0);
}

if (result.error) {
  throw result.error;
}

process.exit(result.status ?? 1);

