<img src="src/ContadorBaus/Assets/icone.png" alt="ChestCounter" width="96">

# ChestCounter — Contador de Baús (Fortnite)

[English](README.md) · Português

Conta todos os baús que abres no Fortnite, lendo os replays que o jogo grava de cada partida.

Disponível em **português, English, español, français e Deutsch** (Definições → Idioma). O nome muda com a língua — Contador de Baús, Chest Counter, Contador de Cofres, Compteur de Coffres, Truhenzähler — e o nome base é **ChestCounter**.

Cinco abas, em preto e dourado:

- **Painel**: o total de baús a partir da data escolhida, o registo de hoje, partidas, média e melhor partida, o **ritmo da última partida** (uma marca dourada por baú, no minuto em que o abriste) e os baús por dia.
- **Partidas**: cada partida com data, modo, lugar, baús e o seu ritmo. Ao escolher uma vês cada abertura com o minuto, o tipo (normal, raro, enterrado, de relíquia…) e **quantos baús a equipa abriu** nessa partida (tu + colegas).
- **Modos**: partidas, baús, média e vitórias em cada modo, e que parte dos baús veio de cada um.
- **Equipa**: o total da equipa e cada colega com quem jogaste (partidas juntos, baús, média, vitórias juntos).
- **Definições**: que **contentores extra** somam ao total (caixas de munição, canhões de saque, caixas de comida, cartazes, outros — por defeito só contam baús), a **janela compacta**, a pasta dos replays e o histórico.

**Janela compacta**: uma janela pequena sempre por cima do jogo com os baús da última partida e o total. **Bloqueada**, os cliques passam para o jogo; desbloqueada arrasta-se com o rato. Liga-se nas Definições ou no ícone junto ao relógio. No Fortnite usa o modo de ecrã *Ecrã inteiro em janela*. O Fortnite só deixa ler o replay no fim da partida, por isso o número atualiza quando cada partida acaba.

Também: lê os replays que já tinhas antes de instalar a app, guarda o histórico em `%AppData%\ContadorBaus\dados.json` (mesmo depois de o Fortnite apagar os replays) e continua a contar minimizado na bandeja.

Só lê ficheiros de replay; nunca mexe no jogo nem na memória dele.

## Como usar

1. No Fortnite: **Definições → Jogo → Replays → Gravar replays: Ativado**. Sem replays não há nada para contar.
2. Abre `dist\ChestCounter.exe`.
3. Em **“Contar baús a partir de”** escolhe a data inicial (ou usa *Desde sempre*, *Hoje*, *7 dias*, *Este mês*).

O botão **Replays ▾** permite abrir a pasta dos replays, escolher outra pasta ou voltar a tentar replays que deram erro.

## O que conta como baú

Baús normais, raros, enterrados, de relíquia/extração e de cartaz, abertos **por ti** (ou partidos por ti — o loot sai na mesma). Caixas de munição, de comida e canhões de loot aparecem à parte e não entram no total. Replays de ilhas criativas (UEFN) são ignorados. Uma partida em que o jogo fechou/crashou antes do fim não tem dados legíveis.

## Compilar

Precisa do [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Corre `build.bat` → gera `dist\ChestCounter.exe` (um só ficheiro, não precisa de instalar nada).

`tools\TesteReplays` analisa uma pasta de replays na consola. `dotnet test` corre os testes do cálculo das estatísticas. A arquitetura está em `docs/adr/0001-arquitetura-v2.md`.

## Créditos

- Leitura de replays: [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) (MIT, © Shiqan), com as alterações descritas em `lib/PATCHES.md`.
- Inspirado em [ChestCounter](https://github.com/xdoofy92/ChestCounter) (MIT, © LED-N) — as alterações à biblioteca de replays vêm desse projeto.
