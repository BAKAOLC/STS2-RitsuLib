namespace STS2RitsuLib.Diagnostics.Logging
{
    internal static class RitsuDebugLogViewerStaticAssets
    {
        public const string IndexHtml =
            """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>RitsuLib Debug Logs</title>
              <style>
                :root {
                  color-scheme: light dark;
                  font-family: "Segoe UI", system-ui, sans-serif;
                  --bg: #101114;
                  --panel: #191b20;
                  --line: #2a2d35;
                  --text: #e8eaf0;
                  --muted: #9ba2b2;
                  --warn: #f0b84a;
                  --error: #f26d6d;
                  --info: #70a7ff;
                  --debug: #86d39b;
                }
                * { box-sizing: border-box; }
                body {
                  margin: 0;
                  background: var(--bg);
                  color: var(--text);
                  font-size: 13px;
                }
                header {
                  display: flex;
                  align-items: center;
                  gap: 12px;
                  height: 48px;
                  padding: 0 14px;
                  border-bottom: 1px solid var(--line);
                  background: #14161a;
                }
                h1 {
                  margin: 0;
                  font-size: 15px;
                  font-weight: 650;
                }
                button, input, select {
                  border: 1px solid var(--line);
                  background: #20232a;
                  color: var(--text);
                  border-radius: 5px;
                  min-height: 30px;
                  padding: 4px 8px;
                }
                button { cursor: pointer; }
                main {
                  display: grid;
                  grid-template-columns: 280px 1fr;
                  min-height: calc(100vh - 48px);
                }
                aside {
                  border-right: 1px solid var(--line);
                  padding: 12px;
                  background: var(--panel);
                  overflow: auto;
                }
                section { min-width: 0; }
                label {
                  display: block;
                  margin: 10px 0 4px;
                  color: var(--muted);
                  font-size: 12px;
                }
                input[type="search"], input[type="text"], select {
                  width: 100%;
                }
                select { min-height: 108px; }
                .row {
                  display: flex;
                  align-items: center;
                  gap: 8px;
                  flex-wrap: wrap;
                }
                .levels label {
                  display: inline-flex;
                  align-items: center;
                  gap: 4px;
                  margin: 4px 8px 4px 0;
                  color: var(--text);
                }
                .stats {
                  margin-top: 12px;
                  color: var(--muted);
                  line-height: 1.7;
                }
                .toolbar {
                  display: flex;
                  align-items: center;
                  gap: 8px;
                  padding: 10px;
                  border-bottom: 1px solid var(--line);
                  background: #14161a;
                }
                #logs {
                  height: calc(100vh - 96px);
                  overflow: auto;
                  font-family: "Cascadia Mono", Consolas, monospace;
                  font-size: 12px;
                }
                .log {
                  display: grid;
                  grid-template-columns: 84px 60px 210px 150px 1fr;
                  gap: 8px;
                  padding: 5px 10px;
                  border-bottom: 1px solid rgba(255,255,255,.05);
                  white-space: pre-wrap;
                  overflow-wrap: anywhere;
                }
                .log:hover { background: rgba(255,255,255,.04); }
                .time, .source, .category { color: var(--muted); }
                .INFO .level { color: var(--info); }
                .WARN .level { color: var(--warn); }
                .ERROR .level { color: var(--error); }
                .DEBUG .level, .VERYDEBUG .level, .LOAD .level { color: var(--debug); }
                details {
                  grid-column: 1 / -1;
                  color: var(--muted);
                }
                pre {
                  margin: 6px 0 0;
                  padding: 8px;
                  border: 1px solid var(--line);
                  border-radius: 5px;
                  overflow: auto;
                  background: #0d0e11;
                }
                @media (max-width: 900px) {
                  main { grid-template-columns: 1fr; }
                  aside { border-right: 0; border-bottom: 1px solid var(--line); }
                  #logs { height: 60vh; }
                  .log { grid-template-columns: 70px 52px 1fr; }
                  .category { display: none; }
                  .message { grid-column: 1 / -1; }
                }
              </style>
            </head>
            <body>
              <header>
                <h1>RitsuLib Debug Logs</h1>
                <span id="connection">connecting</span>
              </header>
              <main>
                <aside>
                  <div class="row">
                    <button id="pause">Pause</button>
                    <button id="clear">Clear View</button>
                  </div>
                  <label>Keyword</label>
                  <input id="keyword" type="search" placeholder="Search message, source, category">
                  <label>Regex</label>
                  <input id="regex" type="text" placeholder="Optional JavaScript regex">
                  <label>Levels</label>
                  <div class="levels" id="levels"></div>
                  <label>Sources</label>
                  <select id="sources" multiple></select>
                  <label>Categories</label>
                  <select id="categories" multiple></select>
                  <label>Hide contains</label>
                  <input id="hide" type="text" placeholder="Comma separated phrases">
                  <div class="stats" id="stats"></div>
                </aside>
                <section>
                  <div class="toolbar">
                    <button id="follow">Follow</button>
                    <button id="export">Export JSONL</button>
                    <span id="visible"></span>
                  </div>
                  <div id="logs"></div>
                </section>
              </main>
              <script>
                const params = new URLSearchParams(location.search);
                const token = params.get("token") || "";
                const levels = ["VERYDEBUG", "LOAD", "DEBUG", "INFO", "WARN", "ERROR"];
                const records = [];
                const state = {
                  paused: false,
                  follow: true,
                  selectedLevels: new Set(["INFO", "WARN", "ERROR"]),
                  selectedSources: new Set(),
                  selectedCategories: new Set()
                };
                const el = id => document.getElementById(id);
                const logs = el("logs");
                const sources = el("sources");
                const categories = el("categories");
                const stats = el("stats");
                const visible = el("visible");

                for (const level of levels) {
                  const label = document.createElement("label");
                  const input = document.createElement("input");
                  input.type = "checkbox";
                  input.checked = state.selectedLevels.has(level);
                  input.addEventListener("change", () => {
                    input.checked ? state.selectedLevels.add(level) : state.selectedLevels.delete(level);
                    render();
                  });
                  label.append(input, level);
                  el("levels").append(label);
                }

                function api(path) {
                  const join = path.includes("?") ? "&" : "?";
                  return `${path}${join}token=${encodeURIComponent(token)}`;
                }

                async function loadHistory() {
                  const res = await fetch(api("/api/history?limit=10000"));
                  const items = await res.json();
                  records.splice(0, records.length, ...items);
                  refreshOptions();
                  render();
                }

                async function loadStatus() {
                  try {
                    const res = await fetch(api("/api/status"));
                    const s = await res.json();
                    stats.innerHTML =
                      `buffer ${s.bufferCount}/${s.bufferCapacity}<br>` +
                      `queue ${s.queueDepth}/${s.queueCapacity}<br>` +
                      `dropped ${s.dropped}`;
                  } catch {
                    stats.textContent = "status unavailable";
                  }
                }

                function connect() {
                  const es = new EventSource(api("/api/events"));
                  es.onopen = () => el("connection").textContent = "connected";
                  es.onerror = () => el("connection").textContent = "reconnecting";
                  es.addEventListener("log", event => {
                    const record = JSON.parse(event.data);
                    records.push(record);
                    if (records.length > 20000) records.splice(0, records.length - 20000);
                    refreshOptions();
                    if (!state.paused) render();
                  });
                }

                function refreshOptions() {
                  const selectedSources = new Set([...sources.selectedOptions].map(o => o.value));
                  const selectedCategories = new Set([...categories.selectedOptions].map(o => o.value));
                  fillSelect(sources, [...new Set(records.map(r => r.source).filter(Boolean))].sort(), selectedSources);
                  fillSelect(categories, [...new Set(records.map(r => r.category).filter(Boolean))].sort(), selectedCategories);
                  state.selectedSources = new Set([...sources.selectedOptions].map(o => o.value));
                  state.selectedCategories = new Set([...categories.selectedOptions].map(o => o.value));
                }

                function fillSelect(select, values, selected) {
                  select.textContent = "";
                  for (const value of values) {
                    const option = document.createElement("option");
                    option.value = value;
                    option.textContent = value;
                    option.selected = selected.has(value);
                    select.append(option);
                  }
                }

                function selected(select) {
                  return new Set([...select.selectedOptions].map(o => o.value));
                }

                function filtered() {
                  const keyword = el("keyword").value.trim().toLowerCase();
                  const hide = el("hide").value.split(",").map(x => x.trim().toLowerCase()).filter(Boolean);
                  const sourceSet = selected(sources);
                  const categorySet = selected(categories);
                  let regex = null;
                  try {
                    const pattern = el("regex").value.trim();
                    if (pattern) regex = new RegExp(pattern, "i");
                  } catch {
                    regex = null;
                  }

                  return records.filter(r => {
                    const hay = `${r.body || ""} ${r.source || ""} ${r.category || ""}`;
                    const lower = hay.toLowerCase();
                    if (!state.selectedLevels.has(r.severityText)) return false;
                    if (sourceSet.size && !sourceSet.has(r.source || "")) return false;
                    if (categorySet.size && !categorySet.has(r.category || "")) return false;
                    if (keyword && !lower.includes(keyword)) return false;
                    if (regex && !regex.test(hay)) return false;
                    if (hide.some(x => lower.includes(x))) return false;
                    return true;
                  });
                }

                function render() {
                  const items = filtered();
                  visible.textContent = `${items.length} visible / ${records.length} total`;
                  const tail = items.slice(-700);
                  const atBottom = logs.scrollTop + logs.clientHeight >= logs.scrollHeight - 8;
                  logs.textContent = "";
                  for (const r of tail) {
                    const row = document.createElement("div");
                    row.className = `log ${r.severityText}`;
                    row.innerHTML =
                      `<span class="time">${new Date(r.timestamp).toLocaleTimeString()}</span>` +
                      `<strong class="level">${r.severityText}</strong>` +
                      `<span class="source"></span>` +
                      `<span class="category"></span>` +
                      `<span class="message"></span>`;
                    row.querySelector(".source").textContent = r.source || "";
                    row.querySelector(".category").textContent = r.category || "";
                    row.querySelector(".message").textContent = r.body || "";
                    const details = document.createElement("details");
                    const summary = document.createElement("summary");
                    summary.textContent = "event";
                    const pre = document.createElement("pre");
                    pre.textContent = JSON.stringify(r, null, 2);
                    details.append(summary, pre);
                    row.append(details);
                    logs.append(row);
                  }
                  if (state.follow && atBottom) logs.scrollTop = logs.scrollHeight;
                }

                el("pause").onclick = () => {
                  state.paused = !state.paused;
                  el("pause").textContent = state.paused ? "Resume" : "Pause";
                  if (!state.paused) render();
                };
                el("follow").onclick = () => {
                  state.follow = !state.follow;
                  el("follow").textContent = state.follow ? "Follow" : "Unfollow";
                  if (state.follow) logs.scrollTop = logs.scrollHeight;
                };
                el("clear").onclick = () => { records.splice(0, records.length); render(); };
                el("export").onclick = () => {
                  const data = filtered().map(r => JSON.stringify(r)).join("\n");
                  const blob = new Blob([data], { type: "application/x-ndjson" });
                  const a = document.createElement("a");
                  a.href = URL.createObjectURL(blob);
                  a.download = "ritsulib-debug-logs.jsonl";
                  a.click();
                  URL.revokeObjectURL(a.href);
                };
                for (const id of ["keyword", "regex", "hide"]) el(id).addEventListener("input", render);
                sources.addEventListener("change", render);
                categories.addEventListener("change", render);
                setInterval(loadStatus, 2000);
                loadHistory().then(connect).then(loadStatus);
              </script>
            </body>
            </html>
            """;
    }
}
