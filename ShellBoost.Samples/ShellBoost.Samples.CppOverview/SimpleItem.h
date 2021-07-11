#pragma once

using namespace System;
using namespace ShellBoost::Core;

ref class SimpleItem : ShellItem
{
public:
	SimpleItem(ShellFolder^ parent, String^ text);

	virtual bool TryGetPropertyValue(WindowsPropertySystem::PropertyKey key, Object^% value) override;
	virtual ShellContent^ GetContent() override;
};

