window.__ModuleLoader__.load({
	id: "@dsh-external/dsh-client-ui-whale-girl-pet",
	factory: (require) => {
		var module = { exports: {} };
		var exports = module.exports;
		Object.defineProperty(exports, Symbol.toStringTag, { value: "Module" });
		//#region src/client/index.js — 鲸鱼娘桌宠 (whale-girl pet)
		const React = require("react");

		const CDN = "https://cdn.jsdelivr.net/gh/Small-tailqwq/dsh-deep-whale@main/maid-atelier/assets/";
		const ART = {
			left: CDN + "maid-atelier-maid-left-v5.webp",
			right: CDN + "maid-atelier-maid-right-v6.webp",
		};
		const PLUGIN_ID = "@dsh-external/dsh-client-ui-whale-girl-pet";

		const L = {
			click: [
				"主人~叫鲸鱼娘有什么事吗?(*´∀`)",
				"嘿嘿,主人今天也要元气满满哦!",
				"深海女仆工坊的鲸鱼娘,随叫随到!",
				"主人摸我头的话…会开心的(小声)",
				"今天的海面风平浪静,适合好好干活呢~",
				"主人,累的话就休息一下,鲸鱼娘给你吹吹~",
				"鲸鱼可是深海的大小姐哦…但在主人面前就是小女仆啦!",
				"诶嘿,被主人发现了~鲸鱼娘一直在看着主人哦!",
				"深海3000米小知识:抹香鲸能潜一个多小时~主人午休也可以哦!",
				"主人加油!鲸鱼娘在深海里给你呐喊——咕噜咕噜~",
			],
			idle: [
				"主人现在在忙什么呢…(晃尾巴)",
				"唔…深海好安静,主人这里好热闹~",
				"鲸鱼娘待机中…随时准备为主人服务!",
				"啊,今天的鱼干真好吃…欸?被主人听到了?",
				"主人不在的时候,鲸鱼娘就在海里数星星~",
				"女仆守则第一条:主人的笑容最重要!",
				"泡泡…咕噜…啊!分心了!",
				"鲸鱼娘有点想主人了…(戳手指)",
			],
			greet: [
				"主人早安~今天的深海很平静,适合精神满满地出发哦!",
				"主人午安~记得按时吃饭,鲸鱼娘会给你加油的!",
				"主人下午好~喝杯水休息一下,鲸鱼娘陪你!",
				"主人晚上好~深海女仆已经准备好听主人今天的趣事啦!",
				"夜深了主人…鲸鱼娘会守在深海里,保护主人安睡哦~",
			],
			hug: [
				"呜哇——!被主人抱住了…好幸福(〃∀〃)",
				"主人…再抱紧一点点也可以的哦~",
				"呼…主人体贴的味道,鲸鱼娘记在心里啦!",
			],
			night: [
				"晚安主人~鲸鱼娘要沉到深海里数鱼(不是数羊)啦…zzz",
				"做个好梦哦主人~明天的深海也为你亮着灯!",
			],
			back: [
				"主人~鲸鱼娘回来啦!想我了吗?",
				"呼——深海好冷,还是主人身边暖和~",
			],
			pose: [
				"怎么样主人~这个姿势的鲸鱼娘也很可爱吧?",
				"诶嘿,换个角度看你家小女仆~",
			],
		};

		const pick = (a) => a[Math.floor(Math.random() * a.length)];
		const greeting = () => {
			const h = new Date().getHours();
			if (h >= 5 && h < 11) return L.greet[0];
			if (h >= 11 && h < 14) return L.greet[1];
			if (h >= 14 && h < 18) return L.greet[2];
			if (h >= 18 && h < 23) return L.greet[3];
			return L.greet[4];
		};

		let greeted = false;

		function Pet() {
			const [bubble, setBubble] = React.useState(null);
			const [hidden, setHidden] = React.useState(false);
			const [pose, setPose] = React.useState("right");
			const [menu, setMenu] = React.useState(false);
			const [hearts, setHearts] = React.useState(0);
			const [imgOk, setImgOk] = React.useState(true);

			React.useEffect(() => {
				if (greeted) return;
				greeted = true;
				const t = window.setTimeout(() => setBubble(greeting()), 1200);
				return () => window.clearTimeout(t);
			}, []);
			React.useEffect(() => {
				const t = window.setInterval(() => { if (!hidden) setBubble(pick(L.idle)); }, 75000);
				return () => window.clearInterval(t);
			}, [hidden]);
			React.useEffect(() => {
				if (bubble === null) return;
				const t = window.setTimeout(() => setBubble(null), 5000);
				return () => window.clearTimeout(t);
			}, [bubble]);
			React.useEffect(() => {
				if (hearts === 0) return;
				const t = window.setTimeout(() => setHearts(0), 1800);
				return () => window.clearTimeout(t);
			}, [hearts]);

			const say = (line) => { setMenu(false); setBubble(line); };
			const onClickAvatar = () => say(pick(L.click));
			const onHug = () => { setMenu(false); setBubble(pick(L.hug)); setHearts((n) => n + 1); };
			const onNight = () => { setMenu(false); setBubble(pick(L.night)); };
			const onPose = () => { setMenu(false); setPose((p) => (p === "right" ? "left" : "right")); setBubble(pick(L.pose)); };
			const onHide = () => { setMenu(false); setHidden(true); setBubble(null); };
			const onRecall = () => { setHidden(false); setBubble(pick(L.back)); };

			if (hidden) {
				return React.createElement(
					"div",
					{ className: "wp-recall", title: "把鲸鱼娘叫回来", onClick: onRecall },
					"🐳",
				);
			}

			return React.createElement(
				"div",
				{ className: "wp-pet", onContextMenu: (e) => { e.preventDefault(); setMenu(true); } },
				bubble !== null
					? React.createElement("div", { className: "wp-bubble", key: bubble }, bubble)
					: null,
				menu
					? React.createElement(React.Fragment, null,
						React.createElement("div", { className: "wp-menu-backdrop", onClick: () => setMenu(false) }),
						React.createElement("div", { className: "wp-menu" },
							React.createElement("div", { className: "wp-menu-item", onClick: onPose }, "🔄 换个姿势"),
							React.createElement("div", { className: "wp-menu-item", onClick: onHug }, "💙 抱抱我"),
							React.createElement("div", { className: "wp-menu-item", onClick: onNight }, "🌙 说晚安"),
							React.createElement("div", { className: "wp-menu-item", onClick: onHide }, "🙈 藏起来"),
						),
					)
					: null,
				React.createElement("div", { className: "wp-float" },
					React.createElement("div", { className: "wp-avatar", onClick: onClickAvatar, title: "鲸鱼娘小助理" },
						imgOk
							? React.createElement("img", { src: ART[pose], alt: "鲸鱼娘", onError: () => setImgOk(false) })
							: React.createElement("span", { className: "wp-emoji" }, "🐳"),
						React.createElement("span", { className: "wp-status" }),
						hearts > 0 ? React.createElement("span", { className: "wp-hearts", key: hearts }, "💙💛") : null,
					),
				),
			);
		}

		function apply(ctx) {
			const slots = ctx.get("slots");
			if (slots === undefined) return;
			const styleEl = document.createElement("style");
			styleEl.setAttribute("data-plugin", PLUGIN_ID);
			styleEl.textContent = [
				".wp-pet{position:fixed;right:22px;bottom:22px;z-index:9999;pointer-events:auto;font-family:inherit;user-select:none;-webkit-user-select:none}",
				".wp-float{animation:wp-float 3.4s ease-in-out infinite}",
				".wp-avatar{width:92px;height:92px;border-radius:50%;cursor:pointer;overflow:hidden;position:relative;border:3px solid rgba(197,164,104,.9);box-shadow:0 8px 26px rgba(6,16,34,.5),inset 0 0 0 2px rgba(255,255,255,.22);background:linear-gradient(160deg,#16324f,#0d1e37);transition:transform .18s ease}",
				".wp-avatar:hover{transform:scale(1.09)}",
				".wp-avatar img{width:100%;height:100%;object-fit:cover;object-position:50% 8%;display:block}",
				".wp-emoji{display:flex;align-items:center;justify-content:center;width:100%;height:100%;font-size:44px}",
				".wp-status{position:absolute;right:1px;bottom:1px;width:15px;height:15px;border-radius:50%;background:#67d98a;border:2.5px solid #0d1e37;animation:wp-blink 2.6s ease-in-out infinite}",
				".wp-hearts{position:absolute;top:-10px;right:-6px;font-size:17px;pointer-events:none;animation:wp-heart 1.8s ease-out forwards;text-shadow:0 0 6px rgba(255,255,255,.6)}",
				".wp-bubble{position:absolute;bottom:calc(100% + 16px);right:-6px;width:max-content;max-width:240px;background:rgba(250,247,241,.97);color:#1c2b45;border-radius:14px 14px 4px 14px;padding:9px 13px;font-size:13px;line-height:1.55;box-shadow:0 10px 30px rgba(6,16,34,.4);border:1px solid rgba(197,164,104,.55);animation:wp-pop .22s ease}",
				".wp-bubble::after{content:'';position:absolute;right:18px;bottom:-8px;border:8px solid transparent;border-top-color:rgba(250,247,241,.97)}",
				".wp-menu-backdrop{position:fixed;inset:0;z-index:5}",
				".wp-menu{position:absolute;bottom:calc(100% + 44px);right:0;z-index:10;background:rgba(19,31,53,.97);color:#eef2f9;border-radius:10px;padding:5px;min-width:126px;box-shadow:0 12px 32px rgba(0,0,0,.5);border:1px solid rgba(197,164,104,.5);animation:wp-pop .18s ease}",
				".wp-menu-item{padding:8px 12px;border-radius:7px;font-size:12.5px;cursor:pointer;white-space:nowrap}",
				".wp-menu-item:hover{background:rgba(197,164,104,.24)}",
				".wp-recall{position:fixed;right:18px;bottom:16px;width:48px;height:48px;border-radius:50%;background:linear-gradient(160deg,#16324f,#0d1e37);border:2.5px solid rgba(197,164,104,.9);box-shadow:0 8px 22px rgba(6,16,34,.5);display:flex;align-items:center;justify-content:center;font-size:23px;cursor:pointer;z-index:9999;pointer-events:auto;animation:wp-pop .25s ease;transition:transform .15s ease}",
				".wp-recall:hover{transform:scale(1.1)}",
				"@keyframes wp-float{0%,100%{transform:translateY(0)}50%{transform:translateY(-7px)}}",
				"@keyframes wp-blink{0%,100%{opacity:1}50%{opacity:.4}}",
				"@keyframes wp-heart{0%{opacity:0;transform:translateY(6px) scale(.6)}20%{opacity:1}100%{opacity:0;transform:translateY(-28px) scale(1.2)}}",
				"@keyframes wp-pop{from{opacity:0;transform:translateY(6px) scale(.95)}to{opacity:1;transform:none}}",
			].join("\n");
			document.head.appendChild(styleEl);
			slots.inject("shell.overlay", () => slots.register(
				{ name: "shell.overlay", id: "whale-girl-pet", order: 100, label: "鲸鱼娘桌宠" },
				() => React.createElement(Pet),
			));
		}
		//#endregion
		module.exports = { apply: apply };
		return module.exports;
	}
});
