using Microsoft.AspNetCore.Components;

namespace SuggestionAppUI.Pages;

public partial class Details
{
    [Parameter]
    public string Id { get; set; }

    private SuggestionModel suggestion;
    private UserModel loggedInUser;
    private List<StatusModel> statuses;
    private string settingStatus = "";
    private string urlText = "";
    protected async override Task OnInitializedAsync()
    {
        // Get data of the suggestion that was selected
        suggestion = await suggestionData.GetSuggestion(Id);
        loggedInUser = await authProvider.GetUserFromAuth(userData);
        statuses = await statusData.GetAllStatuses();
    }

    private async Task CompleteSetStatus()
    {
        switch (settingStatus)
        {
            case "completed":
                if (string.IsNullOrWhiteSpace(urlText))
                    return;
                suggestion.SuggestionStatus = statuses.Where(s => s.StatusName.ToLower() == settingStatus.ToLower()).First();
                suggestion.OwnerNotes = $"You are right, this is an important topic for developers. We created resource about it here: <a href='{urlText}' target='_blank'>{urlText}</a>";
                break;
            case "watching":
                suggestion.SuggestionStatus = statuses.Where(s => s.StatusName.ToLower() == settingStatus.ToLower()).First();
                suggestion.OwnerNotes = "We noticed the interest this suggestion is getting! If more people are interested we may address this topic in an upcoming resource.";
                break;
            case "upcoming":
                suggestion.SuggestionStatus = statuses.Where(s => s.StatusName.ToLower() == settingStatus.ToLower()).First();
                suggestion.OwnerNotes = "Great suggestion! We have a resource in the pipeline to address this topic. ";
                break;
            case "dismissed":
                suggestion.SuggestionStatus = statuses.Where(s => s.StatusName.ToLower() == settingStatus.ToLower()).First();
                suggestion.OwnerNotes = "Sometimes a good idea doesn't fit within our scope and vision. This is one of those ideas. ";
                break;
            default:
                return;
        }

        settingStatus = null;
        await suggestionData.UpdateSuggestion(suggestion);
    }

    private void ClosePage()
    {
        navManager.NavigateTo("/");
    }

    private string GetUpvoteTopText()
    {
        if (suggestion.UserVotes?.Count > 0)
        {
            return suggestion.UserVotes.Count.ToString("00"); //01,02,...10
        }
        else
        {
            if (suggestion.Author.Id == loggedInUser?.Id)
            {
                return "Awaiting";
            }
            else
            {
                return "Click To";
            }
        }
    }

    private string GetUpvoteBottomText()
    {
        if (suggestion.UserVotes?.Count > 1)
        {
            return "Upvotes";
        }
        else
        {
            return "Upvote";
        }
    }

    private async Task VoteUp()
    {
        if (loggedInUser is not null)
        {
            if (suggestion.Author.Id == loggedInUser.Id)
            {
                // Can't vote on your own suggestion
                return;
            }

            // UserVotes is a hashSet and cannot have duplicate values
            // Does it locally
            if (suggestion.UserVotes.Add(loggedInUser.Id) == false)
            {
                suggestion.UserVotes.Remove(loggedInUser.Id);
            }

            // Update to database
            await suggestionData.UpVoteSuggestion(suggestion.Id, loggedInUser.Id);
        }
        else
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
        }
    }

    private string GetVoteClass()
    {
        if (suggestion.UserVotes is null || suggestion.UserVotes.Count == 0)
        {
            return "suggestion-detail-no-votes";
        }
        else if (suggestion.UserVotes.Contains(loggedInUser?.Id))
        {
            return "suggestion-detail-voted";
        }
        else
        {
            return "suggestion-detail-not-voted";
        }
    }

    private string GetStatusClass()
    {
        if (suggestion is null || suggestion.SuggestionStatus is null)
        {
            return "suggestion-detail-status-none";
        }

        string output = suggestion.SuggestionStatus.StatusName switch
        {
            "Completed" => "suggestion-detail-status-completed",
            "Watching" => "suggestion-detail-status-watching",
            "Upcoming" => "suggestion-detail-status-upcoming",
            "Dismissed" => "suggestion-detail-status-dismissed",
            _ => "suggestion-detail-status-none"
        };
        return output;
    }
}
