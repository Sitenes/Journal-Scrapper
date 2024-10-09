using System.ComponentModel.DataAnnotations;

namespace CSV2Sql.Models;

public class ISCJournal
{
    public int Id { get; set; }

    [Display(Name = "عنوان")]
    public string Title { get; set; }

    [Display(Name = "شاپا")]
    public string ISSN { get; set; }

    [Display(Name = "شاپای الکترونیکی")]
    public string EISSN { get; set; }

    [Display(Name = "زبان")]
    public string Language { get; set; }

    [Display(Name = "کشور")]
    public string Country { get; set; }

    [Display(Name = "استان")]
    public string Province { get; set; }

    [Display(Name = "ناشر")]
    public string Publisher { get; set; }

    public virtual ICollection<Year> Years { get; set; }
}
public class Quality
{
    public int Id { get; set; }

    [Display(Name = "Q")]
    public string Q { get; set; }

    [Display(Name = "نام")]
    public string Name { get; set; }

    public int YearId { get; set; }
    public Year Year { get; set; }
}

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
