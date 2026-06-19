/*
 * MIMO WebChat — widget embebible (Fase E).
 * Sin paso de build. Se incrusta con:
 *   <script src="https://API/webchat/widget.js" data-tenant="SLUG"></script>
 * data-tenant (req): slug del tenant.  data-api (opc): base de la API (por defecto, el origen del script).
 *
 * Corte 1: conversación con el bot.  Corte 2: funcionario en vivo (SignalR), con respaldo REST, y
 * encuesta de satisfacción al cerrar.
 */
(function () {
  "use strict";

  var script = document.currentScript;
  var tenant = script && script.getAttribute("data-tenant");
  if (!tenant) { console.error("[MIMO WebChat] falta data-tenant en el <script>."); return; }
  var api = (script.getAttribute("data-api") || new URL(script.src).origin).replace(/\/+$/, "");
  var SIGNALR_CDN = "https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.7/dist/browser/signalr.min.js";

  // Roles de mensaje (deben coincidir con Mimo.Core.Enums.MessageRole).
  var ROLE_USER = 0, ROLE_ASSISTANT = 1, ROLE_AGENT = 3;

  var uidKey = "mimo_wc_uid_" + tenant;
  var externalUserId = localStorage.getItem(uidKey);
  if (!externalUserId) {
    externalUserId = (crypto.randomUUID ? crypto.randomUUID() : "u-" + Date.now() + "-" + Math.random().toString(16).slice(2));
    localStorage.setItem(uidKey, externalUserId);
  }

  var state = {
    open: false, started: false, sending: false, conversationId: null,
    primary: "#2563eb", name: "Chat",
    hub: null, useHub: false, agentActive: false, closed: false, surveyShown: false
  };

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
    ".mimo-wc-note{align-self:center;color:#6b7280;font-size:12px;font-style:italic;text-align:center}" +
    ".mimo-wc-foot{display:flex;border-top:1px solid #eee;padding:8px;gap:8px;background:#fff}" +
    ".mimo-wc-foot input{flex:1;border:1px solid #d1d5db;border-radius:10px;padding:9px 12px;font-size:14px;outline:none}" +
    ".mimo-wc-foot button{border:0;border-radius:10px;color:#fff;padding:0 16px;cursor:pointer;font-size:14px}" +
    ".mimo-wc-foot button:disabled{opacity:.5;cursor:default}" +
    ".mimo-wc-survey{align-self:stretch;background:#fff;border:1px solid #e5e7eb;border-radius:12px;padding:12px;display:flex;flex-direction:column;gap:8px}" +
    ".mimo-wc-stars{display:flex;gap:6px;justify-content:center}" +
    ".mimo-wc-star{cursor:pointer;font-size:24px;color:#d1d5db;background:none;border:0;line-height:1}" +
    ".mimo-wc-star.on{color:#f5b301}" +
    ".mimo-wc-survey textarea{border:1px solid #d1d5db;border-radius:8px;padding:6px 8px;font-size:13px;resize:vertical;min-height:48px;font-family:inherit}" +
    ".mimo-wc-survey button.send{border:0;border-radius:8px;color:#fff;padding:8px;cursor:pointer;font-size:14px}" +
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
      connectHub(); // tiempo real (con respaldo REST si falla)
    }).catch(function () {
      addNote("No se pudo iniciar el chat. Intenta de nuevo más tarde.");
    });
  }

  // Conexión SignalR para recibir mensajes del funcionario y cambios de estado en vivo.
  function connectHub() {
    loadSignalR().then(function () {
      var url = api + "/hubs/chat?tenant_slug=" + encodeURIComponent(tenant);
      var conn = new signalR.HubConnectionBuilder()
        .withUrl(url, { withCredentials: false })  // CORS abierto, sin credenciales
        .withAutomaticReconnect()
        .build();

      conn.on("MessageReceived", function (m) {
        if (!m || m.role === ROLE_USER) return;       // ignora el eco de nuestro propio mensaje
        clearTyping();
        addBubble(m.content, m.role === ROLE_AGENT ? "agent" : "bot");
      });
      conn.on("AgentJoined", function (alias) {
        state.agentActive = true; clearTyping();
        addNote((alias || "Un funcionario") + " se unió a la conversación.");
      });
      conn.on("StatusChanged", onStatusChanged);
      conn.on("Error", function (e) { clearTyping(); addNote(typeof e === "string" ? e : "Ocurrió un error."); });

      conn.start()
        .then(function () { return conn.invoke("JoinConversation", state.conversationId); })
        .then(function () { state.hub = conn; state.useHub = true; })
        .catch(function () { state.useHub = false; });
    }).catch(function () { state.useHub = false; }); // sin SignalR → respaldo REST
  }

  function send() {
    var text = input.value.trim();
    if (!text || state.sending || state.closed || !state.conversationId) return;
    addBubble(text, "me");
    input.value = "";

    if (state.useHub && state.hub) {
      if (!state.agentActive) addTyping();             // el bot responderá; muestra "escribiendo…"
      state.hub.invoke("SendMessage", state.conversationId, text).catch(function () {
        clearTyping(); addNote("No se pudo enviar el mensaje.");
      });
      return;
    }

    // Respaldo REST (sin SignalR): el bot responde en la misma petición.
    state.sending = true; sendBtn.disabled = true; input.disabled = true;
    addTyping();
    fetch(api + "/conversations/messages?id=" + encodeURIComponent(state.conversationId), {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-Tenant-Slug": tenant },
      body: JSON.stringify({ content: text })
    }).then(function (r) {
      clearTyping();
      if (r.status === 400) { addNote("Un agente continuará la conversación en breve."); return null; }
      return ok(r);
    }).then(function (msg) {
      if (msg && msg.content) addBubble(msg.content, "bot");
    }).catch(function () {
      clearTyping(); addNote("No se pudo enviar el mensaje.");
    }).finally(function () {
      state.sending = false; sendBtn.disabled = false; input.disabled = false; input.focus();
    });
  }

  function onStatusChanged(status) {
    clearTyping();
    if (status === "Resolved" || status === "Closed") {
      state.closed = true;
      input.disabled = true; sendBtn.disabled = true;
      addNote("La conversación se cerró.");
      showSurvey();
    } else {
      addNote("Estado: " + status);
    }
  }

  // ── Encuesta de satisfacción ───────────────────────────────────────────────────
  function showSurvey() {
    if (state.surveyShown) return;
    state.surveyShown = true;
    var card = el("div", "mimo-wc-survey");
    card.appendChild(el("div", null, "¿Cómo calificarías la atención?"));
    var stars = el("div", "mimo-wc-stars");
    var rating = 0;
    var starEls = [];
    for (var i = 1; i <= 5; i++) {
      (function (n) {
        var s = el("button", "mimo-wc-star", "★");
        s.onclick = function () { rating = n; starEls.forEach(function (e, idx) { e.classList.toggle("on", idx < n); }); };
        starEls.push(s); stars.appendChild(s);
      })(i);
    }
    card.appendChild(stars);
    var obs = el("textarea"); obs.placeholder = "Comentario (opcional)";
    card.appendChild(obs);
    var sub = el("button", "send", "Enviar valoración"); sub.style.background = state.primary;
    sub.onclick = function () {
      if (rating < 1) { return; }
      sub.disabled = true;
      fetch(api + "/conversations/survey?conversationId=" + encodeURIComponent(state.conversationId), {
        method: "POST",
        headers: { "Content-Type": "application/json", "X-Tenant-Slug": tenant },
        body: JSON.stringify({ rating: rating, observations: obs.value || null })
      }).then(function (r) {
        card.remove();
        addNote(r.ok ? "¡Gracias por tu valoración!" : "No se pudo registrar la valoración.");
      }).catch(function () { sub.disabled = false; });
    };
    card.appendChild(sub);
    msgs.appendChild(card); scroll();
  }

  // ── Helpers ────────────────────────────────────────────────────────────────────
  function loadSignalR() {
    return new Promise(function (resolve, reject) {
      if (window.signalR) return resolve();
      var s = document.createElement("script");
      s.src = SIGNALR_CDN; s.async = true;
      s.onload = function () { window.signalR ? resolve() : reject(); };
      s.onerror = reject;
      document.head.appendChild(s);
    });
  }
  function addBubble(text, kind) {
    var cls = kind === "me" ? "mimo-wc-me" : "mimo-wc-bot";
    var b = el("div", "mimo-wc-b " + cls, text);
    if (kind === "me") b.style.background = state.primary;
    msgs.appendChild(b); scroll(); return b;
  }
  function addNote(text) { var n = el("div", "mimo-wc-note", text); msgs.appendChild(n); scroll(); return n; }
  var typingEl = null;
  function addTyping() {
    if (typingEl) return;
    typingEl = el("div", "mimo-wc-b mimo-wc-bot");
    typingEl.innerHTML = '<span class="mimo-wc-dots"><span>•</span><span>•</span><span>•</span></span>';
    msgs.appendChild(typingEl); scroll();
  }
  function clearTyping() { if (typingEl) { typingEl.remove(); typingEl = null; } }
  function scroll() { msgs.scrollTop = msgs.scrollHeight; }
  function ok(r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); }
  function el(tag, cls, text) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = text;
    return e;
  }
})();
