using CSV2Sql.Models;
using CsvHelper;
using CsvHelper.Configuration;
using JournalScrapper;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using Profile_Shakhsi.Models.Entity;
using System.Globalization;
using System.Text.RegularExpressions;
using static JournalScrapper.Entity.Entity;
using static Profile_Shakhsi.Models.Entity.Profile;
using static System.Net.Mime.MediaTypeNames;
using static System.Reflection.Metadata.BlobBuilder;

class ExtractProfessorProfile
{
    //دانشکده علوم تربیتی و روانشناسی
    //static List<string> urls = new List<string>
    //    {
    //        "https://ase.ui.ac.ir/~n_akbari",
    //"https://ase.ui.ac.ir/~h.amiri",
    //"https://ase.ui.ac.ir/~i.bastanifar",
    //"https://ase.ui.ac.ir/~r.bakhshi",
    //"https://ase.ui.ac.ir/~l.torki",
    //"https://ase.ui.ac.ir/~m.heidari",
    //"https://ase.ui.ac.ir/~m.rafat",
    //"https://ase.ui.ac.ir/~alimorad",
    //"https://ase.ui.ac.ir/~b_saffari",
    //"https://ase.ui.ac.ir/~s.samadi",
    //"https://ase.ui.ac.ir/~m.toghyani",
    //"https://ase.ui.ac.ir/~sk.tayebi",
    //"https://ase.ui.ac.ir/~sh.farahmand",
    //"https://ase.ui.ac.ir/~gh.kiani",
    //"https://ase.ui.ac.ir/~a.googerdchian",
    //"https://ase.ui.ac.ir/~a.h.mohammadi",
    //"https://ase.ui.ac.ir/~sh.moeeni",
    //"https://ase.ui.ac.ir/~r.moayedfar",
    //"https://ase.ui.ac.ir/~vaez",
    //"https://ase.ui.ac.ir/~renani",
    //"https://ase.ui.ac.ir/~n.izadinia",
    //"https://ase.ui.ac.ir/~a.hajiannejad",
    //"https://ase.ui.ac.ir/~n.hamidian",
    //"https://ase.ui.ac.ir/~a.rostami",
    //"https://ase.ui.ac.ir/~a.rahrovi",
    //"https://ase.ui.ac.ir/~h.fattahi",
    //"https://ase.ui.ac.ir/~foroghi",
    //"https://ase.ui.ac.ir/~a.hashemi",
    //"https://ase.ui.ac.ir/~a.arashpour",
    //"https://ase.ui.ac.ir/~m.alsharif",
    //"https://ase.ui.ac.ir/~hpoorbafrani",
    //"https://ase.ui.ac.ir/~tavassoli",
    //"https://ase.ui.ac.ir/~m.jalali",
    //"https://ase.ui.ac.ir/~gh.khosroshahi",
    //"https://ase.ui.ac.ir/~m.shadmanfar",
    //"https://ase.ui.ac.ir/~m_shahabi",
    //"https://ase.ui.ac.ir/~tabatabaei",
    //"https://ase.ui.ac.ir/~m.tabibi",
    //"https://ase.ui.ac.ir/~fasihizadeh",
    //"https://ase.ui.ac.ir/~m.ghaemfard",
    //"https://ase.ui.ac.ir/~gholizadeh",
    //"https://ase.ui.ac.ir/~a.maghami",
    //"https://ase.ui.ac.ir/~m.mansouri.b",
    //"https://ase.ui.ac.ir/~gh.noroozi",
    //"https://ase.ui.ac.ir/~a.yazdanian",
    //"https://ase.ui.ac.ir/~sh.ebrahimi",
    //"https://ase.ui.ac.ir/~a.aghahosseini",
    //"https://ase.ui.ac.ir/~aliomidi",
    //"https://ase.ui.ac.ir/~basiri",
    //"https://ase.ui.ac.ir/~a.hatami",
    //"https://ase.ui.ac.ir/~gh.۱۲emami",
    //"https://ase.ui.ac.ir/~h.roohani",
    //"https://ase.ui.ac.ir/~a.samiee",
    //"https://ase.ui.ac.ir/~f.shayan",
    //"https://ase.ui.ac.ir/~m.shahramnia",
    //"https://ase.ui.ac.ir/~a.alihosseini",
    //"https://ase.ui.ac.ir/~h.masoudnia",
    //"https://ase.ui.ac.ir/~h.nassaj",
    //"https://ase.ui.ac.ir/~harsij",
    //"https://ase.ui.ac.ir/~s.vosoughi",
    //"https://ase.ui.ac.ir/~m.esmaelian",
    //"https://ase.ui.ac.ir/~r.ansari",
    //"https://ase.ui.ac.ir/~a.ansari",
    //"https://ase.ui.ac.ir/~m.botshekan",
    //"https://ase.ui.ac.ir/~h.teimouri",
    //"https://ase.ui.ac.ir/~s.jahanyan",
    //"https://ase.ui.ac.ir/~shahin",
    //"https://ase.ui.ac.ir/~shaemi",
    //"https://ase.ui.ac.ir/~r.salehzadeh",
    //"https://ase.ui.ac.ir/~a.safari",
    //"https://ase.ui.ac.ir/~a_sanayei",
    //"https://ase.ui.ac.ir/~dr_allameh",
    //"https://ase.ui.ac.ir/~m.ghandehari",
    //"https://ase.ui.ac.ir/~alik",
    //"https://ltr.ui.ac.ir/~aa.ahmadi",
    //"https://ltr.ui.ac.ir/~t.ejieh",
    //"https://ltr.ui.ac.ir/~h.aghahosaini",
    //"https://ltr.ui.ac.ir/~m.algone",
    //"https://ltr.ui.ac.ir/~mbk",


