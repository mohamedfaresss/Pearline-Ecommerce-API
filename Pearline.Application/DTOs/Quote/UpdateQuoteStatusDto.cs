using Pearline.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using Pearline.Domain.Entities;

namespace Pearline.Application.DTOs.Quote
{
    public class UpdateQuoteStatusDto
    {
        [Required]
        public QuoteStatus Status { get; set; }
    }
}
