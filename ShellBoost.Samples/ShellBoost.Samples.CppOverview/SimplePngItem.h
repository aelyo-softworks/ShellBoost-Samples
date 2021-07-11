#pragma once

using namespace System;
using namespace ShellBoost::Core;
using namespace ShellBoost::Core::WindowsShell;

ref class SimplePngItem;

ref class SimpleThumbnail : ShellThumbnail
{
public:
	property UInt64 Key;
	property SimplePngItem^ Item;

	SimpleThumbnail(SimplePngItem^ item, UInt64 key);

	virtual ShellThumbnailAsIcon^ GetAsIcon(GILIN inOptions) override;
	virtual ShellThumbnailAsImage^ GetAsImage(int width, int height) override;

	static String^ SimpleThumbnail::GetUrl(int width, int height, UInt64 key);
	String^ GetCachePath(int width, int height);
	bool DeleteCache(int width, int height);
};

ref class SimplePngItem : ShellItem
{
public:
	property SimpleThumbnail^ Thumbnail
	{
		SimpleThumbnail^ get() new
		{
			return (SimpleThumbnail^)ShellItem::Thumbnail;
		}

		void set(SimpleThumbnail^ value)
		{
			ShellItem::Thumbnail = value;
		}
	};

	SimplePngItem(ShellFolder^ parent, String^ text, UInt64 key);

	void ClearCache();
	virtual bool TryGetPropertyValue(WindowsPropertySystem::PropertyKey key, Object^% value) override;
	virtual ShellContent^ GetContent() override;
};

