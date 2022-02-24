using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLibrary.DataAccess;

public class MongoSuggestionData : ISuggestionData
{
    private readonly IDbConnection _db;
    private readonly IUserData _userData;
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<SuggestionModel> _suggestions;
    private const string CacheName = "SuggestionData";

    public MongoSuggestionData(IDbConnection db, IUserData userData, IMemoryCache cache)
    {
        _db = db;
        _userData = userData;
        _cache = cache;
        _suggestions = db.SuggestionCollection;
    }

    // Get all suggestions
    public async Task<List<SuggestionModel>> GetAllSuggestions()
    {
        var output = _cache.Get<List<SuggestionModel>>(CacheName);
        if (output is null)
        {
            var result = await _suggestions.FindAsync(s => s.Archived == false);
            output = result.ToList();

            // cache time of 1 minute for suggestions
            _cache.Set(CacheName, output, TimeSpan.FromMinutes(1));
        }

        return output;
    }

    // Get all suggestions including archived suggestions for a particular user
    public async Task<List<SuggestionModel>> GetUsersSuggestions(string userId)
    {
        var output = _cache.Get<List<SuggestionModel>>(userId);
        if (output is null)
        {
            var result = await _suggestions.FindAsync(s => s.Author.Id == userId);
            output = result.ToList();

            _cache.Set(userId, output, TimeSpan.FromMinutes(1));
        }

        return output;
    }

    // Get all approved suggestions
    public async Task<List<SuggestionModel>> GetAllApprovedSuggestions()
    {
        var output = await GetAllSuggestions();
        return output.Where(x => x.ApprovedForRelease).ToList();
    }

    // Get suggestion by Id
    public async Task<SuggestionModel> GetSuggestion(string id)
    {
        var results = await _suggestions.FindAsync(s => s.Id == id);
        return results.FirstOrDefault();
    }

    // Get all suggestions waiting for approval
    public async Task<List<SuggestionModel>> GetAllSuggestionsWaitingForApproval()
    {
        var output = await GetAllSuggestions();
        return output.Where(x =>
                    x.ApprovedForRelease == false &&
                    x.Rejected == false).ToList();
    }

    // Update a suggestion
    public async Task UpdateSuggestion(SuggestionModel suggestion)
    {
        await _suggestions.ReplaceOneAsync(s => s.Id == suggestion.Id, suggestion);
        _cache.Remove(CacheName); // destorys the suggestionData cache
    }

    // Upvote a suggestion
    public async Task UpVoteSuggestion(string suggestionId, string userId)
    {
        var client = _db.Client;
        // Start a transaction (supported by MongoDB Atlas)
        using var session = await client.StartSessionAsync();
        session.StartTransaction();
        try
        {
            // Getting the selected suggestion by Id
            var db = client.GetDatabase(_db.DbName);
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            var suggestion = (await suggestionsInTransaction.FindAsync(s => s.Id == suggestionId)).First();

            // Add/Remove the upvote to a suggestion
            bool isUpvote = suggestion.UserVotes.Add(userId);
            if (!isUpvote)
            {
                suggestion.UserVotes.Remove(userId);
            }

            // After upvote is done, we can update the suggestion by the updated suggestion value
            await suggestionsInTransaction.ReplaceOneAsync(session, s => s.Id == suggestionId, suggestion);

            // Get all users from db
            var userInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);

            // Get the user who upvoted
            var user = await _userData.GetUser(userId);

            if (isUpvote)
            {
                user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
            }
            else
            {
                var suggestionToRemove = user.VotedOnSuggestions.Where(s => s.Id == suggestionId).First();
                user.VotedOnSuggestions.Remove(suggestionToRemove);
            }

            // After upvote is done, we can update the user by updated user value
            await userInTransaction.ReplaceOneAsync(session, u => u.Id == userId, user);

            await session.CommitTransactionAsync();

            _cache.Remove(CacheName); // remove from cache since it's an update
        }
        catch (Exception ex)
        {
            ex.ToString(); // TODO: Add logging here..
            await session.AbortTransactionAsync();
            throw;
        }
    }

    // Create a suggestion
    public async Task CreateSuggestion(SuggestionModel suggestion)
    {
        // Use transaction here, as since a user is creating the suggestion, we also need to update the user account.
        var client = _db.Client;
        using var session = await client.StartSessionAsync();

        session.StartTransaction();
        try
        {
            var db = client.GetDatabase(_db.DbName);

            // Insert new suggestion into suggestion table
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            await suggestionsInTransaction.InsertOneAsync(session, suggestion);

            // Update the user
            var usersInTransactions = db.GetCollection<UserModel>(_db.UserCollectionName);
            var user = await _userData.GetUser(suggestion.Author.Id);
            user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));
            await usersInTransactions.ReplaceOneAsync(session, u => u.Id == user.Id, user);

            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            ex.ToString(); // TODO: Add logging
            await session.AbortTransactionAsync();
            throw;
        }
    }
}
