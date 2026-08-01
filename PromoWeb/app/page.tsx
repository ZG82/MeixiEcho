import GameplayJumpButton from "./GameplayJumpButton";

const localDownloadHref = "/downloads/MeixiEcho_v0.9.5_Windows_x64_Setup.exe";
const downloadHref = process.env.NEXT_PUBLIC_DOWNLOAD_URL?.trim() || localDownloadHref;
const downloadAttribute = downloadHref.startsWith("http") ? undefined : true;

const features = [
  {
    index: "01",
    title: "聆听村庄",
    copy: "沿溪流、古桥与民居采集声音。水滴、竹叶、脚步与乡音，都会成为可演奏的记忆。",
    tone: "cyan",
  },
  {
    index: "02",
    title: "节拍战斗",
    copy: "敌人的攻击写在节拍里。近战突进、远程音符与瞬身闪避，都必须在魂音收束的瞬间回应。",
    tone: "red",
  },
  {
    index: "03",
    title: "双手声部",
    copy: "第三阶段同时操控 Q / E 持续声部与 J / K / SPACE 主旋律，在混乱中完成真正的合奏。",
    tone: "gold",
  },
  {
    index: "04",
    title: "乡村叙事",
    copy: "从泛黄日记到村民对话，跟随林默重新理解技术、音乐，以及人与土地之间被遗忘的连接。",
    tone: "paper",
  },
];

