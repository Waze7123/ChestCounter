# Fortify — contexto do projeto

Este ficheiro dá contexto ao Claude em qualquer sessão nova. Lê-o todo antes de começar.

## Quem é o dono e como trabalhar com ele
- Dono: Jorge (fala **português de Portugal**; responde sempre em PT-PT, tom direto e simples).
- Não é técnico. Prefere que o Claude **faça as coisas** em vez de lhe dar listas de passos.
- **Não quer mexer no GitHub à mão.** Prefere a **Vercel** à Netlify.
- Gosta de ver resultados: publicar, mostrar o link, mostrar screenshots.

## O negócio
**Fortify** — auditoria de segurança de sites para PME em Portugal, com foco em **lojas online**.
- Hacking **ético**: nunca se testa um site sem **contrato de autorização assinado + posse do domínio verificada**. Isto é inegociável (Lei do Cibercrime 109/2009).
- O **check gratuito** é passivo (um GET à página inicial, lê cabeçalhos públicos) e não precisa de autorização.
- Não usar resultados de checks para pressionar/“vender” a desconhecidos (risco de parecer extorsão). Convidar as pessoas a fazer o check elas próprias.
- **Não inventar** testemunhos, logótipos de clientes nem números. Ainda não há clientes.

### Preços (decididos pelo dono)
- Check-up: **25€** (com garantia: se não houver nada a melhorar, devolve-se o valor)
- Auditoria completa: **60€** por site (inclui novo teste grátis até 30 dias após correções). Foi sugerido subir para 99–149€; decisão do dono.
- Monitorização: **15€/mês**

## Onde está tudo
- **Site no ar:** https://fortifyer.vercel.app (projeto Vercel `fortify`, conta `jorgeleitaoc367-4270`)
- **Código:** GitHub `Waze7123/ChestCounter`, branch `claude/vigilant-bohr-ic324m`
  (o repositório também tem o jogo ChestCounter; o site vive em `site/`)
- **Backend:** Supabase, projeto `xfavdiwdukzgmpiiigac` (região eu-north-1)
  - URL: `https://xfavdiwdukzgmpiiigac.supabase.co`
  - Chave pública (publishable): está em `site/assets/*.js` — é pública por natureza
  - O projeto Supabase é partilhado com outra app: **só mexer nas tabelas `profiles` e `contracts`** e nas funções abaixo.

### Estrutura
```
site/
  index.html              landing (tema escuro estilo Linear, foco lojas online)
  plataforma.html         login/registo, onboarding, verificação, assinatura, admin
  relatorio-exemplo.html  relatório de uma loja fictícia (marcado como exemplo)
  termos.html, privacidade.html
  gerador-contrato.html   gerador antigo (não ligado no site)
  assets/                 site.css/js, plataforma.css/js, relatorio.css/js, legal.css
  vercel.json             cabeçalhos de segurança + CSP rigorosa
supabase/
  schema.sql              tabelas profiles/contracts com RLS
  functions/verify-domain verificação de posse (DNS TXT ou ficheiro) — exige login
  functions/quick-check   check gratuito passivo — público, bloqueia IPs privados/localhost
contratos/autorizacao-testes-seguranca.md   modelo do contrato
plano-negocio-ciberseguranca.md
```

### Regras técnicas importantes
- **CSP rigorosa** (`vercel.json`): `script-src 'self'`, `style-src 'self'` + Google Fonts. Por isso:
  - **nada de `<script>` inline, `onclick=`, nem `style="..."`** no HTML ou em HTML gerado por JS;
  - ações usam `data-act="nomeDaFuncao" data-arg="..."` (há um listener delegado em cada `.js`);
  - estilos novos vão para os `.css` em `assets/`.
- O site deve tirar **100/100 no próprio check gratuito**.
- A plataforma fala com o Supabase via `fetch` direto (sem biblioteca externa).
- Admin: a conta `jorgeleitaoc367@gmail.com` fica admin automaticamente (trigger `handle_new_user`).
- O preview de artefactos do Claude bloqueia ligações externas: login e check só funcionam no site publicado.

### Publicar na Vercel
- Deploy via API `POST https://api.vercel.com/v13/deployments` com `name`/`project: "fortify"`, `target: "production"` e os ficheiros de `site/` (incluir `vercel.json` e `assets/*`).
- Precisa de um token da Vercel. Na sessão da cloud está guardado como segredo do ambiente; numa sessão local é preciso um token próprio (pedir ao dono para o criar em vercel.com/account/tokens — **nunca** pedir para o colar no chat; usar a CLI `vercel` com login do dono é uma alternativa).
- O domínio `fortifyer.vercel.app` está ligado ao projeto.

## Estado e próximos passos
Feito: plano de negócio, landing, plataforma com contas reais, contratos com assinatura, verificação automática de domínio, check gratuito, relatório de exemplo, termos e privacidade (sem dados da empresa ainda), segurança no máximo.

Por fazer:
1. **Kit de auditoria no PC do dono** (Windows): OWASP ZAP, testssl.sh, nmap, e um checklist de auditoria. Depois um **ensaio completo no OWASP Juice Shop** (alvo de treino legal) até ao relatório final.
2. O dono deve **aprender o básico** (PortSwigger Web Security Academy) antes do primeiro cliente pago — é ele quem assina e dá a garantia.
3. Dados legais da empresa (nome/NIPC, morada comercial, email) para termos, privacidade e rodapé — o dono vai falar com um contabilista; não quer mostrar dados pessoais (solução: unipessoal + domiciliação + email profissional).
4. Pagamentos na plataforma (ainda não existe forma de cobrar).
5. Avisos por email quando alguém cria conta ou assina contrato.
6. Secção "Quem faz as auditorias" (precisa da informação do dono).
7. Primeiros clientes: rede próxima, preço de lançamento em troca de testemunho real.