    //        //"https://edu.ui.ac.ir/~m.esmaili",
    //        //"https://edu.ui.ac.ir/~n.akrami",
    //        //"https://edu.ui.ac.ir/~h.barati",
    //        //"https://edu.ui.ac.ir/~m.tavakoli",
    //        //"https://edu.ui.ac.ir/~sm.diba",
    //        //"https://edu.ui.ac.ir/~h.samavatyan",
    //        //"https://edu.ui.ac.ir/~s.r.tabaeian",
    //        //"https://edu.ui.ac.ir/~dr.oreyzi",
    //        //"https://edu.ui.ac.ir/~k.asgari",
    //        //"https://edu.ui.ac.ir/~m.b.kaj",
    //        //"https://edu.ui.ac.ir/~mehrdadk",
    //        //"https://edu.ui.ac.ir/~sd.kalani",
    //        //"https://edu.ui.ac.ir/~a.lashkari",
    //        //"https://edu.ui.ac.ir/~a.mehrabi",
    //        //"https://edu.ui.ac.ir/~h.mehrabi",
    //        //"https://edu.ui.ac.ir/~h.neshat",
    //        //"https://edu.ui.ac.ir/~a.sharifi",
    //        //"https://edu.ui.ac.ir/~a.abedi",
    //        //"https://edu.ui.ac.ir/~m.ashori",
    //        //"https://edu.ui.ac.ir/~s.faramarzi",
    //        //"https://edu.ui.ac.ir/~a.ghamarani",
    //        //"https://edu.ui.ac.ir/~g.norouzi",
    //        //"https://edu.ui.ac.ir/~a.akbari",
    //        //"https://edu.ui.ac.ir/~m.pashootanizade",
    //        //"https://edu.ui.ac.ir/~mo.sohrabi",
    //        //"https://edu.ui.ac.ir/~m.rahmani",
    //        //"https://edu.ui.ac.ir/~shabania",
    //        //"https://edu.ui.ac.ir/~m.keshvari",
    //        //"https://edu.ui.ac.ir/~m.ostani",
    //        //"https://edu.ui.ac.ir/~a.mansouri",
    //        //"https://edu.ui.ac.ir/~esfijani",
    //        //"https://edu.ui.ac.ir/~n.dastjerdi",
    //        //"https://edu.ui.ac.ir/~a.r.jamshidian",
    //        //"https://edu.ui.ac.ir/~mh.heidari",
    //        //"https://edu.ui.ac.ir/~h.davarpanah",
    //        //"https://edu.ui.ac.ir/~reza.shavaran",
    //        //"https://edu.ui.ac.ir/~f.sharifian",
    //        //"https://edu.ui.ac.ir/~f.asanjarani",
    //        //"https://edu.ui.ac.ir/~o.etemadi",
    //        //"https://edu.ui.ac.ir/~s.jaberi",
    //        //"https://edu.ui.ac.ir/~razvgaza",
    //        //"https://edu.ui.ac.ir/~s.khanjani",
    //        //"https://edu.ui.ac.ir/~y.doostian",
    //        //"https://edu.ui.ac.ir/~f.samiee",
    //        //"https://edu.ui.ac.ir/~y.abedini",
    //        //"https://edu.ui.ac.ir/~sa.azimi",
    //        //"https://edu.ui.ac.ir/~javad",
    //        //"https://edu.ui.ac.ir/~l.moghtadaei",
    //        //"https://edu.ui.ac.ir/~jafari",
    //        //"https://edu.ui.ac.ir/~arnasr",
    //        //"https://edu.ui.ac.ir/~r.norouzi",
    //        //"https://edu.ui.ac.ir/~a.sadeghi",
    //        //"https://edu.ui.ac.ir/~m.r.abedi",
    //        //"https://edu.ui.ac.ir/~m.fatehizade",
    //        //"https://edu.ui.ac.ir/~sl.mirahmadi",
    //        //"https://edu.ui.ac.ir/~az.naghavi",
    //        //"https://edu.ui.ac.ir/~p.nilforooshan",
    //        //"https://edu.ui.ac.ir/~m.neyestani",
    //        //"https://edu.ui.ac.ir/~m.nili.a",
    //        //"https://edu.ui.ac.ir/~r.hoveida",
    //        //"https://edu.ui.ac.ir/~r.boroujerdi"
    //    };
    static string input = "https://spr.ui.ac.ir/~v.minasian\r\n\r\nhttps://spr.ui.ac.ir/~f.esfarjani\r\n\tزينب\tرضايي\tمشاهده\tz.rezaee[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~z.rezaee\r\n\tجليل\tرئيسي\tمشاهده\tj.reisi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~j.reisi\r\n\tزهره\tشانظري\tمشاهده\tz.shanazari[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~z.shanazari\r\n\tمحمد\tفرامرزي\tمشاهده\tm.faramarzi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.faramarzi\r\n\tمهدي\tكارگرفرد\tمشاهده\tm.kargarfard[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.kargarfard\r\n\tسيدمحمد\tمرندي\tمشاهده\ts.m.marandi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~s.m.marandi\r\n\r\nhttps://spr.ui.ac.ir/~h.esmaeili\r\n\tنادر\tرهنما\tمشاهده\tn.rahnama[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~n.rahnama\r\n\tمرتضي\tصادقي ورنوسفادراني\tمشاهده\tm.sadeghi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.sadeghi\r\n\tغلامعلي\tقاسمي كهريزسنگي\tمشاهده\tgh.ghasemi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~gh.ghasemi\r\n\tشهرام\tلنجان نژاديان اصفهان\tمشاهده\tsh.lenjani[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~sh.lenjani\r\n\tرضا\tمهدوي نژاد\tمشاهده\tr.mahdavinejad[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~r.mahdavinejad\r\n\r\naa.asefi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~aa.asefi\r\n\tمهدي\tرافعي بروجني\tمشاهده\tm.rafei[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.rafei\r\n\tمحمد\tسلطان حسيني\tمشاهده\tm.soltanhoseini[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.soltanhoseini\r\n\tمهدي\tسليمي\tمشاهده\tm.salimi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.salimi\r\n\tحميد\tصالحي\tمشاهده\tsalehi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~salehi\r\n\tاحمد رضا\tموحدي\tمشاهده\ta.movahedi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~a.movahedi\r\n\tحميدرضا\tميرصفيان\tمشاهده\th.mirsafian[at]spr.ui.ac.ir\r\n\r\nm.naderian[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.naderian\r\n\tمريم\tنزاكت الحسيني\tمشاهده\tnezakat[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~nezakat\r\n\tمحسن\tوحداني\tمشاهده\tm.vahdani[at]spr.ui.ac.ir\r\n\r\nhttps://sci.ui.ac.ir/~rasajl\r\n\tزهرا\tاعلمي نيا\tمشاهده\tz.alaminia[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~z.alaminia\r\n\tهاشم\tباقري\tمشاهده\thm.bagheri[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~hm.bagheri\r\n\tعلي\tبهرامي\tمشاهده\ta.bahrami[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.bahrami\r\n\tحميدرضا\tپاكزاد\tمشاهده\thpakzad[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~hpakzad\r\n\tمهرداد\tپسندي\tمشاهده\tm.pasandi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.pasandi\r\n\tميثم\tتدين\tمشاهده\tm.tadayon[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.tadayon\r\nhttps://sci.ui.ac.ir/~torabighodrat\r\n\tحمايت\tجمالي\tمشاهده\th.jamali[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~h.jamali\r\n\tمحمدعلي\tصالحي\tمشاهده\tma.salehi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ma.salehi\r\n\tامرا\tصفري\tمشاهده\tsafari[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~safari\r\n\tسيدمحسن\tطباطبائي منش\tمشاهده\ttabataba[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~tabataba\r\n\tعلي\tفرضي پورصائين\tمشاهده\ta.farzipour[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.farzipour\r\n\tاكبر\tقاضي فرد\tمشاهده\ta.ghazifard[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.ghazifard\r\nhttps://sci.ui.ac.ir/~m.morsali\r\n\tمحمدعلي\tمكي زاده\tمشاهده\tma.makizadeh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ma.makizadeh\r\n\tعليرضا\tنديمي شهركي\tمشاهده\ta.nadimi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.nadimi\r\n\tمرتضي\tهاشمي\tمشاهده\tm-hashemi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m-hashemi\r\n\tحسين\tوزيري مقدم\tمشاهده\thvaziri[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~hvaziri\r\nhttps://geo.ui.ac.ir/~m.entezari\r\n\tعليرضا\tتقيان\tمشاهده\ta.taghian[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~a.taghian\r\n\tرضا\tذاكري نژاد\tمشاهده\tr.zakerinejad[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~r.zakerinejad\r\n\tمحمدحسين\tرامشت\tمشاهده\tm.h.ramesht[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~m.h.ramesht\r\n\tداريوش\tرحيمي\tمشاهده\td.rahimi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~d.rahimi\r\n\tرحمان\tزندي دره غريبي\tمشاهده\tr.zandi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~r.zandi\r\n\tفاطمه\tسبك خيز\tمشاهده\tf.sabokkhiz[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~f.sabokkhiz\r\na.seif[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~a.seif\r\n\tمحمدصادق\tكيخسروي كياني لنباني\tمشاهده\tms.keikhosravikiany[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~ms.keikhosravikiany\r\n\tسيدابوالفضل\tمسعوديان\tمشاهده\ts.a.masoodian[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~s.a.masoodian\r\n\tمجيد\tمنتظري\tمشاهده\tm.montazeri[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~m.montazeri\r\n\tسعيد\tموحدي\tمشاهده\ts.movahedi[at]geo.ui.ac.ir\r\nm.taghvaei[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~m.taghvaei\r\n\tاميررضا\tخاوريان گرمسير\tمشاهده\ta.khavarian[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~a.khavarian\r\n\tعلي\tزنگي آبادي\tمشاهده\ta.zangiabadi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~a.zangiabadi\r\n\tعلي\tصادقي\tمشاهده\talisadeghi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~alisadeghi\r\n\tجمال\tمحمدي سيداحمدياني\tمشاهده\tj.mohammadi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~j.mohammadi\r\n\tرضا\tمختاري ملك آبادي\tمشاهده\tr.mokhtari[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~r.mokhtari\r\n\tحميدرضا\tوارثي\tمشاهده\th.varesi[at]geo.ui.ac.ir\r\nA.amini[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~A.amini\r\n\tحميد\tبرقي\tمشاهده\th.barghi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~h.barghi\r\n\tاحمد\tتقديسي\tمشاهده\ta.taghdisi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~a.taghdisi\r\n\tسيده سميه\tحسيني\tمشاهده\tss.hosseini[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~ss.hosseini\r\n\tحجت اله\tصادقي نعل كناني\tمشاهده\th.sadeghi[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~h.sadeghi\r\n\tسيداسكندر\tصيدائي گل سفيدي\tمشاهده\ts.seidiy[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~s.seidiy\r\n\tمجيد\tغياث\tمشاهده\tdr.ghias[at]yahoo.com\r\ny.ghanbari[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~y.ghanbari\r\n\tحسين\tمختاري هشي\tمشاهده\th.mokhtari[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~h.mokhtari\r\n\tسيدهدايت ا\tنوري زمان آبادي\tمشاهده\th.nouri[at]geo.ui.ac.ir\thttps://geo.ui.ac.ir/~h.nouri\r\n\tنرگس\tوزين\tمشاهده\tn.vazin[at]geo.ui.ac.ir\r\nadibi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~adibi\r\n\tحميدرضا\tبرادران كاشاني\tمشاهده\thrb.kashani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~hrb.kashani\r\n\tحسين\tكارشناس نجف آبادي\tمشاهده\th.karshenas[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.karshenas\r\n\tحسين\tماهوش محمدي\tمشاهده\th.mahvash[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.mahvash\r\n\tاحمدرضا\tنقش نيلچي\tمشاهده\tnilchi[at]eng.ui.ac.ir\r\nm.ezhei[at]comp.ui.ac.ir\thttps://comp.ui.ac.ir/~m.ezhei\r\n\tبهروز\tترك لاداني\tمشاهده\tladani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~ladani\r\n\tمحمدرضا\tخيام باشي\tمشاهده\tm.r.khayyambashi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.r.khayyambashi\r\n\tرضا\tرمضاني\tمشاهده\tr.ramezani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~r.ramezani\r\n\tزهرا\tزجاجي\tمشاهده\tz.zojaji[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~z.zojaji\r\n\tمحمدرضا\tشعرباف\tمشاهده\tm.sharbaf[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.sharbaf\r\n\tآرش\tشفيعي\tمشاهده\ta.shafiei[at]comp.ui.ac.ir\r\na_fatemi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a_fatemi\r\n\tمحمدعلي\tنعمت بخش\tمشاهده\tnematbakhsh[at]eng.ui.ac.ir\r\netemadi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~etemadi\r\n\tعلي\tبهلولي\tمشاهده\tbohlooli[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~bohlooli\r\n\tزهره\tبيكي\tمشاهده\tz.beiki[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~z.beiki\r\n\tكمال\tجمشيدي\tمشاهده\tjamshidi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~jamshidi\r\n\tمحمدرضا\tرشادي نژاد\tمشاهده\tm.reshadinezhad[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.reshadinezhad\r\n\tمهران\tرضايي\tمشاهده\tm.rezaei[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.rezaei\r\n\tمهدي\tكلباسي\tمشاهده\tm.kalbasi[at]comp.ui.ac.\r\nm.h.bateni[at]comp.ui.ac.ir\thttps://comp.ui.ac.ir/~m.h.bateni\r\n\tبهروز\tشاه قلي قهفرخي\tمشاهده\tshahgholi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~shahgholi\r\n\tمائده\tعاشوري تلوكي\tمشاهده\tm.ashouri[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.ashouri\r\n\tمرجان\tكائدي\tمشاهده\tkaedi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~kaedi\r\n\tحميد\tملا\tمشاهده\th.mala[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.mala\r\n\tاحمدرضا\tمنتظرالقائم\tمشاهده\ta.montazerolghaem[at]comp.ui.ac.ir\thttps://comp.ui.ac.ir/~a.montazerolghaem\r\n\tاحسان\tمهدوي\tمشاهده\te.mahdavi[at]comp.ui.ac.ir\r\nm.mahdavi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.mahdavi\r\n\tفريا\tنصيري مفخم\tمشاهده\tfnasiri[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~fnasiri\r\n\tسيدفخرالدين\tنوربهبهاني\tمشاهده\tnoorbehbahani[at]eng.ui.ac.ir\r\nm.esteki[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.esteki\r\n\tغلامرضا\tانصاري فر\tمشاهده\tghr.ansarifar[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~ghr.ansarifar\r\n\tنويد\tايوبيان\tمشاهده\tn.ayoobian[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~n.ayoobian\r\n\tايرج\tجباري\tمشاهده\ti_jabbari[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~i_jabbari\r\n\tخديجه\tرضائي ابراهيم سرائي\tمشاهده\tkh.rezaee[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~kh.rezaee\r\n\tبابك\tشيراني بيدآبادي\tمشاهده\tb.shirani[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~b.shirani\r\n\tمحمدرضا\tعبدي\tمشاهده\tr.abdi[at]sci.ui.ac.ir\r\nm.aliasgarian[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~m.aliasgarian\r\n\tمهدي\tنصري نصر آبادي\tمشاهده\tmnnasrabadi[at]ast.ui.ac.ir\r\nm.bagheri[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.bagheri\r\n\tمرتضي\tحاجي محمودزاده\tمشاهده\tm.hajimahmoodzadeh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.hajimahmoodzadeh\r\n\tعليرضا\tخورسندي\tمشاهده\ta.khorsandi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.khorsandi\r\n\tرسول\tركني زاده\tمشاهده\trokni[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~rokni\r\n\tمرتضي\tسلطاني\tمشاهده\tmo.soltani[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~mo.soltani\r\n\tراضيه\tطالبي\tمشاهده\tr.talebi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~r.talebi\r\n\tمجيد\tعموشاهي خوزاني\tمشاهده\tamooshahi[at]sci.ui.ac.ir\r\nhfallah[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~hfallah\r\n\tابراهيم\tقنبري عديوي\tمشاهده\tghanbari[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ghanbari\r\n\tسعيد\tقوامي صبوري\tمشاهده\tghavami[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ghavami\r\n\tحميدرضا\tمحمدي خشوئي\tمشاهده\thr.mohammadi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~hr.mohammadi\r\n\tمحمد\tملك محمد\tمشاهده\tm.malekmohammad[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.malekmohammad\r\n\tعلي\tمهدي فر\tمشاهده\ta.mahdifar[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.mahdifar\r\n\tمحمدحسين\tنادري\tمشاهده\tmhnaderi[at]sci.ui.ac.ir\r\n\r\n\r\n\tفهيمه\tاسفرجاني\tمشاهده\tf.esfarjani[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~f.esfarjani\r\n\tزينب\tرضايي\tمشاهده\tz.rezaee[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~z.rezaee\r\n\tجليل\tرئيسي\tمشاهده\tj.reisi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~j.reisi\r\n\tزهره\tشانظري\tمشاهده\tz.shanazari[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~z.shanazari\r\n\tمحمد\tفرامرزي\tمشاهده\tm.faramarzi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.faramarzi\r\n\tمهدي\tكارگرفرد\tمشاهده\tm.kargarfard[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~m.kargarfard\r\n\tسيدمحمد\tمرندي\tمشاهده\ts.m.marandi[at]spr.ui.ac.ir\thttps://spr.ui.ac.ir/~s.m.marandi https://ase.ui.ac.ir/~k_azarbayjan//r\n\r\n\tنعمت ا...\tاكبري\tمشاهده\tn_akbari[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~n_akbari\r\n\tهادي\tاميري\tمشاهده\th.amiri[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~h.amiri\r\n\tايمان\tباستاني‌فر\tمشاهده\ti.bastanifar[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~i.bastanifar\r\n\tرسول\tبخشي دستجردي\tمشاهده\tr.bakhshi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~r.bakhshi\r\n\tليلا\tتركي\tمشاهده\tl.torki[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~l.torki\r\n\tمحمدرضا\tحيدري خوراسگاني\tمشاهده\tm.heidari[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.heidari\r\nhttps://ase.ui.ac.ir/~m.rafat\r\n\tعليمراد\tشريفي\tمشاهده\talimorad[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~alimorad\r\n\tبابك\tصفاري\tمشاهده\tb_saffari[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~b_saffari\r\n\tسعيد\tصمدي\tمشاهده\ts.samadi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~s.samadi\r\n\tمهدي\tطغياني\tمشاهده\tm.toghyani[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.toghyani\r\n\tسيدكميل\tطيبي\tمشاهده\tsk.tayebi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~sk.tayebi\r\n\tشكوفه\tفرهمند\tمشاهده\tsh.farahmand[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~sh.farahmand\r\nhttps://ase.ui.ac.ir/~gh.kiani\r\n\tاحمد\tگوگردچيان\tمشاهده\ta.googerdchian[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.googerdchian\r\n\tعبدالحميد\tمعرفي محمدي\tمشاهده\ta.h.mohammadi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.h.mohammadi\r\n\tشهرام\tمعيني\tمشاهده\tsh.moeeni[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~sh.moeeni\r\n\tروزيتا\tمويدفر\tمشاهده\tr.moayedfar[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~r.moayedfar\r\n\tمحمد\tواعظ برزاني\tمشاهده\tvaez[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~vaez\r\n\tمحسن\tوفامهر\tمشاهده\trenani[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~renani\r\nhttps://ase.ui.ac.ir/~n.izadinia\r\n\tامين\tحاجيان نژاد\tمشاهده\ta.hajiannejad[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.hajiannejad\r\n\tنرگس\tحميديان\tمشاهده\tn.hamidian[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~n.hamidian\r\n\tامين\tرستمي\tمشاهده\ta.rostami[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.rostami\r\n\tعليرضا\tرهروي دستجردي\tمشاهده\ta.rahrovi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.rahrovi\r\n\tحسن\tفتاحي نافچي\tمشاهده\th.fattahi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~h.fattahi\r\n\tداريوش\tفروغي\tمشاهده\tforoghi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~foroghi\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tسيدعباس\tهاشمي\tمشاهده\ta.hashemi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.hashemi\r\nhttps://ase.ui.ac.ir/~a.arashpour\r\n\tمحمدمهدي\tالشريف\tمشاهده\tm.alsharif[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.alsharif\r\n\tحسن\tپوربافراني\tمشاهده\thpoorbafrani[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~hpoorbafrani\r\n\tمنوچهر\tتوسلي نائيني\tمشاهده\ttavassoli[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~tavassoli\r\n\tمريم\tجلالي دهكردي\tمشاهده\tjalali.d.maryam[at]gmail.com\t\r\n\tمحمود\tجلالي كروه\tمشاهده\tm.jalali[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.jalali\r\n\tقدرت ا\tخسروشاهي\tمشاهده\tgh.khosroshahi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~gh.khosroshahi\r\nhttps://ase.ui.ac.ir/~m.shadmanfar\r\n\tمهدي\tشهابي\tمشاهده\tm_shahabi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m_shahabi\r\n\tسيدمحمدصادق\tطباطبائي\tمشاهده\ttabatabaei[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~tabatabaei\r\n\tمرتضي\tطبيبي جبلي\tمشاهده\tm.tabibi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.tabibi\r\n\tعليرضا\tفصيحي زاده\tمشاهده\tfasihizadeh[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~fasihizadeh\r\n\tسيدمحسن\tقائم فرد\tمشاهده\tm.ghaemfard[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.ghaemfard\r\n\tاحد\tقلي زاده منقوطاي\tمشاهده\tgholizadeh[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~gholizadeh\r\nhttps://ase.ui.ac.ir/~a.maghami\r\n\tمحمد\tمنصوري بروجني\tمشاهده\tm.mansouri.b[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.mansouri.b\r\n\tقدرت ا...\tنوروزي باغكمه\tمشاهده\tgh.noroozi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~gh.noroozi\r\n\tعليرضا\tيزدانيان\tمشاهده\ta.yazdanian[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.yazdanian\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tشهروز\tابراهيمي\tمشاهده\tsh.ebrahimi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~sh.ebrahimi\r\n\tعليرضا\tآقاحسيني\tمشاهده\ta.aghahosseini[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.aghahosseini\r\n\tعلي\tاميدي\tمشاهده\taliomidi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~aliomidi\r\n\tمحمدعلي\tبصيري\tمشاهده\tbasiri[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~basiri\r\n\tعباس\tحاتمي\tمشاهده\ta.hatami[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.hatami\r\n\tسيدغلامرضا\tدوازده امامي\tمشاهده\tgh.۱۲emami[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~gh.۱۲emami\r\n\tحسين\tروحاني\tمشاهده\th.roohani[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~h.roohani\r\n\r\n\tعليرضا\tسميعي اصفهاني\tمشاهده\ta.samiee[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.samiee\r\n\tفاطمه\tشايان\tمشاهده\tf.shayan[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~f.shayan\r\n\tسيداميرمسعود\tشهرام نيا\tمشاهده\tm.shahramnia[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.shahramnia\r\n\tعلي\tعلي حسيني\tمشاهده\ta.alihosseini[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.alihosseini\r\n\tحسين\tمسعودنيا\tمشاهده\th.masoudnia[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~h.masoudnia\r\n\tحميد\tنساج\tمشاهده\th.nassaj[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~h.nassaj\r\n\tحسين\tهرسيج\tمشاهده\tharsij[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~harsij\r\nhttps://ase.ui.ac.ir/~s.vosoughi\r\n\r\n\tمجيد\tاسماعيليان\tمشاهده\tm.esmaelian[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.esmaelian\r\n\tرضا\tانصاري\tمشاهده\tr.ansari[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~r.ansari\r\n\tآذرنوش\tانصاري طادي\tمشاهده\ta.ansari[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.ansari\r\n\tمحمود\tبت شكن\tمشاهده\tm.botshekan[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.botshekan\r\n\tهادي\tتيموري\tمشاهده\th.teimouri[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~h.teimouri\r\n\tسعيد\tجهانيان\tمشاهده\ts.jahanyan[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~s.jahanyan\r\n\tآرش\tشاهين\tمشاهده\tshahin[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~shahin\r\nhttps://ase.ui.ac.ir/~shaemi\r\n\tرضا\tصالح زاده\tمشاهده\tr.salehzadeh[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~r.salehzadeh\r\n\tعلي\tصفري\tمشاهده\ta.safari[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.safari\r\n\tعلي\tصنايعي\tمشاهده\ta_sanayei[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a_sanayei\r\n\tسيدمحسن\tعلامه\tمشاهده\tdr_allameh[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~dr_allameh\r\n\tسعيد\tفتحي\tمشاهده\ts.fathi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~s.fathi\r\n\tمهسا\tقندهاري\tمشاهده\tm.ghandehari[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.ghandehari\r\nhttps://ase.ui.ac.ir/~alik\r\n\tسعيده\tكتابي\tمشاهده\tsketabi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~sketabi\r\n\tمجيد\tمحمدشفيعي\tمشاهده\tm.shafiee[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~m.shafiee\r\n\tداريوش\tمحمدي زنجيراني\tمشاهده\td.mohamadi[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~d.mohamadi\r\n\tعلي\tنصراصفهاني\tمشاهده\talin[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~alin\r\nhttps://ltr.ui.ac.ir/~aa.ahmadi\r\n\tتقي\tاژه اي\tمشاهده\tt.ejieh[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~t.ejieh\r\n\tحسين\tآقاحسيني دهاقاني\tمشاهده\th.aghahosaini[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~h.aghahosaini\r\n\tمسعود\tالگونه جونقاني\tمشاهده\tm.algone[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.algone\r\n\tمحمود\tبراتي\tمشاهده\tmbk[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~mbk\r\n\tراضيه\tحجتي زاده\tمشاهده\tr.hojatizadeh[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~r.hojatizadeh\r\n\tاميد\tذاكري كيش\tمشاهده\to.zakerikish[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~o.zakerikish\r\nhttps://ltr.ui.ac.ir/~sm.rozatian\r\n\tاحسان\tرئيسي\tمشاهده\te.reisi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~e.reisi\r\n\tالهام\tسيدان قلعه جوزداني\tمشاهده\te.sayyedan[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~e.sayyedan\r\n\tسعيد\tشفيعيون\tمشاهده\ts.shafieioun[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~s.shafieioun\r\n\tاسحاق\tطغياني اسفرجاني\tمشاهده\te.toghyani[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~e.toghyani\r\n\tمحسن\tمحمدي فشاركي\tمشاهده\tm.mohammadi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.mohammadi\r\n\tسيدعلي اصغر\tميرباقري فرد\tمشاهده\tbagheri[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~bagheri\r\nhttps://ltr.ui.ac.ir/~l.mirmojarabian\r\n\tزهره\tنجفي نيسياني\tمشاهده\tz.najafi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~z.najafi\r\n\tمحمدرضا\tنصراصفهاني\tمشاهده\tm.nasr[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.nasr\r\n\tسيدمرتضي\tهاشمي باباحيدري\tمشاهده\tsm.hashemi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~sm.hashemi\r\n\tمحبوبه\tهمتيان ورنوسفادراني\tمشاهده\tm.hematian[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.hematian\r\nhttps://ltr.ui.ac.ir/~n.ahmadi\r\n\tحميدرضا\tپاشازانوس\tمشاهده\th.pasha[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~h.pasha\r\n\tمصطفي\tپيرمراديان نجف آبادي\tمشاهده\tm.pirmoradian[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.pirmoradian\r\n\tعلي اكبر\tجعفري\tمشاهده\ta.jafari[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.jafari\r\n\tمحمدعلي\tچلونگر\tمشاهده\tm.chelongar[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.chelongar\r\n\tمرتضي\tدهقان نژاد\tمشاهده\tm.dehghan[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.dehghan\r\n\tبهمن\tزينلي\tمشاهده\tb.zeynali[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~b.zeynali\r\nhttps://ltr.ui.ac.ir/~e.sangari\r\n\tسيدمسعود\tسيدبنكدار\tمشاهده\tsm.sbonakdar[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~sm.sbonakdar\r\n\tحسين\tشيخ بستان آباد\tمشاهده\thshbostanabad[at]yahoo.com\t\r\n\tعلي اكبر\tعباسي\tمشاهده\taa.abbasi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~aa.abbasi\r\n\tاصغر\tفروغي ابري\tمشاهده\ta.foroughi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.foroughi\r\n\tابوالحسن\tفياض انوش\tمشاهده\ta.fayyaz[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.fayyaz\r\n\tمسعود\tكثيري\tمشاهده\tmasoodkasiri[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~masoodkasiri\r\nhttps://ltr.ui.ac.ir/~kajbaf\r\n\tاصغر\tمنتظرالقائم\tمشاهده\tmontazer[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~montazer\r\n\tمرتضي\tنورائي\tمشاهده\tm.nouraei[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.nouraei\r\nhttps://ltr.ui.ac.ir/~i.azarfaza\r\n\tغلامحسين\tتوكلي\tمشاهده\tg.tavacoly[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~g.tavacoly\r\n\tمرتضي\tحاجي حسيني\tمشاهده\tm.hajihosseini[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.hajihosseini\r\n\tيوسف\tشاقول\tمشاهده\ty.shaghool[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~y.shaghool\r\n\tرضا\tصادقي\tمشاهده\tR.sadeghi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~R.sadeghi\r\n\tمقداد\tقاري\tمشاهده\tm.ghari[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.ghari\r\n\tاميراحسان\tكرباسي زاده\tمشاهده\ta.karbasizadeh[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.karbasizadeh\r\nhttps://ltr.ui.ac.ir/~karbasi\r\n\tسيدعلي\tكلانتري\tمشاهده\ta.kalantari[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.kalantari\r\n\tرضا\tكورنگ بهشتي\tمشاهده\tr.koorangbeheshti[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~r.koorangbeheshti\r\n\tهومن\tمحمدقربانيان\tمشاهده\th.ghorbanian[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~h.ghorbanian\r\n\tمحمد\tمشكات\tمشاهده\tm.meshkat[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.meshkat\r\n\tحامد\tناجي اصفهاني\tمشاهده\th.naji[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~h.naji\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tاحسان\tآقابابايي\tمشاهده\te.aqababaee[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~e.aqababaee\r\n\tحميد\tدهقاني\tمشاهده\th.dehghani[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~h.dehghani\r\n\tعلي\tرباني خوراسگاني\tمشاهده\ta.rabbani[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.rabbani\r\n\tمهري\tشمس قهفرخي\tمشاهده\tm.shams[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.shams\r\n\tوحيد\tقاسمي\tمشاهده\tv.ghasemi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~v.ghasemi\r\n\tعلي\tقنبري برزيان\tمشاهده\ta.ghanbari[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.ghanbari\r\n\tمجيد\tكارشناس نجف آبادي\tمشاهده\tm.karshenas[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.karshenas\r\nhttps://ltr.ui.ac.ir/~m.goudarzi\r\n\tزهرا\tماهر\tمشاهده\tz.maher[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~z.maher\r\n\tثريا\tمعمار\tمشاهده\ts.memar[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~s.memar\r\n\tاحمد\tمهرشاد\tمشاهده\tmehrshad۳۳۰[at]yahoo.com\t\r\n\tسيدعلي\tهاشميان فر\tمشاهده\tj.hashemian[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~j.hashemian\r\n\tرضا\tهمتي\tمشاهده\tr.hemati[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~r.hemati\r\nhttps://fgn.ui.ac.ir/~m.afrouz\r\n\tزهرا\tاميريان ورنوسفادراني\tمشاهده\tz.amirian[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~z.amirian\r\n\tمحمد\tاميريوسفي\tمشاهده\tm.amiryousefi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.amiryousefi\r\n\tحسين\tبراتي جوي آبادي\tمشاهده\tbarati[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~barati\r\n\tنيلوفر\tبهروز\tمشاهده\tn.behrooz۸۹[at]gmail.com\t\r\n\tحسين\tپيرنجم الدين\tمشاهده\tpirnajmuddin[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~pirnajmuddin\r\n\tمنصور\tتوكلي\tمشاهده\ttavakoli[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~tavakoli\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tزهرا\tجان نثاري لاداني\tمشاهده\tz.jannessari[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~z.jannessari\r\n\tاكبر\tحسابي\tمشاهده\ta.hesabi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~a.hesabi\r\n\tروح اله\tداتلي بيگي\tمشاهده\tr.datlibeigi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~r.datlibeigi\r\n\tعزيزاله\tدباغي ورنوسفادراني\tمشاهده\tdabaghi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~dabaghi\r\n\tبهنام\tرضواني سيچاني\tمشاهده\tbehnamrezvanimail[at]gmail.com\t\r\n\tنورا...\tزرين آبادي\tمشاهده\tzarrinabadi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~zarrinabadi\r\n\tمحمدتقي\tشاه نظري درچه\tمشاهده\tm.shahnazari[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.shahnazari\r\nhttps://fgn.ui.ac.ir/~abbasi\r\n\tسعيد\tكتابي\tمشاهده\tketabi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~ketabi\r\n\tاحمد\tمعين زاده\tمشاهده\tmoin[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~moin\r\n\tداريوش\tنژادانصاري مهابادي\tمشاهده\td.nejadansari[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~d.nejadansari\r\nhttps://fgn.ui.ac.ir/~ibnorrasool\r\n\tسردار\tاصلاني\tمشاهده\taslani[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~aslani\r\n\tمريم\tجلائي پيكاني\tمشاهده\tm.jalaei[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.jalaei\r\n\tسميه\tحسنعليان\tمشاهده\ts.hasanalian[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~s.hasanalian\r\n\tمحمد\tخاقاني اصفهاني\tمشاهده\tm.khaqani[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.khaqani\r\n\tمحمد\tرحيمي خويگاني\tمشاهده\tm.rahimi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.rahimi\r\n\tسيدرضا\tسليمان زاده نجفي\tمشاهده\tnajafi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~najafi\r\n\r\n\tاحمدرضا\tصاعدي\tمشاهده\ta.saedi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~a.saedi\r\n\tمهدي\tعابدي جزيني\tمشاهده\tm.abedi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.abedi\r\n\tسميه\tكاظمي نجف آبادي\tمشاهده\ts.kazemi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~s.kazemi\r\n\tنرگس\tگنجي\tمشاهده\tganji[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~ganji\r\n\tاميرصالح\tمعصومي كله لو\tمشاهده\tamirsalehmasoomi[at]yahoo.com\t\r\n\tحسين\tميرزائي نيا\tمشاهده\th.mirzaieniya[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~h.mirzaieniya\r\n\tروح ا...\tنصيري\tمشاهده\tr.nasiri[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~r.nasiri\r\nhttps://fgn.ui.ac.ir/~a.aryan\r\n\tمهدي\tجلالي\tمشاهده\tm.jalali[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.jalali\r\n\tمرضيه\tزهيري هاشم آبادي\tمشاهده\tm.zahiri[at]fgn.ui.ac.it\t\r\n\tآذر\tفرقاني تهراني\tمشاهده\ta.forghani[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~a.forghani\r\n\tمحمد\tملك محمدي\tمشاهده\tm.malekmohammadi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~m.malekmohammadi\r\nhttps://fgn.ui.ac.ir/~sh.sharifi\r\n\tمحمدحسين\tاطرشي\tمشاهده\tmh.otroshi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~mh.otroshi\r\n\tاكرم\tآيتي نجف آبادي\tمشاهده\ta.ayati[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~a.ayati\r\n\tصفورا\tترك لاداني\tمشاهده\ts.torkladani[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~s.torkladani\r\n\tزهرا\tحاجي بابايي\tمشاهده\t--------\t\r\n\tراضيه\tصادقپور\tمشاهده\tr.sadeghpour[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~r.sadeghpour\r\n\tعاليه\tصباغيان\tمشاهده\ta.sabbaghian[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~a.sabbaghian\r\nhttps://fgn.ui.ac.ir/~f.tayebianpour\r\n\tكاميار\tعبدالتاجديني\tمشاهده\tk.tajedini[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~k.tajedini\r\n\tنازيتا\tعظيمي ميبدي\tمشاهده\tazimi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~azimi\r\n\tمژگان\tمهدوي زاده\tمشاهده\tmahdavi[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~mahdavi\r\n\tاردلان\tنصرتي\tمشاهده\ta.nosrati[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~a.nosrati\r\nhttps://fgn.ui.ac.ir/~h.asadi\r\n\tحدائق\tرضايي\tمشاهده\thadaeghrezaei[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~hadaeghrezaei\r\n\tوالي\tرضايي\tمشاهده\tvali.rezai[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~vali.rezai\r\n\tعادل\tرفيعي\tمشاهده\ta.rafiei[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~a.rafiei\r\n\tاسفنديار\tطاهري\tمشاهده\te.taheri[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~e.taheri\r\n\tبتول\tعلي نژاد\tمشاهده\tb.alinezhad[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~b.alinezhad\r\n\tمحمد\tعموزاده مهدي رجي\tمشاهده\tamouzadeh[at]fgn.ui.ac.ir\thttps://fgn.ui.ac.ir/~amouzadeh\r\nhttps://fgn.ui.ac.ir/~r.motavallian\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tمحسن\tابوطالبي اصفهاني\tمشاهده\tm.aboutalebi.e[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.aboutalebi.e\r\n\tمحمدحسن\tاسماعيلي\tمشاهده\tm.h.esmaeili[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.h.esmaeili\r\n\tاحمدرضا\tجعفريان مقدم\tمشاهده\tar.jafarian[at]trn.ui.ac.ir\thttps://trn.ui.ac.ir/~ar.jafarian\r\n\tميثم\tجهانگيري علي كمر\tمشاهده\tm.jahangiri[at]cet.ui.ac.ir\thttps://cet.ui.ac.ir/~m.jahangiri\r\n\tمحمودرضا\tچنگيزيان\tمشاهده\tm.changizian[at]cet.ui.ac.ir\thttps://cet.ui.ac.ir/~m.changizian\r\n\tاحمد\tگلي\tمشاهده\ta.goli[at]trn.ui.ac.ir\thttps://trn.ui.ac.ir/~a.goli\r\n\tنريمان\tنيكو\tمشاهده\tnarimanikoo[at]yahoo.com\t\r\nhttps://eng.ui.ac.ir/~p.hamedani\r\nhttps://eng.ui.ac.ir/~tajmir\r\n\tشروين\tجمشيدي\tمشاهده\tsh.jamshidi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~sh.jamshidi\r\n\tمريم\tداعي\tمشاهده\tm.daei[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.daei\r\n\tعلي\tدهنوي\tمشاهده\ta.dehnavi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a.dehnavi\r\n\tمحمدعلي\tرهگذر\tمشاهده\trahgozar[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~rahgozar\r\n\tسيدمهدي\tزندي آتشبار\tمشاهده\ts.m.zandi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~s.m.zandi\r\n\tمهران\tزينليان دستجردي\tمشاهده\tm.zeynalian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.zeynalian\r\nhttps://eng.ui.ac.ir/~a.shanehsazzadeh\r\n\tعبدالرضا\tعطائي\tمشاهده\ta.ataei[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a.ataei\r\n\tمحمدعلي\tعليجانيان\tمشاهده\tm.alijanian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.alijanian\r\n\tحسين\tعموشاهي\tمشاهده\th.amoushahi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.amoushahi\r\n\tسيداميرمهرداد\tمحمدحجازي\tمشاهده\tm.hejazi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.hejazi\r\n\tفرشيد\tمسيبي برزي\tمشاهده\tmossaiby[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~mossaiby\r\n\tميثم\tمشايخي\tمشاهده\tm.mashayekhi[at]cet.ui.ac.ir\thttps://cet.ui.ac.ir/~m.mashayekhi\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tرامتين\tمعيني\tمشاهده\tr.moeini[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~r.moeini\r\n\tمحمود\tهاشمي اصفهانيان\tمشاهده\tm.hashemi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.hashemi\r\n\tحامد\tهفت برادران\tمشاهده\th.haftbaradaran[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.haftbaradaran\r\n\tحامد\tيزديان\tمشاهده\th.yazdian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.yazdian\r\nhttps://cet.ui.ac.ir/~a.abzal\r\n\tحسين\tباقري\tمشاهده\th.bagheri[at]cet.ui.ac.ir\thttps://cet.ui.ac.ir/~h.bagheri\r\n\tفريد\tچراغي\tمشاهده\tf.cheraghi[at]cet.ui.ac.ir\thttps://cet.ui.ac.ir/~f.cheraghi\r\n\tايمان\tخسروي\tمشاهده\ti.khosravi[at]cet.ui.ac.ir\thttps://cet.ui.ac.ir/~i.khosravi\r\n\tمريم\tسبك خيز\tمشاهده\t--------\t\r\n\tمهران\tستاري آبروي\tمشاهده\tsattari[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~sattari\r\n\tجمال\tعسگري\tمشاهده\tasgari[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~asgari\r\n\r\n\tسيدباقر\tفاطمي نصرآبادي\tمشاهده\tsb.fatemi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~sb.fatemi\r\n\tجمشيد\tمالكي\tمشاهده\tj.maleki[at]cet.ui.ac.ir\thttps://cet.ui.ac.ir/~j.maleki\r\n\tمينا\tمرادي زاده\tمشاهده\tm.moradizadeh[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.moradizadeh\r\n\tحميد\tمهرابي\tمشاهده\th.mehrabi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.mehrabi\r\n\tسيدعبدالحسين\tموسوي الكاظمي\tمشاهده\thmoossavi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~hmoossavi\r\n\tمهدي\tمومني شهركي\tمشاهده\tmomeni[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~momeni\r\nhttps://sci.ui.ac.ir/~aesmaeili\r\n\tزهرا\tاعتمادي فر\tمشاهده\tz.etemadifar[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~z.etemadifar\r\n\tرحمان\tامام زاده\tمشاهده\tr.emamzadeh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~r.emamzadeh\r\n\tمجيد\tبوذري\tمشاهده\tbouzari[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~bouzari\r\n\tبابك\tبيك زاده\tمشاهده\tb.beikzadeh[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~b.beikzadeh\r\n\tسيدمرتضي\tجوادي راد\tمشاهده\tsm.javadirad[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~sm.javadirad\r\n\tفاطمه\tجوادي زرنقي\tمشاهده\tfa.javadi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~fa.javadi\r\nhttps://sci.ui.ac.ir/~z.hojati\r\n\tافروزالسادات\tحسيني ابري\tمشاهده\taf.hosseini[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~af.hosseini\r\n\tفريبا\tدهقانيان\tمشاهده\tfa.dehghanian[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~fa.dehghanian\r\n\tمحمد\tرباني خوراسگاني\tمشاهده\tmo.rabbani[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~mo.rabbani\r\n\tفاتح\tرحيمي\tمشاهده\tf.rahimi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~f.rahimi\r\n\tسهيلا\tرهگذر\tمشاهده\trahgozar[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~rahgozar\r\n\tزهرا\tسوري\tمشاهده\tz.souri[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~z.souri\r\nhttps://sci.ui.ac.ir/~r.shafiei\r\n\tمحمدرضا\tگنجعلي خاني\tمشاهده\tm.ganjalikhany[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.ganjalikhany\r\n\tمجيد\tمتولي باشي نائيني\tمشاهده\tmbashi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~mbashi\r\n\tحميد\tمعادي\tمشاهده\thamid.maadi[at]gmail.com\t\r\n\tمهران\tميراوليايي\tمشاهده\tm.miroliaei[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.miroliaei\r\n\tصادق\tوليان بروجني\tمشاهده\tsvallian[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~svallian\r\nhttps://sci.ui.ac.ir/~ehsanpou\r\n\tفريبا\tاسماعيلي كتكي\tمشاهده\tf.esmaeili[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~f.esmaeili\r\n\tسعيد\tافشارزاده\tمشاهده\ts.afshar[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~s.afshar\r\n\tعلي\tباقري\tمشاهده\ta.bagheri[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.bagheri\r\n\tسيامك\tبهشتي\tمشاهده\ts.beheshti[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~s.beheshti\r\n\tمرضيه\tتقي زاده\tمشاهده\tm.taghizadeh[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~m.taghizadeh\r\n\tحجت اله\tسعيدي\tمشاهده\tho.saeidi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ho.saeidi\r\nhttps://sci.ui.ac.ir/~mansour\r\n\tروح الله\tعباسي\tمشاهده\tr.abbasi[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~r.abbasi\r\n\tوجيهه\tعظيميان زواره\tمشاهده\tvajihe.azimian[at]gmail.com\t\r\n\tمجيد\tمرادمند\tمشاهده\tm.moradmand[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.moradmand\r\n\tسيدحامد\tمعظمي فريدا\tمشاهده\th.moazzami[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~h.moazzami\r\n\tمريم\tنوربخش نيا\tمشاهده\tm.noorbakhshnia[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.noorbakhshnia\r\nhttps://ast.ui.ac.ir/~ma.asadollahi\r\n\tحميد\tاميري دستنائي\tمشاهده\th.amiri[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~h.amiri\r\n\tماندانا\tبهبهاني\tمشاهده\tma.behbahani[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~ma.behbahani\r\n\tسيدرسول\tذاكر\tمشاهده\tr.zaker[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~r.zaker\r\n\tاصغر\tطاهري كفراني\tمشاهده\ta.taheri[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~a.taheri\r\n\tابوالقاسم\tعباسي كجاني\tمشاهده\tagh.abbasi[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~agh.abbasi\r\n\tمحسن\tگلابي\tمشاهده\tm.golabi[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~m.golabi\r\nhttps://ast.ui.ac.ir/~h.mohabatkar\r\n\tصفورا\tميرمحمدصادقي\tمشاهده\ts.mirmohamadsadeghi[at]bio.ui.ac.ir\thttps://bio.ui.ac.ir/~s.mirmohamadsadeghi\r\nhttps://ahl.ui.ac.ir/~a.bankipoor\r\n\tسيدمهدي\tبيابانكي ورنوسفادراني\tمشاهده\tsm.biabanaki[at]ahl.ui.ac.ir\thttps://ahl.ui.ac.ir/~sm.biabanaki\r\n\tصحبت ا...\tحسنوند\tمشاهده\ts.hasanvand[at]ahl.ui.ac.ir\thttps://ahl.ui.ac.ir/~s.hasanvand\r\n\tعاطفه\tحيرت\tمشاهده\ta.heyrat[at]theo.ui.ac.ir\thttps://theo.ui.ac.ir/~a.heyrat\r\n\tحوريه\tرباني اصفهاني\tمشاهده\trabbany۶۱۶۸[at]yahoo.com\t\r\n\tابراهيم\tرضايي\tمشاهده\te.rezaei[at]ahl.ui.ac.ir\thttps://ahl.ui.ac.ir/~e.rezaei\r\n\tمحمدصالح\tطيب نيا\tمشاهده\tms.tayebnia[at]ahl.ui.ac.ir\thttps://ahl.ui.ac.ir/~ms.tayebnia\r\nhttps://ahl.ui.ac.ir/~a.ebadi\r\n\tاحسان\tعلي اكبري بابوكاني\tمشاهده\te.aliakbari[at]ahl.ui.ac.ir\thttps://ahl.ui.ac.ir/~e.aliakbari\r\n\tنفيسه\tفقيهي مقدس\tمشاهده\tn.faghihi[at]ahl.ui.ac.ir\thttps://ahl.ui.ac.ir/~n.faghihi\r\n\tوحيد\tمقدم\tمشاهده\tv.moghadam[at]ahl.ui.ac.ir\thttps://ahl.ui.ac.ir/~v.moghadam\r\nhttps://ltr.ui.ac.ir/~arshad\r\n\tسيدمهدي\tامامي جمعه\tمشاهده\tm.emami[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.emami\r\n\tنفيسه\tاهل سرمدي\tمشاهده\tn.ahlsarmadi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~n.ahlsarmadi\r\n\tجنان\tايزدي\tمشاهده\tj.izadi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~j.izadi\r\n\tمحمد\tبيدهندي\tمشاهده\tm.bid[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.bid\r\n\tفروغ السادات\tرحيم پور\tمشاهده\tf.rahimpour[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~f.rahimpour\r\n\tجعفر\tشانظري\tمشاهده\tj.shanazari[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~j.shanazari\r\nhttps://ltr.ui.ac.ir/~majd\r\n\tمهدي\tگنجور\tمشاهده\tm.ganjvar[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.ganjvar\r\n\tمحمدمهدي\tمشكاتي\tمشاهده\tmm.meshkati[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~mm.meshkati\r\nhttps://ltr.ui.ac.ir/~ahmadnezhad\r\n\tداود\tاسماعيلي دهاقاني\tمشاهده\td.esmaely[at]theo.ui.ac.ir\thttps://theo.ui.ac.ir/~d.esmaely\r\n\tعلي\tبنائيان اصفهاني\tمشاهده\ta.banaeian[at]theo.ui.ac.ir\thttps://theo.ui.ac.ir/~a.banaeian\r\n\tاعظم\tپرچم\tمشاهده\ta.parcham[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.parcham\r\n\tمحسن\tتوكلي ورزنه\tمشاهده\tm.tavakkoli[at]theo.ui.ac.ir\thttps://theo.ui.ac.ir/~m.tavakkoli\r\n\tمحمدرضا\tحاجي اسماعيلي حسين آبادي\tمشاهده\tm.hajiesmaeili[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.hajiesmaeili\r\n\tمحمدرضا\tستوده نيا\tمشاهده\tm.sotudeh[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.sotudeh\r\nhttps://ltr.ui.ac.ir/~m.soltani.r\r\n\tرضا\tشكراني\tمشاهده\tr.shokrani[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~r.shokrani\r\n\tمحسن\tصمدانيان اصفهاني\tمشاهده\tm.samadanian[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.samadanian\r\n\tزهرا\tكلباسي اشترى\tمشاهده\tz.kalbasi[at]theo.ui.ac.ir\thttps://theo.ui.ac.ir/~z.kalbasi\r\n\tسيدمهدي\tلطفي\tمشاهده\tm.lotfi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.lotfi\r\n\tمهدي\tمطيع\tمشاهده\tm.motia[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.motia\r\nhttps://ltr.ui.ac.ir/~h.a.bahrami\r\n\tمحمود\tحاجي احمدي\tمشاهده\tm.hajiahmadi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.hajiahmadi\r\n\tمحمدعلي\tرستميان\tمشاهده\tm.a.rostamian[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.a.rostamian\r\n\tمجتبي\tسپاهي\tمشاهده\tm.sepahi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.sepahi\r\n\tمريم\tسعيديان جزي\tمشاهده\tmsaeedyan[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~msaeedyan\r\n\tمحسن\tشيراوند\tمشاهده\tm.shiravand[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~m.shiravand\r\n\tاحمد\tعزيزخاني\tمشاهده\ta.azizkhani[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.azizkhani\r\n\r\n\tحسين\tعزيزي\tمشاهده\th.azizi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~h.azizi\r\n\tعلي\tغفارزاده آزادلو\tمشاهده\ta.ghafarzadeh[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.ghafarzadeh\r\n\tسيداحمد\tمحمودي\tمشاهده\tsa.mahmoodi[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~sa.mahmoodi\r\n\tعبدالرسول\tمشكات\tمشاهده\ta.meshkat[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~a.meshkat\r\n\tمسعود\tمطهري نسب\tمشاهده\tm.motaharinasab[at]theo.ui.ac.ir\thttps://theo.ui.ac.ir/~m.motaharinasab\r\n\tاسماعيل\tملكوتي خواه\tمشاهده\te.malakooti[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~e.malakooti\r\n\tحامد\tنظرپور\tمشاهده\th.nazarpour[at]ltr.ui.ac.ir\thttps://ltr.ui.ac.ir/~h.nazarpour\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tسيدعلي اصغر\tهاشم زاده اصفهاني\tمشاهده\ta.hashemzadeh[at]theo.ui.ac.ir\thttps://theo.ui.ac.ir/~a.hashemzadeh\r\nhttps://sci.ui.ac.ir/~a.rahmati\r\n\tفاطمه\tرفيع منزلت\tمشاهده\tfrafiemanzelat[at]chem.ui.ac.ir\thttps://chem.ui.ac.ir/~frafiemanzelat\r\n\tحسن\tزالي بوئيني\tمشاهده\th.zali[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~h.zali\r\n\tاسماعيل\tشيباني فهندري\tمشاهده\te.sheibani[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~e.sheibani\r\n\tعلي\tقريه\tمشاهده\ta.gharieh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.gharieh\r\n\tغلامعلي\tكوهمره\tمشاهده\tg.a.koohmareh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~g.a.koohmareh\r\n\tامير\tلندراني اصفهاني\tمشاهده\tlandarani[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~landarani\r\nhttps://sci.ui.ac.ir/~imbaltork\r\n\tعباس\tمحمدي\tمشاهده\ta.mohammadi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.mohammadi\r\n\tحميدرضا\tمعماريان\tمشاهده\tmemarian[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~memarian\r\nhttps://chem.ui.ac.ir/~a.omidvar\r\n\tرضا\tاميديان\tمشاهده\tr.omidyan[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~r.omidyan\r\n\tحسن\tسبزيان\tمشاهده\tsabzyan[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~sabzyan\r\n\tناهيد\tفرضي كاهكش\tمشاهده\tnfarzi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~nfarzi\r\n\tمجيد\tموسوي\tمشاهده\tm.mousavi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.mousavi\r\nhttps://sci.ui.ac.ir/~m.mehrgardi\r\n\tاسماعيل\tشمس سولاري\tمشاهده\te_shams[at]chem.ui.ac.ir\thttps://chem.ui.ac.ir/~e_shams\r\n\tغلامحسن\tعظيمي گندماني\tمشاهده\tgh.azimi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~gh.azimi\r\n\tرضا\tكريمي شروداني\tمشاهده\trkarimi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~rkarimi\r\n\tاكبر\tملك پور\tمشاهده\ta.malekpour[at]chem.ui.ac.ir\thttps://chem.ui.ac.ir/~a.malekpour\r\n\tفريبرز\tمومن بيك\tمشاهده\tf.momen[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~f.momen\r\n\r\n\tهادي\tاميري رودباري\tمشاهده\th.a.rudbari[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~h.a.rudbari\r\n\tشهرام\tتنگستاني نژاد\tمشاهده\tstanges[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~stanges\r\n\tرضا\tكشاورزي\tمشاهده\tr.keshavarzi[at]chem.ui.ac.ir\thttps://chem.ui.ac.ir/~r.keshavarzi\r\n\tمجيد\tمقدم\tمشاهده\tmoghadamm[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~moghadamm\r\n\tولي اله\tميرخاني\tمشاهده\tmirkhani[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~mirkhani\r\n\tبهرام\tيداللهي\tمشاهده\tyadollahi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~yadollahi\r\n\r\n\tمحسن\tخسروي\tمشاهده\tm.khosravi[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~m.khosravi\r\n\tقاسم\tديني تركماني\tمشاهده\tg.dini[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~g.dini\r\n\tمحسن\tمصلحي\tمشاهده\tm.moslehi[at]chem.ui.ac.ir\thttps://chem.ui.ac.ir/~m.moslehi\r\n\tبهروز\tموحدي\tمشاهده\tb.movahedi[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~b.movahedi\r\n\tسيدعبداله\tنوربخش رضايي\tمشاهده\ta.noorbakhshrezaei[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~a.noorbakhshrezaei\r\n\tابوالقاسم\tنورمحمدي آبادچي\tمشاهده\ta.nourmohammadi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.nourmohammadi\r\nhttps://edu.ui.ac.ir/~m.esmaili\r\n\tناهيد\tاكرمي\tمشاهده\tn.akrami[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~n.akrami\r\n\tهاجر\tبراتي احمدآبادي\tمشاهده\th.barati[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~h.barati\r\n\tماهگل\tتوكلي دارگاني\tمشاهده\tm.tavakoli[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.tavakoli\r\n\tسيدميثم\tديباجي\tمشاهده\tsm.diba[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~sm.diba\r\n\tحسين\tسماواتيان\tمشاهده\th.samavatyan[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~h.samavatyan\r\n\tسيده راضيه\tطبائيان\tمشاهده\ts.r.tabaeian[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~s.r.tabaeian\r\nhttps://edu.ui.ac.ir/~dr.oreyzi\r\n\tكريم\tعسگري مباركه\tمشاهده\tk.asgari[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~k.asgari\r\n\tمحمدباقر\tكجباف\tمشاهده\tm.b.kaj[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.b.kaj\r\n\tمهرداد\tكلانتري\tمشاهده\tmehrdadk[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~mehrdadk\r\n\tسيمين دخت\tكلني\tمشاهده\tsd.kalani[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~sd.kalani\r\n\tآرزو\tلشكري باويل عليائي\tمشاهده\ta.lashkari[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~a.lashkari\r\n\tعلي\tمحرابي\tمشاهده\ta.mehrabi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~a.mehrabi\r\nhttps://edu.ui.ac.ir/~h.mehrabi\r\n\tحميدطاهر\tنشاط دوست\tمشاهده\th.neshat[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~h.neshat\r\nhttps://edu.ui.ac.ir/~a.sharifi\r\n\tاحمد\tعابدي\tمشاهده\ta.abedi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~a.abedi\r\n\tمحمد\tعاشوري\tمشاهده\tm.ashori[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.ashori\r\n\tسالار\tفرامرزي\tمشاهده\ts.faramarzi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~s.faramarzi\r\n\tامير\tقمراني\tمشاهده\ta.ghamarani[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~a.ghamarani\r\n\tقاسم\tنوروزي\tمشاهده\tg.norouzi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~g.norouzi\r\nhttps://edu.ui.ac.ir/~a.akbari\r\n\tميترا\tپشوتني زاده\tمشاهده\tm.pashootanizade[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.pashootanizade\r\n\tمهرداد\tچشمه سهرابي\tمشاهده\tmo.sohrabi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~mo.sohrabi\r\n\tمهدي\tرحماني\tمشاهده\tm.rahmani[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.rahmani\r\n\tاحمد\tشعباني\tمشاهده\tshabania[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~shabania\r\n\tمريم\tكشوري\tمشاهده\tm.keshvari[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.keshvari\r\n\tمرتضي\tمحمدي استاني\tمشاهده\tm.ostani[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.ostani\r\nhttps://edu.ui.ac.ir/~a.mansouri\r\nhttps://edu.ui.ac.ir/~esfijani\r\n\tنگين\tبرات دستجردي\tمشاهده\tn.dastjerdi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~n.dastjerdi\r\n\tعبدالرسول\tجمشيديان\tمشاهده\ta.r.jamshidian[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~a.r.jamshidian\r\n\tمحمدحسين\tحيدري\tمشاهده\tmh.heidari[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~mh.heidari\r\n\tسيدهدايت اله\tداورپناه\tمشاهده\th.davarpanah[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~h.davarpanah\r\n\tسيدحميدرضا\tشاوران\tمشاهده\treza.shavaran[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~reza.shavaran\r\n\tفريدون\tشريفيان جزي\tمشاهده\tf.sharifian[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~f.sharifian\r\nhttps://edu.ui.ac.ir/~y.abedini\r\n\tسيدامين\tعظيمي\tمشاهده\tsa.azimi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~sa.azimi\r\n\tمحمدجواد\tلياقت دار\tمشاهده\tjavad[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~javad\r\n\tليلا\tمقتدايي خوراسگاني\tمشاهده\tl.moghtadaei[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~l.moghtadaei\r\n\tسيدابراهيم\tميرشاه جعفري\tمشاهده\tjafari[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~jafari\r\n\tاحمدرضا\tنصراصفهاني\tمشاهده\tarnasr[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~arnasr\r\n\tرضاعلي\tنوروزي\tمشاهده\tr.norouzi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~r.norouzi\r\nhttps://edu.ui.ac.ir/~m.neyestani\r\n\tمحمدرضا\tنيلي احمدآبادي\tمشاهده\tm.nili.a[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.nili.a\r\n\tرضا\tهويدا\tمشاهده\tr.hoveida[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~r.hoveida\r\n\tراضيه\tيوسف بروجردي\tمشاهده\tr.boroujerdi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~r.boroujerdi\r\nhttps://edu.ui.ac.ir/~f.asanjarani\r\n\tعذرا\tاعتمادي تودشكي\tمشاهده\to.etemadi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~o.etemadi\r\n\tسميه\tجابري\tمشاهده\ts.jaberi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~s.jaberi\r\n\tرضوان السادات\tجزايري\tمشاهده\trazvgaza[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~razvgaza\r\n\tسحر\tخانجاني وشكي\tمشاهده\ts.khanjani[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~s.khanjani\r\n\tيونس\tدوستيان\tمشاهده\ty.doostian[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~y.doostian\r\n\tفاطمه\tسميعي\tمشاهده\tf.samiee[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~f.samiee\r\nhttps://edu.ui.ac.ir/~a.sadeghi\r\n\tمحمدرضا\tعابدي\tمشاهده\tm.r.abedi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.r.abedi\r\n\tمريم\tفاتحي زاده\tمشاهده\tm.fatehizade[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~m.fatehizade\r\n\tسيده ليلا\tميراحمدي باباحيدري\tمشاهده\tsl.mirahmadi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~sl.mirahmadi\r\n\tاعظم\tنقوي\tمشاهده\taz.naghavi[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~az.naghavi\r\n\tپريسا\tنيلفروشان\tمشاهده\tp.nilforooshan[at]edu.ui.ac.ir\thttps://edu.ui.ac.ir/~p.nilforooshan\r\nhttps://sci.ui.ac.ir/~m.asadi\r\n\tسميه\tاشرفي ورنوسفادراني\tمشاهده\ts.ashrafi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~s.ashrafi\r\n\tنصرا\tايران پناه\tمشاهده\tiranpanah[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~iranpanah\r\n\tحميد\tبيدرام\tمشاهده\th.bidram[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~h.bidram\r\n\tافشين\tپرورده\tمشاهده\ta.parvardeh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.parvardeh\r\n\tمهدي\tتوانگر\tمشاهده\tm.tavangar[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.tavangar\r\n\tاحسان\tزمان زاده\tمشاهده\tE.zamanzade[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~E.zamanzade\r\nhttps://sci.ui.ac.ir/~f.sajadi\r\n\tهوشنگ\tطالبي حبيب آبادي\tمشاهده\th-talebi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~h-talebi\r\n\tفهيمه\tطوراني فراني\tمشاهده\tft.farani[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ft.farani\r\n\tايرج\tكاظمي\tمشاهده\tikazemi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ikazemi\r\n\tمحمد\tمحمدي\tمشاهده\tm.mohammadi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.mohammadi\r\n\tمحسن\tملكي\tمشاهده\tm.maleki[at]mcs.ui.ac.ir\thttps://mcs.ui.ac.ir/~m.maleki\r\n\tزهرا\tمنصوروار\tمشاهده\tz.mansourvar[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~z.mansourvar\r\nhttps://sci.ui.ac.ir/~f.abtahi\r\n\tجواد\tاسداللهي\tمشاهده\tasadollahi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~asadollahi\r\n\tسعيد\tاعظم\tمشاهده\tazam[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~azam\r\n\tعليرضا\tاميني هرندي\tمشاهده\ta.amini[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.amini\r\n\tجواد\tباقريان\tمشاهده\tbagherian[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~bagherian\r\n\tمحمدرضا\tپورياي ولي\tمشاهده\tpourya[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~pourya\r\n\tمريم\tخاتمي بيدگلي\tمشاهده\tm.khatami[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~m.khatami\r\n\r\n\tفاطمه\tخسروي\tمشاهده\tf.khosravi[at]mcs.ui.ac.ir\thttps://mcs.ui.ac.ir/~f.khosravi\r\n\tشكرا\tسالاريان\tمشاهده\tsh.salarian[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~sh.salarian\r\n\tحميدرضا\tسليمي مقدم\tمشاهده\thr.salimi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~hr.salimi\r\n\tعليرضا\tعبدالهي\tمشاهده\ta.abdollahi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~a.abdollahi\r\n\tمجيد\tفخار\tمشاهده\tfakhar[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~fakhar\r\n\tعليرضا\tنصراصفهاني\tمشاهده\tnasr_a[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~nasr_a\r\n\tمليحه\tيوسف زاده\tمشاهده\tma.yousofzadeh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ma.yousofzadeh\r\n\r\nاستاد\tنام\tنام خانوادگی\tصفحه خانگی\tپست الکترونیک\tصفحه شخصی\r\n\tبهاره\tاختري\tمشاهده\tb.akhtari[at]mcs.ui.ac.ir\thttps://mcs.ui.ac.ir/~b.akhtari\r\n\tندا\tاسماعيلي\tمشاهده\tn.esmaeeli[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~n.esmaeeli\r\n\tجعفر\tالماسي زاده\tمشاهده\tj.almasizadeh[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~j.almasizadeh\r\n\tنجمه\tحسيني منجزي\tمشاهده\tnajmeh.hoseini[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~najmeh.hoseini\r\n\tحسن\tخسرويان عرب\tمشاهده\th.khosravian[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~h.khosravian\r\n\tمجتبي\tرفيعي كركوندي\tمشاهده\tm.rafiee[at]mcs.ui.ac.ir\thttps://mcs.ui.ac.ir/~m.rafiee\r\n\tرضا\tسبحاني\tمشاهده\tr.sobhani[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~r.sobhani\r\nhttps://sci.ui.ac.ir/~m.alambardar\r\n\tفاطمه\tمنصوري\tمشاهده\tf.mansoori[at]mcs.ui.ac.ir\thttps://mcs.ui.ac.ir/~f.mansoori\r\n\tنوشين\tموحديان عطار\tمشاهده\tn.movahedian[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~n.movahedian\r\n\tداود\tميرزايي\tمشاهده\td.mirzaei[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~d.mirzaei\r\n\tصغري\tنوبختيان\tمشاهده\tnobakht[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~nobakht\r\n\tاميرعباس\tورشوي\tمشاهده\tab.varshovi[at]sci.ui.ac.ir\thttps://sci.ui.ac.ir/~ab.varshovi\r\n\r\n\tمهدي\tادريسي\tمشاهده\tedrisi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~edrisi\r\n\tمحسن\tاكراميان\tمشاهده\tm.ekramian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.ekramian\r\n\tفرزاد\tپرورش\tمشاهده\tf.parvaresh[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~f.parvaresh\r\n\tمهدي\tحبيبي\tمشاهده\tmhabibi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~mhabibi\r\n\tامين\tخدابخشيان خوانساري\tمشاهده\taminkh[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~aminkh\r\n\tحسين\tزارعي\tمشاهده\thosein.zareie[at]gmail.com\t\r\n\tحسن\tزماني\tمشاهده\th.zamani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.zamani\r\nhttps://eng.ui.ac.ir/~n.sayyaf\r\n\tكمال\tشاه طالبي\tمشاهده\tshahtalebi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~shahtalebi\r\n\tسيدمحمد\tصابرعلي\tمشاهده\tsm.saberali[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~sm.saberali\r\n\tمحمدفرزان\tصباحي\tمشاهده\tsabahi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~sabahi\r\n\tمحمد\tعطائي\tمشاهده\tataei[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~ataei\r\n\tسيدآروين\tعيوقي\tمشاهده\tsa.ayoughi[at]yahoo.com\t\r\n\tاميررضا\tفروزان\tمشاهده\ta.forouzan[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a.forouzan\r\nhttps://eng.ui.ac.ir/~e.gholipour\r\n\tمحمد\tكاظمي ورنامخواستي\tمشاهده\tm.kazemi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.kazemi\r\n\tيحيي\tكبيري رناني\tمشاهده\ty.kabiri[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~y.kabiri\r\n\tحميدرضا\tكريمي علويجه\tمشاهده\th.karimi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.karimi\r\n\tحميدرضا\tكوفي گر\tمشاهده\tkoofigar[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~koofigar\r\n\tآرش\tكيومرثي\tمشاهده\tkiyoumarsi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~kiyoumarsi\r\n\tسيدمحمد\tمدني\tمشاهده\tm.madani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.madani\r\nhttps://eng.ui.ac.ir/~m.motaharifar\r\n\tپيمان\tمعلم\tمشاهده\tp_moallem[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~p_moallem\r\n\tبهزاد\tميرزائيان دهكردي\tمشاهده\tmirzaeian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~mirzaeian\r\n\tمحسن\tميوه چي\tمشاهده\tmivehchy[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~mivehchy\r\n\tمهدي\tنيرومند\tمشاهده\tmehdi_niroomand[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~mehdi_niroomand\r\n\tرحمت ا\tهوشمند\tمشاهده\thooshmand_r[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~hooshmand_r\r\nhttps://eng.ui.ac.ir/~m.ebrahimian\r\n\tنيما\tجمشيدي\tمشاهده\tn.jamshidi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~n.jamshidi\r\n\tجواد\tراستي\tمشاهده\trasti[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~rasti\r\n\tرضا\tراستي بروجني\tمشاهده\tr.rasti[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~r.rasti\r\n\tمحسن\tرباني\tمشاهده\tm.rabbani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.rabbani\r\n\tمحسن\tصراف بيدآباد\tمشاهده\tm.saraf[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.saraf\r\n\tآزاده\tقوچاني\tمشاهده\ta.ghouchani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a.ghouchani\r\nhttps://eng.ui.ac.ir/~karimian\r\n\tحميدرضا\tمراتب\tمشاهده\th.marateb[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.marateb\r\n\tمهدي\tمهديخاني\tمشاهده\tm.mehdikhani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.mehdikhani\r\n\tمحمدرضا\tيزدچي\tمشاهده\tyazdchi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~yazdchi\r\nhttps://eng.ui.ac.ir/~a.edrisi\r\n\tمسعود\tبهشتي\tمشاهده\tm.behshti[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.behshti\r\n\tداود\tبي ريا\tمشاهده\td.biria[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~d.biria\r\n\tمحمدصادق\tحاتمي پور\tمشاهده\thatami[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~hatami\r\n\tمحمد\tحجت\tمشاهده\tm.hojjat[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.hojjat\r\n\tرضا\tحق بخش\tمشاهده\tr.haghbakhsh[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~r.haghbakhsh\r\n\tمحمدحسن\tخادمي\tمشاهده\tm.khademi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.khademi\r\n\r\n\tامير\tرحيمي\tمشاهده\trahimi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~rahimi\r\n\tعطاا\tساري\tمشاهده\ta.sari[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a.sari\r\n\tعليرضا\tسليماني نظر\tمشاهده\tasolaimany[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~asolaimany\r\n\tمحبوبه\tطغياني دولت آبادي\tمشاهده\tma.toghyani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~ma.toghyani\r\n\tمهرداد\tفرهاديان اصفهاني\tمشاهده\tm.farhadian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.farhadian\r\n\tمهدي\tكمالي\tمشاهده\tm.kamali[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.kamali\r\n\tامير\tگشادرو\tمشاهده\ta.goshadrou[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a.goshadrou\r\nhttps://eng.ui.ac.ir/~o.moini\r\n\tپيام\tملاعباسي\tمشاهده\tp.abbasi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~p.abbasi\r\n\tاميرحسين\tنوارچيان\tمشاهده\tnavarchian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~navarchian\r\n\tمريم\tهمايون فال فيني\tمشاهده\tm.homayoonfal[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.homayoonfal\r\nhttps://eng.ui.ac.ir/~ahmadikia\r\n\tعليرضا\tآريايي\tمشاهده\tariaei[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~ariaei\r\n\tابراهيم\tافشاري\tمشاهده\te.afshari[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~e.afshari\r\n\tرضا\tباربازاصفهاني\tمشاهده\trbarbaz[at]yahoo.com\t\r\n\tاحسان\tبني اسدي\tمشاهده\te.baniasadi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~e.baniasadi\r\n\tحميد\tبهشتي\tمشاهده\thamid.beheshti[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~hamid.beheshti\r\n\tمهرداد\tپورسينا\tمشاهده\tpoursina[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~poursina\r\nhttps://eng.ui.ac.ir/~k.torabi\r\n\tفرهاد\tحاجي ابوطالبي\tمشاهده\tf.hajiaboutalebi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~f.hajiaboutalebi\r\n\tكورش\tحسن پور\tمشاهده\thasanpour[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~hasanpour\r\n\tمحمد\tحيدري راراني\tمشاهده\tm.heidarirarani[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.heidarirarani\r\n\tعليرضا\tدستان\tمشاهده\ta.dastan[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~a.dastan\r\n\tحامد\tشهبازي\tمشاهده\tshahbazi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~shahbazi\r\n\tمسعود\tضيايي راد\tمشاهده\tm.ziaeirad[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.ziaeirad\r\nhttps://eng.ui.ac.ir/~h.karimpour\r\n\tفريبرز\tكريمي طالخونچه\tمشاهده\tf.karimi[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~f.karimi\r\n\tمهدي\tمشرف دهكردي\tمشاهده\tm.mosharaf[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~m.mosharaf\r\n\tرسول\tمهشيد\tمشاهده\tr.mahshid[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~r.mahshid\r\n\tشهرام\tهاديان جزي\tمشاهده\ts.hadian[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~s.hadian\r\nhttps://eng.ui.ac.ir/~m.johari\r\n\tعلي\tذاكري\tمشاهده\ta.zackery[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~a.zackery\r\n\tحميد\tزارعي\tمشاهده\th.zarei[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~h.zarei\r\n\tمحسن\tطاهري دمنه\tمشاهده\tm.taheri[at]ast.ui.ac.ir\thttps://ast.ui.ac.ir/~m.taheri\r\n\tآرزو\tعتيقه چيان\tمشاهده\ta.atighehchian[at]ase.ui.ac.ir\thttps://ase.ui.ac.ir/~a.atighehchian\r\n\tكامران\tكيانفر\tمشاهده\tk.kianfar[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~k.kianfar\r\n\tعليرضا\tگلي\tمشاهده\tgoli.a[at]eng.ui.ac.ir\thttps://eng.ui.ac.ir/~goli.a\r\n\r\n\r\n";


