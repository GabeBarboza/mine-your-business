# Marco 2 - vertical slice local

## Escopo entregue

- Partida local hot-seat para tres pessoas em uma unica instancia Godot.
- Mesa expansivel com cartas de caminho, origem e objetivos em arte vetorial provisoria original.
- Zoom com a roda, pan com o botao do meio e selecao de coordenada por clique.
- Mao interativa e destaque previo dos encaixes validos para cartas de caminho.
- Fluxos de quebra, conserto simples ou duplo, desmoronamento, mapa, descarte e passe automatico quando a mao acaba.
- Barreira de privacidade entre turnos, revelacao individual de papel e memoria privada dos objetivos vistos por mapa.
- Registro publico que nao revela carta descartada, papel, mao ou resultado de mapa.
- Resumo de rodada com papeis revelados e resultado da expedicao.
- Placar final depois de tres rodadas, incluindo empate.
- Controles administrativos para executar uma jogada valida, concluir a rodada atual ou concluir toda a partida.

## Roteiro de validacao manual

1. Execute o projeto e inicie uma partida com os tres nomes e uma seed.
2. Confirme que o papel e a mao so aparecem depois das duas etapas da barreira de privacidade.
3. Selecione uma carta de caminho e confirme que apenas destinos validos ficam verdes.
4. Use cartas de acao quando aparecerem; alvos ficam nos seletores e mapa/desmoronamento usam a mesa.
5. Use `FIM DA RODADA` e confira o resumo com os papeis da rodada encerrada.
6. Use `PLACAR FINAL` e confira as tres rodadas, a ordenacao por pepitas e a indicacao de empate.

## Verificacao automatizada

`LocalMatchControllerTests` cobre o avanco administrativo ate a rodada seguinte, a conclusao das tres rodadas e a preservacao dos papeis revelados antes de um novo sorteio. Os testes do dominio continuam cobrindo as regras individuais consumidas pela interface.
