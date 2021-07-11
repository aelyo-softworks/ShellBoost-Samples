#include "pch.h"
#include "SimplePngItem.h"

using namespace ShellBoost::Core::Utilities;

SimplePngItem::SimplePngItem(ShellFolder^ parent, String^ text, UInt64 key)
	:ShellItem(parent, gcnew StringKeyShellItemId(text))
{
	// this is needed for icon
	ItemType = IOUtilities::PathGetExtension(text);

	Thumbnail = gcnew SimpleThumbnail(this, key);
}

bool SimplePngItem::TryGetPropertyValue(WindowsPropertySystem::PropertyKey key, Object^% value)
{
	// https://docs.microsoft.com/en-us/windows/win32/properties/props-system-thumbnailcacheid
	// NOTE: cache id cannot be 0
	if (key == WindowsPropertySystem::System::ThumbnailCacheId)
	{
		value = Thumbnail->Key;
		return true;
	}

	return ShellItem::TryGetPropertyValue(key, value);
}

void SimplePngItem::ClearCache()
{
	// note this can fail with a sharing violation if the files are accessed (by the Shell itself or something else)
	Thumbnail->DeleteCache(96, 96);
	Thumbnail->DeleteCache(256, 256);
}

ShellContent^ SimplePngItem::GetContent()
{
	auto options = gcnew WebFileCacheRequestOptions();
	options->DontForceServerCheck = true;

	auto content = gcnew WebCacheShellContent(
		Parent->Root->FolderServer->Configuration->GetDefaultWebFileCache(),
		SimpleThumbnail::GetUrl(256, 256, Thumbnail->Key),
		options);
	return content;
}

SimpleThumbnail::SimpleThumbnail(SimplePngItem^ item, UInt64 key)
{
	if (item == nullptr)
		throw gcnew ArgumentNullException(L"item");

	Item = item;
	Key = key;
}

ShellThumbnailAsIcon^ SimpleThumbnail::GetAsIcon(GILIN inOptions)
{
	auto icon = gcnew ShellThumbnailAsIcon(L"shell32.dll");
	icon->Index = 325;
	return icon;
}

// don't get surprised if you see cute cats
// check the doc on https://loremflickr.com/
String^ SimpleThumbnail::GetUrl(int width, int height, UInt64 key)
{
	return L"https://loremflickr.com/" + width + L"/" + height + L"?lock=" + key;
}

String^ SimpleThumbnail::GetCachePath(int width, int height)
{
	auto url = GetUrl(width, height, Key);

	// we use a ShellBoost utility class to download (and cache) the file
	auto cache = Item->Parent->FolderServer->Configuration->GetDefaultWebFileCache();
	auto options = gcnew WebFileCacheRequestOptions();
	options->DontForceServerCheck = true;
	return cache->Download(url, options);
}

bool SimpleThumbnail::DeleteCache(int width, int height)
{
	auto url = GetUrl(width, height, Key);
	auto cache = Item->Parent->FolderServer->Configuration->GetDefaultWebFileCache();
	return cache->Delete(url);
}

ShellThumbnailAsImage^ SimpleThumbnail::GetAsImage(int width, int height)
{
	auto path = GetCachePath(width, height);
	return path != nullptr ? gcnew ShellThumbnailAsImage(path) : nullptr;
}


