# Plano de desenvolvimento - MINE YOUR BUSINESS

## 1. Visao do produto

Criar um jogo digital de estrategia, deducao social e papeis secretos, inspirado no ciclo de jogo de Saboteur, para computadores Windows, Linux e macOS.

O projeto usara Godot 4.7.2 .NET e C#. A versao do motor deve permanecer fixada durante cada marco de desenvolvimento. Atualizacoes do motor so devem ocorrer em uma tarefa separada, depois de validar build, testes e exportacoes nas tres plataformas.

O produto deve usar nome, identidade visual, textos, ilustracoes, audio e interface proprios, salvo se houver uma licenca explicita para usar a propriedade intelectual de Saboteur/PaperGames. O manual de referencia serve para entender comportamento e balanceamento, nao como fonte de assets ou texto para copiar.

## 2. Escopo recomendado

### MVP

- Partidas para 3 a 10 participantes.
- Multiplayer online com uma pessoa criando a sala e as demais entrando por codigo.
- Servidor autoritativo executado pelo anfitriao da sala.
- Reconexao durante uma partida em andamento.
- Tres rodadas por partida.
- Papeis secretos de minerador e sabotador.
- Tabuleiro expansivel de cartas de caminho.
- Cartas de ferramenta quebrada, conserto, desmoronamento e mapa.
- Compra, descarte oculto, passe e fim automatico do turno.
- Revelacao de objetivos e distribuicao de ouro.
- Placar final, empate e opcao de revanche.
- Interface em portugues do Brasil.
- Assets temporarios originais durante o desenvolvimento.

### Depois do MVP

- Partida solo contra bots.
- Pareamento publico e lista de salas.
- Chat, emotes e moderacao.
- Perfis, estatisticas e conquistas.
- Variantes de regras e conteudo adicional original.
- Internacionalizacao.
- Servidor dedicado e persistencia de contas.

### Fora do primeiro escopo

- Exportacao Web. Projetos Godot 4 em C# nao possuem exportacao oficial para Web.
- Versoes mobile.
- Economia, loja ou monetizacao.
- Hot-seat como modo principal: uma unica tela compromete maos e papeis secretos.

## 3. Regras-base extraidas do manual

### Preparacao

- A partida tem tres rodadas e aceita de 3 a 10 jogadores.
- A composicao do baralho de papeis inclui sempre uma carta a mais que o numero de jogadores. Uma carta fica oculta e fora da rodada.
- Distribuicao de papeis:

| Jogadores | Sabotadores | Mineradores | Carta de papel fora da rodada |
|---:|---:|---:|---:|
| 3 | 1 | 3 | 1 |
| 4 | 1 | 4 | 1 |
| 5 | 2 | 4 | 1 |
| 6 | 2 | 5 | 1 |
| 7 | 3 | 5 | 1 |
| 8 | 3 | 6 | 1 |
| 9 | 3 | 7 | 1 |
| 10 | 4 | 7 | 1 |

- A carta inicial fica aberta. Tres objetivos ficam fechados a sete colunas de distancia, com uma linha livre entre objetivos adjacentes.
- O baralho de compra combina 40 caminhos comuns com 27 cartas de acao.
- Mao inicial: 6 cartas para 3-5 jogadores, 5 para 6-7 e 4 para 8-10.
- O jogador mais jovem inicia a primeira rodada; as jogadas seguem no sentido horario.

### Turno

O jogador realiza exatamente uma destas acoes:

1. Joga uma carta de caminho valida.
2. Joga uma carta de acao valida.
3. Descarta uma carta virada para baixo e passa.

Depois, compra uma carta se o baralho ainda tiver cartas. Quando o baralho termina, os turnos continuam sem compra. Sem cartas na mao, o jogador passa sem descartar.

### Caminhos

- Uma carta ocupa uma coordenada inteira e nao pode ser girada em 90 graus.
- A carta deve tocar ortogonalmente uma carta existente.
- Todas as bordas em contato precisam ser compativeis: caminho com caminho e rocha com rocha.
- A nova carta precisa possuir conexao navegavel ate a carta inicial; apenas estar adjacente a uma ilha desconectada nao basta.
- Uma ferramenta quebrada no inicio do turno impede jogar caminho, mas nao impede acao ou passe.
- O tabuleiro pode crescer alem do retangulo ilustrado no manual.

### Acoes

