# HANDOFF — card-engine

Repo: `sarakborges/card-engine`  
Branch de integração: `develop`  
Stack: C# / .NET 8 + Godot .NET (presentation layer)

## Fonte canônica

- `ARCHITECTURE.md`: canon de arquitetura de código e runtime.
- `HANDOFF.md`: estado operacional, fluxo e próximos passos.
- `VERSION`: versão SemVer canônica.

Todo branch de trabalho nasce de `develop` e todo PR de trabalho aponta para `develop`, nunca diretamente para `main`.

## Arquitetura adotada

A arquitetura foi consolidada a partir das práticas de código úteis observadas em `mineclone/develop`, adaptadas ao domínio de uma engine de card games em C#/.NET.

Princípios obrigatórios:

- responsabilidade única por invariant/domain fact;
- módulos pequenos e coesos, com dependências estreitas;
- composição em vez de classes centrais gigantes;
- reuso de invariants reais, não abstrações por semelhança superficial;
- Core como único owner das regras e mutações de gameplay;
- IDs de domínio tipados onde reduzem ambiguidade/erros;
- catálogos autoritativos e conteúdo imutável/validado antes da partida;
- actions como boundary único de mutação;
- efeitos composáveis;
- state machines explícitas para turnos/fases/interações multi-step;
- snapshots read-only com revision para UI/AI/async;
- RNG e ordenação determinísticos;
- tarefas assíncronas limitadas, snapshot-based e stale-safe;
- trabalho derivado change-driven;
- caches somente com owner/key/invalidation/lifetime claros;
- collections especializadas apenas quando encapsulam um invariant útil ou ganho medido;
- performance medida no menor owner responsável, mantendo Core benchmarkável sem Godot;
- API pública mínima e runtime mutable state encapsulado;
- testes focados nos invariants e regressões headless.

`ARCHITECTURE.md` contém as regras detalhadas de organização, componentização, reutilização, performance, estado, concorrência, erros, testes e refactoring triggers.

## Estado atual

`VERSION`: `0.1.1`

Branch atual: `chore/initial-scaffold`  
PR atual: `#1` -> `develop`

O scaffold ainda é deliberadamente pequeno. O `Game` atual é bootstrap e não deve crescer como um god object. Conforme regras forem adicionadas, responsabilidades devem migrar para owners coesos como catálogo, deck/zones, turn/phase state, legal-action rules, effect resolver e terminal rules.

## Próximos passos de código

1. Introduzir `CardId` tipado e `CardCatalog` autoritativo.
2. Fazer deck/mão/runtime referenciar IDs/instâncias, não duplicar `CardDefinition`.
3. Validar catálogos e cross-references antes de criar uma partida.
4. Substituir dependência de `System.Random` por PRNG estável controlado pela engine para replay de longo prazo.
5. Adicionar revision a snapshots/actions e rejeitar decisões stale.
6. Extrair turn/phase state e legal-action logic do `Game` conforme a complexidade aumentar.
7. Implementar sistema de efeitos composáveis com resolução determinística.
8. Criar event/replay log determinístico.
9. Evoluir AI sobre snapshots/simulation state isolado.
10. Criar batch simulation/benchmarks antes de otimizações avançadas.
11. Integrar Godot como adapter fino depois de estabilizar os contratos do Core.

## Validação

CI em `.github/workflows/ci.yml` executa restore, build Release e testes em PRs para `develop` e pushes relevantes. Warnings são tratados como erros.

Mudanças de gameplay devem ganhar testes headless quando reproduzíveis. Mudanças de performance devem ser sustentadas por benchmark/profiling antes de introduzir caches, pooling, custom collections ou concorrência adicional.

## Regra de evolução

Antes de uma mudança material, ler `ARCHITECTURE.md`, abrir os arquivos reais envolvidos e identificar o owner do invariant. Se o design novo exigir alterar o canon, atualizar arquitetura, handoff e versão no mesmo bloco coerente.
