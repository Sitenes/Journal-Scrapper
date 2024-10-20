using JournalScrapper;
using JournalScrapper.Scrap;

//await ExtractISC.ScrapISC();
//CsvToDatabase.ReadProfessorInfoFromCsv();

//await ExtractProfessorProfile.ScrapProfessorProfile();
MySqlToSQL sql = new MySqlToSQL();
await sql.MigrateDataAsync();
//CsvToDatabase.ReadProfessorInfoFromCsv();
