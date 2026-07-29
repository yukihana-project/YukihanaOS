// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public sealed class AccountManager
{
    private readonly Dictionary<UserId, UserAccount> _accounts = [];
    private readonly Dictionary<string, UserId> _names = [];

    public UserAccount Get(UserId id) => _accounts[id];

    public UserAccount Get(string name) => _accounts[_names[name]];

    public void Add(UserAccount account)
    {
        _accounts.Add(account.User.Id, account);
        _names.Add(account.User.Name, account.User.Id);
    }

    public bool Remove(UserId id)
    {
        bool result = _names.Remove(_accounts[id].User.Name) && _accounts.Remove(id);
        return result;
    }
}