export default function Home() {
  return (
    <main>
      <header className="site-nav">
        <a className="brand" href="#top" aria-label="返回首页">
          <span className="brand-mark">回</span>
          <span>
            <b>梅溪回响</b>
            <small>MEIXI ECHO</small>
          </span>
        </a>
        <nav aria-label="主导航">
          <a href="#story">故事</a>
          <a href="#gameplay">玩法</a>
          <a href="#chapters">章节</a>
          <a href="#gallery">海报</a>
          <a href="#release">版本</a>
        </nav>
        <a className="nav-download" href={downloadHref} download={downloadAttribute}>
          下载 Windows 版
        </a>
      </header>

      <section className="hero" id="top">
        <div className="hero-copy">
          <div className="eyebrow">
            <span>第三人称剧情动作音游</span>
            <i />
            <span>Windows</span>
          </div>
          <h1>
            <span>梅溪</span>
            回响
          </h1>
          <p className="latin-title">MEIXI ECHO</p>
          <p className="hero-lead">
            一个来自城市的大学生，走进一座逐渐失去声音的村庄。
            <br />
            用节拍战斗，用聆听找回人与土地之间的连接。
          </p>
          <div className="hero-actions">
            <a className="primary-button" href={downloadHref} download={downloadAttribute}>
              <span>下载 Windows 版</span>
              <b>↓</b>
            </a>
            <GameplayJumpButton />
          </div>
          <div className="hero-meta" aria-label="版本信息">
            <div>
              <small>当前版本</small>
              <strong>v0.9.5</strong>
            </div>
            <div>
              <small>平台</small>
              <strong>Windows x64</strong>
            </div>
            <div>
              <small>模式</small>
              <strong>单人 · 离线</strong>
            </div>
          </div>
        </div>

        <div className="poster-wrap" aria-label="梅溪回响漫画式战斗海报">
          <div className="poster-shadow" />
          <div className="poster">
            <div className="poster-shot poster-combat" />
            <div className="poster-shot poster-village" />
            <div className="poster-cyan-wash" />
            <div className="poster-red-wash" />
            <div className="halftone" />
            <div className="speed-lines" />
            <div className="poster-version">VER. 0.9.5</div>
            <div className="poster-caption">
              <span>LISTEN</span>
              <b>让失去的声音<br />重新响起</b>
            </div>
            <div className="poster-seal">魂<br />音</div>
          </div>
          <p className="poster-note">ORIGINAL KEY ART · v0.9.5</p>
        </div>
      </section>

      <section className="story section" id="story">
        <div className="section-heading">
          <span className="section-number">01</span>
          <div>
            <p className="kicker">A VILLAGE LOSING ITS VOICE</p>
            <h2>村庄沉默之后，<br />谁还记得自己的声音？</h2>
          </div>
        </div>

        <div className="story-grid">
          <div className="diary-frame">
            <div className="diary-image" />
            <span className="frame-label">序章日记 · 回乡之前</span>
          </div>
          <div className="story-copy">
            <p className="dropcap">
              林默，21 岁，电子信息专业学生。他相信技术，却不相信声音拥有记忆。
            </p>
            <p>
              一次乡村数字文化调研，让他来到闽清梅溪村。这里的溪水、山歌、劳作与笑声曾共同构成“魂音”，如今却正在破碎、消失。
            </p>
            <blockquote>“声音怎么可能有记忆？”</blockquote>
            <p>
              从采集第一枚破碎音符开始，林默将面对失调的溪灵，也将面对那个拒绝承认乡村记忆的自己。
            </p>
          </div>
        </div>
      </section>

      <section className="gameplay section" id="gameplay">
        <div className="section-heading compact">
          <span className="section-number">02</span>
          <div>
            <p className="kicker">RHYTHM IS YOUR WEAPON</p>
            <h2>不是看见按键才行动，<br />而是听见危险正在靠近。</h2>
          </div>
        </div>

        <div className="feature-grid">
          {features.map((feature) => (
            <article className={`feature-card ${feature.tone}`} key={feature.index}>
              <div className="feature-index">{feature.index}</div>
              <h3>{feature.title}</h3>
              <p>{feature.copy}</p>
              <span className="feature-line" />
            </article>
          ))}
        </div>

        <div className="combat-strip">
          <div className="combat-image" />
          <div className="combat-overlay">
            <p>FIRST CHAPTER · RHYTHM COMBAT</p>
            <h3>看清锁定，踩准节拍，回应攻击。</h3>
            <div className="key-row" aria-label="战斗按键">
              <span><b>J</b>近战</span>
              <span><b>K</b>远程</span>
              <span><b>SPACE</b>闪避</span>
              <span><b>Q / E</b>左手声部</span>
            </div>
          </div>
        </div>
      </section>

      <section className="chapters section" id="chapters">
        <div className="section-heading compact">
          <span className="section-number">03</span>
          <div>
            <p className="kicker">TWO COMPLETE CHAPTERS</p>
            <h2>两段旅途已经完整，<br />从溪声走到戏台深处。</h2>
          </div>
        </div>
        <div className="chapter-route" aria-label="第一章与第二章内容状态">
          <article className="chapter-route-item chapter-one">
            <span className="chapter-index">I</span>
            <div>
              <small>CHAPTER I · COMPLETE</small>
              <h3>没有声音的村庄</h3>
              <p>调查梅溪村、记录三位村民的声纹，收集溪流魂音，并完成三阶段节拍战斗。</p>
              <ul>
                <li>村庄探索与 NPC 叙事</li>
                <li>魂音采集与三阶段溪灵战</li>
              </ul>
            </div>
          </article>
          <div className="chapter-bridge" aria-hidden="true"><i /><span>余音之路</span><i /></div>
          <article className="chapter-route-item chapter-two">
            <span className="chapter-index">II</span>
            <div>
              <small>CHAPTER II · COMPLETE</small>
              <h3>古厝里的旧曲</h3>
              <p>唤醒传统乐器，解开古戏台机关，深入镜像迷宫，并把最后的回响带回梅溪村。</p>
              <ul>
                <li>乐器互动、物件检视与机关解谜</li>
                <li>木偶追逐、镜像迷宫与终末之书</li>
              </ul>
            </div>
          </article>
        </div>
      </section>

      <section className="gallery section" id="gallery">
        <div className="section-heading compact">
          <span className="section-number">04</span>
          <div>
            <p className="kicker">SCENES EXPANDED FROM THE GAME</p>
            <h2>源自实机场景，<br />扩展成梅溪的电影记忆。</h2>
          </div>
        </div>
        <div className="poster-gallery">
          <figure className="gallery-poster gallery-village">
            <div className="gallery-label">
              <small>EXPLORATION · 01</small>
              <strong>溪流里的节拍</strong>
              <span>保留第一章地图结构，扩展闽清雨后村景与溪流魂音。</span>
            </div>
          </figure>
          <figure className="gallery-poster gallery-battle">
            <div className="gallery-label">
              <small>COMBAT · PHASE III</small>
              <strong>双手声部</strong>
              <span>青金持续声部与红黑噪音攻击，在同一节拍正面碰撞。</span>
            </div>
          </figure>
          <figure className="gallery-poster gallery-prologue">
            <div className="gallery-label">
              <small>PROLOGUE · RETURN</small>
              <strong>回乡之前</strong>
              <span>横版日记投映出石桥、旧村与两代人的声音记忆。</span>
            </div>
          </figure>
          <figure className="gallery-poster gallery-chapter-two">
            <div className="gallery-label">
              <small>CHAPTER II · OLD SONG</small>
              <strong>古厝里的旧曲</strong>
              <span>走进封闭古厝戏台，让沉默的乐器与墙壁重新显现旧日合奏。</span>
            </div>
          </figure>
        </div>
        <div className="chapter-two-art-grid">
          <figure className="chapter-art chapter-art-stage">
            <figcaption>
              <small>CHAPTER II · INSTRUMENTS</small>
              <strong>旧乐器重新发声</strong>
              <span>以游戏乐器舞台为结构参考，重绘木构、编钟、鼓与古筝的历史质感。</span>
            </figcaption>
          </figure>
          <figure className="chapter-art chapter-art-maze">
            <figcaption>
              <small>CHAPTER II · MIRROR MAZE</small>
              <strong>镜中不是唯一的归路</strong>
              <span>手电照亮错位反射，声纹沿潮湿木廊延伸；只有镜中的木偶知道哪一面通向旧戏台。</span>
            </figcaption>
          </figure>
        </div>
        <div className="campaign-poster-feature">
          <figure className="campaign-poster-image" aria-label="梅溪回响主宣发海报" />
          <div className="campaign-poster-copy">
            <p className="kicker">OFFICIAL CAMPAIGN ART · v0.9.5</p>
            <h3>两章完整，余音归村</h3>
            <p>从雨后梅溪村到封闭古戏台，探索、节拍战斗、乐器互动与终末回流汇成一段完整旅程。</p>
            <dl>
              <div><dt>第一章</dt><dd>村庄调查 · 魂音采集 · 三阶段溪灵战</dd></div>
              <div><dt>第二章</dt><dd>古戏台机关 · 镜像迷宫 · 木偶追逐</dd></div>
              <div><dt>终章</dt><dd>返回梅溪村 · 拾取终末之书</dd></div>
            </dl>
          </div>
        </div>
        <p className="gallery-footnote">海报取材于游戏场景，并以电影式构图重绘梅溪村、古戏台与声纹意象。</p>
      </section>

      <section className="release section" id="release">
        <div className="release-card">
          <div className="release-stamp">
            <small>CURRENT<br />BUILD</small>
            <strong>0.9.5</strong>
          </div>
          <div className="release-copy">
            <p className="kicker">LATEST RELEASE · 2026.08</p>
            <h2>第一章、第二章完整版现已完成</h2>
            <p>
              从梅溪村调查到古厝戏台迷宫，两章主线现已完整贯通。
              v0.9.5 继续完善终章回流、战斗节奏、场景表现与交互体验。
            </p>
            <aside className="update-notice" aria-label="v0.9.5 更新通知">
              <strong>v0.9.5 更新通知</strong>
              <ul>
                <li>第一章、第二章完整内容已完成，主线可连续体验至终章</li>
                <li>第二章加入乐器互动、机关解谜、物件检视、镜像迷宫与木偶追逐</li>
                <li>第二章结束返回梅溪村后，终末之书会在地图中央显现</li>
                <li>第一章第三阶段再次降低难度，双手声部判定放宽至基础窗口的 2.10 倍</li>
                <li>缩短第三阶段持续声部长按时长，并继续美化人物、村庄与戏台表现</li>
              </ul>
            </aside>
            <ul>
              <li>单文件 Windows 安装程序</li>
              <li>无需账号，可完全离线运行</li>
              <li>玩家存档独立保存在当前 Windows 用户目录</li>
            </ul>
          </div>
          <div className="release-action">
            <a className="download-card" href={downloadHref} download={downloadAttribute}>
              <span>
                <small>WINDOWS x64 · v0.9.5 · 约 152 MB</small>
                <strong>下载 0.9.5 安装包</strong>
              </span>
              <b>↓</b>
            </a>
            <p>支持 Windows 10 / 11 64 位</p>
          </div>
        </div>
      </section>

      <section className="requirements section">
        <div>
          <p className="kicker">BEFORE YOU LISTEN</p>
          <h2>运行建议</h2>
        </div>
        <dl>
          <div><dt>操作系统</dt><dd>Windows 10 / 11 64 位</dd></div>
          <div><dt>内存</dt><dd>建议 8 GB 或以上</dd></div>
          <div><dt>显卡</dt><dd>支持 DirectX 11 / 12</dd></div>
          <div><dt>存储空间</dt><dd>预留约 1 GB</dd></div>
        </dl>
      </section>

      <footer>
        <a className="brand footer-brand" href="#top">
          <span className="brand-mark">回</span>
          <span><b>梅溪回响</b><small>MEIXI ECHO</small></span>
        </a>
        <p>一个关于声音、故乡与记忆的第三人称音乐冒险。</p>
        <span>© 2026 MEIXI DIGITAL CULTURE LAB</span>
      </footer>
    </main>
  );
}
