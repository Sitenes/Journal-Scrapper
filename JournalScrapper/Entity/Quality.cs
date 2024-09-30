using System.ComponentModel.DataAnnotations;

namespace CSV2Sql.Models;

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
