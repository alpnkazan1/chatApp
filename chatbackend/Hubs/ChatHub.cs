using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; // Add this
using chatbackend.Service;
using Microsoft.AspNetCore.Authorization;
using chatbackend.Interfaces; // Add this

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MyAuthorizationService _authorizationService;
    private readonly ITokenService _tokenService;

    public ChatHub(ILogger<ChatHub> logger, IHttpContextAccessor httpContextAccessor,
                   MyAuthorizationService authorizationService, ITokenService tokenService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public override async Task OnConnectedAsync()
    {
        // 1. Get the access token from the query string
        string accessToken = Context.GetHttpContext().Request.Query["access_token"];

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError($"Client connected without an access token. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }

        // 2. Validate the token using your TokenService
        ClaimsPrincipal principal = _tokenService.ValidateToken(accessToken);

        if (principal == null)
        {
            _logger.LogError($"Client connected with invalid access token: {accessToken}. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }

        // 3. Get the UserId from the validated claims
        string userId = principal.FindFirstValue(ClaimTypes.Sub);  // Using JwtRegisteredClaimNames.Sub for user ID

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError($"Client connected with missing user ID claim. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }


        // 4. Get the chatId from the query string
        string chatIdString = Context.GetHttpContext().Request.Query["chatId"];

        if (!Guid.TryParse(chatIdString, out Guid chatId))
        {
            _logger.LogError($"Client connected with invalid chatId format: {chatIdString}. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }

        // 5. Check Authorization
        bool isAuthorized = await _authorizationService.IsAuthorizedForChat(userId, chatId);

        if (!isAuthorized)
        {
            _logger.LogWarning($"User '{userId}' is not authorized for chatId '{chatId}'. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }


        //Join a Group for this chatId, so that you can send messages to just that chat.
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

        _logger.LogInformation($"Client connected to chatId '{chatId}' as user '{userId}'. ConnectionId: {Context.ConnectionId}");

        Context.User = principal;  // Setting Hub Context's User, since the hub's user isn't getting set automatically
        await base.OnConnectedAsync();

    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        string chatIdString = Context.GetHttpContext().Request.Query["chatId"];

        if (!string.IsNullOrEmpty(chatIdString) && Guid.TryParse(chatIdString, out Guid chatId)) //TryParse here too
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
            _logger.LogInformation($"Client disconnected from chatId '{chatId}'. ConnectionId: {Context.ConnectionId}");
        }
        else
        {
            _logger.LogWarning($"Client disconnected without a valid chatId. ConnectionId: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Example method to handle a new message from the front-end.
    // The front-end sends a JSON string.
    public async Task ReceiveMessage(string chatId, string jsonMessage)
    {
        try
        {
            // Deserialize the JSON message to a C# object (optional, but recommended).
            var messageData = System.Text.Json.JsonSerializer.Deserialize<ChatMessage>(jsonMessage);

            // TODO: Implement message handling logic (save to database, etc.)
            _logger.LogInformation($"Received message for chatId '{chatId}': {jsonMessage}");

            // Broadcast the message to all clients in the specified chat group.
            // We send back a JSON string.
            await Clients.Group(chatId).SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(new { chatId = chatId, text = messageData.Text, sender = Context.UserIdentifier }));
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError($"Error deserializing JSON message: {ex.Message}");
            // Consider sending an error message back to the client.
        }
    }

    // Example Last Seen Update (receiving a JSON string)
    public async Task UpdateLastSeen(string chatId, string jsonLastSeen)
    {
        try
        {
            var lastSeenData = System.Text.Json.JsonSerializer.Deserialize<LastSeenUpdate>(jsonLastSeen);
            // TODO: Implement last seen update logic (save to database, etc.)
            _logger.LogInformation($"Received last seen update for chatId '{chatId}': {jsonLastSeen}");

            await Clients.Group(chatId).SendAsync("ReceiveLastSeenUpdate", System.Text.Json.JsonSerializer.Serialize(lastSeenData));
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError($"Error deserializing JSON last seen update: {ex.Message}");
        }
    }
}

// Example C# classes for deserializing JSON messages
public class ChatMessage
{
    public string Text { get; set; }
}

public class LastSeenUpdate
{
    public DateTime LastSeen { get; set; }
}