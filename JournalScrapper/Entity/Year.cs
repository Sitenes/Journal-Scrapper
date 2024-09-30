using System.ComponentModel.DataAnnotations;

namespace CSV2Sql.Models;


public class Year
{
    public int Id { get; set; }

    [Display(Name = "ضریب تاثیر")]
    public string ImpactFactor { get; set; }

    [Display(Name = "سال")]
    public string YearPublished { get; set; }

    [Display(Name = "استنادهای تجمعی")]
    public string CumulativeCitations { get; set; }

    [Display(Name = "ضريب تاثير آنی")]
    public string ImmediateImpactFactor { get; set; }

    public int JournalId { get; set; }
    public virtual ISCJournal Journal { get; set; }
    

    public virtual ICollection<Quality> Qualities { get; set; }
}
