const API = "https://xfavdiwdukzgmpiiigac.supabase.co";
const KEY = "sb_publishable_o2dfHvoDseiCVv1kvWDn8w_RDPX_bVK";

// fetch com timeout, para nunca ficar preso
async function fx(url,opt={},ms=12000){
  const c=new AbortController(); const t=setTimeout(()=>c.abort(),ms);
  try{ return await fetch(url,{...opt,signal:c.signal}); }
  finally{ clearTimeout(t); }
}
// mensagem amigável quando a rede é bloqueada (ex: sandbox do preview) ou falha
function net(e){
  if(e&&e.name==='AbortError') return 'O servidor demorou a responder. Se estás a ver isto no preview do Claude, a sandbox bloqueia ligações externas — testa a plataforma alojada num site real.';
  return 'Falha de ligação ao servidor. No preview do Claude as ligações externas são bloqueadas; funciona quando a página está alojada num site real.';
}

// Cliente mínimo que fala direto com a API do Supabase (sem bibliotecas externas)
function tok(){ return localStorage.getItem('fortify_at'); }
function setTok(a,r){ localStorage.setItem('fortify_at',a); if(r) localStorage.setItem('fortify_rt',r); }
function clearTok(){ localStorage.removeItem('fortify_at'); localStorage.removeItem('fortify_rt'); }

function qb(table){
  const s = { table, filters:[], order:null, single:false, method:'GET', body:null, select:'*' };
  const b = {
    select(c){ s.method='GET'; s.select=c||'*'; return b; },
    insert(o){ s.method='POST'; s.body=o; return b; },
    eq(col,val){ s.filters.push(col+'=eq.'+encodeURIComponent(val)); return b; },
    order(col,opt){ s.order=col+'.'+((opt&&opt.ascending===false)?'desc':'asc'); return b; },
    maybeSingle(){ s.single=true; return b; },
    then(res,rej){ return exec(s).then(res,rej); }
  };
  return b;
}
async function exec(s){
  let path='/rest/v1/'+s.table, params=[];
  if(s.method==='GET'){ params.push('select='+(s.select||'*')); s.filters.forEach(f=>params.push(f)); if(s.order) params.push('order='+s.order); }
  if(params.length) path+='?'+params.join('&');
  const headers={ apikey:KEY, 'Content-Type':'application/json', Authorization:'Bearer '+(tok()||KEY) };
  const opt={ method:s.method, headers };
  if(s.method==='POST'){ headers['Prefer']='return=minimal'; opt.body=JSON.stringify(s.body); }
  try{
    const r=await fx(API+path,opt);
    if(!r.ok){ const t=await r.text(); return {data:null,error:{message:t||('HTTP '+r.status)}}; }
    if(s.method==='POST') return {data:null,error:null};
    let data=await r.json();
    if(s.single) data=Array.isArray(data)?(data[0]||null):data;
    return {data,error:null};
  }catch(e){ return {data:null,error:{message:String(e)}}; }
}
const sb = {
  auth:{
    async getSession(){
      try{
        const at=tok(); if(!at) return {data:{session:null}};
        const r=await fx(API+'/auth/v1/user',{headers:{apikey:KEY,Authorization:'Bearer '+at}});
        if(!r.ok){ clearTok(); return {data:{session:null}}; }
        const user=await r.json(); return {data:{session:{user}}};
      }catch(e){ return {data:{session:null}}; }
    },
    async signUp({email,password,options}){
      try{
        const r=await fx(API+'/auth/v1/signup',{method:'POST',headers:{apikey:KEY,'Content-Type':'application/json'},body:JSON.stringify({email,password,data:options&&options.data})});
        const j=await r.json();
        if(!r.ok) return {data:null,error:{message:j.msg||j.error_description||j.error||'Erro ao registar'}};
        if(j.access_token){ setTok(j.access_token,j.refresh_token); return {data:{session:{user:j.user}},error:null}; }
        return {data:{session:null},error:null};
      }catch(e){ return {data:null,error:{message:net(e)}}; }
    },
    async signInWithPassword({email,password}){
      try{
        const r=await fx(API+'/auth/v1/token?grant_type=password',{method:'POST',headers:{apikey:KEY,'Content-Type':'application/json'},body:JSON.stringify({email,password})});
        const j=await r.json();
        if(!r.ok) return {error:{message:j.msg||j.error_description||'Erro ao entrar'}};
        setTok(j.access_token,j.refresh_token); return {error:null};
      }catch(e){ return {error:{message:net(e)}}; }
    },
    async signOut(){ clearTok(); return {}; }
  },
  from: qb,
  functions:{
    async invoke(name,{body}){
      try{
        const r=await fx(API+'/functions/v1/'+name,{method:'POST',headers:{apikey:KEY,'Content-Type':'application/json',Authorization:'Bearer '+(tok()||KEY)},body:JSON.stringify(body)});
        if(!r.ok) return {data:null,error:{message:'HTTP '+r.status}};
        return {data:await r.json(),error:null};
      }catch(e){ return {data:null,error:{message:String(e)}}; }
    }
  }
};

