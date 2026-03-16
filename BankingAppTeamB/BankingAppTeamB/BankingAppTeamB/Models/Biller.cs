using System;
namespace BankingAppTeamB.Models
{
    public class Biller
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
    }
}