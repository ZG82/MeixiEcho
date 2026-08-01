"use client";

import type { MouseEvent } from "react";

export default function GameplayJumpButton() {
  const jumpToGameplay = (event: MouseEvent<HTMLButtonElement>) => {
    const button = event.currentTarget;
    const target = document.getElementById("gameplay");

    if (!target) return;

    event.preventDefault();
    button.blur();
    window.getSelection()?.removeAllRanges();
    document.documentElement.style.overflowY = "auto";
    document.body.style.overflowY = "auto";

    const top = target.getBoundingClientRect().top + window.scrollY - 24;
    window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}#gameplay`);
    window.scrollTo({ top: Math.max(0, top), behavior: "auto" });
  };

  return (
    <button className="text-link gameplay-jump" type="button" onClick={jumpToGameplay}>
      查看玩法 <span aria-hidden="true">↘</span>
    </button>
  );
}