const $ = s => document.querySelector(s);
let draft = {};
let sigPad = null;
let me = null;      // { id, email, name, is_admin }

const token = () => 'fortify-verify-' + Math.random().toString(36).slice(2,10);

/* ---------- SESSION BOOT ---------- */
async function boot(){
  const { data:{ session } } = await sb.auth.getSession();
  if(!session){ me=null; render('auth'); return; }
  const { data:prof } = await sb.from('profiles').select('*').eq('id', session.user.id).maybeSingle();
  me = { id: session.user.id, email: session.user.email, name: prof?.name || '', is_admin: !!prof?.is_admin };
  if(!me.is_admin && prefill()){ draft={ site: prefill() }; clearPrefill(); render('onboard'); return; }
  render(me.is_admin ? 'admin' : 'dash');
}

/* ---------- ROUTING ---------- */
function render(view){
  const app = $('#app');
  if(view==='auth')    return app.innerHTML = viewAuth();
  if(!me)              return app.innerHTML = viewAuth();
  if(view==='dash')    return void viewDash();
  if(view==='admin')   return void viewAdmin();
  if(view==='onboard'){ app.innerHTML = viewOnboard(); return; }
  if(view==='verify'){  app.innerHTML = viewVerify(); return; }
  if(view==='sign'){    app.innerHTML = viewSign(); initSig(); renderContract(); return; }
}
window.go = render;

