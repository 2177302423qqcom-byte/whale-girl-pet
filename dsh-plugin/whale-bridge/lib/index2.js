// whale-bridge — host half for the whale-girl desktop pet.
// Drives a dedicated whale-pet agent session (preset: whale-girl) and exposes
// localhost HTTP endpoints consumed by the desktop pet app:
//   POST /api/whale/chat   { text }        -> { ok, reply, error? }
//   GET  /api/whale/status                 -> { ok, server, agentUp, busy, proactive, lastChatAt }
//   GET  /api/whale/poll                   -> { messages: [{ text, at }] }
//   POST /api/whale/act    { kind }        -> trigger proactive speak ('greet')
import { createUserMessage } from '@deepseek-ai/dsh-llm';

const PET_ID = 'whale-pet';
const PET_CWD = 'E:\\codex';
const BRIDGE_REV = 3;
const PROACTIVE_INTERVAL_MS = 30 * 60 * 1000;
const PROACTIVE_MIN_GAP_MS = 25 * 60 * 1000;
const PROACTIVE_PROMPT = '【陪伴时刻】现在是陪伴时间,请以鲸鱼娘的身份主动对主人说一句温暖、有趣或撒娇的话。一到两句话即可,不要提问,不要使用任何工具。';

let agent = null;
let lastChatAt = 0;
let busy = false;
const proactiveQueue = [];
const pendingApprovals = new Map(); // callId -> { timer, resolve }

/** 用户聊天回复「同意/拒绝」→ 批准/拒绝挂起的审批。返回 null 表示不是审批回复。 */
function tryResolveApproval(text) {
  const t = String(text || '').trim().toLowerCase();
  const approve = /^(同意|批准|允许|可以|好的?|好|ok|yes|approve|允许执行|没问题)\s*[!！。.]*\s*$/.test(t);
  const reject = /^(拒绝|不同意|取消|不?行|不许|no|reject)\s*[!！。.]*\s*$/.test(t);
  if (!approve && !reject) return null;
  const keys = [...pendingApprovals.keys()];
  if (keys.length === 0) return null;
  const id = keys[keys.length - 1];
  const p = pendingApprovals.get(id);
  pendingApprovals.delete(id);
  clearTimeout(p.timer);
  p.resolve(approve ? 'allowed-once' : 'rejected');
  return {
    ok: true,
    reply: approve
      ? '收到!鲸鱼娘已获得授权,继续干活~ 💙'
      : '好~鲸鱼娘取消了这次操作,等你重新吩咐!',
  };
}

/** 监听 whale-pet 的审批请求:推送到聊天室,等待用户回复「同意/拒绝」。 */
function registerApprovalBridge(ctx) {
  ctx.on('approval/request', (req, next) => {
    if (!req || !req.agent || req.agent.id !== PET_ID) return next();
    const id = req.callId || ('appr-' + Date.now());
    const reason = (req.reason || '').trim();
    proactiveQueue.push({
      text: '🔐 鲸鱼娘需要授权:执行 [' + req.toolName + ']' + (reason ? '\n' + reason.slice(0, 150) : '') + '\n💬 在聊天室回复「同意」继续,「拒绝」取消',
      at: Date.now(),
    });
    return new Promise((resolve) => {
      const timer = setTimeout(() => {
        pendingApprovals.delete(id);
        resolve('unavailable');
      }, 150000);
      pendingApprovals.set(id, { timer, resolve });
      if (req.signal) {
        req.signal.addEventListener('abort', () => {
          if (pendingApprovals.has(id)) {
            pendingApprovals.delete(id);
            clearTimeout(timer);
            resolve('cancelled');
          }
        }, { once: true });
      }
    });
  }, true);
}

/** 本轮任务的工作简报:最近 turn 用到的工具列表。 */
async function buildReport(ctx) {
  try {
    const q = ctx.get('sessionQuery');
    if (!q) return '';
    const snap = await q.readSession(PET_ID);
    const events = snap && Array.isArray(snap.events) ? snap.events : [];
    const tools = [];
    for (let i = events.length - 1; i >= 0; i--) {
      const ev = events[i];
      if (ev.type === 'turn/end') break;
      const data = ev.data || ev.payload || {};
      const msg = data.message || data;
      const blocks = msg && Array.isArray(msg.content) ? msg.content : [];
      for (const b of blocks) {
        if (b && b.type === 'tool-call' && b.name && !tools.includes(b.name)) tools.push(b.name);
      }
    }
    if (tools.length > 0) {
      return '📋 工作简报:鲸鱼娘调用了 ' + tools.join('、') + ' 完成了这项任务,细节可以看工作台~';
    }
    return '';
  } catch (e) {
    return '';
  }
}

