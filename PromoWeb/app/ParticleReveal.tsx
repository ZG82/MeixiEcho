"use client";

import { useEffect, useRef } from "react";

type Vec = { x: number; y: number };
type Rgb = readonly [number, number, number];

type Particle = {
  x: number;
  y: number;
  s1: Vec; // 梅溪回响
  s2: Vec; // ZJH logo
};

const MAX_DEVICE_PIXEL_RATIO = 1.5;
const MEIXI_KEY = 0.55; // 滚动进度 0.55 处开始从梅溪回响变形为 ZJH
const CAPTION_KEY = MEIXI_KEY + (1 - MEIXI_KEY) * 0.5; // 0.775：caption 在变形中点切换
const SAMPLE_H = 900; // 采样高度固定，宽度随视口宽高比变化，保证字形比例不被拉伸
const SAMPLE_STEP = 2; // 采样步长越小，笔画内粒子越密
const MAX_SHAPE_POINTS = 16000;

const COLOR_MEIXI: Rgb = [88, 230, 220];
const COLOR_ZJH: Rgb = [74, 145, 250];

/**
 * 滚动驱动粒子变形：进入区块即显示完整的“梅溪回响”粒子字形（预加载），
 * 继续滚动逐步重组成 ZJH 粒子 logo（蓝色圆环 + 星芒 + 水波）。
 * 仅在区块可见时渲染，尊重 prefers-reduced-motion。
 */
