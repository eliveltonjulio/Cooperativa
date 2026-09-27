-- Estrutura e restricoes da tabela "Cooperados".
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Cooperados'
ORDER BY ordinal_position;

SELECT conname, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = '"Cooperados"'::regclass;

SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'Cooperados';

SELECT COUNT(*) AS total_cooperados FROM "Cooperados";

SELECT "Id", "Nome", "CPF", "Email", "Ativo", "DataAdmissao"
FROM "Cooperados"
ORDER BY "DataAdmissao" DESC NULLS LAST
LIMIT 5;
