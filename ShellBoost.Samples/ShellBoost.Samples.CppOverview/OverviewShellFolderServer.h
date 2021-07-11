#pragma once

using namespace System;
using namespace ShellBoost::Core;

ref class OverviewShellFolderServer : ShellFolderServer
{
private:
    ShellFolder^ _root;

public:
    virtual ShellFolder^ GetFolderAsRoot(ShellItemIdList^ idList) override;
};

