#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace ShellBoost::Core;
using namespace ShellBoost::Core::WindowsShell;

ref class SimpleFolder : ShellFolder
{
public:
	property int Level;
	property Boolean ShowImages;

	SimpleFolder(SimpleFolder^ parent, String^ text);
	SimpleFolder(ShellItemIdList^ idList);

	virtual IEnumerable<ShellItem^>^ EnumItems(SHCONTF options) override;
	virtual void MergeContextMenu(ShellFolder^ folder, IReadOnlyList<ShellItem^>^ items, ShellMenu^ existingMenu, ShellMenu^ appendMenu) override;
};

