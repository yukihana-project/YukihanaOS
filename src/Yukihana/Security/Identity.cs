// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public readonly record struct UserId(uint Value)
{
    public static readonly UserId Root = new(0);

    public override string ToString() => Value.ToString();
}


public readonly record struct GroupId(uint Value)
{
    public static readonly GroupId Root = new(0);

    public override string ToString() => Value.ToString();
}
