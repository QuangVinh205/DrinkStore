
namespace DrinkStore.Services
{
    public interface IPaymentGateway
    {
        Task<bool> ChargeAsync(decimal amount, string requestKey);

        Task<bool> VerifyCallbackAsync(string callbackData);

        Task<string> QueryStatusAsync(string providerRef);
    }
}
