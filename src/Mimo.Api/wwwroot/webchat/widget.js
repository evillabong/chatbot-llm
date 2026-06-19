/*
 * MIMO WebChat — widget embebible (Fase E, corte 1).
 * Sin dependencias ni paso de build. Se incrusta con:
 *   <script src="https://API/webchat/widget.js" data-tenant="SLUG"></script>
 * data-tenant (req): slug del tenant.  data-api (opc): base de la API (por defecto, el origen del script).
 *
 * Corte 1: conversación con el bot por REST (start + mensajes). El agente en vivo (SignalR),
 * la cola y la encuesta llegan en el corte 2.
 */
(function () {
  "use strict";

  var script = document.currentScript;
  var tenant = script && script.getAttribute("data-tenant");
  if (!tenant) { console.error("[MIMO WebChat] falta data-tenant en el <script>."); return; }
  var api = (script.getAttribute("data-api") || new URL(script.src).origin).replace(/\/+$/, "");

  // Id estable por navegador para reanudar la conversación del mismo usuario.
  var uidKey = "mimo_wc_uid_" + tenant;
  var externalUserId = localStorage.getItem(uidKey);
  if (!externalUserId) {
    externalUserId = (crypto.randomUUID ? crypto.randomUUID() : "u-" + Date.now() + "-" + Math.random().toString(16).slice(2));
    localStorage.setItem(uidKey, externalUserId);
  }

  var state = { open: false, started: false, sending: false, conversationId: null, primary: "#2563eb", name: "Chat" };

  // ── Estilos (namespaced) ────────────────────────────────────────────────────
  var css =
    ".mimo-wc-btn{position:fixed;bottom:20px;right:20px;width:56px;height:56px;border-radius:50%;border:0;cursor:pointer;box-shadow:0 4px 16px rgba(0,0,0,.2);color:#fff;font-size:24px;z-index:2147483000}" +
    ".mimo-wc-panel{position:fixed;bottom:88px;right:20px;width:360px;max-width:calc(100vw - 40px);height:520px;max-height:calc(100vh - 120px);background:#fff;border-radius:14px;box-shadow:0 12px 40px rgba(0,0,0,.25);display:none;flex-direction:column;overflow:hidden;z-index:2147483000;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif}" +
    ".mimo-wc-panel.open{display:flex}" +
    ".mimo-wc-head{color:#fff;padding:14px 16px;font-weight:600;display:flex;align-items:center;justify-content:space-between}" +
    ".mimo-wc-head button{background:transparent;border:0;color:#fff;font-size:20px;cursor:pointer;line-height:1}" +
    ".mimo-wc-msgs{flex:1;overflow-y:auto;padding:14px;background:#f7f7f9;display:flex;flex-direction:column;gap:8px}" +
    ".mimo-wc-b{max-width:80%;padding:9px 12px;border-radius:14px;font-size:14px;line-height:1.4;white-space:pre-wrap;word-wrap:break-word}" +
    ".mimo-wc-bot{align-self:flex-start;background:#fff;border:1px solid #e5e7eb;color:#111;border-bottom-left-radius:4px}" +
    ".mimo-wc-me{align-self:flex-end;color:#fff;border-bottom-right-radius:4px}" +
    ".mimo-wc-note{align-self:center;color:#6b7280;font-size:12px;font-style:italic}" +
    ".mimo-wc-foot{display:flex;border-top:1px solid #eee;padding:8px;gap:8px;background:#fff}" +
    ".mimo-wc-foot input{flex:1;border:1px solid #d1d5db;border-radius:10px;padding:9px 12px;font-size:14px;outline:none}" +
    ".mimo-wc-foot button{border:0;border-radius:10px;color:#fff;padding:0 16px;cursor:pointer;font-size:14px}" +
    ".mimo-wc-foot button:disabled{opacity:.5;cursor:default}" +
    ".mimo-wc-dots{display:inline-block}.mimo-wc-dots span{animation:mimo-wc-bl 1s infinite}.mimo-wc-dots span:nth-child(2){animation-delay:.2s}.mimo-wc-dots span:nth-child(3){animation-delay:.4s}" +
    "@keyframes mimo-wc-bl{0%,80%,100%{opacity:.2}40%{opacity:1}}";
  var styleEl = document.createElement("style");
  styleEl.textContent = css;
  document.head.appendChild(styleEl);

  // ── DOM ──────────────────────────────────────────────────────────────────────
  var btn = el("button", "mimo-wc-btn", "💬");
  var panel = el("div", "mimo-wc-panel");
  var head = el("div", "mimo-wc-head");
  var title = el("span", null, "Chat");
  var closeBtn = el("button", null, "×");
  head.appendChild(title); head.appendChild(closeBtn);
  var msgs = el("div", "mimo-wc-msgs");
  var foot = el("div", "mimo-wc-foot");
  var input = el("input"); input.type = "text"; input.placeholder = "Escribe un mensaje…"; input.disabled = true;
  var sendBtn = el("button", null, "Enviar"); sendBtn.disabled = true;
  foot.appendChild(input); foot.appendChild(sendBtn);
  panel.appendChild(head); panel.appendChild(msgs); panel.appendChild(foot);
  document.body.appendChild(btn); document.body.appendChild(panel);

  btn.onclick = toggle;
  closeBtn.onclick = toggle;
  sendBtn.onclick = send;
  input.addEventListener("keydown", function (e) { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); send(); } });

  // ── Lógica ────────────────────────────────────────────────────────────────────
  function toggle() {
    state.open = !state.open;
    panel.classList.toggle("open", state.open);
    if (state.open && !state.started) start();
  }

  function applyTheme() {
    btn.style.background = state.primary;
    head.style.background = state.primary;
    sendBtn.style.background = state.primary;
    title.textContent = state.name;
    Array.prototype.forEach.call(msgs.querySelectorAll(".mimo-wc-me"), function (b) { b.style.background = state.primary; });
  }

  function start() {
    state.started = true;
    fetch(api + "/webchat/config", { headers: { "X-Tenant-Slug": tenant } })
      .then(ok).then(function (cfg) {
        state.name = cfg.name || "Chat";
        if (cfg.primaryColor) state.primary = cfg.primaryColor;
        applyTheme();
        if (cfg.welcomeMessage) addBubble(cfg.welcomeMessage, "bot");
      }).catch(function () { applyTheme(); });

    fetch(api + "/conversations", {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-Tenant-Slug": tenant },
      body: JSON.stringify({ externalUserId: externalUserId, channel: 0 })
    }).then(ok).then(function (conv) {
      state.conversationId = conv.id;
      input.disabled = false; sendBtn.disabled = false; input.focus();
    }).catch(function () {
      addNote("No se pudo iniciar el chat. Intenta de nuevo más tarde.");
    });
  }

  function send() {
    var text = input.value.trim();
    if (!text || state.sending || !state.conversationId) return;
    addBubble(text, "me");
    input.value = "";
    state.sending = true; sendBtn.disabled = true; input.disabled = true;
    var typing = addTyping();

    fetch(api + "/conversations/messages?id=" + encodeURIComponent(state.conversationId), {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-Tenant-Slug": tenant },
      body: JSON.stringify({ content: text })
    }).then(function (r) {
      typing.remove();
      if (r.status === 400) { addNote("Un agente continuará la conversación en breve."); return null; }
      return ok(r);
    }).then(function (msg) {
      if (msg && msg.content) addBubble(msg.content, "bot");
    }).catch(function () {
      typing.remove();
      addNote("No se pudo enviar el mensaje.");
    }).finally(function () {
      state.sending = false; sendBtn.disabled = false; input.disabled = false; input.focus();
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────────
  function addBubble(text, kind) {
    var b = el("div", "mimo-wc-b " + (kind === "me" ? "mimo-wc-me" : "mimo-wc-bot"), text);
    if (kind === "me") b.style.background = state.primary;
    msgs.appendChild(b); scroll(); return b;
  }
  function addNote(text) { var n = el("div", "mimo-wc-note", text); msgs.appendChild(n); scroll(); return n; }
  function addTyping() {
    var b = el("div", "mimo-wc-b mimo-wc-bot");
    b.innerHTML = '<span class="mimo-wc-dots"><span>•</span><span>•</span><span>•</span></span>';
    msgs.appendChild(b); scroll(); return b;
  }
  function scroll() { msgs.scrollTop = msgs.scrollHeight; }
  function ok(r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); }
  function el(tag, cls, text) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = text;
    return e;
  }
})();