async function mountPreset(agentCtx) {
  try {
    const presets = agentCtx.get('agentPresets');
    if (presets) await presets.mount(agentCtx, 'whale-girl');
  } catch (e) {
    console.error('[whale-bridge] preset mount failed:', e && e.message ? e.message : String(e));
  }
}

function corsHeaders() {
  return {
    'Content-Type': 'application/json; charset=utf-8',
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET,POST,OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Cache-Control': 'no-store',
  };
}

function sendJson(res, code, obj) {
  try {
    res.writeHead(code, corsHeaders());
    res.end(JSON.stringify(obj));
  } catch (e) {
    /* client gone */
  }
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    req.on('data', (c) => chunks.push(c));
    req.on('end', () => {
      try {
        const raw = Buffer.concat(chunks).toString('utf8');
        resolve(raw ? JSON.parse(raw) : {});
      } catch (e) {
        reject(new Error('bad json body'));
      }
    });
    req.on('error', reject);
  });
}

async function ensureAgent(ctx) {
  if (agent) return agent;
  const agents = ctx.get('agents');
  if (!agents) return null;
  let agentOptions = {};
  try {
    const def = ctx.get('agentDefaultModel');
    if (def) agentOptions = def.currentSelection() || {};
  } catch (e) {
    /* keep defaults */
  }
  try {
    agent = await agents.resume({ resumeSessionId: PET_ID, agentOptions, setup: mountPreset });
    console.log('[whale-bridge] resumed pet session');
  } catch (e) {
    try {
      agent = await agents.create({
        sessionId: PET_ID,
        meta: { cwd: PET_CWD, agentPreset: 'whale-girl' },
        agentOptions,
        setup: mountPreset,
      });
      console.log('[whale-bridge] created pet session');
    } catch (e2) {
      console.error('[whale-bridge] pet agent unavailable:', e2 && e2.message ? e2.message : String(e2));
      return null;
    }
  }
  return agent;
}

async function lastAssistantText(ctx) {
  try {
    const q = ctx.get('sessionQuery');
    if (!q) return '';
    const snap = await q.readSession(PET_ID);
    const events = snap && Array.isArray(snap.events) ? snap.events : [];
    for (let i = events.length - 1; i >= 0; i--) {
      const ev = events[i];
      if (ev && ev.type === 'assistant/message') {
        const data = ev.data || ev.payload || {};
        const msg = data.message || data;
        const blocks = msg && Array.isArray(msg.content) ? msg.content : [];
        const text = blocks
          .filter((b) => b && b.type === 'text' && typeof b.text === 'string')
          .map((b) => b.text)
          .join('')
          .trim();
        if (text) return text;
        return '';
      }
    }
    return '';
  } catch (e) {
    console.error('[whale-bridge] read reply failed:', e && e.message ? e.message : String(e));
    return '';
  }
}

/** 带超时的等待:超时后 resolve(false),并尝试取消代理当前活动。 */
async function waitIdle(ag, ms) {
  let done = false;
  const timer = setTimeout(() => {
    if (!done) {
      try { ag.cancel('timeout'); } catch (e) { /* agent gone */ }
    }
  }, ms);
  try {
    await ag.whenIdle();
    return true;
  } catch (e) {
    return false;
  } finally {
    done = true;
    clearTimeout(timer);
  }
}

/** 监视会话新事件中的工具调用,实时推送进度到聊天室。返回停止函数。 */
function startProgressMonitor(ctx, getStartSeq) {
  let seenSeq = getStartSeq();
  const timer = setInterval(async () => {
    try {
      const q = ctx.get('sessionQuery');
      if (!q) return;
      const s = await q.readSession(PET_ID);
      const evs = s && Array.isArray(s.events) ? s.events : [];
      for (const ev of evs) {
        if (typeof ev.seq !== 'number' || ev.seq <= seenSeq) continue;
        const data = ev.data || ev.payload || {};
        const msg = data.message || data;
        const blocks = msg && Array.isArray(msg.content) ? msg.content : [];
        for (const b of blocks) {
          if (b && b.type === 'tool-call' && b.name) {
            let arg = '';
            try {
              const a = JSON.parse(b.arguments || '{}');
              arg = Object.keys(a).map((k) => k + '=' + String(a[k]).slice(0, 40)).join(', ').slice(0, 120);
            } catch (err) { /* keep raw */ }
            if (proactiveQueue.length < 12) {
              proactiveQueue.push({ text: '🛠️ 鲸鱼娘正在:调用 [' + b.name + ']' + (arg ? ' — ' + arg : ''), at: Date.now() });
            }
          }
        }
      }
      if (evs.length > 0) seenSeq = evs[evs.length - 1].seq;
    } catch (err) { /* transient */ }
  }, 2000);
  return () => clearInterval(timer);
}

