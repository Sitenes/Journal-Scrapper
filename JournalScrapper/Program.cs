﻿using JournalScrapper;
using JournalScrapper.Scrap;
using JournalScrapper.Scrap.FromCSV;
using Microsoft.Extensions.Configuration;


await ExtractISC.ScrapISC();
//CsvToDatabase.ReadProfessorInfoFromCsv();
//var configuration = new ConfigurationBuilder()
//    .AddInMemoryCollection([new KeyValuePair<string, string?>("ArticleUrl", "https://jcr.isc.ac/main.aspx")]).Build();

//var journalScrapper = new JournalScrapper.Scrap.JournalScrapper(configuration);
//await journalScrapper.Scrap();

//await ExtractProfessorProfile.ScrapProfessorProfile();
//MySqlToSQL sql = new MySqlToSQL();
//await sql.MigrateDataAsync();
//ExtractPersonnelData.ReadPersonnelDataFromCsv();
//CsvToDatabase.ReadProfessorInfoFromCsv();
//ScuposCSVtoDB.ExctractScuposCSVtoDB();