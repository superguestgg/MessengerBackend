namespace Messenger.Chats.Application;

// Wakes requests that wait for new messages in a chat.
public interface IChatSignals
{
    // Completes on the next Notify for the chat. Take it before reading,
    // so a message saved between the read and the wait is not missed.
    Task Next(Guid chatId);

    void Notify(Guid chatId);
}