export default function ParticleReveal() {
  const sectionRef = useRef<HTMLElement | null>(null);
  const stageRef = useRef<HTMLDivElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const captionRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const section = sectionRef.current;
    const stage = stageRef.current;
    const canvas = canvasRef.current;
    if (!section || !stage || !canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const dpr = Math.min(window.devicePixelRatio || 1, MAX_DEVICE_PIXEL_RATIO);
    const reducedMotion = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;
    const lerpFactor = reducedMotion ? 1 : 0.12;

    let width = 0;
    let height = 0;
    let rafId = 0;
    let particles: Particle[] = [];
    let sampleW = 900;
    let initVersion = 0;
    const state = { ready: false, visible: false, progress: 0 };

    const clamp01 = (v: number) => Math.min(1, Math.max(0, v));
    const smooth = (v: number) => v * v * (3 - 2 * v);
    const lerp = (a: number, b: number, k: number) => a + (b - a) * k;
    const lerpVec = (a: Vec, b: Vec, k: number): Vec => ({
      x: lerp(a.x, b.x, k),
      y: lerp(a.y, b.y, k),
    });
    const lerpColor = (a: Rgb, b: Rgb, k: number): Rgb => [
      Math.round(lerp(a[0], b[0], k)),
      Math.round(lerp(a[1], b[1], k)),
      Math.round(lerp(a[2], b[2], k)),
    ];

    const drawStar = (
      c: CanvasRenderingContext2D,
      cx: number,
      cy: number,
      outer: number,
      inner: number,
    ) => {
      c.beginPath();
      for (let i = 0; i < 10; i += 1) {
        const r = i % 2 === 0 ? outer : inner;
        const angle = (Math.PI / 5) * i - Math.PI / 2;
        const x = cx + Math.cos(angle) * r;
        const y = cy + Math.sin(angle) * r;
        if (i === 0) c.moveTo(x, y);
        else c.lineTo(x, y);
      }
      c.closePath();
      c.fill();
    };

    const drawMeixi = (c: CanvasRenderingContext2D, w: number, h: number) => {
      // 两行排布（梅溪 / 回响），大字占满屏幕中央（约 24%–76% 高度），上下留白均衡。
      const font = Math.min(w / 2.4, h * 0.31);
      c.font = `700 ${Math.round(font)}px "STKaiti","KaiTi","Microsoft YaHei",serif`;
      c.textAlign = "center";
      c.textBaseline = "middle";
      const cy = h * 0.5;
      c.fillText("梅溪", w / 2, cy - font * 0.5);
      c.fillText("回响", w / 2, cy + font * 0.5);
    };

    const drawZjh = (c: CanvasRenderingContext2D, w: number, h: number) => {
      const cx = w / 2;
      const cy = h * 0.48;
      const r = h * 0.32;

      // 圆环（加粗，保证粒子化后仍可辨识）
      c.lineWidth = Math.max(10, r * 0.12);
      c.beginPath();
      c.arc(cx, cy, r, 0, Math.PI * 2);
      c.stroke();

      // 顶部星芒
      drawStar(c, cx, cy - r, r * 0.28, r * 0.12);

      // 中央 ZJH（textAlign/textBaseline 必须显式居中；-0.04r 补偿 Z/J/H 字形的视觉右重）
      c.font = `900 ${Math.round(r * 0.7)}px "Arial Black",Arial,sans-serif`;
      c.textAlign = "center";
      c.textBaseline = "middle";
      c.fillText("ZJH", cx - r * 0.04, cy - r * 0.05);

      // 字母下方水波
      c.lineWidth = Math.max(5, r * 0.05);
      for (let i = 0; i < 3; i += 1) {
        const baseY = cy + r * 0.5 + i * r * 0.075;
        c.beginPath();
        for (let x = cx - r * 0.6; x <= cx + r * 0.6; x += 8) {
          const y = baseY + Math.sin(x * 0.045 + i * 1.1) * r * 0.045;
          if (x === cx - r * 0.6) c.moveTo(x, y);
          else c.lineTo(x, y);
        }
        c.stroke();
      }

      // 圆环右侧像素方块
      const px = Math.max(7, r * 0.06);
      for (let i = 0; i < 8; i += 1) {
        const col = i % 2;
        const row = Math.floor(i / 2);
        c.fillRect(cx + r + 8 + col * (px + 4), cy - r * 0.55 + row * (px + 4), px, px);
      }
    };

    const samplePoints = (
      draw: (c: CanvasRenderingContext2D, w: number, h: number) => void,
      step: number,
    ): Vec[] => {
      const off = document.createElement("canvas");
      off.width = sampleW;
      off.height = SAMPLE_H;
      const c = off.getContext("2d");
      if (!c) return [];
      c.fillStyle = "#fff";
      c.strokeStyle = "#fff";
      draw(c, sampleW, SAMPLE_H);
      const data = c.getImageData(0, 0, sampleW, SAMPLE_H).data;
      const points: Vec[] = [];
      for (let y = 0; y < SAMPLE_H; y += step) {
        for (let x = 0; x < sampleW; x += step) {
          if (data[(y * sampleW + x) * 4 + 3] > 120) {
            points.push({ x: x / sampleW, y: y / SAMPLE_H });
          }
        }
      }
      // 超出上限时均匀抽样（保持空间分布均匀，不破坏字形）。
      if (points.length > MAX_SHAPE_POINTS) {
        const keep = Math.ceil(points.length / MAX_SHAPE_POINTS);
        return points.filter((_, i) => i % keep === 0);
      }
      return points;
    };

    const buildParticles = (meixi: Vec[], zjh: Vec[]) => {
      const count = Math.min(16000, Math.max(8000, Math.round((width * height) / 75)));
      particles = [];
      const pick = (list: Vec[], i: number) => {
        // 均匀步长映射：点数多于粒子数时，列表末尾的点（扫描序靠下的部分）
        // 也必须分到粒子，否则字形/圆环底部永远缺一块。
        const idx =
          list.length <= count
            ? i % list.length
            : Math.floor((i * list.length) / count);
        const base = list[idx];
        // 抖动以屏幕像素为单位（约 ±0.8px），避免把字形笔画打散。
        const jitter = () => ((Math.random() - 0.5) * 1.6) / width;
        return { x: base.x + jitter(), y: base.y + jitter() };
      };
      for (let i = 0; i < count; i += 1) {
        const s1 = pick(meixi, i);
        particles.push({
          x: s1.x, // 预加载：初始即位于“梅溪回响”字形上
          y: s1.y,
          s1,
          s2: pick(zjh, i),
        });
      }
    };

    // 同步采样：不等待字体加载，系统字体立即可用，进入视口第一帧就有完整字形。
    const initShapes = () => {
      const version = ++initVersion;
      sampleW = Math.max(300, Math.round((SAMPLE_H * width) / height));
      const meixi = samplePoints(drawMeixi, SAMPLE_STEP);
      const zjh = samplePoints(drawZjh, SAMPLE_STEP);
      if (version !== initVersion) return; // 尺寸已变化，丢弃过期采样结果
      buildParticles(meixi, zjh);
      state.ready = true;
      drawFrame();
    };

    const drawFrame = () => {
      if (!state.ready) return;
      const p = state.progress;
      ctx.clearRect(0, 0, width, height);

      const segment = p < MEIXI_KEY ? 0 : 1;
      const k =
        segment === 0 ? 0 : smooth((p - MEIXI_KEY) / (1 - MEIXI_KEY));
      // caption 在变形中点切换，避免“ZJH”标题配着未变形的汉字
      const caption = p < CAPTION_KEY ? "梅溪回响" : "ZJH";
      const color =
        segment === 0 ? COLOR_MEIXI : lerpColor(COLOR_MEIXI, COLOR_ZJH, k);
      ctx.fillStyle = `rgb(${color[0]},${color[1]},${color[2]})`;
      ctx.globalAlpha = 0.85;
      const dotSize = Math.max(2.6, width / 300);

      for (let i = 0; i < particles.length; i += 1) {
        const pt = particles[i];
        const target =
          segment === 0 ? pt.s1 : lerpVec(pt.s1, pt.s2, k);
        pt.x += (target.x - pt.x) * lerpFactor;
        pt.y += (target.y - pt.y) * lerpFactor;
        ctx.fillRect(pt.x * width, pt.y * height, dotSize, dotSize);
      }
      ctx.globalAlpha = 1;
    };

    const loop = () => {
      drawFrame();
      rafId = requestAnimationFrame(loop);
    };

    const sync = () => {
      cancelAnimationFrame(rafId);
      if (!document.hidden && state.visible) {
        rafId = requestAnimationFrame(loop);
      }
    };

    const onScroll = () => {
      const rect = section.getBoundingClientRect();
      const range = rect.height - window.innerHeight;
      state.progress = range > 0 ? clamp01(-rect.top / range) : 0;

      if (captionRef.current) {
        captionRef.current.textContent =
          state.progress < CAPTION_KEY ? "梅溪回响" : "ZJH";
      }
    };

    const resizeBitmap = () => {
      const rect = canvas.getBoundingClientRect();
      const nextWidth = Math.max(1, Math.floor(rect.width));
      const nextHeight = Math.max(1, Math.floor(rect.height));
      const nextBitmapWidth = Math.round(nextWidth * dpr);
      const nextBitmapHeight = Math.round(nextHeight * dpr);
      if (canvas.width === nextBitmapWidth && canvas.height === nextBitmapHeight) {
        return;
      }
      width = nextWidth;
      height = nextHeight;
      canvas.width = nextBitmapWidth;
      canvas.height = nextBitmapHeight;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      // 宽高比变化时重新采样字形，保证比例不失真。
      const nextSampleW = Math.max(300, Math.round((SAMPLE_H * width) / height));
      if (state.ready && Math.abs(nextSampleW - sampleW) > 4) {
        state.ready = false;
        initShapes();
      }
      if (state.ready && reducedMotion) drawFrame();
    };

    const onVisibilityChange = () => sync();

    const onIntersect = (entries: IntersectionObserverEntry[]) => {
      state.visible = entries[0]?.isIntersecting ?? false;
      sync();
    };

    resizeBitmap();
    initShapes();
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", resizeBitmap, { passive: true });
    document.addEventListener("visibilitychange", onVisibilityChange);

    const intersectionObserver = new IntersectionObserver(onIntersect, {
      rootMargin: "0px",
    });
    intersectionObserver.observe(stage);

    const resizeObserver = new ResizeObserver(resizeBitmap);
    resizeObserver.observe(canvas);

    return () => {
      cancelAnimationFrame(rafId);
      intersectionObserver.disconnect();
      resizeObserver.disconnect();
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", resizeBitmap);
      document.removeEventListener("visibilitychange", onVisibilityChange);
    };
  }, []);

  return (
    <section
      className="particle-reveal"
      ref={sectionRef}
      aria-label="滚动粒子动画：梅溪回响与 zjh"
    >
      <div className="particle-reveal-stage" ref={stageRef}>
        <canvas ref={canvasRef} aria-hidden="true" role="presentation" />
        <div className="particle-caption" ref={captionRef} />
      </div>
    </section>
  );
}
