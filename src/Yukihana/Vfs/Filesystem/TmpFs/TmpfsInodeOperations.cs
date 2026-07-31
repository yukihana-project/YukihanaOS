// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Cosmos.Kernel.HAL.Vfs;
using Yukihana.Core.Extensions.Cosmos;

namespace Yukihana.Vfs.Filesystem.TmpFs;

public sealed class TmpfsInodeOperations : IInodeOperations
{
    public bool Create(IVfsInode dir, ReadOnlySpan<char> name, ModeEnum mode, out IVfsInode? inode)
    {
        inode = null;
        if (dir is not TmpfsDirectoryInode dirInode)
        {
            return false;
        }

        var now = VfsTimespec.Now();

        string strName = new(name);

        TmpfsInode child;
        
        if ((mode & ModeEnum.Directory) != 0)
        {
            child = new TmpfsDirectoryInode()
            {
                Name = strName,
                Mode = mode,
                Ctime = now,
                Mtime = now,
                Atime = now,
                LinkCount = 2,
                Parent = dirInode
            };
        }
        else // assume regular file
        {
            child = new TmpfsFileInode()
            {
                Name = strName,
                Mode = mode,
                Ctime = now,
                Mtime = now,
                Atime = now,
                LinkCount = 1,
                Parent = dirInode,
            };
        }

        dirInode.Children.Add(strName, child);

        inode = child;
        return true;
    }

    public bool GetAttr(IVfsInode inode, out VfsStat stat)
    {
        stat = new VfsStat();
        if (inode is not TmpfsInode tmpfsInode)
        {
            return false;
        }

        stat = new VfsStat()
        {
            Ino = tmpfsInode.InodeNumber,
            Atime = tmpfsInode.Atime,
            Ctime = tmpfsInode.Ctime,
            Gid = tmpfsInode.Gid,
            Uid = tmpfsInode.Uid,
            Mode = tmpfsInode.Mode,
            Mtime = tmpfsInode.Mtime,
            NLink = tmpfsInode.LinkCount,
            Size = (ulong)tmpfsInode.Size
        };

        if (tmpfsInode is TmpfsDirectoryInode dir)
        {
            stat.Size += (ulong)dir.Children.Select(c => c.Value).Sum(i => i.Size);
        }
        
        return true;
    }

    public bool Lookup(IVfsInode dir, ReadOnlySpan<char> name, out IVfsInode? child)
    {
        child = null;
        if (dir is not TmpfsDirectoryInode dirInode)
        {
            return false;
        }

        string strName = new(name);

        if (!dirInode.Children.TryGetValue(strName, out TmpfsInode? childInode))
        {
            return false;
        }

        child = childInode;
        return true;
    }
    public bool Mkdir(IVfsInode dir, ReadOnlySpan<char> name, ModeEnum mode, out IVfsInode? inode)
    {
        inode = null;
        if (dir is not TmpfsDirectoryInode dirInode)
        {
            return false;
        }

        var now = VfsTimespec.Now();

        string strName = new(name);

        TmpfsDirectoryInode child = new()
        {
            Atime = now,
            Mtime = now,
            Ctime = now,
            Gid = dirInode.Gid,
            Uid = dirInode.Uid,
            LinkCount = 2,
            Name = strName,
            Mode = mode,
            Parent = dirInode
        };

        dirInode.LinkCount++;
        dirInode.Children.Add(strName, child);
        inode = child;
        return true;
    }
    public bool ReadDir(IVfsInode dir, out IReadOnlyList<IVfsInode> entries)
    {
        entries = [];
        if (dir is not TmpfsDirectoryInode dirInode)
        {
            return false;
        }

        entries = dirInode.Children.Select(c => c.Value).ToList();

        return true;
    }

    public bool Rename(IVfsInode oldParent, ReadOnlySpan<char> oldName, IVfsInode newParent, ReadOnlySpan<char> newName)
    {
        if (oldParent is not TmpfsDirectoryInode oldParentInode
            || newParent is not TmpfsDirectoryInode newParentInode)
        {
            return false;
        }

        string oldStrName = new(oldName);
        string newStrName = new(newName);

        if (!oldParentInode.Children.TryGetValue(oldStrName, out TmpfsInode? inode))
        {
            return false;
        }

        inode.Parent = newParentInode;

        oldParentInode.Children.Remove(oldStrName);
        newParentInode.Children.Add(newStrName, inode);

        if (inode is not TmpfsDirectoryInode)
        {
            return true;
        }

        oldParentInode.LinkCount--;
        newParentInode.LinkCount++;

        return true;
    }
    public bool Rmdir(IVfsInode dir, ReadOnlySpan<char> name)
    {
        if (dir is not TmpfsDirectoryInode dirInode)
        {
            return false;
        }

        string strName = new(name);

        if (!dirInode.Children.TryGetValue(strName, out TmpfsInode? inode) || inode is not TmpfsDirectoryInode)
        {
            return false;
        }

        dirInode.Children.Remove(strName);
        dirInode.LinkCount--;

        return true;
    }
    public bool SetAttr(IVfsInode inode, SetAttrFlags flags, in VfsStat attributes)
    {
        if (inode is not TmpfsInode tmpfsInode)
        {
            return false;
        }

        switch (flags)
        {
            case SetAttrFlags.Mode:
                tmpfsInode.Mode = attributes.Mode;
                break;
            case SetAttrFlags.Uid:
                tmpfsInode.Uid = attributes.Uid;
                break;
            case SetAttrFlags.Gid:
                tmpfsInode.Gid = attributes.Gid;
                break;
            case SetAttrFlags.Atime:
                tmpfsInode.Atime = attributes.Atime;
                break;
            case SetAttrFlags.Mtime:
                tmpfsInode.Mtime = attributes.Mtime;
                break;
            case SetAttrFlags.Ctime:
                tmpfsInode.Ctime = attributes.Ctime;
                break;
            case SetAttrFlags.None:
            case SetAttrFlags.Size:
            default:
                return false;
        }

        return true;
    }
    public bool Symlink(IVfsInode dir, ReadOnlySpan<char> name, ReadOnlySpan<char> target, out IVfsInode? inode)
    {
        inode = null;
        if (dir is not TmpfsDirectoryInode dirInode)
        {
            return false;
        }

        string strName = new(name);
        string strTarget = new(name);

        var now = VfsTimespec.Now();

        var link = new TmpfsSymlinkInode()
        {
            Target = strTarget,
            Ctime = now,
            Atime = now,
            Mtime = now,
            Gid = dirInode.Gid,
            Uid = dirInode.Uid,
            Name = strName,
            Parent = dirInode,
            LinkCount = 1
        };

        dirInode.Children.Add(strName, link);
        inode = link;
        return true;
    }
    public bool Unlink(IVfsInode dir, ReadOnlySpan<char> name)
    {
        if (dir is not TmpfsDirectoryInode dirInode)
        {
            return false;
        }

        string strName = new(name);

        if (!dirInode.Children.TryGetValue(strName, out TmpfsInode? inode) || inode is not TmpfsSymlinkInode)
        {
            return false;
        }

        dirInode.Children.Remove(strName);
        return true;
    }
}