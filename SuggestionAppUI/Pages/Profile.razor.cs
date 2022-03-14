namespace SuggestionAppUI.Pages;

public partial class Profile
{
    private UserModel loggedInUser;
    // Create seperate lists for each type of suggestion
    private List<SuggestionModel> submissions;
    private List<SuggestionModel> approved;
    private List<SuggestionModel> archived;
    private List<SuggestionModel> pending;
    private List<SuggestionModel> rejected;
    protected async override Task OnInitializedAsync()
    {
        loggedInUser = await authProvider.GetUserFromAuth(userData);
        var results = await suggestionData.GetUsersSuggestions(loggedInUser.Id);
        if (loggedInUser is not null && results is not null)
        {
            submissions = results.OrderByDescending(s => s.DateCreated).ToList();
            approved = submissions.Where(s => s.ApprovedForRelease && s.Archived == false && s.Rejected == false).ToList();
            archived = submissions.Where(s => s.Archived && s.Rejected == false).ToList();
            pending = submissions.Where(s => s.ApprovedForRelease == false && s.Rejected == false).ToList();
            rejected = submissions.Where(s => s.Rejected).ToList();
        }
    }

    private void ClosePage()
    {
        navManager.NavigateTo("/");
    }
}
