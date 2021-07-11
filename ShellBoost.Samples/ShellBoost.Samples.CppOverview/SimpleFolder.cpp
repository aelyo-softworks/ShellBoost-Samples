#include "pch.h"
#include "SimpleFolder.h"
#include "SimpleItem.h"
#include "SimplePngItem.h"

using namespace System::Linq;

SimpleFolder::SimpleFolder(SimpleFolder^ parent, String^ text)
	:ShellFolder(parent, gcnew StringKeyShellItemId(text))
{
	Level = parent->Level + 1;
	ShowImages = true;
}

SimpleFolder::SimpleFolder(ShellItemIdList^ idList)
	:ShellFolder(idList)
{
	// Level = 0
	ShowImages = true;
}

IEnumerable<ShellItem^>^ SimpleFolder::EnumItems(SHCONTF options)
{
	// real IEnumerable/yield is not a free lunch on C++/CLI, so we use a list
	auto list = gcnew List<ShellItem^>();

	// add folders
	// note in this sample we only add folders up to two levels
	auto maxLevels = 2;
	if (options.HasFlag(SHCONTF::SHCONTF_FOLDERS) && Level <= maxLevels)
	{
		auto maxFolders = 2;
		for (auto i = 0; i < maxFolders; i++)
		{
			list->Add(gcnew SimpleFolder(this, "Virtual Folder " + Level + "." + i));
		}
	}

	// add items
	if (options.HasFlag(SHCONTF::SHCONTF_NONFOLDERS))
	{
		auto maxItems = 2;
		if (ShowImages)
		{
			maxItems *= 2;
			auto  i = 0;
			for (; i < maxItems / 2; i++)
			{
				list->Add(gcnew SimpleItem(this, "Virtual Item #" + i + ".txt"));
			}

			for (; i < maxItems; i++)
			{
				auto  imgKey = (UInt64)(Level * 10 + i);
				list->Add(gcnew SimplePngItem(this, "Virtual Image Key#" + imgKey + ".png", imgKey));
			}
		}
		else
		{
			for (auto i = 0; i < maxItems; i++)
			{
				list->Add(gcnew SimpleItem(this, "Virtual Item #" + i + ".txt"));
			}
		}
	}

	return list;
}

ref class InvokeEventHandler
{
public:
	void Handler(Object^ sender, ShellMenuInvokeEventArgs^ e)
	{
		auto enumerator = Enumerable::OfType<SimplePngItem^>(e->Items)->GetEnumerator();
		while (enumerator->MoveNext())
		{
			enumerator->Current->ClearCache();
		}
	}
};

void SimpleFolder::MergeContextMenu(ShellFolder^ folder, IReadOnlyList<ShellItem^>^ items, ShellMenu^ existingMenu, ShellMenu^ appendMenu)
{
	if (Enumerable::Count(Enumerable::OfType<ShellItem^>(items)) > 0)
	{
		auto clearLocal = gcnew ShellMenuItem(appendMenu, "Clear Cache");
		auto handler = gcnew InvokeEventHandler();
		clearLocal->Invoke += gcnew EventHandler<ShellMenuInvokeEventArgs^>(handler, &InvokeEventHandler::Handler);

		appendMenu->Items->Add(clearLocal);
	}
}
