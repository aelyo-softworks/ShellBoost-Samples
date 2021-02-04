# ShellBoost Samples
This repository contains [ShellBoost](https://www.shellboost.com) official samples.

ShellBoost comes with the following samples:
*	**Overview**: A very simple Console application shell folder that contains a fixed set of virtual shell items and folders.

*	**Local Folder**: A Console application shell folder that supports a combination of virtual shell items and physical shell items.

*	**Registry Folder**: A Console application shell folder that mimics the official Windows Registry Editor (“regedit.exe”) with only virtual shell items and custom UI using WinForms. Also supports the "Send To" context menu.

*	**SevenZip Folder**: A Console application shell folder server that demonstrates how to create a non-rooted, virtual file folder that integrates files with the .7z extension in the shell namespace, just like Windows does using .zip files.

*	**Physical Overview Folder**: A Console application shell folder that uses a physical folder as a back-end for all operations. It demonstrates the following features: Advanced Drag & drop, Copy & Paste operations, Common Dialog (Open, Save) operations, Shell Change Notifications from other folders.

*	**Google Drive Folder**: A Winforms application that is an equivalent of One Drive for Google Drive. It demonstrates the File On-Demand ShellBoost feature. It’s not currently exposing a Shell Namespace Extension, but the sample could be modified to integrate the code that can be found in the Physical Overview sample.

*	**Web Folder**: A WPF application shell folder that connects to a custom Web Server (that exposes a custom REST/JSON API) that’s also included. The newer Cloud Folder sample is preferred over this one.

*	**Folder Service**: A Console application shell folder that demonstrates a Windows Service written using ShellBoost.

*	**Cloud Folder**: A list of projects based on a common back-end (Cloud Folder Site) representing a cloud storage service hosted somewhere on the internet.

	*	**Cloud Folder**: A .NET Core console application shell folder. Demonstrates file system features (Copy/Paste, Drag/Drop, New menus). The server app is an ASP.NET Core web application using SQL Server as a storage back-end.

	*	**Cloud Folder Site**: An ASP.NET Core application companion of the Cloud Folder sample that implements an API-only file system over the web. It's using abstract interfaces, currently implemented with an SQL Database, but other storages can be added.

	*	**Cloud Folder Client**: A .NET Core Winfoms application. Can be used as a front-end to the Cloud Folder Site back-end, independent from the other Cloud Folder and Cloud Folder Sync samples. Useful for tests.

	*   **Cloud Folder Sync**: A .NET 5 console application that is an equivalent of One Drive for the Cloud Folder Site sample. It demonstrates the File On-Demand ShellBoost feature over Windows 10 Cloud API. It’s not currently exposing a Shell Namespace Extension, but you can use the Cloud Folder sample as a namespace extension to test synchronization.

*	**Device Manager Folder**: A .NET Core console application folder that mimics the official Windows Device Manager application. Demonstrates asynchronous namespace hierarchy building.

*	**Mirror**: A .NET Core console application that demonstrates a namespace application mirroring a physical folder present on the disk. It’s implemented in only a few lines of code.

*	**Amalga Drive**: A WPF application shell folder. Demonstrates the File On-Demand feature. Also comes with a custom WebDAV Server for demonstration purposes. *Note: this sample uses ShellBoost legacy technology and should not be used for new projects*.