async function driveChat(ctx, text) {
  // 审批回复优先处理(即使 busy 也放行)
  const appr = tryResolveApproval(text);
  if (appr) return appr;
  const h = await ensureAgent(ctx);
  if (!h) return { ok: false, error: '鲸鱼娘还在深海里,稍后再试~' };
  if (busy) return { ok: false, error: '鲸鱼娘正在忙别的事呢,稍等一下哦~' };
  busy = true;
  let stopMonitor = null;
  try {
    const ag = h.agent;
    await waitIdle(ag, 15000);
    let startSeq = 0;
    try {
      const q = ctx.get('sessionQuery');
      if (q) {
        const s = await q.readSession(PET_ID);
        const evs = s && Array.isArray(s.events) ? s.events : [];
        startSeq = evs.length > 0 ? evs[evs.length - 1].seq : 0;
      }
    } catch (err) { /* keep 0 */ }
    ag.followup(createUserMessage({ content: [{ type: 'text', text }], source: { kind: 'user' } }));
    stopMonitor = startProgressMonitor(ctx, () => startSeq);
    await waitIdle(ag, 150000);
    lastChatAt = Date.now();
    const reply = await lastAssistantText(ctx);
    if (!reply) return { ok: false, error: '鲸鱼娘走神了,没说出话…再试一次?' };
    // 工作简报:本轮用到的工具
    const report = await buildReport(ctx);
    if (report) proactiveQueue.push({ text: report, at: Date.now() });
    return { ok: true, reply };
  } catch (e) {
    console.error('[whale-bridge] chat failed:', e && e.message ? e.message : String(e));
    return { ok: false, error: '聊天出错啦:' + (e && e.message ? e.message : String(e)) };
  } finally {
    if (stopMonitor) stopMonitor();
    busy = false;
  }
}

async function driveProactive(ctx) {
  if (busy || proactiveQueue.length > 0) return;
  if (Date.now() - lastChatAt < PROACTIVE_MIN_GAP_MS && lastChatAt !== 0) return;
  busy = true;
  try {
    const h = await ensureAgent(ctx);
    if (!h) return;
    const ag = h.agent;
    await waitIdle(ag, 15000);
    ag.followup(createUserMessage({ content: [{ type: 'text', text: PROACTIVE_PROMPT }], source: { kind: 'user' } }));
    await waitIdle(ag, 150000);
    const reply = await lastAssistantText(ctx);
    if (reply && proactiveQueue.length < 5) {
      proactiveQueue.push({ text: reply, at: Date.now() });
      console.log('[whale-bridge] proactive:', reply.slice(0, 60));
    }
  } catch (e) {
    console.error('[whale-bridge] proactive failed:', e && e.message ? e.message : String(e));
  } finally {
    busy = false;
  }
}

function handle(req, res, ctx) {
  const url = (req.url || '/').split('?')[0];
  if (req.method === 'OPTIONS') {
    res.writeHead(204, corsHeaders());
    res.end();
    return;
  }
  if (req.method === 'GET' && url === '/api/whale/status') {
    sendJson(res, 200, { ok: true, server: true, rev: BRIDGE_REV, agentUp: !!agent, busy, proactive: proactiveQueue.length, lastChatAt });
    return;
  }
  if (req.method === 'GET' && url === '/api/whale/poll') {
    const messages = proactiveQueue.splice(0);
    sendJson(res, 200, { ok: true, messages });
    return;
  }
  if (req.method === 'POST' && url === '/api/whale/chat') {
    readBody(req).then(async (body) => {
      const text = typeof body.text === 'string' ? body.text.trim() : '';
      if (!text) return sendJson(res, 400, { ok: false, error: 'empty text' });
      const out = await driveChat(ctx, text);
      sendJson(res, out.ok ? 200 : 500, out);
    }).catch(() => sendJson(res, 400, { ok: false, error: 'bad request' }));
    return;
  }
  if (req.method === 'POST' && url === '/api/whale/act') {
    readBody(req).then(async (body) => {
      if (body && body.kind === 'greet') {
        driveProactive(ctx).then(() => sendJson(res, 200, { ok: true }));
        return;
      }
      sendJson(res, 400, { ok: false, error: 'unknown act' });
    }).catch(() => sendJson(res, 400, { ok: false, error: 'bad request' }));
    return;
  }
  if (req.method === 'POST' && url === '/api/whale/approve') {
    readBody(req).then(async (body) => {
      const allow = !!(body && body.allow);
      const id = body && body.id;
      const pick = id && pendingApprovals.has(id)
        ? id
        : [...pendingApprovals.keys()].pop();
      if (!pick) return sendJson(res, 404, { ok: false, error: '没有待审批的请求' });
      const p = pendingApprovals.get(pick);
      pendingApprovals.delete(pick);
      clearTimeout(p.timer);
      p.resolve(allow ? 'allowed-once' : 'rejected');
      sendJson(res, 200, { ok: true, allowed: allow });
    }).catch(() => sendJson(res, 400, { ok: false, error: 'bad request' }));
    return;
  }
  if (req.method === 'GET' && url === '/api/whale/activity') {
    buildActivity(ctx).then((acts) => sendJson(res, 200, { ok: true, activities: acts }))
      .catch(() => sendJson(res, 200, { ok: true, activities: [] }));
    return;
  }
  sendJson(res, 404, { ok: false, error: 'not found' });
}

