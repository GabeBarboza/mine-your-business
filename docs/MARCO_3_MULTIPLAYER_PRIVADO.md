# Marco 3 - multiplayer privado

## Escopo entregue

- Salas privadas de 3 a 10 pessoas com codigo de convite, prontidao e inicio exclusivo pelo anfitriao.
- Transporte direto por ENet/UDP, com endereco e porta configuraveis.
- Host autoritativo: clientes enviam comandos e nunca alteram o estado por conta propria.
- Envelope de comando com partida, jogador, turno, identificador unico e revisao esperada.
- Protecao idempotente: o reenvio do mesmo comando aceito devolve o recibo original sem aplicar a jogada novamente.
- Rejeicao de comandos atrasados, de outra partida, de outro assento ou fora do turno.
- Snapshot publico com mesa, turno, contadores e ferramentas; objetivos fechados usam ID, bordas e conteudo sanitizados.
- Snapshot privado por assento com apenas o proprio papel, mao, mapas e ouro quando permitido.
- Mesa online visual, controles para todas as categorias de carta, descarte e passe.
- Pausa de 45 segundos quando um cliente cai, reconexao com token criptograficamente aleatorio e restauracao do mesmo assento.
- Encerramento da partida quando a janela de reconexao expira.
- Preservacao do modo local e dos controles administrativos do Marco 2.

## Modelo de rede

O `OnlineSessionNode` adapta o multiplayer ENet do Godot. Ele transporta requisicoes JSON pequenas e entrega cada uma ao `AuthoritativeRoom`, que nao depende de Godot. O servidor associa o peer ENet a um unico assento antes de aceitar comandos.

Depois de qualquer mudanca, o host gera separadamente o `PlayerSnapshot` de cada conexao. Nao existe um snapshot completo enviado a todos para ser ocultado na interface.

O codigo de sala e uma credencial de convite, nao um servico de descoberta. Para jogar entre redes diferentes no MVP, o anfitriao precisa encaminhar a porta UDP e compartilhar seu endereco publico. Relay, NAT traversal automatico, migracao de host e servidor dedicado permanecem evolucoes posteriores.

## Roteiro de validacao manual

1. Abra tres instancias do jogo.
2. Na primeira, crie uma sala na porta UDP `24828` e copie o codigo exibido.
3. Nas demais, entre usando `127.0.0.1`, a mesma porta e o codigo; em maquinas distintas, use o endereco do anfitriao.
4. Marque prontidao nos clientes e inicie pelo anfitriao.
5. Confirme que cada instancia mostra uma mao e um papel diferentes, sem revelar os dados privados dos demais.
6. Jogue ou descarte cartas ate o turno avancar e confirme a mesma revisao, mesa e contadores em todas as instancias.
7. Feche um cliente: a partida deve pausar e mostrar a espera de reconexao.
8. No cliente, use `RECONECTAR` dentro de 45 segundos e confirme o retorno ao mesmo papel e a mesma mao.
9. Repita a queda sem reconectar e confirme o encerramento depois da janela.

Para validar entre redes diferentes, encaminhe a porta UDP escolhida no roteador do anfitriao e repita o roteiro usando o endereco publico. Firewalls locais tambem precisam permitir o executavel e a porta.

## Verificacao automatizada

`AuthoritativeRoomTests` cobre:

- conclusao deterministica de tres rodadas para todos os tamanhos de sala, de 3 a 10;
- ausencia de papel, ouro e objetivo fechado de outras pessoas nos snapshots;
- idempotencia de comando aceito;
- rejeicao de revisao antiga e tentativa de usar outro assento;
- restauracao do estado privado apos reconexao;
- encerramento ao expirar a janela de reconexao.

O smoke test headless do Godot confirma que a cena principal e o adaptador ENet carregam no motor fixado em 4.7.2 .NET.
