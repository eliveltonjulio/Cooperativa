-- Sanidade do filtro de competencia (mes/ano) sobre RegistrosPonto.
SELECT COUNT(*) AS dezembro_2025
FROM "RegistrosPonto"
WHERE "Data" >= '2025-12-01 00:00:00+00'
  AND "Data" < '2026-01-01 00:00:00+00';
