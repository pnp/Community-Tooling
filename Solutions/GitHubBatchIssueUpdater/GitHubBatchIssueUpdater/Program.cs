using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace GitHubBatchIssueUpdater
{
    class Program
    {
        static void Main()
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            var userId = GetValueConfigOrInput("UserId", "GitHub user id");
            var userPwd = GetValueConfigOrInput("Pwd", "GitHub password or access token"); 
            var org = GetValueConfigOrInput("Organization", "GitHub organization");
            var repositoryToUpdate = GetValueConfigOrInput("Repository", "GitHub organization");
            var labelException = GetValueConfigOrInput("IgnoreLabel", "Ignore with label value of this (empty = no filter)");
            int olderThanDays = int.Parse(GetValueConfigOrInput("Days", "How many days without activity having issues will be closed (number)"));
            var closingComment = ReadCommentFromTxt(olderThanDays);

            var client = new GitHubClient(new ProductHeaderValue("my-issue-batch-updater"));
            var basicAuth = new Credentials(userId, userPwd);
            client.Credentials = basicAuth;

            // Connect to the GitHub and do the trick
            var repository = await client.Repository.Get(org, repositoryToUpdate);

            // Get all open issues from the repository
            var issuesForOctokit = await client.Issue.GetAllForRepository(org, repositoryToUpdate);
            foreach (var item in issuesForOctokit)
            {
                // Skip issues with specific label
                if (item.Labels.Any(i => i.Name == labelException))
                {
                    continue;
                }

                if (item.UpdatedAt < DateTime.Now.AddDays(olderThanDays * -1))
                {
                    Console.WriteLine(string.Format("Closing item #{0} with title of '{1}'.", item.Number, item.Title));

                    // Adding a new comment to the issue
                    var comment = client.Issue.Comment.Create(repository.Id, item.Number, closingComment);

                    // Close the given issue
                    var issue = await client.Issue.Get(org, repositoryToUpdate, item.Number);
                    var update = issue.ToUpdate();
                    update.State = ItemState.Closed;

                    // Call back to close the issue
                    await client.Issue.Update(repository.Id, item.Number, update);
                }

            }
            Console.WriteLine("---");
            Console.ReadKey();
        }

        /// <summary>
        /// Reads the closing comment from text file and replaces optionally day marker in the file 
        /// dynamically based on provided values.
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        private static string ReadCommentFromTxt(int days)
        {
            string content =  System.IO.File.ReadAllText(System.IO.Path.Combine(System.Environment.CurrentDirectory, "ClosingComment.txt"));
            if (content.Contains("XYZ_days"))
            {
                return content.Replace("XYZ_days", days.ToString());
            }
            return content;
        }

        /// <summary>
        /// Get value either from app.config or request that from the user
        /// </summary>
        /// <param name="key"></param>
        /// <param name="label"></param>
        /// <param name="isPassword"></param>
        /// <returns></returns>
        private static string GetValueConfigOrInput(string key, string label, bool isPassword = false)
        {
            // User id
            if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings[key]))
            {
                return System.Configuration.ConfigurationManager.AppSettings[key];
            } 
            else
            {
                return GetInput(label, isPassword);
            }
        }

        /// <summary>
        /// Generic method to request the needed details for the operations. Used if values are not given from the app.config
        /// </summary>
        /// <param name="label"></param>
        /// <param name="isPassword"></param>
        /// <param name="defaultForeground"></param>
        /// <returns></returns>
        private static string GetInput(string label, bool isPassword = false, ConsoleColor defaultForeground = ConsoleColor.White)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("{0} : ", label);
            Console.ForegroundColor = defaultForeground;

            string value = "";

            for (ConsoleKeyInfo keyInfo = Console.ReadKey(true); keyInfo.Key != ConsoleKey.Enter; keyInfo = Console.ReadKey(true))
            {
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (value.Length > 0)
                    {
                        value = value.Remove(value.Length - 1);
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                }
                else if (keyInfo.Key != ConsoleKey.Enter)
                {
                    if (isPassword)
                    {
                        Console.Write("*");
                    }
                    else
                    {
                        Console.Write(keyInfo.KeyChar);
                    }
                    value += keyInfo.KeyChar;

                }

            }
            Console.WriteLine("");

            return value;
        }
    }
}





