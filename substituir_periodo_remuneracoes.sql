-- Substitui a coluna "PeriodoReferencia" por "DataInicio" e "DataFim" na tabela "Remuneracoes".
DO $$
DECLARE
    total integer;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'Remuneracoes' AND column_name = 'PeriodoReferencia'
    ) THEN
        RAISE NOTICE 'Coluna PeriodoReferencia nao existe; nada a fazer.';
        RETURN;
    END IF;

    EXECUTE 'ALTER TABLE "Remuneracoes" ADD COLUMN IF NOT EXISTS "DataInicio" date';
    EXECUTE 'ALTER TABLE "Remuneracoes" ADD COLUMN IF NOT EXISTS "DataFim" date';

    EXECUTE 'SELECT COUNT(*) FROM "Remuneracoes"' INTO total;
    IF total > 0 THEN
        -- Migra registros existentes: o mes do antigo PeriodoReferencia vira a
        -- vigencia (inicio = primeiro dia do mes, fim = ultimo dia do mes).
        EXECUTE 'UPDATE "Remuneracoes" SET
                    "DataInicio" = date_trunc(''month'', "PeriodoReferencia")::date,
                    "DataFim" = (date_trunc(''month'', "PeriodoReferencia") + interval ''1 month - 1 day'')::date';
        RAISE NOTICE '% linha(s) migrada(s) a partir do PeriodoReferencia.', total;
    ELSE
        RAISE NOTICE 'Tabela vazia: sem dados para migrar.';
    END IF;

    EXECUTE 'ALTER TABLE "Remuneracoes" ALTER COLUMN "DataInicio" SET NOT NULL';
    EXECUTE 'ALTER TABLE "Remuneracoes" ALTER COLUMN "DataFim" SET NOT NULL';
    EXECUTE 'ALTER TABLE "Remuneracoes" DROP COLUMN "PeriodoReferencia"';
    RAISE NOTICE 'Substituicao concluida: PeriodoReferencia -> DataInicio/DataFim.';
END $$;