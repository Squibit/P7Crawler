﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReviewCrawler.Products.Reviews;
using System.Text.RegularExpressions;
using System.IO;
using MySql.Data.MySqlClient;

namespace ReviewCrawler.Sites.Sub
{
    class SiteGuru3d : ReviewSite
    {
        private const double maxRating = 5;
        //Used to get the productype transfered to the parse method
        private Dictionary<string, string> productTypes = new Dictionary<string, string>();

        public SiteGuru3d()
        {
            domainUrl = "http://www.guru3d.com/";
            //searchQueue.Enqueue("http://www.guru3d.com/articles-categories/videocards.html");
            //searchQueue.Enqueue("http://www.guru3d.com/articles-categories/processors.html");
            //searchQueue.Enqueue("http://www.guru3d.com/articles-categories/mainboards.html");
            //searchQueue.Enqueue("http://www.guru3d.com/articles-categories/memory-(ddr2%7Cddr3)-and-storage-(hdd%7Cssd).html");
            //searchQueue.Enqueue("http://www.guru3d.com/articles-categories/pc-cases-and-modding.html");
            searchQueue.Enqueue("http://www.guru3d.com/articles-categories/psu-power-supply-units.html");
        }

        public override void CrawlPage(string siteData)
        {
            string tempLink = "";
            List<string> tempReviewLinks;
            string tempProductType = "unknown";

            if (currentSite.Contains("articles-categories"))
            {
                tempLink = GetSearchLinks(siteData, "pagelinkselected", "pagelink", false);
                //Returns domainUrl if no link is found
                if (tempLink != domainUrl)
                {
                    searchQueue.Enqueue(tempLink);
                }
                tempProductType = GetProductType(currentSite);
            }

            tempReviewLinks = GetItemLinks(siteData, "<br />", "<a href=\"articles-pages", "<div class=\"content\">",
                true);
            foreach (var item in tempReviewLinks)
            {
                searchQueue.Enqueue(item);
                productTypes.Add(item, tempProductType);
            }
            CrawlReviewPages(siteData);
        }

        public void CrawlReviewPages(string siteData)
        {
            string tempLink = "";

            if (currentSite.Contains(",1.html"))
            {
                string siteNumber = Regex.Match(siteData, "<span class=\"pagelink\">.*? pages</span>").Value;
                siteNumber = siteNumber.Replace("<span class=\"pagelink\">", "");
                siteNumber = siteNumber.Replace(" pages</span>", "").Trim();
                int totalPages = int.Parse(siteNumber);

                for (int i = 1; i <= totalPages; i++)
                {
                    tempLink = currentSite.Replace("1.html", i + ".html");
                    itemQueue.Enqueue(tempLink);
                }
                itemQueue.Enqueue(GetSiteKey(currentSite.Replace("articles-pages", "articles-summary")));
            }
        }


        public override string GetSiteKey(string url) //evt rename, not used as key anymore
        {
            for (int i = url.Length - 1; i > 0; i--)
            {
                if (url[i] == ',')
                {
                    url = url.Remove(i, url.Length - i);
                    break;
                }
            }

            return url;
        }

        public override string GetProductType(string tempLink)
        {
            if (tempLink.Contains("articles-categories/videocards"))
            {
                return "GPU";
            }
            else if (tempLink.Contains("articles-categories/processors"))
            {
                return "CPU";
            }
            else if (tempLink.Contains("articles-categories/soundcards-and-speakers"))
            {
                return "SoundCard";
            }
            else if (tempLink.Contains("articles-categories/mainboards"))
            {
                return "Motherboard";
            }
            else if (tempLink.Contains("articles-categories") && tempLink.Contains("memory") && tempLink.Contains("storage"))
            {
                return "RAM/HDD";
            }
            else if (tempLink.Contains("articles-categories/pc-cases-and-modding"))
            {
                return "Chassis";
            }
            else if (tempLink.Contains("articles-categories/psu-power-supply-units"))
            {
                return "PSU";
            }
            else if (tempLink.Contains("articles-categories/cooling"))
            {
                return "Cooling";
            }
            else
            {
                Debug.WriteLine("couldnt determine product type - GetProductType, guru3d");
            }

            return "";
        }

        public override bool Parse(string siteData)
        {
            string siteContentParsed = removeTagsFromReview(siteData);
            string tempProductType = "";

            if (!currentSite.Contains("articles-summary"))
            {
                if (currentSite.Contains(",1.html"))
                {
                    if (productTypes.ContainsKey(currentSite))
                    {
                        tempProductType = productTypes[currentSite];
                        productTypes.Remove(currentSite);
                    }
                    else
                    {
                        tempProductType = "unknown";
                    }

                    review = new Review(currentSite, tempProductType, true);
                    review.title = GetTitle(siteData);
                    review.productRating = GetRating(siteData);
                    review.maxRating = maxRating;
                    review.crawlDate = DateTime.Now;
                    review.reviewDate = GetReviewDate(siteData);
                }
                review.content += siteContentParsed;
            }
            else
            {
                review.comments = GetReviewComments(siteData);

                return true;
            }
            return false;
        }

        public string GetTitle(string data)
        {
            string result = "";

            result =
                Regex.Match(data, "<meta itemprop=\"name\" property=\"og:title\" content=\".*?/>",
                    RegexOptions.Singleline).Value;

            result = result.Replace("<meta itemprop=\"name\" property=\"og:title\" content=\"", "");
            result = result.Replace("\" />", "");

            return result;
        }

