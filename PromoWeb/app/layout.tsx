import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "梅溪回响 · 第三人称乡村音乐冒险",
  description:
    "《梅溪回响》v0.9.3 Windows 体验版：在逐渐失去声音的闽清村庄中，用节拍战斗找回人与土地之间的连接。",
  openGraph: {
    title: "梅溪回响 · MEIXI ECHO",
    description: "第三人称剧情动作音游 / 音乐冒险 RPG，v0.9.3 Windows 体验版现已开放下载。",
    type: "website",
  },
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="zh-CN">
      <body>{children}</body>
    </html>
  );
}
