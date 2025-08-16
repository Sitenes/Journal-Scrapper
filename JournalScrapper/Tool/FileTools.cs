using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper.Tool
{
    public static class FileTools
    {
        public static string FindDirectoryInParents(string directoryName = "Extra")
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            while (!string.IsNullOrEmpty(currentDirectory))
            {
                string potentialPath = Path.Combine(currentDirectory, directoryName);
                if (Directory.Exists(potentialPath))
                {
                    return potentialPath;
                }

                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }
            return "";
        }
    }
}
