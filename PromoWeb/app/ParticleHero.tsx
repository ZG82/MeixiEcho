"use client";

import { useEffect, useRef } from "react";

type Particle = {
  x: number;
  y: number;
  phase: number;
  speed: number;
  size: number;
  jitter: number;
};

const COLORS = {
  calm: "#58e6dc",
  peak: "#e8b75a",
  trough: "#ff4e3f",
} as const;

const RIPPLE_RADIUS = 170;
const MAX_DEVICE_PIXEL_RATIO = 1.5;

/**
 * OpenAI 发布页风格的交互式粒子波场：粒子按网格排布，随声波式正弦起伏，
 * 鼠标移动会在指针周围产生扩散的“回声”波纹。仅在 Hero 可见时渲染，
 * 尊重 prefers-reduced-motion，避免低配设备卡顿。
 */
export default function ParticleHero() {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const dpr = Math.min(window.devicePixelRatio || 1, MAX_DEVICE_PIXEL_RATIO);
    const reducedMotion = window.matchMedia(
      "(prefers-reduced-motion: reduce)",
    ).matches;

    let width = 0;
    let height = 0;
    let particles: Particle[] = [];
    let rafId = 0;
    let isVisible = true;
    const pointer = { x: -1e4, y: -1e4, strength: 0 };
    const random = mulberry32(20260908);

    const buildParticles = () => {
      const spacing = width < 720 ? 22 : 26;
      const cols = Math.ceil(width / spacing);
      const rows = Math.ceil(height / spacing);
      particles = [];
      for (let row = 0; row < rows; row += 1) {
        for (let col = 0; col < cols; col += 1) {
          particles.push({
            x: col * spacing + random() * spacing * 0.3,
            y: row * spacing + random() * spacing * 0.3,
            phase: random() * Math.PI * 2,
            speed: 0.35 + random() * 0.65,
            size: 1.1 + random() * 1.4,
            jitter: random() * spacing * 0.3,
          });
        }
      }
    };

    const drawFrame = (now: number) => {
      ctx.clearRect(0, 0, width, height);
      const t = now / 1000;

      for (let i = 0; i < particles.length; i += 1) {
        const p = particles[i];
        // 双层声波：主波沿 x 铺展，次波沿 y 交叠，形成缓慢的“回响”起伏。
        const wave =
          Math.sin(t * p.speed + p.phase + p.x * 0.012) * 6 +
          Math.sin(t * 0.55 + p.phase * 1.7 + p.y * 0.009) * 4;

        let offsetX = 0;
        let offsetY = wave;

        // 鼠标“回声”波纹：把粒子推离指针并带出一圈扩散波。
        if (pointer.strength > 0.01) {
          const dx = p.x - pointer.x;
          const dy = p.y - pointer.y;
          const dist = Math.hypot(dx, dy);
          if (dist < pointer.strength && dist > 0.001) {
            const falloff = 1 - dist / pointer.strength;
            const push = falloff * falloff * 26;
            offsetX += (dx / dist) * push;
            offsetY += (dy / dist) * push + Math.sin(t * 6 + dist * 0.04) * falloff * 10;
          }
        }

        const intensity = Math.hypot(offsetX, offsetY - wave) / 16;
        ctx.fillStyle =
          intensity > 0.3 ? COLORS.peak : intensity < -0.3 ? COLORS.trough : COLORS.calm;
        ctx.globalAlpha = 0.38 + Math.min(0.62, Math.abs(intensity) * 0.9);
        ctx.fillRect(p.x + offsetX, p.y + offsetY, p.size, p.size);
      }
      ctx.globalAlpha = 1;
    };

    const loop = (now: number) => {
      if (pointer.strength > 0.01) pointer.strength = Math.max(0, pointer.strength - 1.1);
      drawFrame(now);
      rafId = requestAnimationFrame(loop);
    };

    const syncAnimation = () => {
      cancelAnimationFrame(rafId);
      if (!reducedMotion && isVisible && !document.hidden) {
        rafId = requestAnimationFrame(loop);
      }
    };

    const resize = () => {
      const rect = canvas.getBoundingClientRect();
      const nextWidth = Math.max(1, Math.floor(rect.width));
      const nextHeight = Math.max(1, Math.floor(rect.height));
      const nextBitmapWidth = Math.round(nextWidth * dpr);
      const nextBitmapHeight = Math.round(nextHeight * dpr);
      // 位图尺寸未变化时直接返回，避免与 ResizeObserver 形成尺寸放大循环。
      if (
        canvas.width === nextBitmapWidth &&
        canvas.height === nextBitmapHeight
      ) {
        return;
      }
      width = nextWidth;
      height = nextHeight;
      canvas.width = nextBitmapWidth;
      canvas.height = nextBitmapHeight;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      buildParticles();
      if (reducedMotion) drawFrame(0);
    };

    const onPointerMove = (event: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      pointer.x = event.clientX - rect.left;
      pointer.y = event.clientY - rect.top;
      pointer.strength = RIPPLE_RADIUS;
    };

    const onVisibilityChange = () => syncAnimation();

    const onIntersect = (entries: IntersectionObserverEntry[]) => {
      isVisible = entries[0]?.isIntersecting ?? true;
      syncAnimation();
    };

    resize();
    syncAnimation();

    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(canvas);

    const intersectionObserver = new IntersectionObserver(onIntersect, {
      rootMargin: "64px",
    });
    intersectionObserver.observe(canvas);

    window.addEventListener("pointermove", onPointerMove, { passive: true });
    document.addEventListener("visibilitychange", onVisibilityChange);

    return () => {
      cancelAnimationFrame(rafId);
      resizeObserver.disconnect();
      intersectionObserver.disconnect();
      window.removeEventListener("pointermove", onPointerMove);
      document.removeEventListener("visibilitychange", onVisibilityChange);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      className="hero-canvas"
      aria-hidden="true"
      role="presentation"
    />
  );
}

function mulberry32(seed: number) {
  let state = seed | 0;
  return () => {
    state = (state + 0x6d2b79f5) | 0;
    let t = Math.imul(state ^ (state >>> 15), 1 | state);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
