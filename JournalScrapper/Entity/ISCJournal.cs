using OpenQA.Selenium.BiDi.Log;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSV2Sql.Models;

public class Qurtile
{
    public int Id { get; set; }

    [Display(Name = "Q")]
    public int QLevel { get; set; }
     
    public int JournalCategoryId { get; set; }
    public ScopusJournalCategory JournalCategory { get; set; } = null!;

    public int YearId { get; set; }
    public Year Year { get; set; } = null!;
}

public class Year
{
    public int Id { get; set; }

    [Display(Name = "ضریب تاثیر")]
    public string ImpactFactor { get; set; }  = "";

    [Display(Name = "سال")]
    public string YearPublished { get; set; }  = "";

    [Display(Name = "استنادهای تجمعی")]
    public string CumulativeCitations { get; set; }  = "";

    [Display(Name = "ضريب تاثير آنی")]
    public string ImmediateImpactFactor { get; set; }  = "";

    [Display(Name = "ضريب خود استنادی")]
    public string SelfCitationFactor { get; set; } = "";

    [Display(Name = "ضریب تاثیر بدون خوداستنادی")]
    public string ImpactFactorWithoutSelfCitation { get; set; } = "";

    [Display(Name = "وضعیت نشریه (هسته ، در انتظار تایید ، لیست اولیه)")]
    public string JournalStatus { get; set; } = "";

    [Display(Name = "h index")]
    public string HIndex { get; set; } = "";

    public int JournalId { get; set; }
    [ForeignKey(nameof(JournalId))]
    public Journal Journal { get; set; } = null!;


    public virtual ICollection<Qurtile> Qualities { get; set; } = new List<Qurtile>();
}
public class Journal
{
    [Key]
    public int Id { get; set; }

    public string? Sourceid { get; set; }

    public string? Title_EN { get; set; }
    public string? Title_Fa { get; set; }

    public string? Type { get; set; }

    public string? ISSN { get; set; }
    public string? EISSN { get; set; }

    public string? URL { get; set; }

    public string? Language { get; set; }

    public string? Publisher { get; set; }

    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }

    public string? MacroLevelIssue { get; set; }
    public string? IntermediateLevelIssue { get; set; }
    public string? MicroLevelIssue { get; set; }

    public string? CoverageStartYear { get; set; }
    public string? CoverageEndYear { get; set; }

    public DateTime? LastUpdate { get; set; }

    public virtual ICollection<Year> Years { get; set; } = new List<Year>();
    //public virtual ICollection<Article> Articles { get; set; } = new List<Article>();

    //public virtual ICollection<ScopusJournalDetail> ScopusJournalDetails { get; set; } = new List<ScopusJournalDetail>();
    //public virtual ICollection<WOSJournalDetail> WOSJournalDetails { get; set; } = new List<WOSJournalDetail>();
    //public virtual ICollection<WOSJournalCategory> WOSJournalCategories { get; set; } = new List<WOSJournalCategory>();
    //public virtual ICollection<ScopusJournalCategoryRelation> JournalCategoryRelations { get; set; } = new List<ScopusJournalCategoryRelation>();
}
public class ScopusJournalCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SourceId { get; set; }

    //public virtual ICollection<ScopusCategorySnip> CategorySnips { get; set; } = new List<ScopusCategorySnip>();

    //public virtual ICollection<ScopusCategoryRank> CategoryRank { get; set; } = new List<ScopusCategoryRank>();

    public int? SubjectAreaId { get; set; }

    public virtual ScopusSubjectArea? SubjectArea { get; set; } = null!;
}


public class ScopusSubjectArea
{
    [Key]
    public int Id { get; set; }
    public string? Name { get; set; }
    public int SourceId { get; set; }
    public List<ScopusJournalCategory> JournalCategories { get; set; } = null!;
}

