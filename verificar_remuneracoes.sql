-- Verificacao pos-migracao da tabela "Remuneracoes".
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Remuneracoes'
ORDER BY ordinal_position;

SELECT "Id", "EmpresaId", "FuncaoId", "DataInicio", "DataFim", "Valor", "TipoRemuneracao"
FROM "Remuneracoes";
