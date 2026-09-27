-- Confere se o registro de teste foi gravado.
SELECT "Nome", "CPF", "Sexo", "Cidade", "Bairro", "CEP", "NumeroPis"
FROM "Cooperados"
WHERE "Nome" LIKE 'Teste Repro%';
