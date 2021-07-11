#include "pch.h"
#include "OverviewShellFolderServer.h"

using namespace System;
using namespace ShellBoost::Core;
using namespace ShellBoost::Core::Utilities;

void Run(bool register, RegistrationMode regMode);

ref class LicensingEventHandler
{
public:
	void Handler(Object^ sender, LicensingEventArgs^ e)
	{
		Console::WriteLine(L"LicenseDataIsValid: " + ShellFolderServer::LicenseDataIsValid);
		Console::WriteLine(L"LicenseExpirationDate: " + ShellFolderServer::LicenseExpirationDate);
		Console::WriteLine(L"LicenseRegisteredCompany: " + ShellFolderServer::LicenseRegisteredCompany);
	}
};

int main(array<String^>^ args)
{
	Console::WriteLine(L"ShellBoost Samples C++/CLI - Overview - " + (IntPtr::Size == 4 ? L"32" : L"64") + L"-bit - Copyright (C) 2017-" + DateTime::Now.Year + L" Aelyo Softworks. All rights reserved.");
	Console::WriteLine(L"ShellBoost Runtime Version " + AssemblyUtilities::GetInformationalVersion(ShellContext::typeid->Assembly));
	Console::WriteLine();

	// use "ShellBoost.Samples.CppOverview.exe /mode:machine" to switch to machine registration.
	auto regMode = CommandLine::GetArgument<RegistrationMode>(L"mode", RegistrationMode::User);
	if (regMode == RegistrationMode::None)
	{
		regMode = RegistrationMode::User;
	}

	Console::WriteLine(L"RegistrationMode: " + regMode.ToString());
	Console::WriteLine();
	Console::WriteLine(L"Press a key:");
	Console::WriteLine();
	Console::WriteLine(L"   '1' Register the native proxy, run this sample, and unregister on exit.");
	Console::WriteLine(L"   '2' Register the native proxy.");
	Console::WriteLine(L"   '3' Run this sample (the native proxy will need to be registered somehow for Explorer to display something).");
	Console::WriteLine(L"   '4' Unregister the native proxy.");
	Console::WriteLine();
	Console::WriteLine(L"   Any other key will exit.");
	Console::WriteLine();
	auto key = Console::ReadKey(true);
	switch (key.KeyChar)
	{
	case '1':
		Run(true, regMode);
		ShellFolderServer::UnregisterNativeDll(regMode);
		break;

	case '2':
		ShellFolderServer::RegisterNativeDll(regMode);
		Console::WriteLine(L"Registered");
		break;

	case '3':
		Run(false, regMode);
		break;

	case '4':
		ShellFolderServer::UnregisterNativeDll(regMode);
		Console::WriteLine(L"Unregistered");
		break;
	}
	return 0;
}

void Run(bool reg, RegistrationMode regMode)
{
	auto server = gcnew OverviewShellFolderServer();
	try
	{
		auto config = gcnew ShellFolderConfiguration();
		if (reg)
		{
			config->NativeDllRegistration = regMode;
		}

#ifdef _DEBUG
		config->Logger = gcnew ConsoleLogger();
#endif
		auto handler = gcnew LicensingEventHandler();
		server->Licensing += gcnew EventHandler<LicensingEventArgs^>(handler, &LicensingEventHandler::Handler);

		server->Start(config);
		Console::WriteLine(L"Started listening on proxy id " + ShellFolderServer::ProxyId + L". Press ESC key to stop serving folders.");
		Console::WriteLine(L"If you open Windows Explorer, you should now see the extension under the ShellBoost.Overview folder.");
		while (Console::ReadKey(true).Key != ConsoleKey::Escape)
		{
		}
	}
	finally
	{
		delete server;
	}
	Console::WriteLine(L"Stopped");
}

