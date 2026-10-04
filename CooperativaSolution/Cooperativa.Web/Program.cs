using Cooperativa.Data;
using Cooperativa.Models;
using Cooperativa.Web.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var cwd = Directory.GetCurrentDirectory();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
});

var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

    // Somente usuários logados podem acessar o sistema.
    // As actions de autenticação (login, MFA e redefinição de senha) devem declarar [AllowAnonymous].
    options.Filters.Add(new AuthorizeFilter());
});

if (builder.Environment.IsDevelopment())
{
    // Permite que edições em views (.cshtml) sejam refletidas sem novo build/restart em ambiente de desenvolvimento.
    mvcBuilder.AddRazorRuntimeCompilation();
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("A connection string 'DefaultConnection' não foi encontrada.");
}

builder.Services.AddDbContext<CooperativaDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<UsuarioService>();
builder.Services.AddSingleton(_ => new MunicipiosService(MunicipiosService.LocalizarArquivoPadrao()));

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CooperativaDbContext>();

    try
    {
        dbContext.Database.Migrate();
        dbContext.Database.ExecuteSqlRaw("""
            ALTER TABLE "JornadaContratual"
                ADD COLUMN IF NOT EXISTS hora_inicio_intervalo time without time zone NULL,
                ADD COLUMN IF NOT EXISTS hora_fim_intervalo time without time zone NULL;

            ALTER TABLE "RegistrosPonto"
                ADD COLUMN IF NOT EXISTS "InicioIntervalo" timestamp with time zone NULL,
                ADD COLUMN IF NOT EXISTS "FimIntervalo" timestamp with time zone NULL,
                ADD COLUMN IF NOT EXISTS "Ocorrencia" character varying(20) NULL;

            ALTER TABLE "FolhasPagamento"
                ADD COLUMN IF NOT EXISTS "Detalhes" text NULL;

            ALTER TABLE "Contratos"
                ADD COLUMN IF NOT EXISTS "Logradouro" character varying(200) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Bairro" character varying(80) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Cidade" character varying(80) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Uf" character varying(2) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Cep" character varying(10) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Contato1Nome" character varying(120) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Contato1Telefone" character varying(20) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Contato2Nome" character varying(120) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Contato2Telefone" character varying(20) NOT NULL DEFAULT '';

            -- Funções: inclui Nome e CBO e desativa o antigo "ValorBase" (não usado pela aplicação).
            ALTER TABLE "Funcoes"
                ADD COLUMN IF NOT EXISTS "Nome" character varying(120) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Cbo" character varying(10) NOT NULL DEFAULT '';

            UPDATE "Funcoes" SET "Nome" = "Descricao" WHERE "Nome" = '';
            UPDATE "Funcoes" SET "Cbo" = '' WHERE "Cbo" IS NULL;

            -- Alinha vínculos legados que ainda apontavam para a antiga tabela Empresas,
            -- localizando o contrato correspondente pelo CNPJ.
            DO $$
            BEGIN
                IF to_regclass('"Empresas"') IS NOT NULL THEN
                    UPDATE "Remuneracoes" r
                       SET "ContratoId" = c."Id"
                      FROM "Empresas" e
                      JOIN "Contratos" c
                        ON upper(regexp_replace(c."Cnpj", '[^0-9A-Za-z]', '', 'g'))
                         = upper(regexp_replace(e."CNPJ", '[^0-9A-Za-z]', '', 'g'))
                     WHERE r."ContratoId" = e."Id"
                       AND NOT EXISTS (SELECT 1 FROM "Contratos" c2 WHERE c2."Id" = r."ContratoId");

                    UPDATE "JornadaContratual" j
                       SET contrato_id = c."Id"
                      FROM "Empresas" e
                      JOIN "Contratos" c
                        ON upper(regexp_replace(c."Cnpj", '[^0-9A-Za-z]', '', 'g'))
                         = upper(regexp_replace(e."CNPJ", '[^0-9A-Za-z]', '', 'g'))
                     WHERE j.contrato_id = e."Id"
                       AND NOT EXISTS (SELECT 1 FROM "Contratos" c2 WHERE c2."Id" = j.contrato_id);
                END IF;
            END $$;

            -- Remove remunerações sem contrato válido: não podem ser exibidas no cálculo nem editadas.
            DO $$
            BEGIN
                IF to_regclass('"Remuneracoes"') IS NOT NULL THEN
                    DELETE FROM "Remuneracoes" r
                     WHERE r."ContratoId" IS NULL
                        OR NOT EXISTS (SELECT 1 FROM "Contratos" c WHERE c."Id" = r."ContratoId");
                END IF;
            END $$;

            -- Remunerações: a data fim passa a ser opcional (vigência sem data de término).
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Remuneracoes' AND column_name = 'DataFim') THEN
                    ALTER TABLE "Remuneracoes" ALTER COLUMN "DataFim" DROP NOT NULL;
                END IF;

                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Alocacoes' AND column_name = 'DataFim') THEN
                    ALTER TABLE "Alocacoes" ALTER COLUMN "DataFim" DROP NOT NULL;
                END IF;

                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Contratos' AND column_name = 'DataFim') THEN
                    ALTER TABLE "Contratos" ALTER COLUMN "DataFim" DROP NOT NULL;
                END IF;
            END $$;

            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Funcoes' AND column_name = 'Cbo') THEN
                    ALTER TABLE "Funcoes" ALTER COLUMN "Cbo" SET DEFAULT '';
                    ALTER TABLE "Funcoes" ALTER COLUMN "Cbo" SET NOT NULL;
                END IF;

                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Funcoes' AND column_name = 'ValorBase') THEN
                    ALTER TABLE "Funcoes" ALTER COLUMN "ValorBase" DROP NOT NULL;
                END IF;
            END $$;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.tables 
                    WHERE table_name = 'Cooperados'
                ) THEN
                    ALTER TABLE "Cooperados"
                        ADD COLUMN IF NOT EXISTS "Equipe" character varying(100) NULL,
                        ADD COLUMN IF NOT EXISTS "equipe" character varying(100) NULL;
                END IF;
            END $$;

            -- Cooperados: "Endereco" passa a se chamar "Logradouro", inclui UF e o nº de dependentes fica opcional.
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cooperados' AND column_name = 'Endereco')
                   AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cooperados' AND column_name = 'Logradouro') THEN
                    ALTER TABLE "Cooperados" RENAME COLUMN "Endereco" TO "Logradouro";
                END IF;

                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cooperados' AND column_name = 'NumeroDependentes') THEN
                    ALTER TABLE "Cooperados" ALTER COLUMN "NumeroDependentes" DROP NOT NULL;
                END IF;
            END $$;

            ALTER TABLE "Cooperados"
                ADD COLUMN IF NOT EXISTS "Uf" character varying(2) NOT NULL DEFAULT '';

            -- Formata os telefones legados (somente dígitos) para (00) 00000-0000.
            UPDATE "Cooperados"
               SET "Telefone" = '(' || substr(regexp_replace("Telefone", '\D', '', 'g'), 1, 2) || ') '
                             || CASE WHEN length(regexp_replace("Telefone", '\D', '', 'g')) = 11
                                     THEN substr(regexp_replace("Telefone", '\D', '', 'g'), 3, 5) || '-' || substr(regexp_replace("Telefone", '\D', '', 'g'), 8, 4)
                                     ELSE substr(regexp_replace("Telefone", '\D', '', 'g'), 3, 4) || '-' || substr(regexp_replace("Telefone", '\D', '', 'g'), 7, 4)
                                END
             WHERE length(regexp_replace("Telefone", '\D', '', 'g')) IN (10, 11)
               AND "Telefone" NOT LIKE '(%';

            CREATE TABLE IF NOT EXISTS "UsuariosSistema" (
                "Id" uuid PRIMARY KEY,
                "Login" character varying(50) NOT NULL,
                "Senha" character varying(255) NOT NULL,
                "Perfil" character varying(50) NOT NULL,
                "Nome" character varying(150) NULL,
                "Email" character varying(150) NULL,
                "Celular" character varying(20) NULL,
                "Ativo" boolean NOT NULL DEFAULT true,
                "CooperadoId" uuid NULL,
                "ContratoId" uuid NULL,
                "Cargo" character varying(100) NULL,
                "Equipe" character varying(100) NULL,
                "DataIngresso" date NULL,
                "UltimoAcesso" timestamp with time zone NULL,
                "MfaCodigoTemp" character varying(10) NULL,
                "MfaCodigoExpiracao" timestamp with time zone NULL,
                "TokenRedefinicaoSenha" character varying(100) NULL,
                "TokenRedefinicaoExpiracao" timestamp with time zone NULL
            );

            ALTER TABLE "UsuariosSistema"
                ADD COLUMN IF NOT EXISTS "Nome" character varying(150) NULL,
                ADD COLUMN IF NOT EXISTS "Email" character varying(150) NULL,
                ADD COLUMN IF NOT EXISTS "Celular" character varying(20) NULL,
                ADD COLUMN IF NOT EXISTS "Ativo" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "CooperadoId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "ContratoId" uuid NULL,
                ADD COLUMN IF NOT EXISTS "Cargo" character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS "Equipe" character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS "DataIngresso" date NULL,
                ADD COLUMN IF NOT EXISTS "UltimoAcesso" timestamp with time zone NULL,
                ADD COLUMN IF NOT EXISTS "MfaCodigoTemp" character varying(10) NULL,
                ADD COLUMN IF NOT EXISTS "MfaCodigoExpiracao" timestamp with time zone NULL,
                ADD COLUMN IF NOT EXISTS "TokenRedefinicaoSenha" character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS "TokenRedefinicaoExpiracao" timestamp with time zone NULL;

            DO $$
            DECLARE
                r RECORD;
                tabela text;
                coluna_antiga text;
                coluna_nova text;
            BEGIN
                -- Remove os vínculos (FKs) legados com a tabela Empresas, que deixou de existir.
                IF to_regclass('"Empresas"') IS NOT NULL THEN
                    FOR r IN (
                        SELECT conrelid::regclass::text AS nome_tabela, conname
                        FROM pg_constraint
                        WHERE contype = 'f' AND confrelid = to_regclass('"Empresas"')
                    ) LOOP
                        EXECUTE format('ALTER TABLE %s DROP CONSTRAINT IF EXISTS %I', r.nome_tabela, r.conname);
                    END LOOP;
                END IF;

                -- Renomeia as colunas de empresa para contrato nas tabelas migradas.
                FOR tabela, coluna_antiga, coluna_nova IN
                    VALUES ('Cooperados', 'EmpresaId', 'ContratoId'),
                           ('Remuneracoes', 'EmpresaId', 'ContratoId'),
                           ('UsuariosSistema', 'EmpresaId', 'ContratoId'),
                           ('JornadaContratual', 'empresa_id', 'contrato_id'),
                           ('regra_hora_extra', 'empresa_id', 'contrato_id')
                LOOP
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = tabela AND column_name = coluna_antiga) THEN
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = tabela AND column_name = coluna_nova) THEN
                            -- As duas colunas existem (nova criada como nula): preserva os dados e remove a antiga.
                            EXECUTE format('UPDATE %I SET %I = %I WHERE %I IS NULL', tabela, coluna_nova, coluna_antiga, coluna_nova);
                            EXECUTE format('ALTER TABLE %I DROP COLUMN %I', tabela, coluna_antiga);
                        ELSE
                            EXECUTE format('ALTER TABLE %I RENAME COLUMN %I TO %I', tabela, coluna_antiga, coluna_nova);
                        END IF;
                    END IF;
                END LOOP;
            END $$;

            -- Vínculo de adicionais: a tabela legada por empresa passa a apontar para o contrato.
            DO $$
            BEGIN
                IF to_regclass('"ContratoFuncaoAdicional"') IS NULL
                   AND to_regclass('"ContratoEmpresaAdicional"') IS NOT NULL THEN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'ContratoEmpresaAdicional' AND column_name = 'empresa_id') THEN
                        ALTER TABLE "ContratoEmpresaAdicional" RENAME COLUMN empresa_id TO contrato_id;
                    END IF;

                    ALTER TABLE "ContratoEmpresaAdicional" RENAME TO "ContratoFuncaoAdicional";
                END IF;
            END $$;

            CREATE TABLE IF NOT EXISTS "ContratoFuncaoAdicional" (
                contrato_id uuid NOT NULL,
                funcao_id uuid NOT NULL,
                adicional_id uuid NOT NULL,
                CONSTRAINT "PK_ContratoFuncaoAdicional" PRIMARY KEY (contrato_id, funcao_id, adicional_id),
                CONSTRAINT "FK_ContratoFuncaoAdicional_Funcoes_funcao_id" FOREIGN KEY (funcao_id) REFERENCES "Funcoes" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ContratoFuncaoAdicional_TipoAdicional_adicional_id" FOREIGN KEY (adicional_id) REFERENCES "TipoAdicional" ("id") ON DELETE CASCADE
            );

            DO $$
            BEGIN
                -- Vínculos legados apontavam para a empresa: realinha para o contrato de mesmo CNPJ.
                IF to_regclass('"Empresas"') IS NOT NULL AND to_regclass('"ContratoFuncaoAdicional"') IS NOT NULL THEN
                    UPDATE "ContratoFuncaoAdicional" v
                       SET contrato_id = c."Id"
                      FROM "Empresas" e
                      JOIN "Contratos" c
                        ON upper(regexp_replace(c."Cnpj", '[^0-9A-Za-z]', '', 'g'))
                         = upper(regexp_replace(e."CNPJ", '[^0-9A-Za-z]', '', 'g'))
                     WHERE v.contrato_id = e."Id";
                END IF;

                -- Remove vínculos sem contrato correspondente (dados legados sem correspondência).
                IF to_regclass('"ContratoFuncaoAdicional"') IS NOT NULL THEN
                    DELETE FROM "ContratoFuncaoAdicional" v
                     WHERE NOT EXISTS (SELECT 1 FROM "Contratos" c WHERE c."Id" = v.contrato_id);
                END IF;

                -- Garante o vínculo (FK) com Contratos para que a exclusão do contrato limpe os adicionais.
                IF to_regclass('"ContratoFuncaoAdicional"') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1 FROM pg_constraint
                       WHERE conrelid = to_regclass('"ContratoFuncaoAdicional"')
                         AND contype = 'f'
                         AND confrelid = to_regclass('"Contratos"')
                   ) THEN
                    ALTER TABLE "ContratoFuncaoAdicional"
                        ADD CONSTRAINT "FK_ContratoFuncaoAdicional_Contratos_contrato_id"
                        FOREIGN KEY (contrato_id) REFERENCES "Contratos" ("Id") ON DELETE CASCADE;
                END IF;
            END $$;

            DO $$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN (
                    SELECT conname 
                    FROM pg_constraint 
                    WHERE conrelid = '"JornadaContratual"'::regclass 
                      AND contype = 'u'
                ) LOOP
                    EXECUTE 'ALTER TABLE "JornadaContratual" DROP CONSTRAINT IF EXISTS "' || r.conname || '" CASCADE';
                END LOOP;

                EXECUTE 'DROP INDEX IF EXISTS "IX_JornadaContratual_EmpresaId_FuncaoId" CASCADE';
                EXECUTE 'DROP INDEX IF EXISTS "IX_JornadaContratual_empresa_id_funcao_id" CASCADE';

                CREATE INDEX IF NOT EXISTS "IX_JornadaContratual_contrato_id_funcao_id" ON "JornadaContratual" (contrato_id, funcao_id);
            END $$;

            -- Unicidade de cadastro: nome de função único e uma remuneração por contrato/função.
            DO $$
            DECLARE
                duplicada RECORD;
                grupo RECORD;
                nome_base text;
                novo_nome text;
                sufixo int;
            BEGIN
                IF to_regclass('"Funcoes"') IS NOT NULL THEN
                    -- Normaliza o nome antes de validar a unicidade.
                    UPDATE "Funcoes" SET "Nome" = 'Função' WHERE "Nome" IS NULL;
                    UPDATE "Funcoes" SET "Nome" = btrim("Nome") WHERE "Nome" <> btrim("Nome");
                    UPDATE "Funcoes" SET "Nome" = 'Função' WHERE btrim("Nome") = '';

                    -- Renomeia os nomes repetidos (mantém o registro mais antigo) em vez de apagar cadastros.
                    FOR duplicada IN (
                        SELECT f."Id", f."Nome" AS nome_base
                        FROM "Funcoes" f
                        WHERE EXISTS (
                            SELECT 1 FROM "Funcoes" f2
                            WHERE lower(btrim(f2."Nome")) = lower(btrim(f."Nome"))
                              AND f2."Id" < f."Id")
                        ORDER BY f."Id"
                    ) LOOP
                        nome_base := left(coalesce(duplicada.nome_base, 'Função'), 110);
                        sufixo := 2;

                        LOOP
                            novo_nome := nome_base || ' (' || sufixo || ')';
                            EXIT WHEN NOT EXISTS (
                                SELECT 1 FROM "Funcoes" f3
                                WHERE lower(btrim(f3."Nome")) = lower(novo_nome));
                            sufixo := sufixo + 1;
                        END LOOP;

                        UPDATE "Funcoes" SET "Nome" = novo_nome WHERE "Id" = duplicada."Id";
                    END LOOP;

                    IF EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = current_schema()
                          AND indexname = 'IX_Funcoes_Nome'
                          AND indexdef NOT LIKE 'UNIQUE%')
                    THEN
                        DROP INDEX "IX_Funcoes_Nome";
                    END IF;

                    -- Mesma regra do cadastro: ignora maiúsculas/minúsculas e espaços nas pontas.
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Funcoes_Nome"
                        ON "Funcoes" (lower(btrim("Nome")));
                END IF;

                IF to_regclass('"Remuneracoes"') IS NOT NULL THEN
                    -- Guarda apenas a vigência mais recente de cada contrato/função antes de criar o índice único.
                    FOR grupo IN (
                        SELECT "ContratoId", "FuncaoId"
                        FROM "Remuneracoes"
                        GROUP BY "ContratoId", "FuncaoId"
                        HAVING count(*) > 1
                    ) LOOP
                        DELETE FROM "Remuneracoes" r
                         WHERE r."ContratoId" = grupo."ContratoId"
                           AND r."FuncaoId" = grupo."FuncaoId"
                           AND r."Id" <> (
                               SELECT r2."Id" FROM "Remuneracoes" r2
                               WHERE r2."ContratoId" = grupo."ContratoId"
                                 AND r2."FuncaoId" = grupo."FuncaoId"
                               ORDER BY r2."DataInicio" DESC NULLS LAST, r2."Id"
                               LIMIT 1);
                    END LOOP;

                    IF EXISTS (
                        SELECT 1 FROM pg_indexes
                        WHERE schemaname = current_schema()
                          AND indexname = 'IX_Remuneracoes_ContratoId_FuncaoId'
                          AND indexdef NOT LIKE 'UNIQUE%')
                    THEN
                        DROP INDEX "IX_Remuneracoes_ContratoId_FuncaoId";
                    END IF;

                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Remuneracoes_ContratoId_FuncaoId"
                        ON "Remuneracoes" ("ContratoId", "FuncaoId");
                END IF;
            END $$;
            """);

        // Seed admin padrão se não houver
        var adminExistente = dbContext.UsuariosSistema.FirstOrDefault(u => u.Login.ToLower() == "admin");
        if (adminExistente == null)
        {
            dbContext.UsuariosSistema.Add(new Usuario
            {
                Id = Guid.NewGuid(),
                Nome = "Administrador do Sistema",
                Login = "admin",
                Email = "admin@cooperativa.com.br",
                Celular = "11999999999",
                Senha = BCrypt.Net.BCrypt.HashPassword("123456"),
                Perfil = "Administrador",
                Ativo = true,
                Cargo = "Administração Geral",
                DataIngresso = DateTime.UtcNow.Date
            });
            dbContext.SaveChanges();
        }
        else if (adminExistente.Perfil != "Administrador")
        {
            adminExistente.Perfil = "Administrador";
            dbContext.SaveChanges();
        }

        app.Logger.LogInformation("Banco de dados migrado e inicializado com sucesso na inicialização.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Ocorreu um erro ao migrar o banco de dados.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

// app.UseHttpsRedirection(); // Descomente se quiser forçar HTTPS
app.UseStaticFiles(); // Serve arquivos de wwwroot (e.g., /css/site.css, /js/site.js)

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Text("web-program-marker-2026"));
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
