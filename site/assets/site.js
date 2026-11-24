  // Check gratuito: chama a função pública quick-check (só leitura passiva).
  var API="https://xfavdiwdukzgmpiiigac.supabase.co", KEY="sb_publishable_o2dfHvoDseiCVv1kvWDn8w_RDPX_bVK";
  function esc(t){return String(t).replace(/[&<>"]/g,function(c){return {"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c];});}
  function focusCheck(){ setTimeout(function(){ var d=document.getElementById('dom'); d&&d.focus(); },350); }
  function cleanDom(v){ return (v||'').trim().replace(/^https?:\/\//i,'').replace(/[\/?#].*$/,'').toLowerCase(); }
  function goFull(host){ try{localStorage.setItem('fortify_site_prefill','https://'+host);}catch(_){} location.href='plataforma.html?site='+encodeURIComponent(host); }
  async function quickCheck(e){
    e.preventDefault();
    var host=cleanDom(document.getElementById('dom').value); if(!host) return false;
    var box=document.getElementById('qres'), btn=document.getElementById('qbtn'), label=btn.innerHTML;
    btn.disabled=true; btn.innerHTML='<span class="spin"></span> A verificar…';
    box.hidden=false; box.innerHTML='<p class="qerr">A ler '+esc(host)+' como um visitante normal…</p>';
    try{
      var c=new AbortController(), t=setTimeout(function(){c.abort();},25000);
      var r=await fetch(API+'/functions/v1/quick-check',{method:'POST',signal:c.signal,headers:{'content-type':'application/json',apikey:KEY},body:JSON.stringify({site:host})});
      clearTimeout(t);
      var j=await r.json();
      if(!r.ok) throw new Error(j.error||'erro');
      var bad=j.checks.filter(function(x){return !x.ok;}).length;
      var tone=j.score>=80?'q-good':j.score>=50?'q-mid':'q-bad';
      box.innerHTML='<div class="qtop"><div class="qscore '+tone+'">'+j.score+'<small> / 100</small></div><p><b>'+esc(j.host)+'</b><br>'+(bad? bad+(bad>1?' pontos a melhorar':' ponto a melhorar')+' só no que se vê de fora.':'Tudo certo no que se vê de fora. O check grátis não testa formulários, logins nem a loja por dentro.')+'</p></div>'
        +'<ul class="qlist">'+j.checks.map(function(x){return '<li><span class="m '+(x.ok?'y':'n')+'">'+(x.ok?'✓':'✕')+'</span><span><b>'+esc(x.label)+'</b>'+(x.ok?'':esc(x.tip))+'</span></li>';}).join('')+'</ul>'
        +'<div class="qcta"><button class="btn btn-p" data-act="goFull" data-arg="'+esc(j.host)+'">Receber relatório completo — 25€</button><a class="btn btn-g" href="relatorio-exemplo.html">Ver um exemplo</a><small>O relatório completo testa também formulários, logins e ficheiros expostos, com a correção para cada falha. Garantia: se não houver nada a melhorar, devolvemos os 25€.</small></div>';
    }catch(err){
      var net=!(err&&err.message&&err.name!=='AbortError'&&err.message!=='Failed to fetch');
      var preview=/claude|claudeusercontent/i.test(location.hostname)||location.protocol==='file:';
      var msg=!net?err.message:(preview?'O check não corre nesta pré-visualização: ela bloqueia ligações a servidores externos. No site publicado funciona normalmente.':'Não conseguimos ligar ao servidor de análise. Tenta de novo daqui a um minuto.');
      box.innerHTML='<p class="qerr">'+esc(msg)+'</p><div class="qcta"><button class="btn btn-p" data-act="goFull" data-arg="'+esc(host)+'">Continuar para o relatório completo</button></div>';
    }finally{ btn.disabled=false; btn.innerHTML=label; }
    return false;
  }
  // Barra fixa no telemóvel: aparece depois do topo, esconde-se perto do fim.
  (function(){
    var bar=document.getElementById('mbar'); if(!bar) return;
    var hero=document.querySelector('.scan'), end=document.querySelector('.end');
    function upd(){
      var past=hero.getBoundingClientRect().bottom<0;
      var near=end.getBoundingClientRect().top<window.innerHeight;
      bar.classList.toggle('on',past&&!near);
    }
    addEventListener('scroll',upd,{passive:true}); upd();
  })();

// Ações sem JavaScript dentro do HTML (compatível com CSP rigorosa)
document.addEventListener('click', function(e){
  var el = e.target.closest('[data-act]'); if(!el) return;
  var fn = window[el.dataset.act];
  if (typeof fn === 'function') fn(el.dataset.arg);
});
document.getElementById('check').addEventListener('submit', quickCheck);
