using Cooperativa.Models;
using Microsoft.EntityFrameworkCore;

namespace Cooperativa.Data
{
    public class CooperativaDbContext : DbContext
    {
        public CooperativaDbContext(DbContextOptions<CooperativaDbContext> options)
            : base(options) { }

        public DbSet<Cooperado> Cooperados { get; set; } = null!;
        public DbSet<UsuarioSistema> Usuarios { get; set; } = null!;
        public DbSet<RegistroPonto> RegistrosPonto { get; set; } = null!;
        public DbSet<FolhaPagamento> FolhasPagamento { get; set; } = null!;
        public DbSet<Funcao> Funcoes { get; set; } = null!;
        public DbSet<Contrato> Contratos { get; set; } = null!;
        public DbSet<Alocacao> Alocacoes { get; set; } = null!;
        public DbSet<Remuneracao> Remuneracoes { get; set; } = null!;
        public DbSet<Usuario> UsuariosSistema { get; set; } = null!;
        public DbSet<TipoAdicional> TiposAdicionais { get; set; } = null!;
        public DbSet<ContratoFuncaoAdicional> ContratosFuncoesAdicionais { get; set; } = null!;
        public DbSet<DespesaFolha> DespesasFolha { get; set; } = null!;
        public DbSet<ApuracaoPontoMensal> ApuracoesPontoMensais { get; set; } = null!;
        public DbSet<Feriado> Feriados { get; set; } = null!;
        public DbSet<RegraHoraExtra> RegrasHoraExtra { get; set; } = null!;
        public DbSet<JornadaContratual> JornadasContratuais { get; set; } = null!;
        public DbSet<ConfiguracaoHoraNoturna> ConfiguracoesHoraNoturna { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TipoAdicional>(entity =>
            {
                entity.ToTable("TipoAdicional");
                entity.Property(a => a.Id).HasColumnName("id");
                entity.Property(a => a.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
                entity.Property(a => a.TipoCalculo).HasColumnName("tipo_calculo").HasMaxLength(30).IsRequired();
                entity.Property(a => a.Valor).HasColumnName("valor").HasPrecision(18, 2).IsRequired();
            });

            modelBuilder.Entity<ContratoFuncaoAdicional>(entity =>
            {
                entity.ToTable("ContratoFuncaoAdicional");
                entity.HasKey(a => new { a.ContratoId, a.FuncaoId, a.AdicionalId });
                entity.Property(a => a.ContratoId).HasColumnName("contrato_id");
                entity.Property(a => a.FuncaoId).HasColumnName("funcao_id");
                entity.Property(a => a.AdicionalId).HasColumnName("adicional_id");

                entity.HasOne(a => a.Contrato)
                    .WithMany()
                    .HasForeignKey(a => a.ContratoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Funcao)
                    .WithMany()
                    .HasForeignKey(a => a.FuncaoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Adicional)
                    .WithMany()
                    .HasForeignKey(a => a.AdicionalId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DespesaFolha>(entity =>
            {
                entity.ToTable("DespesaFolha");
                entity.Property(d => d.Id).HasColumnName("id");
                entity.Property(d => d.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
                entity.Property(d => d.TipoCalculo).HasColumnName("tipo_calculo").HasMaxLength(30).IsRequired();
                entity.Property(d => d.Valor).HasColumnName("valor").HasPrecision(18, 2).IsRequired();
                entity.Property(d => d.TetoRetencao).HasColumnName("teto_retencao").HasPrecision(18, 2);
                entity.Property(d => d.Ativo).HasColumnName("ativo").IsRequired();
            });

            modelBuilder.Entity<ApuracaoPontoMensal>(entity =>
            {
                entity.ToTable("ApuracaoPontoMensal");
                entity.HasKey(a => new { a.CooperadoId, a.MesAno });
                entity.Property(a => a.CooperadoId).HasColumnName("cooperado_id");
                entity.Property(a => a.MesAno).HasColumnName("mes_ano").HasColumnType("date");
                entity.Property(a => a.QtdHorasNormais).HasColumnName("qtd_horas_normais").HasPrecision(12, 2).IsRequired();
                entity.Property(a => a.QtdHorasExtras).HasColumnName("qtd_horas_extras").HasPrecision(12, 2).IsRequired();
                entity.Property(a => a.QtdHorasNoturnas).HasColumnName("qtd_horas_noturnas").HasPrecision(12, 2).IsRequired();

                entity.HasOne(a => a.Cooperado)
                    .WithMany()
                    .HasForeignKey(a => a.CooperadoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RegistroPonto>(entity =>
            {
                entity.Property(registro => registro.InicioIntervalo).HasColumnName("InicioIntervalo");
                entity.Property(registro => registro.FimIntervalo).HasColumnName("FimIntervalo");
                entity.Property(registro => registro.Ocorrencia).HasColumnName("Ocorrencia").HasMaxLength(20);
            });

            modelBuilder.Entity<Feriado>(entity =>
            {
                entity.ToTable("feriado");
                entity.Property(f => f.Id).HasColumnName("id");
                entity.Property(f => f.Data).HasColumnName("data").HasColumnType("date").IsRequired();
                entity.Property(f => f.Descricao).HasColumnName("descricao").HasMaxLength(100).IsRequired();
                entity.Property(f => f.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired().HasDefaultValue("NACIONAL");
                entity.HasIndex(f => f.Data).IsUnique();
            });

            modelBuilder.Entity<RegraHoraExtra>(entity =>
            {
                entity.ToTable("regra_hora_extra");
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.ContratoId).HasColumnName("contrato_id");
                entity.Property(r => r.PercentualHeComum).HasColumnName("percentual_he_comum").HasPrecision(5, 2).HasDefaultValue(50.00m).IsRequired();
                entity.Property(r => r.PercentualHeDomingoFeriado).HasColumnName("percentual_he_domingo_feriado").HasPrecision(5, 2).HasDefaultValue(100.00m).IsRequired();

                entity.HasOne(r => r.Contrato)
                    .WithMany()
                    .HasForeignKey(r => r.ContratoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<JornadaContratual>(entity =>
            {
                entity.ToTable("JornadaContratual");
                entity.Property(j => j.Id).HasColumnName("id");
                entity.Property(j => j.ContratoId).HasColumnName("contrato_id");
                entity.Property(j => j.FuncaoId).HasColumnName("funcao_id");
                entity.Property(j => j.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time without time zone").IsRequired();
                entity.Property(j => j.HoraFim).HasColumnName("hora_fim").HasColumnType("time without time zone").IsRequired();
                entity.Property(j => j.HoraInicioIntervalo).HasColumnName("hora_inicio_intervalo").HasColumnType("time without time zone");
                entity.Property(j => j.HoraFimIntervalo).HasColumnName("hora_fim_intervalo").HasColumnType("time without time zone");
                entity.Property(j => j.DiasSemana).HasColumnName("dias_semana").IsRequired();
                entity.HasIndex(j => new { j.ContratoId, j.FuncaoId });

                entity.HasOne(j => j.Contrato)
                    .WithMany()
                    .HasForeignKey(j => j.ContratoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(j => j.Funcao)
                    .WithMany()
                    .HasForeignKey(j => j.FuncaoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ConfiguracaoHoraNoturna>(entity =>
            {
                entity.ToTable("ConfiguracaoHoraNoturna");
                entity.Property(c => c.Id).HasColumnName("id");
                entity.Property(c => c.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time without time zone").IsRequired();
                entity.Property(c => c.HoraFim).HasColumnName("hora_fim").HasColumnType("time without time zone").IsRequired();
                entity.Property(c => c.HoraReduzida).HasColumnName("hora_reduzida").IsRequired();
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("UsuariosSistema");
                entity.Property(u => u.Id).HasColumnName("Id");
                entity.Property(u => u.Nome).HasColumnName("Nome").HasMaxLength(150);
                entity.Property(u => u.Email).HasColumnName("Email").HasMaxLength(150);
                entity.Property(u => u.Celular).HasColumnName("Celular").HasMaxLength(20);
                entity.Property(u => u.Login).HasColumnName("Login").HasMaxLength(50).IsRequired();
                entity.Property(u => u.Senha).HasColumnName("Senha").HasMaxLength(255).IsRequired();
                entity.Property(u => u.Perfil).HasColumnName("Perfil").HasMaxLength(50).IsRequired();
                entity.Property(u => u.Ativo).HasColumnName("Ativo").IsRequired();
                entity.Property(u => u.CooperadoId).HasColumnName("CooperadoId");
                entity.Property(u => u.ContratoId).HasColumnName("ContratoId");
                entity.Property(u => u.Cargo).HasColumnName("Cargo").HasMaxLength(100);
                entity.Property(u => u.Equipe).HasColumnName("Equipe").HasMaxLength(100);
                entity.Property(u => u.DataIngresso).HasColumnName("DataIngresso").HasColumnType("date");
                entity.Property(u => u.UltimoAcesso).HasColumnName("UltimoAcesso");
                entity.Property(u => u.MfaCodigoTemp).HasColumnName("MfaCodigoTemp").HasMaxLength(10);
                entity.Property(u => u.MfaCodigoExpiracao).HasColumnName("MfaCodigoExpiracao");
                entity.Property(u => u.TokenRedefinicaoSenha).HasColumnName("TokenRedefinicaoSenha").HasMaxLength(100);
                entity.Property(u => u.TokenRedefinicaoExpiracao).HasColumnName("TokenRedefinicaoExpiracao");

                entity.HasIndex(u => u.Login).IsUnique();

                entity.HasOne(u => u.Cooperado)
                    .WithMany()
                    .HasForeignKey(u => u.CooperadoId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.Contrato)
                    .WithMany()
                    .HasForeignKey(u => u.ContratoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Cooperado>()
                .HasIndex(c => c.CPF)
                .IsUnique();

            modelBuilder.Entity<Cooperado>()
                .HasOne(c => c.Contrato)
                .WithMany()
                .HasForeignKey(c => c.ContratoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Cooperado>()
                .HasOne(c => c.Funcao)
                .WithMany()
                .HasForeignKey(c => c.FuncaoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Alocacao>()
                .HasOne(a => a.Cooperado)
                .WithMany()
                .HasForeignKey(a => a.CooperadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Alocacao>()
                .HasOne(a => a.Contrato)
                .WithMany(c => c.Alocacoes)
                .HasForeignKey(a => a.ContratoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Alocacao>()
                .HasOne(a => a.Funcao)
                .WithMany(f => f.Alocacoes)
                .HasForeignKey(a => a.FuncaoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Remuneracao>(entity =>
            {
                entity.Property(r => r.DataInicio).HasColumnName("DataInicio").HasColumnType("date").IsRequired();
                entity.Property(r => r.DataFim).HasColumnName("DataFim").HasColumnType("date");
            });

            modelBuilder.Entity<Remuneracao>()
                .HasOne(r => r.Contrato)
                .WithMany()
                .HasForeignKey(r => r.ContratoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Remuneracao>()
                .HasOne(r => r.Funcao)
                .WithMany()
                .HasForeignKey(r => r.FuncaoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}