- Ferramenta quebrada: aplicada a qualquer jogador; nao pode haver duas quebras do mesmo tipo no mesmo alvo.
- Conserto: remove uma quebra compativel. Uma carta com dois simbolos conserta apenas uma ferramenta.
- Desmoronamento: remove um caminho comum escolhido pelo jogador que usou a carta; inicio e objetivos nao podem ser removidos.
- Mapa: permite ao jogador olhar privadamente um objetivo e devolve-lo ao mesmo lugar.

### Fim da rodada

- Ao conectar um caminho continuo da origem a um objetivo, o objetivo e revelado.
- Se for ouro, a rodada termina imediatamente.
- Se for pedra, o objetivo fica aberto e passa a integrar o labirinto. Ele pode excepcionalmente permanecer com bordas incompatíveis quando nao houver encaixe possivel.
- Dois objetivos tocados pela mesma jogada sao revelados juntos.
- A rodada tambem termina quando o baralho e todas as maos se esgotam.
- Todos os papeis sao revelados ao final da rodada.

### Pontuacao

- Se o ouro foi conectado, os mineradores recebem cartas de pepita. Quem completou o caminho escolhe primeiro e a selecao segue no sentido anti-horario apenas entre mineradores.
- Se o ouro nao foi conectado, cada sabotador recebe 4 pepitas quando ha um sabotador, 3 quando ha dois ou tres e 2 quando ha quatro.
- Se a carta extra fez uma rodada de 3 ou 4 jogadores ficar sem sabotador e o ouro nao foi alcancado, ninguem pontua.
- O ouro individual permanece secreto ate o fim da terceira rodada.
- A rodada seguinte comeca com a pessoa a esquerda de quem jogou o ultimo caminho da rodada anterior.
- Depois de tres rodadas, vence quem tiver mais pepitas; empates dividem a vitoria.

## 4. Decisoes de produto que precisam ser fechadas antes da arte final

1. Licenciamento: porte oficial de Saboteur ou jogo original apenas inspirado em suas mecanicas.
2. Rede: somente convite entre amigos no MVP ou salas publicas tambem.
3. Ausencias: pausar, substituir por bot ou remover a pessoa quando ela desconectar.
4. Comunicacao: chat livre, frases prontas, emotes ou voz externa.
5. Informacao de pontuacao: reproduzir o segredo do manual ou exibir apenas totais parciais anonimos.
6. Publico e classificacao indicativa: isso influencia texto, moderacao e direcao de arte.

As hipoteses deste plano sao: jogo original, salas privadas por convite, chat de frases prontas, reconexao com pausa curta e pontuacao secreta ate o fim.

## 5. Arquitetura tecnica

Separar as regras puras da apresentacao e da rede. O motor de regras nao deve depender de Nodes, cenas, animacoes ou chamadas RPC.

```text
UI/Scenes
    -> Application (casos de uso e fluxo da partida)
        -> Domain (regras deterministicas em C# puro)
        -> Infrastructure (rede, salvamento, logs, configuracao)
```

### Camada de dominio

- `GameState`: rodada, turno, jogadores, pilhas e fase atual.
- `BoardState`: dicionario de coordenadas para cartas no tabuleiro.
- `PlayerState`: papel no estado privado, mao, ferramentas e ouro.
- `CardDefinition`: dados imutaveis de cada tipo de carta.
- `GameCommand`: intencoes como jogar caminho, quebrar ferramenta ou passar.
- `GameEvent`: fatos resultantes como carta jogada, objetivo revelado ou rodada encerrada.
- `RulesEngine`: valida comandos e produz eventos.
- `RoundScoringService`: calcula e distribui recompensas.
- `SeededRandom`: embaralhamento reproduzivel para testes, replays e sincronizacao.

### Estado publico e privado

O servidor mantem o estado completo. Cada cliente recebe uma projecao propria:

- Publico: tabuleiro, jogador da vez, quantidade de cartas, ferramentas, descarte e papeis ja revelados.
- Privado do jogador: mao, papel, objetivos vistos com mapa e cartas de ouro recebidas.
- Somente servidor: papeis e maos de todos, ordem dos baralhos e objetivos fechados.

Nunca enviar o estado completo e ocultar elementos apenas na interface. Dados secretos que chegam ao cliente podem ser inspecionados.

### Rede

- O cliente envia comandos, nao mutacoes de estado.
- O anfitriao valida o comando, aplica-o e publica eventos e uma nova revisao do estado.
- Cada comando inclui `MatchId`, `PlayerId`, `TurnNumber`, `CommandId` e `ExpectedRevision`.
- Comandos repetidos sao idempotentes; comandos atrasados ou fora do turno sao rejeitados.
- Um snapshot sanitizado permite reconexao.
- A primeira versao pode usar ENetMultiplayerPeer. A interface de transporte deve ser abstraida para permitir servidor dedicado posteriormente.

