import "jsr:@supabase/functions-js/edge-runtime.d.ts";

// Fortify — verificação automática de posse de domínio.
// Verifica se o código (token) existe num registo DNS TXT ou num ficheiro na raiz do site.

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

function hostOf(site: string): string {
  let s = (site || "").trim();
  if (!/^https?:\/\//i.test(s)) s = "https://" + s;
  return new URL(s).hostname;
}

async function checkDns(host: string, token: string): Promise<boolean> {
  const url = `https://dns.google/resolve?name=${encodeURIComponent(host)}&type=TXT`;
  const r = await fetch(url, { headers: { accept: "application/json" } });
  if (!r.ok) return false;
  const j = await r.json();
  const answers = Array.isArray(j.Answer) ? j.Answer : [];
  for (const a of answers) {
    const data = String(a.data || "").replace(/"/g, "");
    if (data.includes(token)) return true;
  }
  return false;
}

async function checkFile(host: string, token: string): Promise<boolean> {
  const url = `https://${host}/${token}.txt`;
  const ctrl = new AbortController();
  const t = setTimeout(() => ctrl.abort(), 8000);
  try {
    const r = await fetch(url, { signal: ctrl.signal, redirect: "follow" });
    clearTimeout(t);
    if (!r.ok) return false;
    const body = (await r.text()).trim();
    return body.includes(token);
  } catch (_e) {
    clearTimeout(t);
    return false;
  }
}

Deno.serve(async (req: Request) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  try {
    const { site, token, method } = await req.json();
    if (!site || !token) {
      return new Response(JSON.stringify({ verified: false, error: "site e token obrigatórios" }), { status: 400, headers: { ...cors, "Content-Type": "application/json" } });
    }
    const host = hostOf(site);
    let verified = false;
    let used = method;
    if (method === "dns") {
      verified = await checkDns(host, token);
    } else if (method === "file") {
      verified = await checkFile(host, token);
    } else {
      verified = await checkDns(host, token);
      used = verified ? "dns" : "file";
      if (!verified) verified = await checkFile(host, token);
    }
    return new Response(JSON.stringify({ verified, host, method: used }), { headers: { ...cors, "Content-Type": "application/json" } });
  } catch (e) {
    return new Response(JSON.stringify({ verified: false, error: String(e) }), { status: 500, headers: { ...cors, "Content-Type": "application/json" } });
  }
});
