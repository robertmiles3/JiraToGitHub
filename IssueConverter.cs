using System;
using Microsoft.Extensions.Logging;

namespace JiraToGitHub
{
    public interface IIssueConverter
    {
        (string RepoOwner, string RepoName, Octokit.NewIssue Issue) ToGitHubIssue(Atlassian.Jira.Issue issue);
    }

    public class IssueConverter : IIssueConverter
    {
        private readonly ILogger Logger;

        public IssueConverter(ILogger<IssueConverter> logger) => Logger = logger;
        
        public (string RepoOwner, string RepoName, Octokit.NewIssue Issue) ToGitHubIssue(Atlassian.Jira.Issue issue)
        {
            var ghIssue = new Octokit.NewIssue(issue.Summary)
            {
                Body = issue.Description
            };
            
            // Map Jira epic to GitHub repo
            var epic = issue["Epic Link"].Value;
            if (!Constants.JiraEpicToGitHubRepo.TryGetValue(epic, out var repo))
                throw new Exception($"FAILED to map {issue.Key} epic {epic} to a GitHub repo");
            var repoOwner = repo.RepoOwner;
            var repoName = repo.RepoName;

            // Map Jira assignee to GitHub assignee
            if (!string.IsNullOrEmpty(issue.Assignee) && Constants.JiraUserToGitHubUser.TryGetValue(issue.Assignee, out var gitHubAssignee))
                ghIssue.Assignees.Add(gitHubAssignee);

            // TODO: any other field mappings

            return (repoOwner, repoName, ghIssue);
        }
    }
}