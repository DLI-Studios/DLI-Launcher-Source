using DLI.Connect.Firebase;
using DLI.Connect.Models;
using DLI.Connect.Services;
using DLI.Connect.Services.Interfaces;
using NSubstitute;
using Xunit;

namespace DLI.Connect.Tests;

public class FriendServiceTests
{
    private readonly IFirebaseFirestore _db;
    private readonly ISessionManager _session;
    private readonly FriendService _service;

    private const string Me = "uid-me";
    private const string Other = "uid-other";

    public FriendServiceTests()
    {
        _db = Substitute.For<IFirebaseFirestore>();
        _session = Substitute.For<ISessionManager>();
        _session.CurrentUser.Returns(new FirebaseUser { Uid = Me });
        _service = new FriendService(_db, _session);
    }

    private static FriendRequest Request(string from, string to, string status = "pending") => new()
    {
        RequestId = $"{from}_{to}",
        FromUid = from,
        ToUid = to,
        Status = status
    };

    [Fact]
    public async Task SearchUsersAsync_EmptyQuery_ReturnsEmptyList()
    {
        var result = await _service.SearchUsersAsync("   ", Me);

        Assert.Empty(result);
        await _db.DidNotReceiveWithAnyArgs().SearchUsersAsync(default!, default!, default);
    }

    [Fact]
    public async Task SearchUsersAsync_TrimsAndLowercasesQuery()
    {
        await _service.SearchUsersAsync("  Ahmet  ", Me);

        await _db.Received(1).SearchUsersAsync("ahmet", Me, 20);
    }

    [Fact]
    public async Task SendFriendRequestAsync_ToSelf_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendFriendRequestAsync(Me));
    }

    [Fact]
    public async Task SendFriendRequestAsync_AlreadyFriends_Throws()
    {
        _db.FriendshipExistsAsync(Me, Other).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendFriendRequestAsync(Other));
    }

    [Fact]
    public async Task SendFriendRequestAsync_OutgoingPending_Throws()
    {
        _db.FriendshipExistsAsync(Me, Other).Returns(false);
        _db.GetFriendRequestAsync(Me, Other).Returns(Request(Me, Other));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendFriendRequestAsync(Other));
    }

    [Fact]
    public async Task SendFriendRequestAsync_IncomingPending_ThrowsWithAcceptHint()
    {
        _db.FriendshipExistsAsync(Me, Other).Returns(false);
        _db.GetFriendRequestAsync(Me, Other).Returns((FriendRequest?)null);
        _db.GetFriendRequestAsync(Other, Me).Returns(Request(Other, Me));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SendFriendRequestAsync(Other));

        Assert.Contains("kabul edebilirsin", ex.Message);
    }

    [Fact]
    public async Task SendFriendRequestAsync_HappyPath_CreatesRequest()
    {
        _db.FriendshipExistsAsync(Me, Other).Returns(false);
        _db.GetFriendRequestAsync(Me, Other).Returns((FriendRequest?)null);
        _db.GetFriendRequestAsync(Other, Me).Returns((FriendRequest?)null);

        await _service.SendFriendRequestAsync(Other);

        await _db.Received(1).CreateFriendRequestAsync(Me, Other);
    }

    [Fact]
    public async Task AcceptRequestAsync_WrongRequestId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AcceptRequestAsync("garbage", Other));
    }

    [Fact]
    public async Task AcceptRequestAsync_HappyPath_CreatesFriendshipAndDeletesRequest()
    {
        await _service.AcceptRequestAsync($"{Other}_{Me}", Other);

        await _db.Received(1).CreateFriendshipAsync(Me, Other);
        await _db.Received(1).CreateFriendshipAsync(Other, Me);
        await _db.Received(1).DeleteDocumentAsync($"friend_requests/{Other}_{Me}");
    }

    [Fact]
    public async Task GetRelationStateAsync_AlreadyFriends_ReturnsAlreadyFriends()
    {
        _db.FriendshipExistsAsync(Me, Other).Returns(true);

        Assert.Equal(RequestRelationState.AlreadyFriends,
            await _service.GetRelationStateAsync(Other));
    }

    [Fact]
    public async Task GetRelationStateAsync_NoRelation_ReturnsNone()
    {
        _db.FriendshipExistsAsync(Me, Other).Returns(false);
        _db.GetFriendRequestAsync(Me, Other).Returns((FriendRequest?)null);
        _db.GetFriendRequestAsync(Other, Me).Returns((FriendRequest?)null);

        Assert.Equal(RequestRelationState.None,
            await _service.GetRelationStateAsync(Other));
    }

    [Fact]
    public async Task GetFriendsAsync_OrdersOnlineFirstThenName()
    {
        _session.CurrentUser.Returns(new FirebaseUser { Uid = Me });
        _db.ListFriendUidsAsync(Me).Returns(new List<string> { "b", "a" });

        _db.GetUserAsync("a").Returns(new UserProfile { Username = "a", DisplayName = "Alice", Status = "online", LastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
        _db.GetUserAsync("b").Returns(new UserProfile { Username = "b", DisplayName = "Bob", Status = "offline", LastSeen = 0 });

        var friends = await _service.GetFriendsAsync();

        Assert.Equal(2, friends.Count);
        Assert.Equal("Alice", friends[0].DisplayName);
        Assert.Equal("Bob", friends[1].DisplayName);
    }

    [Fact]
    public async Task GetFriendsAsync_WithoutSession_ReturnsEmpty()
    {
        _session.CurrentUser.Returns((FirebaseUser?)null);

        var friends = await _service.GetFriendsAsync();

        Assert.Empty(friends);
        await _db.DidNotReceiveWithAnyArgs().ListFriendUidsAsync(default!);
    }
}
