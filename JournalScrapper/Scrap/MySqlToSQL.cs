using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper.Scrap
{
    public class MySqlToSQL
    {
        public async Task MigrateDataAsync()
        {
            using (var sourceContext = new ISCMySqlDbContext())
            using (var destinationContext = new ProfileShakhsiDbContext())
            {
                const int batchSize = 1000; // اندازه هر Batch

                // پردازش All_Articles
                await MigrateTableInBatches(sourceContext.scholar_all_article, destinationContext, destinationContext.All_Articles, batchSize);

                // پردازش ResearcherFavorites
                await MigrateTableInBatches(sourceContext.areas_of_interest, destinationContext, destinationContext.ResearcherFavorites, batchSize);

                // پردازش Keywords
                await MigrateTableInBatches(sourceContext.keywords_articles_isc_xml, destinationContext, destinationContext.Keywords, batchSize);

                // پردازش Authors
                await MigrateTableInBatches(sourceContext.scholar_profile_authors, destinationContext, destinationContext.Authors, batchSize);

                // پردازش ISC_Articles
                await MigrateTableInBatches(sourceContext.article_isc_xml, destinationContext, destinationContext.ISC_Articles, batchSize);

                // پردازش Author_Articles
                await MigrateTableInBatches(sourceContext.author_article_relation, destinationContext, destinationContext.Author_Articles, batchSize);

                // پردازش Journals
                await MigrateTableInBatches(sourceContext.iranian_journals, destinationContext, destinationContext.Journals, batchSize);

                // پردازش Author_ISCs
                await MigrateTableInBatches(sourceContext.authors_isc_xml, destinationContext, destinationContext.Author_ISCs, batchSize);

                // پردازش CitationAll_Articles
                await MigrateTableInBatches(sourceContext.citation_article_scholar, destinationContext, destinationContext.CitationAll_Articles, batchSize);

                // پردازش CitationAuthors
                await MigrateTableInBatches(sourceContext.all_citation_authors, destinationContext, destinationContext.CitationAuthors, batchSize);

                // پردازش InputMasters
                //await MigrateTableInBatches(sourceContext.InputMasters, destinationContext, destinationContext.InputMasters, batchSize);

                // پردازش Author_Article_ISCs
                await MigrateTableInBatches(sourceContext.author_article_isc_relation, destinationContext, destinationContext.Author_Article_ISCs, batchSize);
            }
        }

        private async Task MigrateTableInBatches<TEntity>(DbSet<TEntity> sourceTable, DbContext destinationContext, DbSet<TEntity> destinationTable, int batchSize)
            where TEntity : class
        {
            int totalRecords = await sourceTable.CountAsync();
            for (int i = 0; i < totalRecords; i += batchSize)
            {
                var batch = await sourceTable.Skip(i).Take(batchSize).ToListAsync();
                destinationTable.AddRange(batch);
                await destinationContext.SaveChangesAsync(); // از DbContext مقصد به صورت مستقیم استفاده کنید
            }
        }


    }
}
