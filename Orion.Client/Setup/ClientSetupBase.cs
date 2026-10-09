using Orion.Client.User;

namespace Orion.Client.Setup
{
    public abstract class ClientSetupBase
    {
        protected UserAccount UserAccount;

        protected ClientSetupBase()
        {
            UserAccount = new UserAccount();
        }
    }
}