### Tabuleiro e conectividade

Representar cada carta por uma coordenada `Vector2I` e uma mascara de quatro bits para as aberturas Norte, Leste, Sul e Oeste.

- Compatibilidade local: cada aresta deve coincidir com a aresta oposta do vizinho.
- Alcance da origem: uma busca em largura percorre apenas conexoes abertas dos dois lados.
- Colocacao valida: espaco vazio, ao menos um vizinho, todas as arestas adjacentes compativeis e nova carta alcancavel a partir da origem.
- Objetivos sao posicionados em `(8, -2)`, `(8, 0)` e `(8, 2)` se a origem for `(0, 0)`: existem sete espacos de carta entre a origem e cada objetivo.
- A camera e o container do tabuleiro devem aceitar coordenadas negativas e expansao sem limites artificiais.

### Maquina de estados

```text
Lobby
  -> RoundSetup
  -> AwaitingAction
  -> ResolvingAction
  -> DrawingCard
  -> CheckingRoundEnd
       -> AwaitingAction (proximo jogador)
       -> RewardSelection / SaboteurReward
  -> RoundSummary
       -> RoundSetup (rodadas 2 e 3)
       -> MatchSummary
```

As selecoes que exigem resposta do jogador, como escolher ouro ou o alvo de um conserto duplo, sao fases explicitas. Isso evita logica de interface escondida dentro do turno.

## 6. Estrutura inicial do projeto

```text
mine-your-business/
  project.godot
  MineYourBusiness.sln
  src/
    Domain/
      Cards/
      Board/
      Match/
      Commands/
      Events/
    Application/
      MatchController.cs
      Projections/
    Infrastructure/
      Networking/
      Persistence/
      Logging/
    Presentation/
      Screens/
      Components/
      Input/
  scenes/
    bootstrap/
    lobby/
    match/
    results/
    components/
  assets/
    art/
    audio/
    fonts/
  data/
    cards/
    localization/
  tests/
    MineYourBusiness.Domain.Tests/
    MineYourBusiness.Integration.Tests/
  docs/
```

As definicoes das cartas devem ser Resources ou JSON validados, sem codificar quantidades e arte diretamente na logica. O dominio trabalha com IDs e propriedades tipadas.

## 7. Cenas principais

- `Bootstrap`: composicao de dependencias, configuracao e navegacao.
- `MainMenu`: criar sala, entrar em sala, configuracoes e sair.
- `Lobby`: codigo da sala, lista de jogadores, prontidao e inicio.
- `Match`: tabuleiro, mao, status dos jogadores, pilhas e log de eventos.
- `PrivateRevealOverlay`: papel, mapa e recompensas, sempre com confirmacao explicita.
- `RoundSummary`: papeis revelados e resultado da rodada.
- `MatchSummary`: placar final e revanche.

## 8. UX para desktop

- Arrastar uma carta e clicar em um destino devem ser equivalentes.
- Destinos validos aparecem antes da confirmacao; destinos invalidos explicam o motivo.
- Zoom com roda do mouse, pan com botao do meio e comandos equivalentes por teclado.
- Animacoes nunca bloqueiam a regra; o estado visual consome eventos confirmados.
- Informacoes privadas exigem uma camada visual clara e nao aparecem em logs, notificacoes ou capturas auxiliares.
- O log publico descreve acoes sem revelar a carta descartada, mao, papel ou resultado de mapa.
- Contraste, escala de UI, navegacao por teclado e alternativas a cor entram no MVP.

## 9. Estrategia de testes

### Unidade

- Todas as composicoes de papeis e tamanhos de mao.
- Encaixe de cada borda, vizinhanca e proibicao de rotacao.
- Conectividade ate a origem, inclusive apos desmoronamento.
- Quebras duplicadas, consertos simples e duplos.
- Revelacao simultanea de dois objetivos.
- Esgotamento do baralho e das maos.
- Pontuacao de mineradores e de 0 a 4 sabotadores.
- Ordem do primeiro jogador entre rodadas.

### Propriedades e simulacao

- Nenhuma carta aparece em dois lugares ao mesmo tempo.
- A soma de cartas em maos, pilhas, tabuleiro e descarte permanece constante.
- Um cliente nunca recebe segredo de outro jogador.
- Uma partida sempre alcanca um estado terminal sob uma politica de comandos validos.
- Milhares de partidas automatizadas com seeds registradas.

