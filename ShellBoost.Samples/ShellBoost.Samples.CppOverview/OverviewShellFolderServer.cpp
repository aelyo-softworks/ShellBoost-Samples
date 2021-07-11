#include "pch.h"
#include "OverviewShellFolderServer.h"
#include "SimpleFolder.h"

ShellFolder^ OverviewShellFolderServer::GetFolderAsRoot(ShellItemIdList^ idList)
{
    if (_root == nullptr)
    {
        _root = gcnew SimpleFolder(idList);
    }
    return _root;
};
