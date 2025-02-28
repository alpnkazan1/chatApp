using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; // Add this
using chatbackend.Service;
using Microsoft.AspNetCore.Authorization;
using chatbackend.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using chatbackend.DTOs.Messages; // Add this

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MyAuthorizationService _authorizationService;
    private readonly ITokenService _tokenService;
    private readonly IMessageService _messageService;

    public ChatHub(ILogger<ChatHub> logger, IHttpContextAccessor httpContextAccessor,
                   MyAuthorizationService authorizationService, ITokenService tokenService,
                   IMessageService messageService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        // 1. Get the access token securely
        string accessToken = httpContext?.Request.Headers["Authorization"]
            .ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError($"Client connected without an access token. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }

        // 2. Validate the token using your TokenService
        var principal = _tokenService?.ValidateToken(accessToken);

        if (principal == null)
        {
            _logger.LogError($"Client connected with invalid access token: {accessToken}. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }

        // 3. Get the UserId
        string userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) 
                        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError($"Client connected with missing user ID claim. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }


        // 4. Get the chatId from the query string
        string chatIdString = httpContext?.Request.Query["chatId"];

        if (!Guid.TryParse(chatIdString, out Guid chatId))
        {
            _logger.LogError($"Client connected with invalid chatId format: {chatIdString}. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }

        // 5. Check Authorization
        bool isAuthorized = _authorizationService != null && await _authorizationService.IsAuthorizedForChat(userId, chatId);

        if (!isAuthorized)
        {
            _logger.LogWarning($"User '{userId}' is not authorized for chatId '{chatId}'. ConnectionId: {Context.ConnectionId}");
            await Task.Delay(0);
            Context.Abort();
            return;
        }


        // 6. Join chat group
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

        _logger.LogInformation($"Client connected to chatId '{chatId}' as user '{userId}'. ConnectionId: {Context.ConnectionId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var httpContext = Context.GetHttpContext();
        string chatIdString = httpContext?.Request.Query["chatId"];

        // If middleware stored chatId in Context.Items, retrieve it
        if (string.IsNullOrEmpty(chatIdString) && Context.Items.TryGetValue("chatId", out var chatIdObj))
        {
            chatIdString = chatIdObj?.ToString();
        }

        if (!string.IsNullOrEmpty(chatIdString) && Guid.TryParse(chatIdString, out Guid chatId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
            _logger.LogInformation($"Client disconnected from chatId '{chatId}'. ConnectionId: {Context.ConnectionId}");
        }
        else
        {
            _logger.LogWarning($"Client disconnected without a valid chatId. ConnectionId: {Context.ConnectionId}");
        }

        // Log any exception (if exists)
        if (exception != null)
        {
            _logger.LogError(exception, $"Exception occurred during disconnection. ConnectionId: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(MessageSendDto messageDto)
    {
        string userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning($"Unauthorized user attempted to send a message. ConnectionId: {Context.ConnectionId}");
            Context.Abort();
            return;
        }

        // Check if the user is authorized to send messages in this chat
        bool isAuthorized = await _authorizationService.IsAuthorizedForChat(userId, messageDto.ChatId);
        if (!isAuthorized)
        {
            _logger.LogWarning($"User {userId} is not authorized to send messages in chat {messageDto.ChatId}. ConnectionId: {Context.ConnectionId}");
            return;
        }

        // Save message using MessageService
        var messageReqDto = await _messageService.SaveMessageAsync(userId, messageDto);

        if (messageReqDto == null)
        {
            _logger.LogError($"Failed to save message from user {userId} in chat {messageDto.ChatId}");
            return;
        }

        // Broadcast the message to all clients in the chat
        await Clients.Group(messageDto.ChatId.ToString()).SendAsync("newMessage", messageReqDto);

        _logger.LogInformation($"Message sent in chat {messageDto.ChatId} by user {userId}. Message ID: {messageReqDto.MessageId}");
    }
}