### Integracao

- Host e 2-9 clientes em processos separados.
- Comando duplicado, atrasado, invalido e fora do turno.
- Queda e reconexao do host e dos clientes conforme a politica escolhida.
- Snapshot seguido de eventos incrementais produz o mesmo estado.
- Exportacoes Windows, Linux e macOS abrem e concluem uma partida de teste.

## 10. Marcos de entrega

### Marco 0 - fundacao

- Criar o projeto Godot .NET, solution C# e projetos de teste.
- Configurar formatacao, analise estatica, CI e exports de desenvolvimento.
- Registrar decisoes de licenciamento, rede e desconexao.

Saida: projeto abre, compila e executa uma cena de bootstrap nas plataformas-alvo.

### Marco 1 - motor de regras

- Modelar cartas, baralho, jogadores, tabuleiro e fases.
- Implementar comandos, eventos, validacao de caminhos e pontuacao.
- Cobrir regras criticas com testes deterministas.

Saida: uma partida completa roda sem interface por meio de testes e simulador textual.

### Marco 2 - vertical slice local

- Criar mesa, mao, colocacao de caminho e acoes.
- Implementar overlays privados, resumos e placar.
- Usar arte provisoria original.

Saida: tres pessoas conseguem concluir uma partida em uma unica instancia de depuracao com controles administrativos de teste.

### Marco 3 - multiplayer privado

- Criar e entrar em sala.
- Sincronizar estado publico/privado, turnos e reconexao.
- Adicionar validacao autoritativa e protecao contra comandos repetidos.

Saida: partida completa de 3 a 10 pessoas em redes diferentes.

### Marco 4 - conteudo e polimento

- Direcao de arte, audio, tutorial, acessibilidade e configuracoes.
- Telemetria opcional e respeitosa a privacidade.
- Testes de usabilidade e balanceamento.

Saida: beta fechado distribuivel.

### Marco 5 - lancamento

- QA de regressao, testes de carga, crash reporting e processo de suporte.
- Builds assinadas, pagina de loja, politica de privacidade e creditos.
- Checklist de licencas de fontes, audio, arte e bibliotecas.

Saida: versao 1.0 para desktop.

## 11. Primeiro backlog executavel

1. Inicializar Godot 4.7.2 .NET e testes xUnit.
2. Criar os value objects `CardId`, `PlayerId`, `BoardPosition` e `EdgeMask`.
3. Implementar `BoardState` e testes de encaixe/conectividade.
4. Implementar composicao de papeis, embaralhamento com seed e mao inicial.
5. Implementar a maquina de estados e os comandos de turno.
6. Implementar cartas de acao.
7. Implementar objetivos, fim da rodada e pontuacao.
8. Criar um simulador textual para concluir partidas sem Godot UI.
9. Criar a cena da mesa consumindo snapshots e eventos do dominio.
10. Adicionar transporte de rede somente depois de o motor completar partidas deterministicas.

## 12. Criterios de pronto do MVP

- Uma sala de 3 a 10 pessoas conclui tres rodadas sem intervencao administrativa.
- Todas as regras listadas neste documento possuem ao menos um teste automatizado.
- Nenhum cliente recebe dados privados pertencentes a outro jogador.
- Uma desconexao dentro da janela definida permite retorno ao mesmo assento.
- Uma seed e uma lista de comandos reproduzem exatamente a partida.
- Interface funcional em 1920x1080 e 1280x720, com escala configuravel.
- Build de release sem erros para Windows, Linux e macOS.
- Nome, textos e assets passaram por revisao de propriedade intelectual.

## 13. Riscos principais

| Risco | Impacto | Mitigacao |
|---|---|---|
| Uso indevido da propriedade intelectual | Bloqueio de publicacao | Definir licenca ou identidade original antes da arte final |
| Vazamento de papeis/maos | Quebra total da partida | Projecoes de estado por cliente e testes de privacidade |
| Host abandona a sala | Partida interrompida | Pausa/reconexao no MVP; migracao de host ou servidor dedicado depois |
| Regra acoplada a animacao | Bugs e dessincronizacao | Dominio puro, comandos e eventos deterministas |
| Tabuleiro expansivel prejudica UX | Dificuldade de leitura | Camera livre, foco automatico e indicadores de jogadas validas |
| Escopo online cresce cedo demais | Atraso do prototipo | Completar motor e vertical slice antes de matchmaking e contas |
