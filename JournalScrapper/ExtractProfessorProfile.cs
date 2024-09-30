using CSV2Sql.Models;
using CsvHelper;
using CsvHelper.Configuration;
using JournalScrapper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using static JournalScrapper.Entity.Entity;

class ExtractProfessorProfile
{
    //دانشکده علوم تربیتی و روانشناسی
    static List<string> urls = new List<string>
        {
            "https://edu.ui.ac.ir/~m.esmaili",
            "https://edu.ui.ac.ir/~n.akrami",
            "https://edu.ui.ac.ir/~h.barati",
            "https://edu.ui.ac.ir/~m.tavakoli",
            "https://edu.ui.ac.ir/~sm.diba",
            "https://edu.ui.ac.ir/~h.samavatyan",
            "https://edu.ui.ac.ir/~s.r.tabaeian",
            "https://edu.ui.ac.ir/~dr.oreyzi",
            "https://edu.ui.ac.ir/~k.asgari",
            "https://edu.ui.ac.ir/~m.b.kaj",
            "https://edu.ui.ac.ir/~mehrdadk",
            "https://edu.ui.ac.ir/~sd.kalani",
            "https://edu.ui.ac.ir/~a.lashkari",
            "https://edu.ui.ac.ir/~a.mehrabi",
            "https://edu.ui.ac.ir/~h.mehrabi",
            "https://edu.ui.ac.ir/~h.neshat",
            "https://edu.ui.ac.ir/~a.sharifi",
            "https://edu.ui.ac.ir/~a.abedi",
            "https://edu.ui.ac.ir/~m.ashori",
            "https://edu.ui.ac.ir/~s.faramarzi",
            "https://edu.ui.ac.ir/~a.ghamarani",
            "https://edu.ui.ac.ir/~g.norouzi",
            "https://edu.ui.ac.ir/~a.akbari",
            "https://edu.ui.ac.ir/~m.pashootanizade",
            "https://edu.ui.ac.ir/~mo.sohrabi",
            "https://edu.ui.ac.ir/~m.rahmani",
            "https://edu.ui.ac.ir/~shabania",
            "https://edu.ui.ac.ir/~m.keshvari",
            "https://edu.ui.ac.ir/~m.ostani",
            "https://edu.ui.ac.ir/~a.mansouri",
            "https://edu.ui.ac.ir/~esfijani",
            "https://edu.ui.ac.ir/~n.dastjerdi",
            "https://edu.ui.ac.ir/~a.r.jamshidian",
            "https://edu.ui.ac.ir/~mh.heidari",
            "https://edu.ui.ac.ir/~h.davarpanah",
            "https://edu.ui.ac.ir/~reza.shavaran",
            "https://edu.ui.ac.ir/~f.sharifian",
            "https://edu.ui.ac.ir/~f.asanjarani",
            "https://edu.ui.ac.ir/~o.etemadi",
            "https://edu.ui.ac.ir/~s.jaberi",
            "https://edu.ui.ac.ir/~razvgaza",
            "https://edu.ui.ac.ir/~s.khanjani",
            "https://edu.ui.ac.ir/~y.doostian",
            "https://edu.ui.ac.ir/~f.samiee",
            "https://edu.ui.ac.ir/~y.abedini",
            "https://edu.ui.ac.ir/~sa.azimi",
            "https://edu.ui.ac.ir/~javad",
            "https://edu.ui.ac.ir/~l.moghtadaei",
            "https://edu.ui.ac.ir/~jafari",
            "https://edu.ui.ac.ir/~arnasr",
            "https://edu.ui.ac.ir/~r.norouzi",
            "https://edu.ui.ac.ir/~a.sadeghi",
            "https://edu.ui.ac.ir/~m.r.abedi",
            "https://edu.ui.ac.ir/~m.fatehizade",
            "https://edu.ui.ac.ir/~sl.mirahmadi",
            "https://edu.ui.ac.ir/~az.naghavi",
            "https://edu.ui.ac.ir/~p.nilforooshan",
            "https://edu.ui.ac.ir/~m.neyestani",
            "https://edu.ui.ac.ir/~m.nili.a",
            "https://edu.ui.ac.ir/~r.hoveida",
            "https://edu.ui.ac.ir/~r.boroujerdi"
        };

    public static async Task ScrapProfessorProfile()
    {
        var _context = new AppDbContext();
        foreach (var url in urls)
        {
            WebScraper.GetPageContent(url);

        }
    }
}
