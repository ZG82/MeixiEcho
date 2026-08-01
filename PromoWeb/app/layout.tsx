import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "梅溪回响 · 第三人称乡村音乐冒险",
  description:
    "《梅溪回响》v0.9.5：第一章与第二章完整内容现已完成，在闽清村庄与古厝戏台之间找回失落的声音。",
  openGraph: {
    title: "梅溪回响 · MEIXI ECHO",
    description: "第三人称剧情动作音游 / 音乐冒险 RPG，v0.9.5 第一章与第二章完整内容现已完成。",
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
