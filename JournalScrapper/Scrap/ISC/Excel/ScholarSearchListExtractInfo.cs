using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace JournalScrapper.Scrap
{
	public static class ScholarSearchListExtractInfo
	{
		public static async Task ExtractScrap()
		{
			string baseDirectory = WebScraper.FindDirectoryInParents();
			string csvFilePath = Path.Combine(baseDirectory, "ProfileList.csv");
			string imagesDirectory = Path.Combine(baseDirectory, "images");

			// اطمینان از وجود پوشه images
			if (!Directory.Exists(imagesDirectory))
			{
				Directory.CreateDirectory(imagesDirectory);
			}

			// نوشتن سرستون‌ها در فایل CSV
			using (StreamWriter writer = new StreamWriter(csvFilePath, false))
			{
				writer.WriteLine("Id, Name, Affi, Email, Citation, Keywords");
			}

			WebScraper.GetPageContent("https://scholar.google.com/citations?view_op=search_authors&hl=en&mauthors=carbon&before_author=Wpyl_89KAAAJ&astart=0");

			while (true)
			{
				try
				{
					// استخراج عناصر مربوطه
					IReadOnlyCollection<IWebElement> productElements = WebScraper.driver.FindElements(By.ClassName("gs_ai"));
					foreach (IWebElement productElement in productElements)
					{
						string name = productElement.FindElement(By.ClassName("gs_ai_name")).Text.Replace(",", " ");
						string affi = productElement.FindElement(By.ClassName("gs_ai_aff")).Text.Replace(",", " ");
						string email = productElement.FindElement(By.ClassName("gs_ai_eml")).Text.Replace(",", " ").Replace("Verified email at", "", StringComparison.CurrentCultureIgnoreCase);
						string citation = productElement.FindElement(By.ClassName("gs_ai_cby")).Text.Replace(",", " ").Replace("Cited by ", "", StringComparison.CurrentCultureIgnoreCase);
						string id = productElement.FindElement(By.CssSelector("h3.gs_ai_name > a")).GetAttribute("href").Split("user=")[1];
						var items = productElement.FindElements(By.CssSelector(".gs_ai_one_int"));
						string keywords = string.Join(",", items.Select(x => x.Text));

						// استخراج لینک تصویر
						//string imageUrl = productElement.FindElement(By.CssSelector(".gs_ai_pho img")).GetAttribute("src");

						//// ذخیره تصویر
						//string localImagePath = Path.Combine(imagesDirectory, $"{name}-{id}.jpg");
						//var handler = new HttpClientHandler
						//{
						//	ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true // غیرفعال کردن بررسی SSL
						//};
						//using (HttpClient httpClient = new HttpClient(handler))
						//{
						//	try
						//	{
						//		httpClient.DefaultRequestHeaders.Add("User-Agent",WebScraper.GetRandomUserAgent());
						//		Thread.Sleep(300);
						//		byte[] imageBytes = httpClient.GetByteArrayAsync(imageUrl).Result;
						//		File.WriteAllBytes(localImagePath, imageBytes);
						//	}
						//	catch (Exception ex)
						//	{
						//		Console.WriteLine($"Error downloading image: {ex.Message}");
						//		localImagePath = "ErrorDownloadingImage";
						//	}
						//}
						//localImagePath = localImagePath.Split("Extra\\").Last();
						// نوشتن اطلاعات در فایل CSV
						using (StreamWriter writer = new StreamWriter(csvFilePath, true))
						{
							writer.WriteLine($"{id}, {name}, {affi}, {email}, {citation}, {keywords}");
						}
					}

					// کلیک روی دکمه بعدی
					IWebElement nextButton = WebScraper.driver.FindElement(By.CssSelector("button.gs_btnPR.gs_in_ib.gs_btn_half.gs_btn_lsb.gs_btn_srt.gsc_pgn_pnx"));
					if (nextButton.GetAttribute("disabled") != null)
						break;

					nextButton.Click();
					Thread.Sleep(300); // تأخیر کوتاه برای بارگذاری صفحه بعدی
				}
				catch (NoSuchElementException)
				{
					Console.WriteLine("No more pages to scrape.");
					break;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"An error occurred: {ex.Message}");
					break;
				}
			}

			WebScraper.driver.Quit();
		}

	}
}
