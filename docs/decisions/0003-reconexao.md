# ADR 0003 - Pausa curta para reconexao

- Status: aceito para desenvolvimento
- Data: 2026-08-28

## Contexto

Substituir imediatamente uma pessoa por bot pode alterar uma partida social. Esperar indefinidamente permite que um abandono bloqueie a sala.

## Decisao

Ao perder um cliente, a partida entra em pausa por uma janela configuravel. O assento e o estado privado sao restaurados se a pessoa retornar. Ao expirar a janela, a sala encerra a partida no MVP.

## Consequencias

- Snapshots sanitizados precisam permitir reconexao idempotente.
- O lobby deve exibir claramente a contagem de reconexao.
- Substituicao por bot e migracao de host sao evolucoes futuras.