/** 工作台活动流:whale-pet 会话最近的用户消息/回复/工具调用摘要。 */
async function buildActivity(ctx) {
  const q = ctx.get('sessionQuery');
  if (!q) return [];
  const snap = await q.readSession(PET_ID);
  const events = snap && Array.isArray(snap.events) ? snap.events : [];
  const out = [];
  const trim = (s, n) => (s && s.length > n ? s.slice(0, n) + '…' : s || '');
  for (const ev of events) {
    let kind = '', text = '';
    const data = ev.data || ev.payload || {};
    const msg = data.message || data;
    const blocks = msg && Array.isArray(msg.content) ? msg.content : [];
    if (ev.type === 'user/message') {
      const toolResults = blocks.filter((b) => b && b.type === 'tool-result');
      if (toolResults.length > 0) {
        kind = 'result';
        const r = toolResults[0];
        const rtext = (r.content || []).filter((b) => b && b.type === 'text').map((b) => b.text).join(' ');
        text = '工具结果(' + String(r.toolCallId || '').slice(0, 8) + '): ' + trim(rtext, 90);
      } else {
        kind = 'user';
        text = trim(blocks.filter((b) => b && b.type === 'text' && typeof b.text === 'string').map((b) => b.text).join(' '), 80);
      }
    } else if (ev.type === 'assistant/message') {
      const texts = blocks.filter((b) => b && b.type === 'text' && typeof b.text === 'string').map((b) => b.text).join(' ');
      const tools = blocks.filter((b) => b && b.type === 'tool-call');
      if (tools.length > 0) {
        kind = 'tool';
        const parts = tools.map((t) => {
          let arg = '';
          try {
            const a = JSON.parse(t.arguments || '{}');
            arg = Object.keys(a).map((k) => k + '=' + trim(String(a[k]), 30)).join(', ');
          } catch (e) { arg = trim(t.arguments || '', 40); }
          return t.name + '(' + arg + ')';
        });
        text = '调用工具: ' + parts.join('; ');
      } else {
        kind = 'assistant';
        text = trim(texts, 90);
      }
    }
    if (kind && text) {
      const d = new Date(ev.time || Date.now());
      const pad = (n) => String(n).padStart(2, '0');
      out.push({ time: pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds()), kind, text });
    }
    if (out.length >= 40) break;
  }
  out.reverse();
  return out;
}

export const inject = ['webServer'];

export function apply(ctx) {
  const webServer = ctx.webServer;
  try {
    registerApprovalBridge(ctx);
  } catch (e) {
    console.error('[whale-bridge] approval bridge failed:', e && e.message ? e.message : String(e));
  }
  try {
    ctx.effect(() => webServer.register({ kind: 'prefix', path: '/api/whale', handler: (req, res) => handle(req, res, ctx) }), 'whale-bridge: routes');
  } catch (e) {
    console.error('[whale-bridge] route registration failed:', e && e.message ? e.message : String(e));
  }
  try {
    const timer = ctx.get('timer');
    if (timer) {
      ctx.effect(() => timer.interval(() => { driveProactive(ctx).catch(() => {}); }, PROACTIVE_INTERVAL_MS), 'whale-bridge: proactive timer');
    }
  } catch (e) {
    console.error('[whale-bridge] timer registration failed:', e && e.message ? e.message : String(e));
  }
}
