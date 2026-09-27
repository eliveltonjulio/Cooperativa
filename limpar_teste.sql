-- Remove registros criados durante os testes automatizados de reproducao.
DELETE FROM "Cooperados" WHERE "Nome" LIKE 'Teste Repro%';
DELETE FROM "Cooperados" WHERE "Nome" LIKE 'Diag Repro%';
