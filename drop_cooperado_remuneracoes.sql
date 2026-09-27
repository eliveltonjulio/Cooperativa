-- Remove o vínculo da tabela Remuneracoes com Cooperados (requisito:
-- remunerações identificadas somente por empresa e função).
DO $$
DECLARE
    r record;
BEGIN
    FOR r IN
        SELECT conname
        FROM pg_constraint
        WHERE conrelid = '"Remuneracoes"'::regclass
          AND contype = 'f'
          AND pg_get_constraintdef(oid) LIKE '%CooperadoId%'
    LOOP
        EXECUTE format('ALTER TABLE %I DROP CONSTRAINT %I', 'Remuneracoes', r.conname);
        RAISE NOTICE 'Removida constraint: %', r.conname;
    END LOOP;
END $$;

ALTER TABLE "Remuneracoes" DROP COLUMN IF EXISTS "CooperadoId";