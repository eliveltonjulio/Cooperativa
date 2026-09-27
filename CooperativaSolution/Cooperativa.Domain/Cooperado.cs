using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cooperativa.Models
{
    public class Cooperado
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [StringLength(120, ErrorMessage = "O nome completo deve ter até 120 caracteres.")]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [Display(Name = "Data de nascimento")]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(14, ErrorMessage = "O CPF deve ter até 14 caracteres.")]
        [RegularExpression(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$", ErrorMessage = "Informe o CPF no formato 000.000.000-00.")]
        [Display(Name = "CPF")]
        public string CPF { get; set; } = string.Empty;

        [Display(Name = "Nome do pai")]
        [StringLength(120, ErrorMessage = "O nome do pai deve ter até 120 caracteres.")]
        public string NomePai { get; set; } = string.Empty;

        [Display(Name = "Nome da mãe")]
        [Required(ErrorMessage = "O nome da mãe é obrigatório.")]
        [StringLength(120, ErrorMessage = "O nome da mãe deve ter até 120 caracteres.")]
        public string NomeMae { get; set; } = string.Empty;

        [Display(Name = "Naturalidade")]
        [StringLength(50, ErrorMessage = "A naturalidade deve ter até 50 caracteres.")]
        public string Naturalidade { get; set; } = string.Empty;

        [Display(Name = "Escolaridade")]
        [StringLength(60, ErrorMessage = "A escolaridade deve ter até 60 caracteres.")]
        public string Escolaridade { get; set; } = string.Empty;

        [Display(Name = "Nacionalidade")]
        [StringLength(50, ErrorMessage = "A nacionalidade deve ter até 50 caracteres.")]
        public string Nacionalidade { get; set; } = string.Empty;

        [Required(ErrorMessage = "O sexo é obrigatório.")]
        [StringLength(20, ErrorMessage = "O sexo deve ter até 20 caracteres.")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; } = string.Empty;

        [Display(Name = "Número da identidade")]
        [StringLength(20, ErrorMessage = "O número da identidade deve ter até 20 caracteres.")]
        public string NumeroIdentidade { get; set; } = string.Empty;

        [Display(Name = "Órgão emissor da identidade")]
        [StringLength(30, ErrorMessage = "O órgão emissor deve ter até 30 caracteres.")]
        public string OrgaoEmissorIdentidade { get; set; } = string.Empty;

        [Display(Name = "Certificado de reservista")]
        [StringLength(30, ErrorMessage = "O certificado de reservista deve ter até 30 caracteres.")]
        public string CertificadoReservista { get; set; } = string.Empty;

        [Display(Name = "Número do PIS")]
        [StringLength(20, ErrorMessage = "O número do PIS deve ter até 20 caracteres.")]
        public string NumeroPis { get; set; } = string.Empty;

        [Display(Name = "Título de eleitor")]
        [StringLength(20, ErrorMessage = "O título de eleitor deve ter até 20 caracteres.")]
        public string TituloEleitor { get; set; } = string.Empty;

        [Display(Name = "Zona eleitoral")]
        [StringLength(10, ErrorMessage = "A zona eleitoral deve ter até 10 caracteres.")]
        public string ZonaEleitoral { get; set; } = string.Empty;

        [Display(Name = "Seção eleitoral")]
        [StringLength(10, ErrorMessage = "A seção eleitoral deve ter até 10 caracteres.")]
        public string SecaoEleitoral { get; set; } = string.Empty;

        [Display(Name = "Número de dependentes (opcional)")]
        [Range(0, 50, ErrorMessage = "Informe um número de dependentes entre 0 e 50.")]
        public int? NumeroDependentes { get; set; }

        [Display(Name = "Estado civil")]
        [StringLength(30, ErrorMessage = "O estado civil deve ter até 30 caracteres.")]
        public string EstadoCivil { get; set; } = string.Empty;

        [Display(Name = "Nome do cônjuge")]
        [StringLength(120, ErrorMessage = "O nome do cônjuge deve ter até 120 caracteres.")]
        public string NomeConjuge { get; set; } = string.Empty;

        [Display(Name = "Regime de casamento")]
        [StringLength(60, ErrorMessage = "O regime de casamento deve ter até 60 caracteres.")]
        public string RegimeCasamento { get; set; } = string.Empty;

        [Display(Name = "Banco")]
        [StringLength(80, ErrorMessage = "O banco deve ter até 80 caracteres.")]
        public string Banco { get; set; } = string.Empty;

        [Display(Name = "Agência")]
        [StringLength(20, ErrorMessage = "A agência deve ter até 20 caracteres.")]
        public string Agencia { get; set; } = string.Empty;

        [Display(Name = "Conta corrente")]
        [StringLength(20, ErrorMessage = "A conta corrente deve ter até 20 caracteres.")]
        public string ContaCorrente { get; set; } = string.Empty;

        [Display(Name = "Chave PIX")]
        [StringLength(120, ErrorMessage = "A chave PIX deve ter até 120 caracteres.")]
        public string ChavePix { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe o e-mail em um formato válido (ex: nome@email.com).")]
        [StringLength(150, ErrorMessage = "O e-mail deve ter até 150 caracteres.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [RegularExpression(@"^\(\d{2}\) \d{4,5}-\d{4}$", ErrorMessage = "Informe o telefone no formato (00) 00000-0000.")]
        [StringLength(20, ErrorMessage = "O telefone deve ter até 20 caracteres.")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "O logradouro é obrigatório.")]
        [StringLength(200, ErrorMessage = "O logradouro deve ter até 200 caracteres.")]
        [Display(Name = "Logradouro")]
        public string Logradouro { get; set; } = string.Empty;

        [Required(ErrorMessage = "O bairro é obrigatório.")]
        [StringLength(80, ErrorMessage = "O bairro deve ter até 80 caracteres.")]
        [Display(Name = "Bairro")]
        public string Bairro { get; set; } = string.Empty;

        [Required(ErrorMessage = "A cidade é obrigatória.")]
        [StringLength(80, ErrorMessage = "A cidade deve ter até 80 caracteres.")]
        [Display(Name = "Cidade")]
        public string Cidade { get; set; } = string.Empty;

        /// <summary>Unidade federativa (UF) do endereço. Opcional: validada quando informada.</summary>
        [StringLength(2, ErrorMessage = "A UF deve ter 2 caracteres.")]
        [RegularExpression(@"^[A-Za-z]{2}$", ErrorMessage = "Informe a UF com 2 letras (ex.: MG).")]
        [Display(Name = "UF")]
        public string Uf { get; set; } = string.Empty;

        [Display(Name = "CEP")]
        [Required(ErrorMessage = "O CEP é obrigatório.")]
        [StringLength(10, ErrorMessage = "O CEP deve ter até 10 caracteres.")]
        [RegularExpression(@"^\d{2}\.?\d{3}-\d{3}$", ErrorMessage = "Informe o CEP no formato 35.000-000.")]
        public string CEP { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de admissão é obrigatória.")]
        [Display(Name = "Data de admissão")]
        [DataType(DataType.Date)]
        public DateTime DataAdmissao { get; set; }

        [Display(Name = "Declara imposto de renda")]
        public bool DeclaraImpostoRenda { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Equipe")]
        [StringLength(100, ErrorMessage = "A equipe deve ter até 100 caracteres.")]
        public string? Equipe { get; set; }

        public Guid? FuncaoId { get; set; }
        public Funcao? Funcao { get; set; }

        public Guid? ContratoId { get; set; }
        public Contrato? Contrato { get; set; }

        public ICollection<RegistroPonto> RegistrosPonto { get; set; } = new List<RegistroPonto>();
    }
}