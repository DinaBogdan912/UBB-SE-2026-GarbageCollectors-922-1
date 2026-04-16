namespace BankingAppTeamB.Models.DTOs
{
    public class ExchangeDto
    {
        public int UserId { get; set; }
        public int SourceAccountId { get; set; }
        public int TargetAccountId { get; set; }
        public required string SourceCurrency { get; set; }
        public required string TargetCurrency { get; set; }
        public decimal SourceAmount { get; set; }
        public decimal LockedRate { get; set; }
    }
}
