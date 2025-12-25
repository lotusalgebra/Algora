namespace Algora.Chatbot.Application.Interfaces;

public interface IShopContext
{
    string ShopDomain { get; }
    string? AccessToken { get; }
}