        public List<ReviewComment> GetReviewComments(string siteData) //look into nested quotes
        {
            List<ReviewComment> commentResults = new List<ReviewComment>();
            Regex regexTags = new Regex("(<.*?>)+", RegexOptions.Singleline);
            Regex regexDateRemove = new Regex("(<strong>Posted on:(.*?)</strong>)", RegexOptions.Singleline);
            Regex regexQuote = new Regex("(<table(.*?)</table>)", RegexOptions.Singleline);
            Regex regexQuoteBlock = new Regex("(<div class=\"quoteblock(.*?)</div>)", RegexOptions.Singleline);
            string tempComment;

            string[] commentSplit = siteData.Split(new string[] {"<div class=\"comments\">"}, StringSplitOptions.None);

            for (int i = 0; i < commentSplit.Length; i++)
            {
                tempComment =
                    Regex.Match(commentSplit[i], "(<strong>Posted on:.*?<!-- Template: articles)",
                        RegexOptions.Singleline).Value;
                if (tempComment != "")
                {
                    tempComment += '>';
                }


                tempComment = regexDateRemove.Replace(tempComment, "");
                tempComment = regexTags.Replace(tempComment, "");
                tempComment = regexQuote.Replace(tempComment, "");
                tempComment = regexQuoteBlock.Replace(tempComment, "");

                if (tempComment != "")
                {
                    commentResults.Add(new ReviewComment(tempComment, 0));
                }
            }
            return commentResults;
        }

        public DateTime GetReviewDate(string siteData)
        {
            DateTime date;
            string temp = Regex.Match(siteData, "<span itemprop=\"dtreviewed\">.*?</span>").Value;


            temp = temp.Replace("<span itemprop=\"dtreviewed\">", "");
            temp = temp.Replace("</span>", "");

            string[] tempDate = temp.Split('/');

            tempDate[2] = tempDate[2].Substring(0, 4);

            if ((tempDate[0])[0] == '0')
            {
                tempDate[0] = (tempDate[0])[1].ToString();
            }

            int day = int.Parse(tempDate[1]);
            int month = int.Parse(tempDate[0]);
            int year = int.Parse(tempDate[2]);

            date = new DateTime(year, month, day);

            return date;
        }

        public double GetRating(string siteData)
        {
            string strValue = "";
            string temp = Regex.Match(siteData, "<meta itemprop=\"value\" content=\".*?\"/>").Value;

            for (int i = 0; i < temp.Length - 1; i++)
            {
                if (char.IsNumber(temp[i]))
                {
                    strValue += temp[i];
                }
            }
            if (strValue != "")
            {
                return double.Parse(strValue);
            }
            else
            {
                return -1;
            }
        }

        public override void LoadCrawlerState(MySqlConnection connection)
        {
            connection.Open();
            MySqlCommand command = new MySqlCommand("SELECT * FROM CrawlProgress WHERE Site=@site", connection);
            command.Parameters.AddWithValue("@Site", this.GetType().ToString());

            MySqlDataReader reader = command.ExecuteReader();


            if (reader.Read())
            {
                string queue = (string)reader.GetValue(1);
                string[] queueSplit = queue.Split(new string[] { "%%&&##" }, StringSplitOptions.None);
                string[] sQueue = queueSplit[0].Split(new string[] { "#%&/#" }, StringSplitOptions.None);
                string[] iQueue = queueSplit[1].Split(new string[] { "#%&/#" }, StringSplitOptions.None);
                string[] pTypes = queueSplit[2].Split(new string[] { "#%&/#" }, StringSplitOptions.None);

                for (int i = 0; i < sQueue.Length - 1; i++)
                {
                    searchQueue.Enqueue(sQueue[i]);
                }
                for (int i = 0; i < iQueue.Length - 1; i++)
                {
                    itemQueue.Enqueue(iQueue[i]);
                }
                for (int i = 0; i < pTypes.Length - 1; i++)
                {
                    string[] tempTypes = pTypes[i].Split(new string[] { "####%%%%####" }, StringSplitOptions.None);
                    productTypes.Add(tempTypes[0], tempTypes[1]);
                }
            }
            reader.Close();
            connection.Close();
        }

        public override void SaveCrawlerState(MySqlConnection connection)
        {
            connection.Open();
            if (DoesSiteExist(connection))
            {
                InsertSiteInDB(connection);
            }


            string queue = "";
            while (searchQueue.Count > 0)
            {
                queue += (searchQueue.Dequeue() + "#%&/#");
            }
            queue += "%%&&##";
            while (itemQueue.Count > 0)
            {
                queue += (itemQueue.Dequeue() + "#%&/#");
            }
            queue += "%%&&##";
            foreach (var item in productTypes)
            {
                queue += (item.Key + "####%%%%####" + item.Value + "#%&/#");
            }



            MySqlCommand command =
                new MySqlCommand("UPDATE CrawlProgress SET Queue = @queue && date = @date WHERE site=@site", connection);
            command.Parameters.AddWithValue("@queue", queue);
            command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@site", this.GetType().ToString());
            command.ExecuteNonQuery();

            connection.Close();
        }
    }
}