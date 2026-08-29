# Marco 4 - conteudo e polimento

## Escopo entregue

- Identidade visual original de mina industrial noturna, com paleta azul-ardosia, verde mineral e ambar.
- Fundo ilustrado original no menu, mantido sob uma camada escura para preservar contraste e legibilidade.
- Sinais sonoros originais sintetizados em tempo de execucao para clique, sucesso, erro e revelacao; nao ha arquivo de audio ou licenca de terceiros.
- Tutorial de quatro etapas na primeira abertura e acesso permanente por `COMO JOGAR`.
- Configuracoes persistentes de escala da interface, volume, alto contraste e movimento reduzido.
- Navegacao por `Tab`, atalhos `1` a `9` para cartas e controle da mesa por setas, `Enter`, `+`, `-` e `F`.
- Marcacoes redundantes na mesa: cor, contorno, ponto central, formas distintas de objetivo e cursor de teclado.
- Telemetria beta local, opcional, desativada por padrao e apagavel pela propria tela de configuracoes.
- Testes automatizados para persistencia, normalizacao, padroes de privacidade, consentimento e exclusao da telemetria.
- Roteiros mensuraveis de usabilidade e balanceamento para o beta fechado.

## Direcao audiovisual

O menu usa `assets/art/main-menu-mine.png`, criado especificamente para este projeto. A composicao reserva uma area central escura para a interface e concentra detalhes de tunel e iluminacao nas bordas. A imagem nao contem texto, logotipo, personagem reconhecivel ou elemento copiado de outro jogo.

Prompt de producao:

> Original atmospheric underground mine operations room for a social-deduction strategy game, with branching timber-supported tunnels and a distant warm mineral glow; deep slate-blue cavern, subtle industrial work lights, empty mine junction, polished painterly stylized realism, central negative space, restrained amber and muted teal lighting; no text, logos, trademarks, characters or resemblance to existing board-game artwork.

Os avisos sonoros sao ondas curtas com envelope suave geradas por `AudioCuePlayer`. Essa abordagem oferece feedback imediato, respeita o controle de volume e evita dependencia de assets externos. Audio ambiente e musica permanecem deliberadamente fora desta beta ate que sejam testados sem prejudicar a conversa entre participantes.

## Acessibilidade e configuracoes

As preferencias ficam em `user://settings.json` e sao normalizadas ao carregar:

- escala da interface entre `0,80x` e `1,50x`;
- volume entre `0%` e `100%`;
- alto contraste com cores mais separadas e contornos reforcados;
- movimento reduzido, garantindo as transicoes instantaneas usadas nesta versao;
- estado de conclusao do tutorial;
- consentimento de telemetria, sempre falso na primeira execucao.

O tabuleiro preserva operacao por mouse, mas nao depende mais dela. Quando recebe foco, um cursor visivel pode ser movido com as setas e confirmado com `Enter`. O estado de ferramentas e escrito por extenso, portanto nao depende apenas de cor.

## Privacidade e telemetria

A telemetria desta beta e somente local. Nenhum dado e transmitido. Quando a pessoa opta por participar, o jogo grava `user://beta-telemetry.jsonl` com:

- nome fixo e validado do evento;
- data e hora UTC;
- contadores numericos previamente definidos, como numero de participantes, turno e rodada.

O coletor rejeita nomes livres de eventos e contadores. Nao registra nome de participante, IP, porta, codigo de sala, seed, papel, mao, mapa, mensagem ou texto digitado. Desativar impede novas gravacoes; `APAGAR TELEMETRIA LOCAL` remove o arquivo existente.

## Roteiro de usabilidade do beta fechado

Executar com pelo menos cinco grupos que ainda nao tenham jogado esta versao. Uma pessoa observa sem explicar a interface e registra tempos, erros e comentarios apenas com consentimento.

1. Abrir o jogo, concluir o guia e alterar escala e volume.
2. Criar uma sala; duas ou mais pessoas devem entrar e marcar prontidao.
3. Identificar papel e mao sem expor informacao privada.
4. Jogar um caminho valido, descartar uma carta e usar ao menos uma acao.
5. Usar zoom e pan; repetir uma jogada usando somente teclado.
6. Simular queda de cliente e concluir a reconexao.
7. Terminar uma partida e localizar o placar e a revanche.

Registrar por grupo:

| Medida | Meta para promover a beta |
|---|---:|
| Conclusao do lobby sem ajuda | pelo menos 90% |
| Primeira jogada valida em ate 2 minutos | pelo menos 85% |
| Exposicao acidental de informacao privada | 0 ocorrencias |
| Conclusao da reconexao em ate 45 segundos | pelo menos 80% |
| Tarefa equivalente concluida apenas por teclado | pelo menos 80% |
| Texto legivel em 1280x720 e 1920x1080 | 100% dos grupos |

Para cada falha, registrar tarefa, resolucao, dispositivo de entrada, configuracao de escala, comportamento esperado e evidencia. Nao registrar nomes reais no relatorio compartilhado.

## Roteiro de balanceamento

Cada configuracao de 3 a 10 participantes deve acumular pelo menos 30 rodadas validas antes de qualquer mudanca de quantidade de cartas ou pontuacao. Registrar por rodada:

- numero de participantes e seed;
- papel vencedor, chegada ou nao ao ouro e numero de turnos;
- quantidade final de cartas no baralho e nas maos;
- uso de quebra, conserto, mapa e desmoronamento;
- desconexao ou intervencao administrativa;
- avaliacao anonima de agencia de 1 a 5.

Faixas de investigacao, nao metas para forcar artificialmente:

- um lado vence menos de 35% ou mais de 65% das rodadas em uma configuracao;
- mediana de duracao difere em mais de 30% entre configuracoes vizinhas;
- uma categoria de acao aparece em menos de 5% das oportunidades;
- agencia mediana abaixo de 3 para qualquer papel.

Revisar primeiro problemas de compreensao e interface. Alteracoes de regra exigem uma decisao registrada, nova versao de ruleset, seeds reproduziveis e repeticao da amostra. Partidas com queda ou intervencao administrativa nao entram na amostra de vitoria.

## Validacao manual

1. Apague `user://settings.json`, abra o jogo e confirme que o tutorial aparece antes do menu.
2. Percorra o guia, reabra-o pelo menu e confirme os quatro indicadores de progresso.
3. Salve escala `1,50x`, volume `0%`, alto contraste e movimento reduzido; reinicie e confirme persistencia.
4. Entre em uma partida local, use `Tab`, `1` a `9`, setas e `Enter` sem mouse.
5. Confirme que erro, sucesso e revelacao usam sinais diferentes quando o volume esta acima de zero.
6. Confirme que a telemetria desativada nao cria arquivo.
7. Ative a telemetria, inicie uma partida, inspecione o JSONL e confirme a ausencia de dados privados.
8. Apague a telemetria pela interface e confirme a remocao do arquivo.
9. Repita os fluxos em 1280x720 e 1920x1080, nos tres sistemas de desktop exportados.

## Limite da entrega

O software, os testes automatizados e os protocolos estao prontos para distribuicao de beta fechado. Sessoes humanas de usabilidade e a amostra estatistica de balanceamento dependem dos participantes do beta e devem produzir dados antes de qualquer promocao ao Marco 5.
