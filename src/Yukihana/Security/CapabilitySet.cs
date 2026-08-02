// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

namespace Yukihana.Security;

public readonly record struct CapabilitySet(Capability Value)
{
    public static readonly CapabilitySet Empty = new(Capability.None);

    public static readonly CapabilitySet Root = new(Capability.All);

    public bool Contains(Capability capability)
        => (Value & capability) == capability;

    public bool Intersects(Capability capabilities)
        => (Value & capabilities) != 0;

    public bool ContainsAll(Capability capabilities)
        => (Value & capabilities) == capabilities;

    public CapabilitySet Add(Capability capability)
        => this with { Value = Value | capability };

    public CapabilitySet Remove(Capability capability)
        => this with { Value = Value & ~capability };
}