    public static async Task ScrapProfessorProfile()
    {
        var _context = new ProfileShakhsiDbContext();
        //var a = _context.ProfessorProfiles.Select(x => new { ProfileShakhsiEn = "http://93.126.41.157:82/en/" + x.FullName.Replace(" ", "-"), ProfileShakhsiFa = "http://93.126.41.157:82/fa/" + x.FullName.Replace(" ", "-") + "", ElmSanji = "http://93.126.41.157:85/" + x.FullName.Replace(" ", "-") + "" });
        //string pattern = @"http[s]?://[\w\.-]+(?:\.[\w\.-]+)+[/\w\?\&\=\.\-]*";
        string pattern = @"https?://[^\s]+";
        // لیستی برای ذخیره سایت‌ها
        List<string> urls = new List<string>();

        // جستجو و اضافه کردن URL ها به لیست
        foreach (Match match in Regex.Matches(input, pattern))
        {
            urls.Add(match.Value);
        }

        List<string> Failed = new List<string>();

        urls.Insert(0, "https://sprold.ui.ac.ir/~m.kargarfard/");
        foreach (var url in urls)
        {
            try
            {
                WebScraper.GetPageContent(url);
                Thread.Sleep(200);

                var professor = new Profile_Shakhsi.Models.Entity.Profile.ProfessorProfile();

                var titleName = GetElement(By.CssSelector("h1.card-title"))?.Text;
                professor.FullNameEn = titleName?.Split(",").FirstOrDefault("") ?? "";
                professor.Degree = titleName?.Split(",").ElementAtOrDefault(1) ?? "";
                professor.Position = GetElement(By.CssSelector("h5:nth-of-type(1)"))?.Text ?? "";
                professor.Department = GetElement(By.CssSelector("h5:nth-of-type(2)"))?.Text ?? "";
                if (string.IsNullOrEmpty(professor.FullNameEn))
                    continue;
                var exist = await _context.ProfessorProfiles.AnyAsync(x => x.FullNameEn.Equals(professor.FullNameEn) && x.Department.Equals(professor.Department));
                if (exist)
                    continue;
                professor.Email = GetElement(By.CssSelector("a[href^='mailto']"))?.Text.Replace("[at]", "@") ?? "";
                professor.Phone = GetElement(By.XPath("//dt[normalize-space()='PHONE']/following-sibling::dd"))?.Text ?? "";
                professor.Address = GetElement(By.XPath("//dt[text()='ADDRESS']/following-sibling::dd"))?.Text ?? "";
                professor.PostalCode = ExtractPostalCode(professor.Address) ?? "";
                var text = GetElement(By.XPath("//*[contains(@class, 'card-body') and contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'area of study')]"))?.Text ?? "";

                professor.AreaOfStudy = Regex.Match(text, @"Area of Study(.*?)(Education|$)", RegexOptions.Singleline).Groups[1].Value.Trim();

                professor.Research = Regex.Match(text, @"Research(.*)", RegexOptions.Singleline).Groups[1].Value.Trim();

                var professorImageDiv = GetElement(By.XPath("//div[contains(@class, 'profile-image')]"));
                var backgroundImage = professorImageDiv?.GetCssValue("background-image") ?? "";

                // استخراج آدرس تصویر از رشته backgroundImage
                var imageUrl = backgroundImage.Replace("url(", "").Replace(")", "").Replace("\"", "").Trim();

                professor.ImageUrl = await DownloadImageAndSaveAsync(imageUrl);
                // استخراج لینک‌ها (LinkedIn, ResearchGate و ... )
                professor.Articles = ExtractArticles(professor.Id);
                await _context.ProfessorProfiles.AddAsync(professor);

                professor.WebLinks = ScrapeWebLinks(professor.Id);
                await _context.AddRangeAsync(professor.WebLinks ?? new List<WebLink>());

                professor.Educations = ScrapeEducations(professor.Id);
                await _context.AddRangeAsync(professor.Educations ?? new List<Education>());

                professor.ResearchAreas = ScrapeResearchAreas(professor.Id);
                await _context.AddRangeAsync(professor.ResearchAreas ?? new List<ResearchArea>());

                //professor.Articles = 
                //await _context.AddRangeAsync(Articles);

                professor.TeachingInterests = ScrapeTeachingInterests(professor.Id);
                await _context.AddRangeAsync(professor.TeachingInterests ?? new List<TeachingInterest>());

                professor.Books = ExtractBooks(professor.Id);
                await _context.AddRangeAsync(professor.Books ?? new List<Book>());

                IWebElement coursesElement = GetElement(By.XPath("//h4[contains(text(), 'Courses')]"));

                ICollection<IWebElement> badgeElements = GetElements(coursesElement, By.XPath("..//..//h5"));

                professor.Courses = badgeElements?.Select(x => new Course
                {
                    Title = x.Text,
                }).ToList();
                await _context.AddRangeAsync(professor.Courses ?? new List<Course>());

                //[text()=Contains('Membership')]/following-sibling::p//span[@class='badge badge-pill badge-light-green mb-2']

                IWebElement professionalActivitiesElement = GetElement(By.XPath("//p[strong[contains(text(), 'Professional activities')]]"));

                // پیدا کردن همه تگ‌های P بعد از 'Professional Activities' تا قبل از 'Membership'
                professor.ProfessionalActivities = GetElements(professionalActivitiesElement,
                    By.XPath("following-sibling::p[not(preceding-sibling::p[strong[contains(text(), 'Membership')]])]"))?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Text) && x.Text != "Membership")
                    .Select(x => new ProfessionalActivity { Title = x.Text }).ToList();
                await _context.AddRangeAsync(professor.ProfessionalActivities ?? new List<ProfessionalActivity>());

                professor.Memberships = GetElements(coursesElement,
                    By.XPath("//p[strong[contains(text(), 'Membership')]]/following-sibling::/*[@class='badge badge-pill badge-light-green mb-2']"))?
                    .Select(x => new Membership { Title = x.Text, }).ToList();
                await _context.AddRangeAsync(professor.Memberships ?? new List<Membership>());

                IWebElement linksElement = GetElement(By.XPath("//h4[contains(text(), 'Links')]"));
                professor.Links = GetElements(linksElement,
                    By.XPath("..//..//a"))?
                    .Select(x => new ProfessorLink { Title = x.Text, Link = x.GetAttribute("href") }).ToList();
                await _context.AddRangeAsync(professor.Links ?? new List<ProfessorLink>());
                await _context.SaveChangesAsync();



                //IReadOnlyCollection<IWebElement> professionalActivitiesElements = WebScraper.driver.FindElements(By.XPath("//p[@class='badge-pill badge-light-green mb-2']"));
                //List<string> ProfessionalActivities = new List<string>();
                //foreach (var element in professionalActivitiesElements)
                //{
                //    ProfessionalActivities.Add(element.Text);
                //}

            }
            catch (Exception e)
            {
                Failed.Add(url);
            }
        }
        foreach (var url in Failed)
        {
            WebScraper.GetPageContent(url);
            Thread.Sleep(500);
        }
    }

    static List<Articles> ExtractArticles(int professorId)
    {
        var articlesList = new List<Articles>();
        IWebElement publicationsElement = WebScraper.driver.FindElement(By.ClassName("publicationsList"));
        string publicationsHtml = publicationsElement.GetAttribute("innerHTML");
        string articlesPattern = @"<p[^>]*>(.*?)<\/p>";
        var matches = Regex.Matches(publicationsHtml, articlesPattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            string title = Regex.Replace(match.Value, "<.*?>", "").Trim();
            //string titlePattern = @"<a[^>]*>(.*?)<\/a>";
            //    string title = Regex.Match(match.Value, titlePattern).Groups[1].Value.Trim();
            if (title == "Articles" || title == "More Articles")
                continue;
            string linkPattern = @"href=""(.*?)""";
            string link = Regex.Match(match.Value, linkPattern)?.Groups[1]?.Value.Trim() ?? "";

            articlesList.Add(new Articles
            {
                Title = title,
                Link = link,
                ProfessorProfileId = professorId
            });
        }

        return articlesList;
    }

    static List<Book> ExtractBooks(int professorId)
    {
        var booksList = new List<Book>();
        IWebElement publicationsElement = WebScraper.driver.FindElement(By.ClassName("publicationsList"));
        string publicationsHtml = publicationsElement.GetAttribute("innerHTML");
        string booksPattern = @"<div class=""book"">(.*?)<\/div>";
        string booksContent = Regex.Match(publicationsHtml, booksPattern, RegexOptions.Singleline).Groups[1].Value;

        string bookPattern = @"<p[^>]*>(.*?)<\/p>";
        var matches = Regex.Matches(booksContent, bookPattern, RegexOptions.Singleline);

        //int idCounter = 1;
        foreach (Match match in matches.Skip(1))
        {
            //string titlePattern = @"<strong><em>(.*?)<\/em><\/strong>";
            //string title = Regex.Match(match.Value, titlePattern).Groups[1].Value.Trim();
            var title = match.Groups[1].Value.Trim();
            if (title == "More books")
                booksList.Add(new Book
                {
                    Title = title,
                    ProfessorProfileId = professorId
                });
        }

        return booksList;
    }
    private static IWebElement GetElement(By by)
    {
        try
        {
            return WebScraper.driver.FindElement(by);
        }
        catch (Exception)
        {
            return null; // در صورت بروز هر نوع خطای دیگر، مقدار null برگردانده می‌شود
        }
    }
    private static IWebElement GetElement(IWebElement webElement, By by)
    {
        try
        {
            return webElement.FindElement(by);
        }
        catch (Exception)
        {
            return null; // در صورت بروز هر نوع خطای دیگر، مقدار null برگردانده می‌شود
        }
    }
    private static ICollection<IWebElement> GetElements(By by)
    {
        try
        {
            var result = WebScraper.driver.FindElements(by);
            return result;
        }
        catch (Exception)
        {
            return new List<IWebElement>(); // در صورت بروز هر نوع خطای دیگر، مقدار null برگردانده می‌شود
        }
    }
    private static ICollection<IWebElement> GetElements(IWebElement webElement, By by)
    {
        try
        {
            var result = webElement.FindElements(by);
            return result;
        }
        catch (Exception)
        {
            return null; // در صورت بروز هر نوع خطای دیگر، مقدار null برگردانده می‌شود
        }
    }
    private static ICollection<WebLink> ScrapeWebLinks(int professorId)
    {
        var links = new List<WebLink>();
        var webLinkElements = GetElements(By.XPath("//dd[@class='web-links']/a"));
        foreach (var element in webLinkElements)
        {
            links.Add(new WebLink
            {
                Title = element.Text,
                Link = element.GetAttribute("href"),
                ProfessorProfileId = professorId
            });
        }
        return links;
    }

    private static ICollection<Education> ScrapeEducations(int professorId)
    {
        var educations = new List<Education>();
        var educationElements = GetElements(By.XPath("//h2[contains(text(), 'Education')]/following-sibling::div//p"));
        foreach (var element in educationElements.SkipLast(1))
        {
            educations.Add(new Education
            {
                Title = element.Text,
                ProfessorProfileId = professorId
            });
        }
        return educations;
    }

    private static ICollection<ResearchArea> ScrapeResearchAreas(int professorId)
    {
        var researchAreas = new List<ResearchArea>();
        var researchElements = GetElements(By.XPath("//h5[contains(text(), 'Area of Study')]/following-sibling::p/span"));
        foreach (var element in researchElements)
        {
            researchAreas.Add(new ResearchArea
            {
                Title = element.Text,
                ProfessorProfileId = professorId
            });
        }
        return researchAreas;
    }

    private static ICollection<Articles> ScrapeArticles(int professorId)
    {
        var articles = new List<Articles>();
        var articleElements = GetElements(By.XPath("//div[@id='publication']//a"));
        foreach (var element in articleElements)
        {
            articles.Add(new Articles
            {
                Title = element.Text,
                Link = element.GetAttribute("href"),
                ProfessorProfileId = professorId
            });
        }
        return articles;
    }
    private static ICollection<TeachingInterest> ScrapeTeachingInterests(int professorId)
    {
        var teachingInterests = new List<TeachingInterest>();
        var teachingElements = GetElements(By.XPath("//div[@id='TeachingInterest']//p"));
        foreach (var element in teachingElements)
        {
            teachingInterests.Add(new TeachingInterest
            {
                Title = element.Text,
                ProfessorProfileId = professorId
            });
        }
        return teachingInterests;
    }
    public static async Task<string> DownloadImageAndSaveAsync(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return "";
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // دانلود عکس
                byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                // ساخت نام فایل با استفاده از GUID
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageUrl);
                string folderPath = Path.Combine(WebScraper.FindDirectoryInParents(), "ProfileImage"); // مسیر پوشه‌ای که می‌خواهید تصویر را ذخیره کنید

                // اطمینان از وجود پوشه
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // مسیر کامل برای ذخیره تصویر
                string filePath = Path.Combine(folderPath, fileName);

                // ذخیره تصویر در پوشه
                await File.WriteAllBytesAsync(filePath, imageBytes);

                // برگرداندن آدرس جدید تصویر
                return fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
                return "";
            }
        }
    }

    private static string ExtractPostalCode(string address)
    {
        var postalCodeKeyword = "Postal code: ";
        var startIndex = address.IndexOf(postalCodeKeyword);
        if (startIndex != -1)
        {
            startIndex += postalCodeKeyword.Length;
            var postalCode = address.Substring(startIndex, 10); // فرض می‌کنیم کدپستی 10 رقمی است
            return postalCode;
        }
        return null;
    }
}
