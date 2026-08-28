# MINE YOUR BUSINESS

*Just do your job. Probably.*

Jogo de estrategia, caminhos e papeis secretos para desktop, desenvolvido com Godot 4.7.2 .NET e C#.

O projeto concluiu o vertical slice local do Marco 2. A identidade, os textos e os assets finais serao originais; o manual de Saboteur e usado apenas como referencia de regras e comportamento.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Godot Engine 4.7.2 .NET](https://godotengine.org/download/windows/), nao a edicao Standard
- Git, recomendado para controle de versao

## Abrir e executar

1. Abra `project.godot` no editor Godot 4.7.2 .NET.
2. Aguarde a restauracao e compilacao do projeto C#.
3. Pressione F6 para executar a cena atual ou F5 para executar o projeto.

Na tela inicial, informe os nomes das tres pessoas e a seed. Cada turno comeca com uma barreira de privacidade antes de revelar papel, mao e informacoes de mapa. A mesa aceita clique, zoom pela roda e pan com o botao do meio.

Os controles `JOGADA AUTO`, `FIM DA RODADA` e `PLACAR FINAL` sao atalhos administrativos de depuracao para validar o fluxo completo sem depender de uma sequencia manual longa.

Pela linha de comando:

```powershell
dotnet restore MineYourBusiness.sln
dotnet build MineYourBusiness.sln --configuration Debug --no-restore
dotnet test --solution MineYourBusiness.sln --configuration Debug --no-build
```

O simulador textual executa uma partida deterministica completa, sem Godot ou interface:

```powershell
dotnet run --project src/MineYourBusiness.Simulator -- --players 3 --seed 20260828
```

Se o executavel do Godot estiver disponivel, a verificacao completa pode ser executada com:

```powershell
$env:GODOT_BIN = "C:\caminho\para\Godot_v4.7.2-stable_mono_win64.exe"
./scripts/verify.ps1
```

## Estrutura

- `src/MineYourBusiness.Domain`: regras e modelos C# puros, sem dependencia do Godot.
- `src/MineYourBusiness.Application`: coordenacao testavel da partida local e controles administrativos.
- `src/MineYourBusiness.Simulator`: partida automatizada e log textual do dominio.
- `src/MineYourBusiness.Game`: composicao e apresentacao do cliente Godot.
- `scenes`: cenas e componentes visuais.
- `tests/MineYourBusiness.Domain.Tests`: testes automatizados do dominio.
- `docs`: plano, decisoes arquiteturais e documentacao.
- `scripts`: verificacoes reproduziveis para desenvolvimento e CI.

Veja o [plano de desenvolvimento](docs/PLANO_DE_DESENVOLVIMENTO.md) para o escopo e os marcos.
O escopo entregue e o roteiro de validacao do vertical slice estao em [docs/MARCO_2_VERTICAL_SLICE.md](docs/MARCO_2_VERTICAL_SLICE.md).
