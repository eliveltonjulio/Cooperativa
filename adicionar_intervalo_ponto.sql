ALTER TABLE "JornadaContratual"
    ADD COLUMN IF NOT EXISTS hora_inicio_intervalo time without time zone NULL,
    ADD COLUMN IF NOT EXISTS hora_fim_intervalo time without time zone NULL;

ALTER TABLE "RegistrosPonto"
    ADD COLUMN IF NOT EXISTS "InicioIntervalo" timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS "FimIntervalo" timestamp with time zone NULL;