/* ---------- AUTH ---------- */
function viewAuth(){
  return `<div class="center p2">
    <h1><span class="dot"></span> Fortify</h1>
    <p class="muted p3">${prefill() ? 'Cria a tua conta grátis para analisar <b class="p4">'+esc(prefill().replace(/^https?:\/\//,''))+'</b>.' : 'Cria a tua conta grátis e começa a proteger o teu site.'}</p>
    <div class="panel">
      <div class="tabs">
        <div class="tab on" id="tabReg" data-act="switchAuth" data-arg="reg">Criar conta</div>
        <div class="tab" id="tabLog" data-act="switchAuth" data-arg="log">Entrar</div>
      </div>
      <div id="regForm">
        <div class="field"><label>Nome</label><input id="r_name" placeholder="O teu nome"></div>
        <div class="field"><label>Email</label><input id="r_email" type="email" placeholder="email@empresa.pt"></div>
        <div class="field"><label>Palavra-passe</label><input id="r_pass" type="password" placeholder="mín. 6 caracteres"></div>
        <button class="btn full" id="regBtn" data-act="register">Criar conta grátis</button>
        <p class="note p5">Ao criar conta, aceitas os <a href="termos.html" target="_blank">Termos de Serviço</a> e tomas conhecimento da <a href="privacidade.html" target="_blank">Política de Privacidade</a>.</p>
        <div class="err hidden" id="regErr"></div>
        <div class="ok hidden" id="regOk"></div>
      </div>
      <div id="logForm" class="hidden">
        <div class="field"><label>Email</label><input id="l_email" type="email"></div>
        <div class="field"><label>Palavra-passe</label><input id="l_pass" type="password"></div>
        <button class="btn full" id="logBtn" data-act="login">Entrar</button>
        <div class="err hidden" id="logErr"></div>
      </div>
    </div>
    <p class="note p6">Plataforma segura — as tuas contas e contratos ficam guardados em servidor. · <a href="index.html">← Voltar ao site</a></p>
  </div>`;
}
window.switchAuth = t => {
  $('#tabReg').classList.toggle('on',t==='reg');$('#tabLog').classList.toggle('on',t==='log');
  $('#regForm').classList.toggle('hidden',t!=='reg');$('#logForm').classList.toggle('hidden',t!=='log');
};
window.register = async () => {
  const name=$('#r_name').value.trim(), email=$('#r_email').value.trim().toLowerCase(), pass=$('#r_pass').value;
  hide('regErr'); hide('regOk');
  if(!name||!email||!pass) return showErr('regErr','Preenche todos os campos.');
  const btn=$('#regBtn'); btn.disabled=true; btn.textContent='A criar…';
  const { data, error } = await sb.auth.signUp({ email, password:pass, options:{ data:{ name } } });
  btn.disabled=false; btn.textContent='Criar conta grátis';
  if(error) return showErr('regErr', error.message);
  if(data.session){ await boot(); }
  else { showOk('regOk','✓ Conta criada! Confirma o email na tua caixa de entrada e depois entra.'); switchAuth('log'); }
};
window.login = async () => {
  const email=$('#l_email').value.trim().toLowerCase(), pass=$('#l_pass').value;
  hide('logErr');
  const btn=$('#logBtn'); btn.disabled=true; btn.textContent='A entrar…';
  const { error } = await sb.auth.signInWithPassword({ email, password:pass });
  btn.disabled=false; btn.textContent='Entrar';
  if(error) return showErr('logErr','Email ou palavra-passe errados (ou email por confirmar).');
  await boot();
};
window.logout = async () => { await sb.auth.signOut(); me=null; render('auth'); };
function showErr(id,msg){const e=$('#'+id);e.textContent=msg;e.classList.remove('hidden');}
function showOk(id,msg){const e=$('#'+id);e.textContent=msg;e.classList.remove('hidden');}
function hide(id){$('#'+id)?.classList.add('hidden');}

/* ---------- TOPBAR ---------- */
function topbar(extra=''){
  return `<div class="topbar"><h1><span class="dot"></span> Fortify</h1>
    <nav><span class="muted">${me.name||me.email}</span>${me.is_admin?` <a data-act="go" data-arg="admin">Admin</a> <a data-act="go" data-arg="dash">Cliente</a>`:''}${extra}<a data-act="logout">Sair</a></nav></div>`;
}

