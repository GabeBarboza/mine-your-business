# ADR 0002 - Multiplayer privado e host autoritativo no MVP

- Status: aceito para desenvolvimento
- Data: 2026-08-28

## Contexto

Papeis, maos, mapas e recompensas sao secretos. Hot-seat compromete esses dados, enquanto contas, matchmaking e servidores dedicados aumentam muito o primeiro escopo.

## Decisao

O MVP usara salas privadas por convite. Uma instancia de jogador sera o host autoritativo: clientes enviam comandos, e o host valida regras e distribui projecoes de estado publicas e privadas.

## Consequencias

- O dominio precisa ser deterministico e independente do transporte.
- O cliente nunca recebe segredos de outros jogadores.
- Matchmaking publico e servidor dedicado ficam para depois do MVP.

