import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "梅溪回响 · 第三人称乡村音乐冒险",
  description:
    "《梅溪回响》三章主线现已完整：在梅溪民宿的九日实践、夜间理智与终末之书中，完成最后的真相与选择。",
  openGraph: {
    title: "梅溪回响 · MEIXI ECHO",
    description: "剧情动作音游 / 音乐冒险 RPG，三章完整主线现已收束，最终章“七日回声”正式开放。",
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