/* ---------- CLIENT DASHBOARD ---------- */
async function viewDash(){
  $('#app').innerHTML = topbar() + `<div class="panel"><p class="muted">A carregar contratos…</p></div>`;
  const { data:mine } = await sb.from('contracts').select('*').eq('user_id', me.id).order('created_at',{ascending:false});
  const rows = (mine&&mine.length) ? mine.map(c=>`<tr><td>${c.id.slice(0,8)}</td><td>${esc(c.empresa)}</td><td>${esc(c.site)}</td>
     <td><span class="badge ${c.verified?'ok':'wait'}">${c.verified?'Site verificado':'Não verificado'}</span></td>
     <td>${c.signed?'<span class="badge ok">Assinado</span>':'<span class="badge wait">Pendente</span>'}</td>
     <td>${new Date(c.created_at).toLocaleDateString('pt-PT')}</td></tr>`).join('')
     : `<tr><td colspan="6" class="muted">Ainda não tens contratos. Começa um novo!</td></tr>`;
  $('#app').innerHTML = topbar() + `
   <div class="panel p7">
     <div><h2 class="p8">Os teus contratos</h2><p class="muted">Autoriza a análise de segurança do teu site.</p></div>
     <button class="btn" data-act="startDraft">+ Novo contrato</button>
   </div>
   <div class="panel"><table>
     <tr><th>Ref</th><th>Empresa</th><th>Site</th><th>Verificação</th><th>Assinatura</th><th>Data</th></tr>
     ${rows}
   </table></div>`;
}
window.startDraft = () => { draft={ site: prefill() }; render('onboard'); };
// Domínio vindo da página principal (?site=... ou localStorage)
function prefill(){
  try{
    const q=new URLSearchParams(location.search).get('site');
    const v=q ? 'https://'+q.replace(/^https?:\/\//i,'') : localStorage.getItem('fortify_site_prefill');
    return v||'';
  }catch(e){ return ''; }
}
function clearPrefill(){ try{ localStorage.removeItem('fortify_site_prefill'); history.replaceState(null,'',location.pathname); }catch(e){} }

/* ---------- ONBOARDING ---------- */
function viewOnboard(){
  return topbar() + `
   <div class="steps"><div class="s on"></div><div class="s"></div><div class="s"></div></div>
   <div class="panel">
     <h2 class="p8">1. Dados da empresa</h2>
     <p class="muted p9">Preenche os dados de quem autoriza a auditoria.</p>
     <div class="field"><label>Nome da empresa</label><input id="o_empresa" value="${esc(draft.empresa)}"></div>
     <div class="row">
       <div class="field"><label>NIF</label><input id="o_nif" value="${esc(draft.nif)}"></div>
       <div class="field"><label>Representante (teu nome)</label><input id="o_rep" value="${esc(draft.rep)}"></div>
     </div>
     <div class="field"><label>Website a analisar</label><input id="o_site" placeholder="https://..." value="${esc(draft.site)}"></div>
     <div class="field"><label>Email / Telefone de contacto</label><input id="o_contacto" value="${esc(draft.contacto)}"></div>
     <div class="info">☑️ Ao avançar, declaras que és o dono do site/empresa ou que tens autoridade para autorizar a análise.</div>
     <div class="field"><label><input class="p10" type="checkbox" id="o_decl"> Confirmo que tenho autoridade para autorizar.</label></div>
     <div class="p11">
       <button class="btn sec sm" data-act="go" data-arg="dash">Cancelar</button>
       <button class="btn" data-act="saveOnboard">Continuar →</button>
     </div>
     <div class="err hidden" id="obErr"></div>
   </div>`;
}
window.saveOnboard = () => {
  draft.empresa=$('#o_empresa').value.trim(); draft.nif=$('#o_nif').value.trim();
  draft.rep=$('#o_rep').value.trim(); draft.site=$('#o_site').value.trim(); draft.contacto=$('#o_contacto').value.trim();
  if(!draft.empresa||!draft.site||!draft.rep) return showErr('obErr','Preenche empresa, website e representante.');
  if(!$('#o_decl').checked) return showErr('obErr','Tens de confirmar a autoridade para autorizar.');
  if(!draft.vtoken) draft.vtoken = token();
  render('verify');
};

/* ---------- VERIFICATION ---------- */
function hostOf(site){ try{ return new URL(site.startsWith('http')?site:'https://'+site).hostname; }catch(e){ return 'o-teu-site.pt'; } }
function viewVerify(){
  const host=hostOf(draft.site);
  return topbar() + `
   <div class="steps"><div class="s on"></div><div class="s on"></div><div class="s"></div></div>
   <div class="panel">
     <h2 class="p8">2. Prova que o site é teu</h2>
     <p class="muted p12">Escolhe um método. Isto confirma que controlas <b>${host}</b> — é o que torna a análise legítima. O sistema verifica <b>automaticamente</b>.</p>
     <div class="tabs">
       <div class="tab on" id="mDns" data-act="vmethod" data-arg="dns">Registo DNS</div>
       <div class="tab" id="mFile" data-act="vmethod" data-arg="file">Ficheiro no servidor</div>
     </div>
     <div id="vDns">
       <p class="muted">Copia este código e adiciona-o como registo <b>TXT</b> no DNS de <b>${host}</b>:</p>
       <div class="token" id="tokDns">${draft.vtoken}</div>
       <button class="btn sec sm" data-act="copyTok" data-arg="${draft.vtoken}">📋 Copiar código</button>
       <div class="tut">
         <h3>Como fazer (passo a passo)</h3>
         <ol>
           <li>Entra no <b>site onde registaste o domínio</b> (onde pagas o <b>${host}</b> — ex: GoDaddy, PTisp, Amen, Cloudflare, OVH).</li>
           <li>Procura a secção <b>"DNS"</b>, <b>"Gestão de DNS"</b> ou <b>"Zona DNS"</b>.</li>
           <li>Clica em <b>"Adicionar registo"</b> e preenche:
             <table class="mini">
               <tr><td>Tipo</td><td><b>TXT</b></td></tr>
               <tr><td>Nome / Host</td><td><b>@</b> &nbsp;(ou vazio = domínio principal)</td></tr>
               <tr><td>Valor / Conteúdo</td><td>o código copiado acima</td></tr>
               <tr><td>TTL</td><td>por defeito</td></tr>
             </table>
           </li>
           <li><b>Guarda.</b> Pode demorar de minutos a algumas horas a propagar.</li>
           <li>Volta aqui e clica em <b>"Verificar posse"</b>.</li>
         </ol>
       </div>
     </div>
     <div id="vFile" class="hidden">
       <p class="muted">Cria um ficheiro de texto com este nome e coloca-o na raiz do teu site:</p>
       <div class="token" id="tokFile">${draft.vtoken}.txt</div>
       <button class="btn sec sm" data-act="copyTok" data-arg="${draft.vtoken}.txt">📋 Copiar nome</button>
       <div class="tut">
         <h3>Como fazer (passo a passo)</h3>
         <ol>
           <li>No teu computador, cria um ficheiro chamado <b>${draft.vtoken}.txt</b>.</li>
           <li>Dentro, escreve o código <b>${draft.vtoken}</b> e guarda.</li>
           <li>Acede ao site por <b>gestor de ficheiros</b> (cPanel/Plesk) ou <b>FTP</b> (ex: FileZilla).</li>
           <li>Coloca o ficheiro na <b>pasta raiz</b> (normalmente <code>public_html</code> ou <code>www</code>).</li>
           <li>Confirma no browser: <b>https://${host}/${draft.vtoken}.txt</b></li>
           <li>Volta aqui e clica em <b>"Verificar posse"</b>.</li>
         </ol>
         <p class="note">💡 WordPress/Wix/Shopify sem acesso a ficheiros? Usa o método <b>Registo DNS</b>.</p>
       </div>
     </div>
     <div class="p13">
       <button class="btn sec sm" data-act="go" data-arg="onboard">← Voltar</button>
       <button class="btn" id="verBtn" data-act="doVerify">Verificar posse</button>
     </div>
     <div class="err hidden" id="vErr"></div>
     <div class="ok hidden" id="vOk"></div>
   </div>`;
}
let vMethod='dns';
window.vmethod = m => {
  vMethod=m;
  $('#mDns').classList.toggle('on',m==='dns');$('#mFile').classList.toggle('on',m==='file');
  $('#vDns').classList.toggle('hidden',m!=='dns');$('#vFile').classList.toggle('hidden',m!=='file');
};
window.copyTok = t => { if(navigator.clipboard) navigator.clipboard.writeText(t).catch(()=>{}); };
window.doVerify = async () => {
  hide('vErr'); hide('vOk');
  const btn=$('#verBtn'); btn.disabled=true; btn.innerHTML='<span class="spin"></span> A verificar…';
  const { data, error } = await sb.functions.invoke('verify-domain', { body:{ site:draft.site, token:draft.vtoken, method:vMethod } });
  btn.disabled=false; btn.textContent='Verificar posse';
  if(error){ return showErr('vErr','Erro ao verificar. Tenta novamente daqui a pouco.'); }
  if(data && data.verified){
    draft.verified=true; draft.method=data.method||vMethod;
    showOk('vOk','✓ Posse confirmada! A redirecionar para o contrato…');
    setTimeout(()=>render('sign'),1000);
  } else {
    showErr('vErr','Ainda não encontrámos o código em '+hostOf(draft.site)+'. Confirma que guardaste e (no DNS) aguarda a propagação.');
  }
};

/* ---------- SIGN / CONTRACT ---------- */
function viewSign(){
  return topbar() + `
   <div class="steps"><div class="s on"></div><div class="s on"></div><div class="s on"></div></div>
   <div class="panel">
     <h2 class="p8">3. Rever e assinar</h2>
     <p class="muted p12">Lê o contrato e assina em baixo (com o rato ou o dedo).</p>
     <div id="contractDoc"></div>
     <div class="p14">
       <label>A tua assinatura</label>
       <canvas id="sig"></canvas>
       <div class="p15"><button class="btn sec sm" data-act="clearSig">Limpar assinatura</button></div>
     </div>
     <div class="p16">
       <button class="btn sec sm" data-act="go" data-arg="verify">← Voltar</button>
       <button class="btn" id="subBtn" data-act="submitContract">Assinar e enviar contrato</button>
     </div>
     <div class="err hidden" id="sgErr"></div>
   </div>`;
}
function renderContract(){
  const d=new Date().toLocaleDateString('pt-PT');
  $('#contractDoc').innerHTML = `
    <h4>CONTRATO DE AUTORIZAÇÃO PARA TESTES DE SEGURANÇA</h4>
    <h5>Partes</h5>
    <p><b>Prestador:</b> Fortify — Auditoria de Segurança</p>
    <p><b>Cliente:</b> ${esc(draft.empresa)} — NIF ${esc(draft.nif)||'—'} · Representante: ${esc(draft.rep)} · ${esc(draft.contacto)}</p>
    <h5>Objeto</h5>
    <p>O Cliente autoriza o Prestador a realizar uma análise de segurança ao website <b>${esc(draft.site)}</b>, cuja posse foi verificada (${esc(draft.method||'')}). A análise cobre o OWASP Top 10, configuração e exposição de dados, excluindo ataques de negação de serviço e qualquer ação destrutiva.</p>
    <h5>Confidencialidade e RGPD</h5>
    <p>As vulnerabilidades encontradas são confidenciais e entregues apenas ao Cliente. Ambas as partes cumprem o RGPD. O Cliente pode revogar esta autorização por escrito a qualquer momento.</p>
    <h5>Declaração</h5>
    <p>O Cliente declara ser o legítimo titular ou ter autoridade para autorizar esta análise. Aplica-se a lei portuguesa.</p>
    <p class="p5">Data: ${d} · Posse do site: <b>verificada</b></p>`;
}
function initSig(){
  const c=$('#sig'); const r=c.getBoundingClientRect();
  c.width=r.width*2; c.height=r.height*2; const ctx=c.getContext('2d'); ctx.scale(2,2);
  ctx.lineWidth=2; ctx.lineCap='round'; ctx.strokeStyle='#111';
  let drawing=false, empty=true;
  const pos=e=>{const b=c.getBoundingClientRect();const t=e.touches?e.touches[0]:e;return{x:t.clientX-b.left,y:t.clientY-b.top}};
  const start=e=>{drawing=true;empty=false;const p=pos(e);ctx.beginPath();ctx.moveTo(p.x,p.y);e.preventDefault()};
  const move=e=>{if(!drawing)return;const p=pos(e);ctx.lineTo(p.x,p.y);ctx.stroke();e.preventDefault()};
  const end=()=>{drawing=false};
  c.addEventListener('mousedown',start);c.addEventListener('mousemove',move);window.addEventListener('mouseup',end);
  c.addEventListener('touchstart',start);c.addEventListener('touchmove',move);c.addEventListener('touchend',end);
  sigPad={isEmpty:()=>empty,clear:()=>{ctx.clearRect(0,0,c.width,c.height);empty=true},data:()=>c.toDataURL('image/png')};
}
window.clearSig = () => sigPad&&sigPad.clear();
window.submitContract = async () => {
  hide('sgErr');
  if(!sigPad||sigPad.isEmpty()) return showErr('sgErr','Assina no quadro antes de enviar.');
  const btn=$('#subBtn'); btn.disabled=true; btn.innerHTML='<span class="spin"></span> A enviar…';
  const now=new Date().toISOString();
  const { error } = await sb.from('contracts').insert({
    user_id: me.id, empresa:draft.empresa, nif:draft.nif, representante:draft.rep, site:draft.site, contacto:draft.contacto,
    verification_method:draft.method, verification_token:draft.vtoken, verified:true, verified_at:now,
    signed:true, signature:sigPad.data(), signed_at:now
  });
  btn.disabled=false; btn.textContent='Assinar e enviar contrato';
  if(error) return showErr('sgErr','Erro ao guardar: '+error.message);
  draft={}; alert('✓ Contrato assinado e guardado! Já aparece no teu painel.'); render('dash');
};

/* ---------- ADMIN DASHBOARD ---------- */
async function viewAdmin(){
  $('#app').innerHTML = topbar('<span class="badge ok">ADMIN</span>') + `<div class="panel"><p class="muted">A carregar…</p></div>`;
  const { data:all } = await sb.from('contracts').select('*').order('created_at',{ascending:false});
  const list = all||[];
  const stats={total:list.length, signed:list.filter(c=>c.signed).length, verified:list.filter(c=>c.verified).length};
  const rows = list.length ? list.map(c=>`<tr>
     <td>${c.id.slice(0,8)}</td><td>${esc(c.empresa)}<br><span class="muted p17">${esc(c.representante)}</span></td>
     <td>${esc(c.site)}</td><td>${esc(c.nif)||'—'}</td>
     <td>${c.verified?'<span class="badge ok">✓ '+(esc(c.verification_method)||'')+'</span>':'<span class="badge wait">—</span>'}</td>
     <td>${c.signature?`<img class="p18" src="${c.signature}">`:'—'}</td>
     <td>${new Date(c.created_at).toLocaleString('pt-PT')}</td></tr>`).join('')
     : `<tr><td colspan="7" class="muted">Ainda não há contratos submetidos.</td></tr>`;
  $('#app').innerHTML = topbar('<span class="badge ok">ADMIN</span>') + `
   <div class="panel p19">
     <div><div class="p20">${stats.total}</div><div class="muted">Contratos</div></div>
     <div><div class="p21">${stats.signed}</div><div class="muted">Assinados</div></div>
     <div><div class="p22">${stats.verified}</div><div class="muted">Sites verificados</div></div>
   </div>
   <div class="panel">
     <h2 class="p8">Contratos recebidos</h2>
     <table>
       <tr><th>Ref</th><th>Cliente</th><th>Site</th><th>NIF</th><th>Posse</th><th>Assinatura</th><th>Data</th></tr>
       ${rows}
     </table>
   </div>`;
}

function esc(s){ return (s==null?'':String(s)).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

boot();

// Ações sem JavaScript dentro do HTML (compatível com CSP rigorosa)
document.addEventListener('click', function(e){
  var el = e.target.closest('[data-act]'); if(!el) return;
  var fn = window[el.dataset.act];
  if (typeof fn === 'function') fn(el.dataset.arg);
});
