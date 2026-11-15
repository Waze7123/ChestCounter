# ADR-0001: Arquitetura da versão 2 (abas, equipa, extras, overlay)

**Estado:** Aceite
**Data:** 2026-11-15

## Contexto
A v1 era uma só janela WinForms com a lógica de contagem misturada na UI. A v2 pede: design preto e dourado mais limpo e compacto, abas (Painel, Partidas, Modos, Equipa, Definições), contentores extra configuráveis, baús dos colegas de equipa e uma janela pequena por cima do jogo. Restrições: um só .exe sem instalação, leitura de replays com estado estático (análise sequencial), replays só legíveis no fim da partida.

## Decisão
1. **Manter WinForms com controlos desenhados à mão (GDI+)**, organizados em páginas (`UserControl`) dentro de uma janela-concha com barra de navegação.
2. **Núcleo puro e testável**: `Stats` (Core) calcula tudo a partir de `MatchRecord` + `CountingOptions`; a UI só formata. Testes xUnit cobrem `Stats`, `ChestKinds` e `Modes`.
3. **Guardar factos, não totais**: o analisador guarda cada abertura (baús e extras, com tempo) e os colegas por tipo; quais extras contam é decidido na leitura, por isso mudar as Definições não exige reanalisar.
4. **Overlay** como `Form` sem bordas, sempre no topo, com `WS_EX_TRANSPARENT` quando bloqueado; controlo pelo ícone da bandeja e pelas Definições.
5. **`AppState`** único: guarda o histórico, o scanner e o período; as páginas reagem ao evento `Changed`.

## Opções consideradas

### A: WinForms desenhado à mão (escolhida)
| Dimensão | Avaliação |
|---|---|
| Complexidade | Média |
| Custo | Reaproveita a v1 |
| Tamanho do exe | Igual (~50 MB self-contained) |
| Suavidade | Boa com double-buffer + animações por timer |

### B: Reescrever em WPF
| Dimensão | Avaliação |
|---|---|
| Complexidade | Alta (reescrita total) |
| Custo | Alto |
| Suavidade | Melhor (composição por GPU) |

**Prós de B:** animações e anti-aliasing nativos. **Contras:** reescrita, mais memória, ganho visual pequeno para uma app compacta.

## Consequências
- Mais fácil: testar contagens, adicionar páginas, mudar o que conta sem reanalisar.
- Mais difícil: cada controlo (calendário, interruptores) é desenhado por nós.
- A rever: a overlay não aparece em ecrã inteiro exclusivo; contagem ao vivo impossível enquanto o Fortnite não escrever a chave no replay.

## Ações
1. [x] Analisador v3 com extras temporizados e colegas.
2. [x] `Stats` + testes.
3. [x] Concha, páginas, overlay, bandeja.
