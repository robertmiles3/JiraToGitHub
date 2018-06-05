using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Jira;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace JiraToGitHub
{
    public class JiraToGitHubService : IHostedService
    {
        private readonly ILogger Logger;
        private readonly AppConfig Config;
        private readonly IIssueConverter IssueConverter;

        public JiraToGitHubService(ILogger<JiraToGitHubService> logger, IOptions<AppConfig> appConfig, IIssueConverter issueConverter)
        {
            Logger = logger;
            Config = appConfig.Value;
            IssueConverter = issueConverter;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"{nameof(JiraToGitHubService)}:{nameof(StartAsync)}: STARTED");

            var jira = Jira.CreateRestClient(Config.JiraUrl, Config.JiraUsername, Config.JiraPassword);
            jira.Issues.MaxIssuesPerRequest = int.MaxValue;

            // TODO: replace JQL with proper search for issues that need to migrate
            var issues = await jira.Issues.GetIssuesFromJqlAsync("project = foo", maxIssues: int.MaxValue, token: cancellationToken);

            var issueCount = issues?.Count();
            Logger.LogInformation($"Found {issueCount ?? 0} issues to transfer");

            if (issueCount > 0)
            {
                var github = new GitHubClient(new ProductHeaderValue(nameof(JiraToGitHub)))
                {
                    Credentials = new Credentials(Config.GitHubPersonalAccessToken)
                };

                foreach (var issue in issues)
                {
                    // TODO: Uncomment below if using a custom field to determine issues already migrated
                    // if (issue.CustomFields.Any(i => i.Name == "GitHub Issue" && i.Values?.Length > 0))
                        // continue;

                    try
                    {
                        Logger.LogInformation($"Converting issue {issue.Key}...");
                        var result = IssueConverter.ToGitHubIssue(issue);

                        Logger.LogInformation($"Creating issue {issue.Key} on GitHub...");
                        var ghIssue = await github.Issue.Create(result.RepoOwner, result.RepoName, result.Issue);
                        Logger.LogInformation($"Created Jira issue {issue.Key} as GitHub {result.RepoOwner}/{result.RepoName} #{ghIssue.Number}");

                        // TODO: Uncomment below if using a custom field to determine issues already migrated
                        // Logger.LogInformation($"Adding GitHub Issue# to custom fields...");
                        // issue.CustomFields.Add("GitHub Issue", ghIssue.Number.ToString());
                        // await issue.SaveChangesAsync();

                        Logger.LogInformation($"Getting comments for {issue.Key}...");
                        var comments = await issue.GetCommentsAsync();
                        var commentCount = comments?.Count();
                        Logger.LogInformation($"Found {commentCount ?? 0} comments to transfer for {issue.Key}");

                        if (commentCount > 0)
                        {
                            Logger.LogInformation($"Creating comments for {issue.Key} on GitHub...");
                            foreach (var comment in comments.OrderBy(c => c.CreatedDate))
                            {
                                // TODO: Any custom formatting of the comment.Body
                                
                                await github.Issue.Comment.Create(result.RepoOwner, result.RepoName, ghIssue.Number, comment.Body.Trim());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, null, null);
                    }
                }
            }

            Logger.LogInformation($"{nameof(JiraToGitHubService)}:{nameof(StartAsync)}: FINISHED");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}