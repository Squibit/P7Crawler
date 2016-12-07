﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using ReviewCrawler.Products.Reviews;

namespace ReviewCrawler.Sites
{
    abstract class ReviewSite : Site
    {
        public Review review;

        public override abstract bool Parse(string siteData, string sQueueData);
        public override abstract void Crawl(string siteData, string sQueueData);
        public abstract string GetProductType(string tempLink);
        //public abstract string GetSiteKey(string url);

        public string removeTagsFromReview(string siteData)
        {
            string tempString = "";

            foreach (
                Match item in
                Regex.Matches(siteData, "((<p>|<p style|<p align=\"left\">).*?(<\\/p>))+", RegexOptions.IgnoreCase))
            {
                tempString += item + "\n";
            }

            tempString = TagRemoval(tempString);

            return tempString;
        }

        public string TagRemoval(string tempString)
        {
            Regex newlineAdd = new Regex("<br />", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Regex regexHtml = new Regex("(<.*?>)+", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Regex apostropheRemover = new Regex("\\&rsquo\\;", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Regex garbageRemover = new Regex("\\&nbsp\\;", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            tempString = newlineAdd.Replace(tempString, " ");
            tempString = regexHtml.Replace(tempString, "");
            tempString = apostropheRemover.Replace(tempString, "");
            tempString = garbageRemover.Replace(tempString, " ");

            tempString += "\n";

            return tempString;
        }

        public override void AddItemToDatabase(MySqlConnection connection)
        {
            review.connection = connection;
            review.AddReviewToDB();
        }
    }
}