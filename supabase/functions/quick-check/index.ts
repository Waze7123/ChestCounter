import "jsr:@supabase/functions-js/edge-runtime.d.ts";

// Fortify — check gratuito e PASSIVO.
// Faz um único pedido GET à página inicial, como qualquer visitante, e lê:
// HTTPS, redirecionamento para HTTPS, cabeçalhos de segurança e versões expostas.
// Não envia ataques. Proteções: só hostnames públicos, sem IPs privados, timeout curto.

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};
const json = (b: unknown, status = 200) =>
  new Response(JSON.stringify(b), { status, headers: { ...cors, "Content-Type": "application/json" } });

function cleanHost(input: string): string | null {
  let s = (input || "").trim().toLowerCase();
  s = s.replace(/^https?:\/\//, "").replace(/[\/?#].*$/, "").replace(/:\d+$/, "");
  if (!/^[a-z0-9.-]{1,253}$/.test(s)) return null;
  if (!s.includes(".") || s.startsWith(".") || s.endsWith(".")) return null;
  if (/^\d+\.\d+\.\d+\.\d+$/.test(s)) return null; // sem IPs literais
  if (/(^|\.)(localhost|local|internal|lan|home|corp|intranet)$/.test(s)) return null;
  return s;
}

function isPrivateV4(ip: string): boolean {
  const p = ip.split(".").map(Number);
  if (p.length !== 4 || p.some((n) => isNaN(n))) return true;
  const [a, b] = p;
  return a === 10 || a === 127 || a === 0 || (a === 169 && b === 254) ||
    (a === 172 && b >= 16 && b <= 31) || (a === 192 && b === 168) ||
    (a === 100 && b >= 64 && b <= 127) || a >= 224;
}

async function resolvesPublic(host: string): Promise<boolean> {
  const r = await fetch(`https://dns.google/resolve?name=${encodeURIComponent(host)}&type=A`, { headers: { accept: "application/json" } });
  if (!r.ok) return false;
  const j = await r.json();
  const ips = (j.Answer || []).filter((a: { type: number }) => a.type === 1).map((a: { data: string }) => a.data);
  return ips.length > 0 && ips.every((ip: string) => !isPrivateV4(ip));
}

async function get(url: string): Promise<Response | null> {
  const c = new AbortController();
  const t = setTimeout(() => c.abort(), 8000);
  try {
    return await fetch(url, { redirect: "manual", signal: c.signal, headers: { "user-agent": "Fortify-QuickCheck/1.0 (+passive)" } });
  } catch (_) {
    return null;
  } finally {
    clearTimeout(t);
  }
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (req.method !== "POST") return json({ error: "método não suportado" }, 405);

  let body: { site?: string } = {};
  try { body = await req.json(); } catch (_) { /* vazio */ }
  const host = cleanHost(body.site || "");
  if (!host) return json({ error: "Endereço inválido. Escreve só o domínio, por exemplo loja.pt." }, 400);
  if (!(await resolvesPublic(host))) return json({ error: "Não encontrámos esse site na internet. Confirma o endereço." }, 400);

  const checks: { id: string; label: string; ok: boolean; sev: "alta" | "media" | "baixa"; tip: string }[] = [];

  // 1) HTTPS
  const https = await get(`https://${host}/`);
  checks.push({ id: "https", label: "Ligação segura (HTTPS)", ok: !!https, sev: "alta",
    tip: "Sem HTTPS, os dados dos clientes viajam sem encriptação." });

  // 2) HTTP -> HTTPS
  const http = await get(`http://${host}/`);
  const loc = http?.headers.get("location") || "";
  const redirects = !!http && http.status >= 300 && http.status < 400 && loc.startsWith("https://");
  checks.push({ id: "redirect", label: "Redireciona HTTP para HTTPS", ok: redirects || !http, sev: "media",
    tip: "Quem escreve o endereço sem https fica numa ligação insegura." });

  // Seguir um redirecionamento dentro do mesmo domínio para ler os cabeçalhos finais
  let res = https;
  if (res && res.status >= 300 && res.status < 400) {
    const next = res.headers.get("location") || "";
    try {
      const u = new URL(next, `https://${host}/`);
      if (u.protocol === "https:" && (u.hostname === host || u.hostname === "www." + host || "www." + u.hostname === host)) {
        res = (await get(u.toString())) || res;
      }
    } catch (_) { /* ignora */ }
  }
  const h = res?.headers ?? new Headers();
  const csp = h.get("content-security-policy") || "";

  const header = (id: string, label: string, ok: boolean, sev: "alta" | "media" | "baixa", tip: string) =>
    checks.push({ id, label, ok: !!res && ok, sev, tip });

  header("hsts", "HSTS (força ligação segura)", !!h.get("strict-transport-security"), "media",
    "Impede que o navegador volte a usar uma ligação insegura.");
  header("csp", "Content-Security-Policy", !!csp, "media",
    "Limita o código que pode correr na página e trava ataques XSS.");
  header("frame", "Proteção contra clickjacking", !!h.get("x-frame-options") || /frame-ancestors/i.test(csp), "media",
    "Impede que o teu site seja metido dentro de outro para enganar clientes.");
  header("nosniff", "X-Content-Type-Options", (h.get("x-content-type-options") || "").toLowerCase() === "nosniff", "baixa",
    "Evita que o navegador interprete ficheiros de forma perigosa.");
  header("referrer", "Referrer-Policy", !!h.get("referrer-policy"), "baixa",
    "Controla que informação é enviada a outros sites quando um cliente clica num link.");

  const server = h.get("server") || "";
  const powered = h.get("x-powered-by") || "";
  const leaks = /\d/.test(server) || !!powered;
  header("version", "Não expõe versões do servidor", !leaks, "baixa",
    `Mostrar versões (${[server, powered].filter(Boolean).join(", ") || "—"}) ajuda atacantes a escolher falhas conhecidas.`);

  const weight = { alta: 25, media: 12, baixa: 6 };
  const max = checks.reduce((s, c) => s + weight[c.sev], 0);
  const got = checks.reduce((s, c) => s + (c.ok ? weight[c.sev] : 0), 0);
  const score = Math.round((got / max) * 100);

  return json({ host, score, reachable: !!res, checks, checkedAt: new Date().toISOString() });
});
