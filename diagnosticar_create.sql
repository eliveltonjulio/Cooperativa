-- Diagnostico: total de cooperados e registros de teste.
SELECT COUNT(*) AS total_cooperados FROM "Cooperados";

SELECT "Nome", "CPF", "Email", "DataAdmissao"
FROM "Cooperados"
ORDER BY "DataAdmissao" DESC
LIMIT 6;
