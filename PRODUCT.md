# Product

<!-- impeccable:product-schema 1 -->

## Platform

desktop (Windows)

## Users

Jogador de Fortnite (Battle Royale, sobretudo Esquadrões) em Windows que quer saber quantos baús abre: por partida, por dia, desde uma data escolhida, por modo e com a equipa. Usa a janela grande entre partidas e uma janela pequena por cima do jogo enquanto joga.

## Product Purpose

Contar automaticamente os baús abertos a partir dos replays que o Fortnite grava, incluindo os replays que já existiam antes de instalar a app, e manter o histórico mesmo depois de o Fortnite apagar os replays antigos.

## Operating Context

- Os replays estão em `%LocalAppData%\FortniteGame\Saved\Demos`; o jogador tem de ativar "Gravar replays".
- Durante a partida o replay está cifrado com a chave a zero: só é legível no fim. "Esta partida" na overlay atualiza quando a partida acaba (confirmado ao utilizador).
- Overlay: por cima do jogo, sempre no topo, transparente a cliques quando bloqueada; precisa do Fortnite em "Ecrã inteiro em janela".
- Histórico em `%AppData%\ContadorBaus\dados.json`.

## Capabilities and Constraints

- Conta só os baús do jogador (abertos ou partidos). Contentores extra (caixas de munição, de comida, canhões de saque, outros) ficam à parte e **não entram no total por defeito**; cada tipo pode ser somado nas Definições.
- Equipa: total da equipa = jogador + colegas; cada colega com os seus baús e partidas jogadas juntos. Um replay só vê o que acontece perto de quem grava: colegas longe podem ficar sub-contados.
- Ilhas criativas (UEFN) são ignoradas. Replays de jogos que crasharam não são legíveis.
- Idioma: português de Portugal.

## Brand Commitments

- Estilo preto e dourado, limpo, suave, interface compacta (pedido explícito do utilizador).
- Nome: "Contador de Baús".

## Evidence on Hand

Dados reais dos replays do utilizador (100 replays, 08–15/11/2026). Não inventar números nem funcionalidades de leitura em direto.

## Product Principles

1. O número de baús é o herói; tudo o resto é contexto.
2. Honesto sobre os limites dos replays (sem prometer contagem ao vivo).
3. Funciona sozinho: deixar aberto e esquecer.
4. Compacto: não rouba espaço nem atenção ao jogo.
