#include "pch.h"
#include "SimpleItem.h"

using namespace ShellBoost::Core::Utilities;

SimpleItem::SimpleItem(ShellFolder^ parent, String^ text)
	:ShellItem(parent, gcnew StringKeyShellItemId(text))
{
	// this is needed for icon
	ItemType = IOUtilities::PathGetExtension(text);
	CanCopy = true;
}

bool SimpleItem::TryGetPropertyValue(WindowsPropertySystem::PropertyKey key, Object^% value)
{
	if (key == WindowsPropertySystem::System::PropList::InfoTip)
	{
		value = nullptr;
		return false;
	}

	// dynamic infotip (aka: tooltip)
	if (key == WindowsPropertySystem::System::InfoTipText)
	{
		value = L"This is " + DisplayName + L", info created " + DateTime::Now;
		return true;
	}

	return ShellItem::TryGetPropertyValue(key, value);
}

ShellContent^ SimpleItem::GetContent()
{
	auto content = gcnew MemoryShellContent(DisplayName + L" - this is dynamic content created from .NET Framework C++/CLI at " + DateTime::Now);
	return content;
}