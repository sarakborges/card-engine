# HANDOFF — card-engine

Repo: `sarakborges/card-engine`  
Branch de integração: `develop`  
Stack: C# / .NET 8 + Godot .NET (presentation layer)

## Fonte canônica e regras de trabalho

Este `HANDOFF.md` na raiz de `develop` é a fonte canônica e persistente do contexto operacional do projeto. `ARCHITECTURE.md` é o canon das regras arquiteturais.

- Todo branch de trabalho nasce de `develop`.
- Todo pull request de trabalho aponta para `develop`, nunca diretamente para `main`.
- `main` é reservado para integração estável/release.
- Antes de escrever, buscar HEAD/VERSION atuais e abrir os arquivos reais envolvidos.
- Commits devem ser pequenos e coerentes; não misturar mudanças arquiteturais sem relação.
- Todo bloco coerente de mudança deve atualizar `VERSION` segundo SemVer: patch para fix/refactor/tooling compatível; minor para feature compatível; major para mudança incompatível.
- Depois de mudança material em código, versão, arquitetura, roadmap ou processo, atualizar este handoff.
- Runtime error/warning reportado pelo usuário tem prioridade sobre roadmap/refactor.
- Não declarar bug visual/gameplay resolvido sem evidência runtime quando a confirmação depender da integração Godot.
- Comunicação e documentação devem privilegiar decisões concretas, invariants e estado atual em vez de narrativa extensa.

## Validação

CI automático em `.github/workflows/ci.yml`:

- `dotnet restore CardEngine.sln`
- `dotnet build CardEngine.sln --configuration Release --no-restore`
- `dotnet test CardEngine.sln --configuration Release --no-build`

Roda em push para `develop`/`main` e em pull requests para `develop`.

Warnings C# são tratados como erros via `Directory.Build.props`. Testes fazem parte do gate porque o core headless, determinismo, regras e IA precisam ser validáveis sem Godot.

## Canon arquitetural

`ARCHITECTURE.md` é a referência principal. Regras operacionais mais importantes:

1. Cada fato de gameplay possui um único owner autoritativo.
2. `CardEngine.Core` é headless e não depende de Godot, UI ou IA.
3. UI/Godot nunca é owner de regra; apenas projeta estado e envia intenções/ações ao core.
4. IA escolhe entre ações legais expostas pelo core; não altera estado diretamente.
5. Aleatoriedade de gameplay é injetada, seedable e reproduzível; evitar RNG global/ambiental.
6. Mesmo seed + mesma configuração + mesma sequência de ações deve produzir o mesmo resultado.
7. Reutilizar invariants, state machines e primitives reais; não abstrair semelhança superficial.
8. Definições de cartas são data-driven sempre que possível; comportamento compartilhado deve usar efeitos composáveis em vez de classes específicas por carta.
9. Estado derivado barato deve ser calculado/cached a partir do owner, não espelhado como segunda fonte de verdade.
10. Atualizações de apresentação e caches devem ser change-driven quando possível; evitar recomputar ou reescrever valores idênticos por frame.
11. Trabalho pesado de IA/simulação pode rodar fora da thread de apresentação usando snapshots imutáveis; resultados assíncronos devem ser validados contra revisão/turno atual antes de aplicar.
12. Metadados imutáveis derivados de catálogo devem ser pré-computados pelo owner no carregamento, não redescobertos em hot paths.
13. Não trocar corretude/determinismo por performance aparente; medir antes de otimizar.
14. Boundaries devem apontar para dentro: Core não conhece AI, Serialization, Runner ou Godot.
15. Testes de regressão devem privilegiar regras, legalidade de ações, replay determinístico e contratos entre módulos.

---

# Estado atual

`VERSION`: `0.1.0`

Branch de trabalho atual: `chore/initial-scaffold`  
PR atual: `#1` -> `develop`

Estrutura inicial:

- `CardEngine.Core`: regras, estado, ações, cartas e RNG determinístico.
- `CardEngine.AI`: `IAgent` e agentes de IA.
- `CardEngine.Serialization`: boundary JSON/data.
- `CardEngine.Runner`: execução headless.
- `CardEngine.Core.Tests` e `CardEngine.AI.Tests`: testes automatizados.
- Godot ainda não foi integrado; será uma camada externa de apresentação.

## Próximos passos

Se nenhum bug/runtime issue tiver prioridade:

1. Definir o modelo canônico de efeitos composáveis (`damage`, `heal`, `draw`, `discard`, modifiers, conditions, targeting).
2. Separar com clareza definição imutável de carta e estado runtime de instância quando cartas passarem a possuir estado próprio.
3. Criar catálogo data-driven com validação na carga e IDs únicos.
4. Formalizar event/replay log determinístico para debugging e reprodução de partidas.
5. Evoluir agentes: greedy/heuristic antes de search/MCTS; manter todos atrás de `IAgent`.
6. Criar simulação em lote headless para balanceamento e avaliação de IA.
7. Integrar Godot somente depois de os contratos de core/ações/efeitos estarem estáveis o suficiente para uma adapter layer fina.

## Direção de performance

- simulações headless devem evitar dependência de frame/render;
- não fazer allocations ou cópias grandes em hot paths sem necessidade demonstrada;
- caches só quando derivados de owner autoritativo e com invalidation clara;
- AI search deve trabalhar sobre snapshots controlados e nunca mutar a partida autoritativa em paralelo;
- preferir atualização change-driven e batch simulation mensurável a otimizações especulativas.
